using System;
using System.Linq;
using NUnit.Framework;
using Tesela.Core;
using Tesela.Meta;

namespace Tesela.Tests
{
    public class MetaTests
    {
        private static EconomyConfig _cfg;
        public static EconomyConfig Cfg => _cfg ??= EconomyConfig.FromJson(TestData.Config("Economy.json"));
        private static readonly DateTime Day0 = new DateTime(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc);

        private static RunSummary Run(int score = 900, string mode = "classic") => new RunSummary
        {
            Mode = mode, Score = score, Lines = 20, Pieces = 80, BestStreak = 4, BoostersUsed = 1, BoardsEmptied = 1, Competitive = true
        };

        [Test]
        public void ConfigLoadsAndValidates()
        {
            Assert.That(Cfg.DailyMissions.Count, Is.GreaterThanOrEqualTo(3));
            Assert.AreEqual(80, Cfg.BoosterPrice(BoosterType.Rotate));
            Assert.AreEqual(100, Cfg.BoosterPrice(BoosterType.Undo));
            Assert.AreEqual(120, Cfg.BoosterPrice(BoosterType.Hammer));
            Assert.AreEqual(150, Cfg.BoosterPrice(BoosterType.NewTray));
            Assert.AreEqual(200, Cfg.BoosterPrice(BoosterType.Broom));
            Assert.AreEqual(300, Cfg.RevivePrice);
            Assert.AreEqual(250, Cfg.ProtectorPrice);
            Assert.AreEqual(1f, Cfg.Chest.Sum(e => Cfg.ChestProbability(e)), 1e-5);
            Assert.That(Cfg.XpForLevel(5), Is.GreaterThan(Cfg.XpForLevel(1)));
            Assert.Throws<FormatException>(() => EconomyConfig.FromJson("{\"coinsScoreDivisor\":0}"));
            Assert.Throws<FormatException>(() => Boosters.Parse("??"));
            for (int i = 0; i < Boosters.Count; i++) Assert.AreEqual((BoosterType)i, Boosters.Parse(Boosters.Key((BoosterType)i)));
        }

        [Test]
        public void RunRewardsCoinsAreCappedAndXpLevelsUp()
        {
            var meta = new MetaService(Cfg, null);
            meta.StartSession(Day0);
            var r = meta.OnRunFinished(Run(100000), Day0);
            Assert.AreEqual(Cfg.RunCoinCap, r.Coins);
            Assert.That(meta.Profile.Level, Is.GreaterThan(1));
            Assert.That(r.LevelsGained, Is.GreaterThan(0));
            var small = meta.OnRunFinished(Run(500), Day0);
            Assert.AreEqual(500 / Cfg.CoinsScoreDivisor, small.Coins);
            Assert.AreEqual(100000, meta.Profile.Best("classic"));
        }

        [Test]
        public void DailyRefillAndMissionsAreDeterministicPerDay()
        {
            var a = new MetaService(Cfg, null);
            var b = new MetaService(Cfg, null);
            a.StartSession(Day0);
            b.StartSession(Day0.AddHours(5));
            CollectionAssert.AreEqual(a.Profile.DailyMissions.Select(m => m.Id), b.Profile.DailyMissions.Select(m => m.Id));
            Assert.AreEqual(Cfg.DailyMissionCount, a.Profile.DailyMissions.Count);
            Assert.AreEqual(Cfg.DailyMissionCount, a.Profile.DailyMissions.Select(m => m.Id).Distinct().Count());
            Assert.AreEqual(Cfg.DailyFreeBoosters[(int)BoosterType.Undo], a.Profile.Boosters[(int)BoosterType.Undo]);
            a.ConsumeBooster(BoosterType.Undo, false);
            a.Rollover(Day0.AddHours(1));
            Assert.AreEqual(Cfg.DailyFreeBoosters[(int)BoosterType.Undo] - 1, a.Profile.Boosters[(int)BoosterType.Undo]);
            a.Rollover(Day0.AddDays(1));
            Assert.AreEqual(Cfg.DailyFreeBoosters[(int)BoosterType.Undo], a.Profile.Boosters[(int)BoosterType.Undo]);
        }

        [Test]
        public void MissionsProgressAndClaimOnce()
        {
            var meta = new MetaService(Cfg, null);
            meta.StartSession(Day0);
            for (int i = 0; i < 6; i++) meta.OnRunFinished(new RunSummary { Mode = "daily", Score = 3000, Lines = 40, Pieces = 100, BestStreak = 6, BoostersUsed = 1, BoardsEmptied = 1, LevelCompleted = true, Stars = 3, Competitive = true }, Day0);
            foreach (var m in meta.Profile.DailyMissions)
            {
                int coins = meta.Profile.Coins;
                Assert.IsTrue(meta.ClaimMission(m.Id, out var reward), m.Id);
                Assert.AreEqual(coins + reward.Coins, meta.Profile.Coins);
                Assert.IsFalse(meta.ClaimMission(m.Id, out _));
            }
            Assert.IsFalse(meta.ClaimMission("nope", out _));
        }

        [Test]
        public void StreakUsesProtectorsAndGivesDaySevenChest()
        {
            var meta = new MetaService(Cfg, null);
            for (int d = 0; d < 6; d++) meta.OnRunFinished(Run(), Day0.AddDays(d));
            Assert.AreEqual(6, meta.Profile.StreakDays);
            Assert.IsTrue(meta.StreakAtRisk(DayKey.FromUtc(Day0.AddDays(6))));
            var r = meta.OnRunFinished(Run(), Day0.AddDays(6));
            Assert.IsTrue(r.StreakChest);
            Assert.AreEqual(7, meta.Profile.StreakDays);
            int prot = meta.Profile.Protectors;
            Assert.That(prot, Is.GreaterThan(0));
            meta.OnRunFinished(Run(), Day0.AddDays(8));
            Assert.AreEqual(8, meta.Profile.StreakDays, "un protector cubre el día perdido");
            Assert.AreEqual(prot - 1, meta.Profile.Protectors);
            meta.Profile.Protectors = 0;
            meta.OnRunFinished(Run(), Day0.AddDays(11));
            Assert.AreEqual(1, meta.Profile.StreakDays);
        }

        [Test]
        public void AchievementsHaveProgressAndTiers()
        {
            var meta = new MetaService(Cfg, null);
            var first = Cfg.Achievements.First(a => a.Id == "first_steps");
            Assert.AreEqual(0f, meta.AchievementProgress(first, out int target));
            Assert.AreEqual(first.Tiers[0], target);
            var r = meta.OnRunFinished(Run(), Day0);
            Assert.IsTrue(r.AchievementsUnlocked.Contains("first_steps#1"));
            float p = meta.AchievementProgress(first, out target);
            Assert.AreEqual(first.Tiers[1], target);
            Assert.That(p, Is.InRange(0f, 1f));
            meta.Profile.Stats["games"] = 100000;
            meta.OnRunFinished(Run(), Day0);
            Assert.AreEqual(1f, meta.AchievementProgress(first, out _));
        }

        [Test]
        public void PassTiersFreeAndPremium()
        {
            var meta = new MetaService(Cfg, null);
            meta.StartSession(Day0);
            meta.Profile.PassXp = Cfg.PassXpPerTier * 3;
            Assert.AreEqual(3, meta.PassTier());
            Assert.IsTrue(meta.ClaimPass(0, false, out _));
            Assert.IsFalse(meta.ClaimPass(0, false, out _));
            Assert.IsFalse(meta.ClaimPass(0, true, out _));
            meta.Profile.PassPremium = true;
            Assert.IsTrue(meta.ClaimPass(0, true, out _));
            Assert.IsFalse(meta.CanClaimPass(5, false));
            Assert.IsFalse(meta.CanClaimPass(-1, false));
            string season = meta.Profile.PassSeason;
            meta.Rollover(Day0.AddDays(Cfg.PassSeasonDays));
            Assert.AreNotEqual(season, meta.Profile.PassSeason);
            Assert.AreEqual(0, meta.Profile.PassXp);
            Assert.IsFalse(meta.Profile.PassPremium);
            Assert.That(meta.SeasonDaysLeft(DayKey.FromUtc(Day0)), Is.InRange(1, Cfg.PassSeasonDays));
        }

        [Test]
        public void ClubDoublesNothingButGivesDailyCoinsAndPassXp()
        {
            var meta = new MetaService(Cfg, null);
            meta.Profile.ClubUntilUtcTicks = Day0.AddDays(30).Ticks;
            meta.StartSession(Day0);
            Assert.AreEqual(Cfg.ClubDailyCoins, meta.Profile.Coins);
            Assert.IsTrue(meta.Profile.AdFree(Day0));
            Assert.IsFalse(meta.Profile.AdFree(Day0.AddDays(31)));
            var plain = new MetaService(Cfg, null);
            plain.StartSession(Day0);
            meta.OnRunFinished(Run(), Day0);
            plain.OnRunFinished(Run(), Day0);
            Assert.That(meta.Profile.PassXp, Is.GreaterThan(plain.Profile.PassXp));
        }

        [Test]
        public void SpendingRules()
        {
            var meta = new MetaService(Cfg, null);
            Assert.IsFalse(meta.BuyBooster(BoosterType.Hammer));
            Assert.IsFalse(meta.ConsumeBooster(BoosterType.Hammer, true));
            meta.Profile.AddCoins(5000, "test");
            Assert.IsTrue(meta.BuyBooster(BoosterType.Hammer, 2));
            Assert.AreEqual(2, meta.Profile.Boosters[(int)BoosterType.Hammer]);
            Assert.IsTrue(meta.ConsumeBooster(BoosterType.Broom, true));
            Assert.AreEqual(5000 - 240 - 200, meta.Profile.Coins);
            Assert.IsTrue(meta.PayRevive());
            Assert.IsTrue(meta.BuyProtector());
            Assert.IsTrue(meta.BuyTheme("theme_oliva_huerto", Day0));
            Assert.IsFalse(meta.BuyTheme("theme_oliva_huerto", Day0));
            Assert.IsFalse(meta.BuyTheme("theme_club_terracota", Day0));
            Assert.IsFalse(meta.BuyTheme("nope", Day0));
            Assert.IsFalse(meta.Profile.TrySpendCoins(-1));
        }

        [Test]
        public void ChestsUseOnlyEarnedCoinsAndMatchProbabilities()
        {
            var meta = new MetaService(Cfg, null);
            var rng = new Pcg32(3);
            Assert.IsNull(meta.OpenChest(ref rng, true));
            var counts = Cfg.Chest.ToDictionary(e => e.Id, e => 0);
            for (int i = 0; i < 20000; i++) counts[meta.OpenChest(ref rng, false).Id]++;
            foreach (var e in Cfg.Chest)
                Assert.AreEqual(Cfg.ChestProbability(e), counts[e.Id] / 20000f, 0.015, e.Id);
        }

        [Test]
        public void ProfileRoundTrips()
        {
            var meta = new MetaService(Cfg, null);
            meta.StartSession(Day0);
            meta.OnRunFinished(Run(2000), Day0);
            meta.Profile.LevelStars["c1_l1"] = 3;
            meta.Profile.OwnedCosmetics.Add("x");
            meta.Profile.PassClaimedFree.Add(2);
            meta.Profile.ClubUntilUtcTicks = Day0.Ticks;
            string a = Json.Serialize(meta.Profile.ToJson());
            var back = PlayerProfile.FromJson(Json.ParseNode(a));
            Assert.AreEqual(a, Json.Serialize(back.ToJson()));
        }

        [Test]
        public void DailyChallengeAndLevelsTracked()
        {
            var meta = new MetaService(Cfg, null);
            meta.OnRunFinished(Run(700, "daily"), Day0);
            meta.OnRunFinished(Run(900, "daily"), Day0);
            Assert.AreEqual(900, meta.Profile.DailyChallengeBest);
            meta.OnRunFinished(Run(100, "daily"), Day0.AddDays(1));
            Assert.AreEqual(2, meta.Profile.DailyStreak);
            meta.OnRunFinished(new RunSummary { Mode = "restoration", LevelId = "c1_l1", LevelCompleted = true, Stars = 2, Score = 300, Lines = 5 }, Day0);
            meta.OnRunFinished(new RunSummary { Mode = "restoration", LevelId = "c1_l1", LevelCompleted = true, Stars = 1, Score = 300, Lines = 5 }, Day0);
            Assert.AreEqual(2, meta.Profile.LevelStars["c1_l1"]);
            Assert.AreEqual(1, meta.Profile.Stat("levels_completed"));
        }

        [Test]
        public void NotificationsMaxOnePerDayAndOnlyWithPermission()
        {
            var meta = new MetaService(Cfg, null);
            int today = DayKey.FromUtc(Day0);
            Assert.IsNull(meta.PlanNotification(today));
            meta.Profile.NotificationsAllowed = true;
            Assert.AreEqual("notif.daily_ready", meta.PlanNotification(today));
            meta.MarkNotificationPlanned(today + 1);
            Assert.IsNull(meta.PlanNotification(today));
            meta.Profile.StreakDays = 3;
            Assert.AreEqual("notif.streak_risk", meta.PlanNotification(today + 1));
        }

        [Test]
        public void EconomySimulatorTypicalPlayerIsHealthy()
        {
            var report = EconomySimulator.Run(Cfg, new PlayerArchetype());
            Assert.AreEqual(30, report.Days.Count);
            Assert.IsTrue(report.Healthy(Cfg), "Saldo final " + report.FinalBalance);
            StringAssert.Contains("Típico", report.ToMarkdown(Cfg));
            Assert.AreEqual(3, EconomySimulator.DefaultArchetypes().Length);
        }

        [Test]
        public void RewardHelpers()
        {
            Assert.IsTrue(default(Reward).IsEmpty);
            var r = Reward.FromJson(Json.ParseNode("{\"coins\":5,\"boosters\":{\"undo\":2}}"));
            Assert.IsFalse(r.IsEmpty);
            Assert.AreEqual(2, r.Boosters[0]);
        }
    }
}
