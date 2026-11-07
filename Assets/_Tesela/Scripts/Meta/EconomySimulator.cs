using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Tesela.Core;

namespace Tesela.Meta
{
    /// <summary>Perfil de jugador para el simulador de economía.</summary>
    public sealed class PlayerArchetype
    {
        public string Name = "Típico";
        public int GamesPerDay = 6;
        public int MeanScore = 900;
        public int ScoreSpread = 500;
        public float DaysPlayedRatio = 0.85f;
        public int BoostersPerDay = 2;
        public float ChestEveryDays = 7f;
        public bool ClaimsMissions = true;
        public float RewardedAdsPerDay = 2f;
        public int CoinsPerRewardedAd = 30;
        public int LevelsPerDay = 2;
        /// <summary>Monedas que el jugador quiere conservar antes de comprar un tema (-1 = nunca compra temas).</summary>
        public int ThemeBuffer = 600;
    }

    public sealed class EconomyDay
    {
        public int Day;
        public bool Played;
        public int Earned;
        public int Spent;
        public int Balance;
        public int Level;
        public int PassTier;
        public int StreakDays;
    }

    public sealed class EconomyReport
    {
        public PlayerArchetype Archetype;
        public readonly List<EconomyDay> Days = new List<EconomyDay>();
        public int FinalBalance => Days.Count == 0 ? 0 : Days[Days.Count - 1].Balance;

        public bool Healthy(EconomyConfig c) => FinalBalance >= c.HealthySavingsMin && FinalBalance <= c.HealthySavingsMax;

        public string ToMarkdown(EconomyConfig c)
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("### " + Archetype.Name + " — saldo final " + FinalBalance + " monedas (" + (Healthy(c) ? "sano" : "FUERA DE RANGO") + ")");
            sb.AppendLine();
            sb.AppendLine("| Día | Jugó | Ganadas | Gastadas | Saldo | Nivel | Pase | Racha |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (var d in Days)
                sb.AppendLine(string.Format(inv, "| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} |", d.Day, d.Played ? "sí" : "no", d.Earned, d.Spent, d.Balance, d.Level, d.PassTier, d.StreakDays));
            return sb.ToString();
        }
    }

    /// <summary>Simula N días de un arquetipo con las reglas reales de MetaService para detectar inflación.</summary>
    public static class EconomySimulator
    {
        public static EconomyReport Run(EconomyConfig config, PlayerArchetype a, int days = 30, ulong seed = 1)
        {
            var report = new EconomyReport { Archetype = a };
            var meta = new MetaService(config, new PlayerProfile());
            var rng = new Pcg32(seed);
            var start = new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);
            double chestAccumulator = 0;
            for (int d = 0; d < days; d++)
            {
                var now = start.AddDays(d);
                long earnedBefore = meta.Profile.Stat("coins_earned");
                long spentBefore = meta.Profile.Stat("coins_spent");
                bool played = rng.NextDouble() < a.DaysPlayedRatio || d == 0;
                if (played)
                {
                    meta.StartSession(now);
                    for (int g = 0; g < a.GamesPerDay; g++)
                    {
                        int score = Math.Max(30, a.MeanScore + (int)((rng.NextDouble() * 2 - 1) * a.ScoreSpread));
                        bool level = g < a.LevelsPerDay;
                        meta.OnRunFinished(new RunSummary
                        {
                            Mode = level ? "restoration" : (g == a.LevelsPerDay ? "daily" : "classic"),
                            Score = score,
                            Lines = score / 35,
                            Pieces = score / 9,
                            BestStreak = 2 + rng.NextInt(5),
                            BoostersUsed = g < a.BoostersPerDay ? 1 : 0,
                            BoardsEmptied = rng.NextDouble() < 0.15 ? 1 : 0,
                            LevelId = level ? "sim_" + d + "_" + g : null,
                            LevelCompleted = level,
                            Stars = level ? 1 + rng.NextInt(3) : 0,
                            Competitive = !level
                        }, now.AddMinutes(g * 6));
                    }
                    for (int b = 0; b < a.BoostersPerDay; b++) meta.ConsumeBooster((BoosterType)(b % Boosters.Count), true);
                    int ads = (int)a.RewardedAdsPerDay + (rng.NextDouble() < a.RewardedAdsPerDay % 1 ? 1 : 0);
                    meta.Profile.AddCoins(ads * a.CoinsPerRewardedAd, "ads");
                    if (a.ClaimsMissions)
                    {
                        foreach (var m in meta.Profile.DailyMissions.ToArray()) meta.ClaimMission(m.Id, out _);
                        foreach (var m in meta.Profile.WeeklyMissions.ToArray()) meta.ClaimMission(m.Id, out _);
                    }
                    for (int t = 0; t < config.PassTiers.Count; t++) meta.ClaimPass(t, false, out _);
                    chestAccumulator += 1.0 / Math.Max(1f, a.ChestEveryDays);
                    if (chestAccumulator >= 1.0 && meta.Profile.Coins >= config.ChestPriceCoins)
                    {
                        chestAccumulator -= 1.0;
                        meta.OpenChest(ref rng, true);
                    }
                    foreach (var theme in config.Themes)
                    {
                        if (theme.PriceCoins <= 0 || theme.ClubOnly || meta.Profile.OwnedCosmetics.Contains(theme.Id)) continue;
                        if (meta.Profile.Coins >= theme.PriceCoins + a.ThemeBuffer) meta.BuyTheme(theme.Id, now);
                        break;
                    }
                    if (meta.Profile.Protectors == 0 && meta.Profile.Coins > config.ProtectorPrice * 4) meta.BuyProtector();
                }
                report.Days.Add(new EconomyDay
                {
                    Day = d + 1,
                    Played = played,
                    Earned = (int)(meta.Profile.Stat("coins_earned") - earnedBefore),
                    Spent = (int)(meta.Profile.Stat("coins_spent") - spentBefore),
                    Balance = meta.Profile.Coins,
                    Level = meta.Profile.Level,
                    PassTier = meta.PassTier(),
                    StreakDays = meta.Profile.StreakDays
                });
            }
            return report;
        }

        public static PlayerArchetype[] DefaultArchetypes() => new[]
        {
            new PlayerArchetype(),
            new PlayerArchetype { Name = "Casual (3 partidas, sin anuncios)", GamesPerDay = 3, MeanScore = 600, DaysPlayedRatio = 0.6f, BoostersPerDay = 1, RewardedAdsPerDay = 0, LevelsPerDay = 1, ChestEveryDays = 14, ThemeBuffer = 1000 },
            new PlayerArchetype { Name = "Intenso (12 partidas)", GamesPerDay = 12, MeanScore = 1600, ScoreSpread = 900, DaysPlayedRatio = 1f, BoostersPerDay = 4, RewardedAdsPerDay = 4, LevelsPerDay = 3, ChestEveryDays = 3 }
        };
    }
}
