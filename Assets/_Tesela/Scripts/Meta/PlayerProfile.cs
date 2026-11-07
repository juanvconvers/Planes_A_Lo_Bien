using System;
using System.Collections.Generic;
using Tesela.Core;

namespace Tesela.Meta
{
    public sealed class MissionState
    {
        public string Id;
        public int Progress;
        public bool Claimed;
    }

    /// <summary>Todo el progreso persistente del jugador. Se guarda con SaveCodec (checksum).</summary>
    public sealed class PlayerProfile
    {
        public int Coins;
        public int Tiles;
        public int Protectors;
        public readonly int[] Boosters = new int[Meta.Boosters.Count];
        public int Xp;
        public int Level = 1;

        public int FirstDay = -1;
        public int LastActiveDay = -1;
        public int Sessions;
        public int GamesPlayed;

        public int StreakDays;
        public int LastStreakDay = -1;
        public int StreakChestDay;

        public int LastDailyRefillDay = -1;
        public int DailyMissionDay = -1;
        public int WeeklyMissionWeek = -1;
        public readonly List<MissionState> DailyMissions = new List<MissionState>();
        public readonly List<MissionState> WeeklyMissions = new List<MissionState>();

        public readonly Dictionary<string, long> Stats = new Dictionary<string, long>();
        public readonly Dictionary<string, int> AchievementTier = new Dictionary<string, int>();
        public readonly Dictionary<string, int> LevelStars = new Dictionary<string, int>();
        public readonly Dictionary<string, int> BestScores = new Dictionary<string, int>();
        public readonly HashSet<string> OwnedCosmetics = new HashSet<string>();
        public string ActiveTheme = "sand";
        public int TileSetProgress;
        public int TileSetsCompleted;

        public int DailyChallengeDay = -1;
        public int DailyChallengeBest;
        public int DailyStreak;

        public string PassSeason;
        public int PassXp;
        public bool PassPremium;
        public bool PassPlus;
        public readonly HashSet<int> PassClaimedFree = new HashSet<int>();
        public readonly HashSet<int> PassClaimedPremium = new HashSet<int>();

        public bool NoAds;
        public long ClubUntilUtcTicks;
        public double TotalSpentUsd;
        public int PurchaseCount;
        public long LastPurchaseUtcTicks;
        public bool StarterPackBought;
        public int StarterPackShownDay = -1;
        public int PiggyCoins;

        public bool TutorialDone;
        public bool ConsentAsked;
        public bool NotificationsAllowed;
        public int LastNotificationDay = -1;

        public bool ClubActive(DateTime utcNow) => ClubUntilUtcTicks > utcNow.Ticks;

        public bool PassPlusActive => PassPremium && PassPlus;

        /// <summary>Sin intersticiales ni banners: compra "Sin anuncios", Club activo o Pase Plus de la temporada.</summary>
        public bool AdFree(DateTime utcNow) => NoAds || ClubActive(utcNow) || PassPlusActive;

        public long Stat(string key) => Stats.TryGetValue(key, out long v) ? v : 0;

        public void AddStat(string key, long amount)
        {
            Stats[key] = Stat(key) + amount;
        }

        public void SetStatMax(string key, long value)
        {
            if (value > Stat(key)) Stats[key] = value;
        }

        public int Best(string mode) => BestScores.TryGetValue(mode, out int v) ? v : 0;

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || Coins < amount) return false;
            Coins -= amount;
            AddStat("coins_spent", amount);
            return true;
        }

        public void AddCoins(int amount, string source)
        {
            if (amount <= 0) return;
            Coins += amount;
            AddStat("coins_earned", amount);
            AddStat("coins_from_" + source, amount);
        }

        public void Grant(Reward r, string source)
        {
            AddCoins(r.Coins, source);
            Tiles += Math.Max(0, r.Tiles);
            Protectors += Math.Max(0, r.Protectors);
            PassXp += Math.Max(0, r.PassXp);
            if (r.Boosters != null)
                for (int i = 0; i < Boosters.Length && i < r.Boosters.Length; i++) Boosters[i] += Math.Max(0, r.Boosters[i]);
            if (!string.IsNullOrEmpty(r.Cosmetic)) OwnedCosmetics.Add(r.Cosmetic);
        }

        // ───────────── Serialización ─────────────

        public Dictionary<string, object> ToJson()
        {
            return new Dictionary<string, object>
            {
                { "coins", Coins }, { "tiles", Tiles }, { "protectors", Protectors }, { "boosters", ToList(Boosters) },
                { "xp", Xp }, { "level", Level }, { "firstDay", FirstDay }, { "lastDay", LastActiveDay },
                { "sessions", Sessions }, { "games", GamesPlayed },
                { "streak", StreakDays }, { "streakDay", LastStreakDay }, { "streakChestDay", StreakChestDay },
                { "refillDay", LastDailyRefillDay }, { "missionDay", DailyMissionDay }, { "missionWeek", WeeklyMissionWeek },
                { "daily", MissionsToJson(DailyMissions) }, { "weekly", MissionsToJson(WeeklyMissions) },
                { "stats", ToDict(Stats) }, { "ach", ToDict(AchievementTier) }, { "stars", ToDict(LevelStars) }, { "best", ToDict(BestScores) },
                { "cosmetics", new List<object>(OwnedCosmetics) }, { "theme", ActiveTheme },
                { "tileSet", TileSetProgress }, { "tileSets", TileSetsCompleted },
                { "dcDay", DailyChallengeDay }, { "dcBest", DailyChallengeBest }, { "dcStreak", DailyStreak },
                { "passSeason", PassSeason }, { "passXp", PassXp }, { "passPremium", PassPremium }, { "passPlus", PassPlus },
                { "passFree", ToList(PassClaimedFree) }, { "passPrem", ToList(PassClaimedPremium) },
                { "noAds", NoAds }, { "clubUntil", ClubUntilUtcTicks.ToString() }, { "spent", TotalSpentUsd },
                { "purchases", PurchaseCount }, { "lastPurchase", LastPurchaseUtcTicks.ToString() },
                { "starter", StarterPackBought }, { "starterShown", StarterPackShownDay }, { "piggy", PiggyCoins },
                { "tutorial", TutorialDone }, { "consent", ConsentAsked }, { "notif", NotificationsAllowed }, { "notifDay", LastNotificationDay }
            };
        }

        public static PlayerProfile FromJson(JsonNode n)
        {
            var p = new PlayerProfile
            {
                Coins = n["coins"].AsInt(), Tiles = n["tiles"].AsInt(), Protectors = n["protectors"].AsInt(),
                Xp = n["xp"].AsInt(), Level = Math.Max(1, n["level"].AsInt(1)),
                FirstDay = n["firstDay"].AsInt(-1), LastActiveDay = n["lastDay"].AsInt(-1),
                Sessions = n["sessions"].AsInt(), GamesPlayed = n["games"].AsInt(),
                StreakDays = n["streak"].AsInt(), LastStreakDay = n["streakDay"].AsInt(-1), StreakChestDay = n["streakChestDay"].AsInt(),
                LastDailyRefillDay = n["refillDay"].AsInt(-1), DailyMissionDay = n["missionDay"].AsInt(-1), WeeklyMissionWeek = n["missionWeek"].AsInt(-1),
                ActiveTheme = n["theme"].AsString("sand"), TileSetProgress = n["tileSet"].AsInt(), TileSetsCompleted = n["tileSets"].AsInt(),
                DailyChallengeDay = n["dcDay"].AsInt(-1), DailyChallengeBest = n["dcBest"].AsInt(), DailyStreak = n["dcStreak"].AsInt(),
                PassSeason = n["passSeason"].AsString(), PassXp = n["passXp"].AsInt(), PassPremium = n["passPremium"].AsBool(), PassPlus = n["passPlus"].AsBool(),
                NoAds = n["noAds"].AsBool(), ClubUntilUtcTicks = n["clubUntil"].AsLong(), TotalSpentUsd = n["spent"].AsDouble(),
                PurchaseCount = n["purchases"].AsInt(), LastPurchaseUtcTicks = n["lastPurchase"].AsLong(),
                StarterPackBought = n["starter"].AsBool(), StarterPackShownDay = n["starterShown"].AsInt(-1), PiggyCoins = n["piggy"].AsInt(),
                TutorialDone = n["tutorial"].AsBool(), ConsentAsked = n["consent"].AsBool(), NotificationsAllowed = n["notif"].AsBool(),
                LastNotificationDay = n["notifDay"].AsInt(-1)
            };
            int[] b = n["boosters"].AsIntArray();
            Array.Copy(b, p.Boosters, Math.Min(b.Length, p.Boosters.Length));
            MissionsFromJson(n["daily"], p.DailyMissions);
            MissionsFromJson(n["weekly"], p.WeeklyMissions);
            foreach (var kv in n["stats"].Properties) p.Stats[kv.Key] = kv.Value.AsLong();
            foreach (var kv in n["ach"].Properties) p.AchievementTier[kv.Key] = kv.Value.AsInt();
            foreach (var kv in n["stars"].Properties) p.LevelStars[kv.Key] = kv.Value.AsInt();
            foreach (var kv in n["best"].Properties) p.BestScores[kv.Key] = kv.Value.AsInt();
            foreach (string c in n["cosmetics"].AsStringArray()) p.OwnedCosmetics.Add(c);
            foreach (int t in n["passFree"].AsIntArray()) p.PassClaimedFree.Add(t);
            foreach (int t in n["passPrem"].AsIntArray()) p.PassClaimedPremium.Add(t);
            return p;
        }

        private static List<object> ToList(IEnumerable<int> values)
        {
            var l = new List<object>();
            foreach (int v in values) l.Add(v);
            return l;
        }

        private static Dictionary<string, object> ToDict<T>(Dictionary<string, T> d)
        {
            var r = new Dictionary<string, object>();
            foreach (var kv in d) r[kv.Key] = kv.Value;
            return r;
        }

        private static List<object> MissionsToJson(List<MissionState> missions)
        {
            var l = new List<object>();
            foreach (var m in missions) l.Add(new Dictionary<string, object> { { "id", m.Id }, { "p", m.Progress }, { "c", m.Claimed } });
            return l;
        }

        private static void MissionsFromJson(JsonNode n, List<MissionState> into)
        {
            foreach (var m in n.Items) into.Add(new MissionState { Id = m["id"].AsString(), Progress = m["p"].AsInt(), Claimed = m["c"].AsBool() });
        }
    }
}
