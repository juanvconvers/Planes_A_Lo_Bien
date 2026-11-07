using System;
using System.Collections.Generic;
using Tesela.Core;

namespace Tesela.Meta
{
    public enum BoosterType
    {
        Undo = 0,
        Reserve = 1,
        Rotate = 2,
        Hammer = 3,
        Broom = 4,
        NewTray = 5
    }

    public static class Boosters
    {
        public const int Count = 6;

        public static string Key(BoosterType t)
        {
            switch (t)
            {
                case BoosterType.Undo: return "undo";
                case BoosterType.Reserve: return "reserve";
                case BoosterType.Rotate: return "rotate";
                case BoosterType.Hammer: return "hammer";
                case BoosterType.Broom: return "broom";
                default: return "newtray";
            }
        }

        public static BoosterType Parse(string key)
        {
            for (int i = 0; i < Count; i++)
                if (Key((BoosterType)i) == key) return (BoosterType)i;
            throw new FormatException("Potenciador desconocido: " + key);
        }
    }

    /// <summary>Recompensa genérica (misiones, cofres, pase, logros, compras).</summary>
    public struct Reward
    {
        public int Coins;
        public int Tiles;
        public int Protectors;
        public int[] Boosters;
        public string Cosmetic;
        public int PassXp;

        public static Reward FromJson(JsonNode n)
        {
            var r = new Reward
            {
                Coins = n["coins"].AsInt(),
                Tiles = n["tiles"].AsInt(),
                Protectors = n["protectors"].AsInt(),
                Cosmetic = n["cosmetic"].AsString(),
                PassXp = n["passXp"].AsInt()
            };
            if (n["boosters"].IsObject)
            {
                r.Boosters = new int[Meta.Boosters.Count];
                foreach (var kv in n["boosters"].Properties) r.Boosters[(int)Meta.Boosters.Parse(kv.Key)] = kv.Value.AsInt();
            }
            return r;
        }

        public bool IsEmpty => Coins == 0 && Tiles == 0 && Protectors == 0 && PassXp == 0 && string.IsNullOrEmpty(Cosmetic) && (Boosters == null || Array.TrueForAll(Boosters, b => b == 0));
    }

    public sealed class MissionDef
    {
        public string Id;
        public string Metric;
        public int Target;
        public Reward Reward;
    }

    public sealed class ChestEntry
    {
        public string Id;
        public float Weight;
        public Reward Reward;
    }

    public sealed class AchievementDef
    {
        public string Id;
        public string Metric;
        public int[] Tiers;
        public int RewardCoins;
    }

    public sealed class PassTier
    {
        public Reward Free;
        public Reward Premium;
    }

    public sealed class ThemeDef
    {
        public string Id;
        public int PriceCoins;
        public bool ClubOnly;
    }

    /// <summary>Economy.json: todos los números de la economía. Remote Config puede sobrescribirlo completo.</summary>
    public sealed class EconomyConfig
    {
        public int CoinsScoreDivisor = 50;
        public int RunCoinCap = 60;
        public int[] BoosterPrices = new int[Boosters.Count];
        public int[] DailyFreeBoosters = new int[Boosters.Count];
        public int RevivePrice = 300;
        public int ProtectorPrice = 250;
        public int XpScoreDivisor = 25;
        public int XpPerLine = 2;
        public int LevelXpBase = 100;
        public float LevelXpGrowth = 1.12f;
        public int TilesPerLine = 1;
        public int LevelCompleteTiles = 10;
        public int DailyMissionCount = 3;
        public int WeeklyMissionCount = 3;
        public readonly List<MissionDef> DailyMissions = new List<MissionDef>();
        public readonly List<MissionDef> WeeklyMissions = new List<MissionDef>();
        public readonly List<ChestEntry> Chest = new List<ChestEntry>();
        public int ChestPriceCoins = 400;
        public Reward StreakDay7;
        public readonly List<AchievementDef> Achievements = new List<AchievementDef>();
        public int PassSeasonDays = 28;
        public int PassXpPerTier = 120;
        public readonly List<PassTier> PassTiers = new List<PassTier>();
        public float ClubPassXpMultiplier = 1.5f;
        public int ClubDailyCoins = 100;
        public readonly List<ThemeDef> Themes = new List<ThemeDef>();
        public int TileSetSize = 12;
        public int TileSetBonusCoins = 150;
        /// <summary>Rango sano del saldo del jugador típico al día 30 (lo comprueba el simulador).</summary>
        public int HealthySavingsMin = 300;
        public int HealthySavingsMax = 4000;

        public int BoosterPrice(BoosterType t) => BoosterPrices[(int)t];

        public static EconomyConfig FromJson(string json)
        {
            var n = Json.ParseNode(json);
            var c = new EconomyConfig
            {
                CoinsScoreDivisor = n["coinsScoreDivisor"].AsInt(50),
                RunCoinCap = n["runCoinCap"].AsInt(60),
                RevivePrice = n["revivePrice"].AsInt(300),
                ProtectorPrice = n["protectorPrice"].AsInt(250),
                XpScoreDivisor = n["xpScoreDivisor"].AsInt(25),
                XpPerLine = n["xpPerLine"].AsInt(2),
                LevelXpBase = n["levelXpBase"].AsInt(100),
                LevelXpGrowth = n["levelXpGrowth"].AsFloat(1.12f),
                TilesPerLine = n["tilesPerLine"].AsInt(1),
                LevelCompleteTiles = n["levelCompleteTiles"].AsInt(10),
                DailyMissionCount = n["dailyMissionCount"].AsInt(3),
                WeeklyMissionCount = n["weeklyMissionCount"].AsInt(3),
                ChestPriceCoins = n["chestPriceCoins"].AsInt(400),
                StreakDay7 = Reward.FromJson(n["streakDay7"]),
                PassSeasonDays = n["pass"]["seasonDays"].AsInt(28),
                PassXpPerTier = n["pass"]["xpPerTier"].AsInt(120),
                ClubPassXpMultiplier = n["club"]["passXpMultiplier"].AsFloat(1.5f),
                ClubDailyCoins = n["club"]["dailyCoins"].AsInt(100),
                TileSetSize = n["tileSetSize"].AsInt(12),
                TileSetBonusCoins = n["tileSetBonusCoins"].AsInt(150),
                HealthySavingsMin = n["healthySavings"][0].AsInt(300),
                HealthySavingsMax = n["healthySavings"][1].AsInt(4000)
            };
            foreach (var kv in n["boosterPrices"].Properties) c.BoosterPrices[(int)Boosters.Parse(kv.Key)] = kv.Value.AsInt();
            foreach (var kv in n["dailyFreeBoosters"].Properties) c.DailyFreeBoosters[(int)Boosters.Parse(kv.Key)] = kv.Value.AsInt();
            ReadMissions(n["dailyMissions"], c.DailyMissions);
            ReadMissions(n["weeklyMissions"], c.WeeklyMissions);
            foreach (var e in n["chest"].Items)
                c.Chest.Add(new ChestEntry { Id = e["id"].AsString(), Weight = e["weight"].AsFloat(1f), Reward = Reward.FromJson(e["reward"]) });
            foreach (var a in n["achievements"].Items)
                c.Achievements.Add(new AchievementDef { Id = a["id"].AsString(), Metric = a["metric"].AsString(), Tiers = a["tiers"].AsIntArray(), RewardCoins = a["rewardCoins"].AsInt(50) });
            foreach (var t in n["pass"]["tiers"].Items)
                c.PassTiers.Add(new PassTier { Free = Reward.FromJson(t["free"]), Premium = Reward.FromJson(t["premium"]) });
            foreach (var t in n["themes"].Items)
                c.Themes.Add(new ThemeDef { Id = t["id"].AsString(), PriceCoins = t["price"].AsInt(), ClubOnly = t["clubOnly"].AsBool() });
            c.Validate();
            return c;
        }

        private static void ReadMissions(JsonNode list, List<MissionDef> into)
        {
            foreach (var m in list.Items)
                into.Add(new MissionDef { Id = m["id"].AsString(), Metric = m["metric"].AsString(), Target = m["target"].AsInt(1), Reward = Reward.FromJson(m["reward"]) });
        }

        private void Validate()
        {
            if (CoinsScoreDivisor <= 0 || XpScoreDivisor <= 0) throw new FormatException("Divisores deben ser > 0");
            if (DailyMissions.Count < DailyMissionCount) throw new FormatException("Faltan misiones diarias en el pool");
            if (WeeklyMissions.Count < WeeklyMissionCount) throw new FormatException("Faltan misiones semanales en el pool");
            if (Chest.Count == 0) throw new FormatException("Cofre sin contenido");
            foreach (var e in Chest) if (e.Weight <= 0) throw new FormatException("Peso de cofre inválido: " + e.Id);
            for (int i = 0; i < Boosters.Count; i++) if (BoosterPrices[i] <= 0) throw new FormatException("Falta precio de " + Boosters.Key((BoosterType)i));
        }

        /// <summary>XP necesaria para pasar del nivel <paramref name="level"/> al siguiente.</summary>
        public int XpForLevel(int level) => (int)Math.Round(LevelXpBase * Math.Pow(LevelXpGrowth, Math.Max(0, level - 1)));

        /// <summary>Probabilidades visibles del cofre (se muestran en la UI antes de abrirlo).</summary>
        public float ChestProbability(ChestEntry e)
        {
            float total = 0f;
            foreach (var c in Chest) total += c.Weight;
            return e.Weight / total;
        }
    }
}
