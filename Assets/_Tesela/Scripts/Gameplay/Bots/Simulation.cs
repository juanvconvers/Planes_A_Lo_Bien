using System;
using System.Collections.Generic;
using System.Text;
using Tesela.Core;

namespace Tesela.Gameplay.Bots
{
    public sealed class SimulationStats
    {
        public string BotName;
        public int Games;
        public int Exceptions;
        public int FalseGameOvers;
        public int TraysWithoutFit;
        public int VarietyViolations;
        public int CappedGames;
        public long TotalScore;
        public long TotalMoves;
        public long TotalLines;
        public int MaxScore;
        public int MaxStreak;
        public readonly List<int> Scores = new List<int>();
        public readonly List<string> ExceptionSamples = new List<string>();

        public double AvgScore => Games == 0 ? 0 : (double)TotalScore / Games;
        public double AvgMoves => Games == 0 ? 0 : (double)TotalMoves / Games;
        public double AvgLines => Games == 0 ? 0 : (double)TotalLines / Games;

        public int Percentile(double p)
        {
            if (Scores.Count == 0) return 0;
            var sorted = new List<int>(Scores);
            sorted.Sort();
            int idx = (int)Math.Round(p * (sorted.Count - 1));
            return sorted[idx];
        }

        public bool Passed => Exceptions == 0 && FalseGameOvers == 0 && TraysWithoutFit == 0;
    }

    /// <summary>Simulación masiva de partidas: valida la detección de fin y las garantías del generador.</summary>
    public static class Simulation
    {
        public static SimulationStats Run(Func<ulong, IBot> botFactory, string botName, int games, ModeRules rules,
                                          ScoringConfig scoring, PieceCatalog catalog, ulong baseSeed = 1, int maxMoves = 3000)
        {
            var stats = new SimulationStats { BotName = botName };
            for (int g = 0; g < games; g++)
            {
                ulong seed = Hashing.Mix(baseSeed + (ulong)g);
                try
                {
                    RunOne(botFactory(seed ^ 0xABCDEFUL), rules, scoring, catalog, seed, maxMoves, stats);
                }
                catch (Exception e)
                {
                    stats.Exceptions++;
                    if (stats.ExceptionSamples.Count < 5) stats.ExceptionSamples.Add(seed + ": " + e.GetType().Name + " " + e.Message);
                }
                stats.Games++;
            }
            return stats;
        }

        private static void RunOne(IBot bot, ModeRules rules, ScoringConfig scoring, PieceCatalog catalog, ulong seed, int maxMoves, SimulationStats stats)
        {
            var s = new GameSession(rules, scoring, catalog, seed);
            var families = new List<string>(4096);
            RecordTray(s, families, stats);
            int moves = 0;
            while (!s.IsOver && moves < maxMoves)
            {
                if (!bot.Choose(s, out int slot, out int x, out int y))
                {
                    stats.FalseGameOvers++;
                    break;
                }
                var r = s.Place(slot, x, y);
                moves++;
                if (r.TrayRefilled) RecordTray(s, families, stats);
            }
            if (moves >= maxMoves && !s.IsOver) stats.CappedGames++;
            if (s.IsOver && s.OverReason == GameOverReason.NoFit && BruteForceAnyFit(s)) stats.FalseGameOvers++;

            for (int i = 2; i < families.Count; i++)
                if (families[i] == families[i - 1] && families[i] == families[i - 2]) stats.VarietyViolations++;

            stats.TotalScore += s.Score;
            stats.TotalMoves += s.Moves;
            stats.TotalLines += s.LinesCleared;
            stats.MaxScore = Math.Max(stats.MaxScore, s.Score);
            stats.MaxStreak = Math.Max(stats.MaxStreak, s.BestStreak);
            stats.Scores.Add(s.Score);
        }

        private static void RecordTray(GameSession s, List<string> families, SimulationStats stats)
        {
            bool anyFits = false;
            foreach (var p in s.Tray)
            {
                if (p.IsEmpty) continue;
                families.Add(p.Shape.Family);
                if (BruteForceFits(s.Board, p.Shape)) anyFits = true;
            }
            if (!anyFits) stats.TraysWithoutFit++;
        }

        /// <summary>Comprobación independiente celda a celda (no usa las máscaras de bits del tablero).</summary>
        public static bool BruteForceFits(Board b, PieceShape shape)
        {
            for (int y = 0; y < b.Height; y++)
                for (int x = 0; x < b.Width; x++)
                {
                    bool ok = true;
                    foreach (var c in shape.Cells)
                    {
                        int cx = x + c.X, cy = y + c.Y;
                        if (cx >= b.Width || cy >= b.Height || b.GetGlaze(cx, cy) != Board.Empty) { ok = false; break; }
                    }
                    if (ok) return true;
                }
            return false;
        }

        public static bool BruteForceAnyFit(GameSession s)
        {
            foreach (var p in s.Tray)
                if (!p.IsEmpty && BruteForceFits(s.Board, p.Shape)) return true;
            return false;
        }

        public static string ToMarkdown(IEnumerable<SimulationStats> all, string title)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# " + title).AppendLine();
            sb.AppendLine("| Bot | Partidas | Excepciones | Fines falsos | Bandejas sin pieza jugable | >2 iguales seguidas | Puntuación media | Mediana | P90 | Máx | Jugadas medias | Líneas medias | Racha máx | Topadas |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var s in all)
            {
                sb.Append("| ").Append(s.BotName).Append(" | ").Append(s.Games).Append(" | ").Append(s.Exceptions).Append(" | ")
                  .Append(s.FalseGameOvers).Append(" | ").Append(s.TraysWithoutFit).Append(" | ").Append(s.VarietyViolations).Append(" | ")
                  .Append(s.AvgScore.ToString("F0", System.Globalization.CultureInfo.InvariantCulture)).Append(" | ")
                  .Append(s.Percentile(0.5)).Append(" | ").Append(s.Percentile(0.9)).Append(" | ").Append(s.MaxScore).Append(" | ")
                  .Append(s.AvgMoves.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)).Append(" | ")
                  .Append(s.AvgLines.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)).Append(" | ")
                  .Append(s.MaxStreak).Append(" | ").Append(s.CappedGames).AppendLine(" |");
            }
            return sb.ToString();
        }
    }
}
