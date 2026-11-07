using System;
using System.Collections.Generic;
using Tesela.Core;

namespace Tesela.Meta
{
    /// <summary>Resumen de una partida terminada (lo produce la capa de juego).</summary>
    public struct RunSummary
    {
        public string Mode;
        public int Score;
        public int Lines;
        public int Pieces;
        public int BestStreak;
        public int BoostersUsed;
        public int BoardsEmptied;
        public string LevelId;
        public int Stars;
        public bool LevelCompleted;
        public bool Competitive;
    }

    public sealed class RunRewards
    {
        public int Coins;
        public int Xp;
        public int Tiles;
        public int LevelsGained;
        public bool NewBest;
        public bool StreakChest;
        public Reward StreakChestReward;
        public int TileSetsCompleted;
        public readonly List<string> MissionsReady = new List<string>();
        public readonly List<string> AchievementsUnlocked = new List<string>();
        public int PassTiersGained;
    }

    /// <summary>Reglas de meta y retención. Puro y determinista: recibe la fecha, no la consulta.</summary>
    public sealed class MetaService
    {
        public readonly EconomyConfig Config;
        public PlayerProfile Profile { get; private set; }

        /// <summary>Las temporadas del pase empiezan cada 28 días desde este día (lunes 5 ene 2026).</summary>
        public static readonly int SeasonEpochDay = DayKey.FromUtc(new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc));

        public MetaService(EconomyConfig config, PlayerProfile profile)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Profile = profile ?? new PlayerProfile();
        }

        // ───────────── Sesión y cambio de día ─────────────

        public void StartSession(DateTime utcNow)
        {
            int day = DayKey.FromUtc(utcNow);
            if (Profile.FirstDay < 0) Profile.FirstDay = day;
            Profile.Sessions++;
            Rollover(utcNow);
        }

        /// <summary>Aplica lo que toca al cambiar de día/semana/temporada. Idempotente dentro del mismo día.</summary>
        public void Rollover(DateTime utcNow)
        {
            int day = DayKey.FromUtc(utcNow);
            Profile.LastActiveDay = day;
            if (Profile.LastDailyRefillDay != day)
            {
                Profile.LastDailyRefillDay = day;
                for (int i = 0; i < Boosters.Count; i++)
                    Profile.Boosters[i] = Math.Max(Profile.Boosters[i], Config.DailyFreeBoosters[i]);
                if (Profile.ClubActive(utcNow))
                {
                    Profile.AddCoins(Config.ClubDailyCoins, "club");
                    for (int i = 0; i < Boosters.Count; i++) Profile.Boosters[i] += 1;
                }
            }
            if (Profile.DailyMissionDay != day)
            {
                Profile.DailyMissionDay = day;
                PickMissions(Config.DailyMissions, Config.DailyMissionCount, (ulong)day, Profile.DailyMissions);
            }
            int week = DayKey.WeekFromDay(day);
            if (Profile.WeeklyMissionWeek != week)
            {
                Profile.WeeklyMissionWeek = week;
                PickMissions(Config.WeeklyMissions, Config.WeeklyMissionCount, (ulong)week * 7919UL, Profile.WeeklyMissions);
            }
            string season = SeasonId(day);
            if (Profile.PassSeason != season)
            {
                Profile.PassSeason = season;
                Profile.PassXp = 0;
                Profile.PassPremium = false;
                Profile.PassPlus = false;
                Profile.PassClaimedFree.Clear();
                Profile.PassClaimedPremium.Clear();
            }
        }

        public string SeasonId(int day) => "S" + (1 + Math.Max(0, day - SeasonEpochDay) / Config.PassSeasonDays);

        public int SeasonDaysLeft(int day) => Config.PassSeasonDays - (Math.Max(0, day - SeasonEpochDay) % Config.PassSeasonDays);

        private static void PickMissions(List<MissionDef> pool, int count, ulong seed, List<MissionState> into)
        {
            into.Clear();
            var rng = new Pcg32(Hashing.Mix(seed + 17UL));
            var indices = new List<int>(pool.Count);
            for (int i = 0; i < pool.Count; i++) indices.Add(i);
            for (int i = 0; i < count && indices.Count > 0; i++)
            {
                int k = rng.NextInt(indices.Count);
                into.Add(new MissionState { Id = pool[indices[k]].Id });
                indices.RemoveAt(k);
            }
        }

        // ───────────── Fin de partida ─────────────

        public RunRewards OnRunFinished(RunSummary run, DateTime utcNow)
        {
            Rollover(utcNow);
            int day = DayKey.FromUtc(utcNow);
            var r = new RunRewards();
            var p = Profile;

            p.GamesPlayed++;
            p.AddStat("games", 1);
            p.AddStat("games_" + run.Mode, 1);
            p.AddStat("lines", run.Lines);
            p.AddStat("pieces", run.Pieces);
            p.AddStat("boards_emptied", run.BoardsEmptied);
            p.SetStatMax("max_streak", run.BestStreak);

            r.Coins = Math.Min(Config.RunCoinCap, run.Score / Config.CoinsScoreDivisor);
            if (run.LevelCompleted) r.Coins += 10 * Math.Max(1, run.Stars);
            p.AddCoins(r.Coins, "runs");
            p.PiggyCoins += r.Coins;

            r.Xp = run.Score / Config.XpScoreDivisor + run.Lines * Config.XpPerLine;
            r.LevelsGained = AddXp(r.Xp);
            int passBefore = PassTier();
            float passMult = p.ClubActive(utcNow) ? Config.ClubPassXpMultiplier : 1f;
            p.PassXp += (int)Math.Round(r.Xp * passMult);
            r.PassTiersGained = PassTier() - passBefore;

            r.Tiles = run.Lines * Config.TilesPerLine + (run.LevelCompleted ? Config.LevelCompleteTiles : 0);
            p.Tiles += r.Tiles;
            p.TileSetProgress += r.Tiles;
            while (p.TileSetProgress >= Config.TileSetSize)
            {
                p.TileSetProgress -= Config.TileSetSize;
                p.TileSetsCompleted++;
                r.TileSetsCompleted++;
                p.AddCoins(Config.TileSetBonusCoins, "collections");
            }
            p.Stats["tilesets"] = p.TileSetsCompleted;

            if (run.Competitive && run.Score > p.Best(run.Mode))
            {
                r.NewBest = p.Best(run.Mode) > 0;
                p.BestScores[run.Mode] = run.Score;
            }
            p.SetStatMax("best_" + run.Mode, run.Score);

            if (!string.IsNullOrEmpty(run.LevelId) && run.LevelCompleted)
            {
                int prev = p.LevelStars.TryGetValue(run.LevelId, out int s) ? s : 0;
                if (prev == 0) p.AddStat("levels_completed", 1);
                p.LevelStars[run.LevelId] = Math.Max(prev, run.Stars);
            }

            if (run.Mode == "daily")
            {
                if (p.DailyChallengeDay != day)
                {
                    p.DailyStreak = p.DailyChallengeDay == day - 1 ? p.DailyStreak + 1 : 1;
                    p.DailyChallengeDay = day;
                    p.DailyChallengeBest = 0;
                }
                p.DailyChallengeBest = Math.Max(p.DailyChallengeBest, run.Score);
            }

            UpdateStreak(day, r);
            AdvanceMissions(run, r);
            CheckAchievements(r);
            return r;
        }

        private int AddXp(int xp)
        {
            int gained = 0;
            Profile.Xp += xp;
            while (Profile.Xp >= Config.XpForLevel(Profile.Level))
            {
                Profile.Xp -= Config.XpForLevel(Profile.Level);
                Profile.Level++;
                gained++;
            }
            return gained;
        }

        /// <summary>Racha de días jugados. Los protectores cubren días perdidos automáticamente (uno por día).</summary>
        private void UpdateStreak(int day, RunRewards r)
        {
            var p = Profile;
            if (p.LastStreakDay == day) return;
            int gap = p.LastStreakDay < 0 ? int.MaxValue : day - p.LastStreakDay;
            if (gap == 1) p.StreakDays++;
            else if (gap != int.MaxValue && gap - 1 <= p.Protectors)
            {
                p.Protectors -= gap - 1;
                p.AddStat("protectors_used", gap - 1);
                p.StreakDays++;
            }
            else p.StreakDays = 1;
            p.LastStreakDay = day;
            p.SetStatMax("max_day_streak", p.StreakDays);
            if (p.StreakDays % 7 == 0 && p.StreakChestDay != day)
            {
                p.StreakChestDay = day;
                p.Grant(Config.StreakDay7, "streak");
                r.StreakChest = true;
                r.StreakChestReward = Config.StreakDay7;
            }
        }

        /// <summary>¿La racha se perderá si hoy no se juega? (para la notificación "racha en riesgo").</summary>
        public bool StreakAtRisk(int day) => Profile.StreakDays >= 2 && Profile.LastStreakDay == day - 1;

        // ───────────── Misiones ─────────────

        public MissionDef FindMission(string id)
        {
            foreach (var m in Config.DailyMissions) if (m.Id == id) return m;
            foreach (var m in Config.WeeklyMissions) if (m.Id == id) return m;
            return null;
        }

        private void AdvanceMissions(RunSummary run, RunRewards r)
        {
            Advance(Profile.DailyMissions, run, r);
            Advance(Profile.WeeklyMissions, run, r);
        }

        private void Advance(List<MissionState> list, RunSummary run, RunRewards r)
        {
            foreach (var m in list)
            {
                var def = FindMission(m.Id);
                if (def == null || m.Claimed) continue;
                bool wasDone = m.Progress >= def.Target;
                switch (def.Metric)
                {
                    case "games": m.Progress += 1; break;
                    case "lines": m.Progress += run.Lines; break;
                    case "pieces": m.Progress += run.Pieces; break;
                    case "boosters": m.Progress += run.BoostersUsed; break;
                    case "score_run": m.Progress = Math.Max(m.Progress, run.Score); break;
                    case "streak_run": m.Progress = Math.Max(m.Progress, run.BestStreak); break;
                    case "daily": if (run.Mode == "daily") m.Progress += 1; break;
                    case "levels": if (run.LevelCompleted) m.Progress += 1; break;
                    case "stars": if (run.LevelCompleted) m.Progress += run.Stars; break;
                    case "empty_board": m.Progress += run.BoardsEmptied; break;
                }
                m.Progress = Math.Min(m.Progress, def.Target);
                if (!wasDone && m.Progress >= def.Target) r.MissionsReady.Add(m.Id);
            }
        }

        public bool ClaimMission(string id, out Reward reward)
        {
            reward = default;
            foreach (var list in new[] { Profile.DailyMissions, Profile.WeeklyMissions })
                foreach (var m in list)
                {
                    if (m.Id != id || m.Claimed) continue;
                    var def = FindMission(id);
                    if (def == null || m.Progress < def.Target) return false;
                    m.Claimed = true;
                    reward = def.Reward;
                    Profile.Grant(reward, "missions");
                    return true;
                }
            return false;
        }

        // ───────────── Logros ─────────────

        public long AchievementMetric(AchievementDef a) => Profile.Stat(a.Metric);

        /// <summary>Progreso hacia el siguiente escalón (0..1) para la barra de progreso.</summary>
        public float AchievementProgress(AchievementDef a, out int nextTarget)
        {
            int tier = Profile.AchievementTier.TryGetValue(a.Id, out int t) ? t : 0;
            if (tier >= a.Tiers.Length)
            {
                nextTarget = a.Tiers[a.Tiers.Length - 1];
                return 1f;
            }
            nextTarget = a.Tiers[tier];
            return Math.Min(1f, AchievementMetric(a) / (float)nextTarget);
        }

        private void CheckAchievements(RunRewards r)
        {
            foreach (var a in Config.Achievements)
            {
                int tier = Profile.AchievementTier.TryGetValue(a.Id, out int t) ? t : 0;
                long value = AchievementMetric(a);
                while (tier < a.Tiers.Length && value >= a.Tiers[tier])
                {
                    tier++;
                    Profile.AddCoins(a.RewardCoins * tier, "achievements");
                    r.AchievementsUnlocked.Add(a.Id + "#" + tier);
                }
                Profile.AchievementTier[a.Id] = tier;
            }
        }

        // ───────────── Pase de temporada ─────────────

        public int PassTier() => Math.Min(Config.PassTiers.Count, Profile.PassXp / Math.Max(1, Config.PassXpPerTier));

        public bool CanClaimPass(int tier, bool premium)
        {
            if (tier < 0 || tier >= Config.PassTiers.Count || tier >= PassTier()) return false;
            if (premium && !Profile.PassPremium) return false;
            return premium ? !Profile.PassClaimedPremium.Contains(tier) : !Profile.PassClaimedFree.Contains(tier);
        }

        public bool ClaimPass(int tier, bool premium, out Reward reward)
        {
            reward = default;
            if (!CanClaimPass(tier, premium)) return false;
            reward = premium ? Config.PassTiers[tier].Premium : Config.PassTiers[tier].Free;
            (premium ? Profile.PassClaimedPremium : Profile.PassClaimedFree).Add(tier);
            Profile.Grant(reward, "pass");
            return true;
        }

        // ───────────── Gastos con monedas ─────────────

        public bool BuyBooster(BoosterType t, int amount = 1)
        {
            if (!Profile.TrySpendCoins(Config.BoosterPrice(t) * amount)) return false;
            Profile.Boosters[(int)t] += amount;
            return true;
        }

        /// <summary>Usa una carga; si no hay, intenta pagarla con monedas. Devuelve false si no se pudo.</summary>
        public bool ConsumeBooster(BoosterType t, bool allowCoins)
        {
            if (Profile.Boosters[(int)t] > 0)
            {
                Profile.Boosters[(int)t]--;
                Profile.AddStat("boosters", 1);
                return true;
            }
            if (!allowCoins || !Profile.TrySpendCoins(Config.BoosterPrice(t))) return false;
            Profile.AddStat("boosters", 1);
            return true;
        }

        public bool PayRevive() => Profile.TrySpendCoins(Config.RevivePrice);

        public bool BuyProtector()
        {
            if (!Profile.TrySpendCoins(Config.ProtectorPrice)) return false;
            Profile.Protectors++;
            return true;
        }

        public bool BuyTheme(string id, DateTime utcNow)
        {
            var theme = Config.Themes.Find(t => t.Id == id);
            if (theme == null || Profile.OwnedCosmetics.Contains(id)) return false;
            if (theme.ClubOnly && !Profile.ClubActive(utcNow)) return false;
            if (!Profile.TrySpendCoins(theme.PriceCoins)) return false;
            Profile.OwnedCosmetics.Add(id);
            return true;
        }

        /// <summary>Cofre: solo con monedas ganadas jugando (nunca con dinero real). Las probabilidades se muestran antes.</summary>
        public ChestEntry OpenChest(ref Pcg32 rng, bool payWithCoins)
        {
            if (payWithCoins && !Profile.TrySpendCoins(Config.ChestPriceCoins)) return null;
            float total = 0f;
            foreach (var e in Config.Chest) total += e.Weight;
            double roll = rng.NextDouble() * total;
            ChestEntry chosen = Config.Chest[Config.Chest.Count - 1];
            foreach (var e in Config.Chest)
            {
                roll -= e.Weight;
                if (roll < 0) { chosen = e; break; }
            }
            Profile.Grant(chosen.Reward, "chest");
            Profile.AddStat("chests", 1);
            return chosen;
        }

        // ───────────── Notificaciones locales ─────────────

        /// <summary>Clave del mensaje a programar para mañana (máx. una al día, solo con permiso), o null.</summary>
        public string PlanNotification(int today)
        {
            if (!Profile.NotificationsAllowed || Profile.LastNotificationDay >= today + 1) return null;
            if (Profile.StreakDays >= 2) return "notif.streak_risk";
            return "notif.daily_ready";
        }

        public void MarkNotificationPlanned(int forDay) => Profile.LastNotificationDay = forDay;
    }
}
