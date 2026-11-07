using System;
using System.Collections.Generic;
using Tesela.Core;
using Tesela.Gameplay.Bots;

namespace Tesela.Gameplay
{
    public struct ValidationResult
    {
        public int Runs;
        public int Successes;
        public float Solvability => Runs == 0 ? 0f : Successes / (float)Runs;
        public int MedianMovesUsed;
        public int[] MovesLeftOnSuccess;
        public bool GreedySolves;
    }

    /// <summary>
    /// Validador de niveles: un solver codicioso que persigue los objetivos más N partidas Monte Carlo (codicioso con ruido). Rechaza niveles con
    /// menos del 70 % de resolución. Lo usan la ventana del editor, la herramienta de generación y los tests.
    /// </summary>
    public static class LevelValidator
    {
        public const float MinSolvability = 0.7f;

        public static ValidationResult Validate(LevelDefinition level, ScoringConfig scoring, PieceCatalog catalog, int runs = 40, int moveLimitOverride = 0)
        {
            var result = new ValidationResult { Runs = runs };
            var used = new List<int>();
            var left = new List<int>();
            int savedLimit = level.MoveLimit;
            if (moveLimitOverride > 0) level.MoveLimit = moveLimitOverride;
            try
            {
                for (int r = 0; r < runs; r++)
                {
                    IBot bot = r == 0 ? new GreedyBot(1, 0, true) : new GreedyBot((ulong)(r * 7919 + 3), 6, true);
                    var s = new GameSession(ModeRules.Restoration(), scoring, catalog, level.Seed, level);
                    while (!s.IsOver)
                    {
                        if (!bot.Choose(s, out int slot, out int x, out int y)) break;
                        s.Place(slot, x, y);
                    }
                    bool ok = s.OverReason == GameOverReason.LevelComplete;
                    if (r == 0) result.GreedySolves = ok;
                    if (!ok) continue;
                    result.Successes++;
                    used.Add(s.Moves);
                    left.Add(s.MovesLeft);
                }
            }
            finally
            {
                level.MoveLimit = savedLimit;
            }
            used.Sort();
            left.Sort();
            result.MedianMovesUsed = used.Count == 0 ? 0 : used[used.Count / 2];
            result.MovesLeftOnSuccess = left.ToArray();
            return result;
        }

        public static int Percentile(int[] sorted, float p)
        {
            if (sorted == null || sorted.Length == 0) return 0;
            int idx = (int)Math.Round(p * (sorted.Length - 1));
            return sorted[Math.Max(0, Math.Min(sorted.Length - 1, idx))];
        }
    }

    /// <summary>
    /// Generador de niveles de Restauración por capítulo. Diseña tablero inicial y objetivos según la dificultad,
    /// ajusta el límite de jugadas con el validador y fija las estrellas con la distribución de jugadas sobrantes.
    /// </summary>
    public static class LevelGenerator
    {
        public static LevelDefinition Generate(int chapter, int index, ScoringConfig scoring, PieceCatalog catalog, int validationRuns = 60)
        {
            int ordinal = (chapter - 1) * 20 + index;
            float t = Math.Min(1f, ordinal / 60f);
            var rng = new Pcg32(Hashing.Mix((ulong)(chapter * 1000 + index)));
            for (int attempt = 0; attempt < 30; attempt++)
            {
                // Si un diseño no resulta resoluble, el siguiente intento baja un poco la exigencia.
                float soft = (float)Math.Pow(0.93, attempt);
                var level = Design(chapter, index, t * soft, ref rng);
                if (TryTune(level, t, scoring, catalog, validationRuns)) return level;
            }
            throw new InvalidOperationException("No se pudo generar un nivel resoluble: " + chapter + "-" + index);
        }

        private static LevelDefinition Design(int chapter, int index, float t, ref Pcg32 rng)
        {
            var level = new LevelDefinition
            {
                Id = "c" + chapter + "_l" + index.ToString("00"),
                Chapter = chapter,
                Index = index,
                Seed = Hashing.Mix(rng.NextUInt() | ((ulong)rng.NextUInt() << 32)) & ((1UL << 40) - 1),
                MoveLimit = 200
            };
            var grid = new char[8, 8];
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) grid[x, y] = '.';

            int cracked = 0, mud = 0;
            if (index >= 6 || chapter > 1) cracked = 1 + (int)(t * 5) + rng.NextInt(2);
            if (chapter >= 2) mud = 1 + (int)(t * 4) + rng.NextInt(2);
            int blocks = 3 + (int)(t * 8);
            bool mirror = rng.NextInt(2) == 0;
            Scatter(grid, 'C', cracked, mirror, ref rng);
            Scatter(grid, 'm', mud, mirror, ref rng);
            for (int i = 0; i < blocks; i++) Scatter(grid, (char)('1' + rng.NextInt(6)), 1, false, ref rng);

            level.Rows = new string[8];
            for (int y = 0; y < 8; y++)
            {
                var row = new char[8];
                for (int x = 0; x < 8; x++) row[x] = grid[x, y];
                level.Rows[y] = new string(row);
            }

            int crackedCells = Count(level.Rows, 'C');
            int mudCells = Count(level.Rows, 'm');
            var pool = new List<ObjectiveDef>();
            int glaze = 1 + rng.NextInt(6);
            pool.Add(new ObjectiveDef { Type = ObjectiveType.CollectGlaze, Glaze = (byte)glaze, Target = 6 + (int)(t * 14) });
            pool.Add(new ObjectiveDef { Type = ObjectiveType.ReachScore, Target = 200 + (int)(t * 900) / 50 * 50 });
            pool.Add(new ObjectiveDef { Type = ObjectiveType.ClearLines, Target = 4 + (int)(t * 9) });
            if (crackedCells > 0) level.Objectives.Add(new ObjectiveDef { Type = ObjectiveType.BreakCracked, Target = crackedCells });
            if (mudCells > 0) level.Objectives.Add(new ObjectiveDef { Type = ObjectiveType.FreeMud, Target = mudCells });
            int extra = (chapter == 3 ? 2 : 1) - (level.Objectives.Count >= 2 ? 1 : 0);
            if (level.Objectives.Count == 0) extra = Math.Max(extra, 1);
            for (int i = 0; i < extra && pool.Count > 0; i++)
            {
                int k = rng.NextInt(pool.Count);
                level.Objectives.Add(pool[k]);
                pool.RemoveAt(k);
            }
            return level;
        }

        private static void Scatter(char[,] grid, char cell, int count, bool mirror, ref Pcg32 rng)
        {
            int placed = 0, guard = 0;
            while (placed < count && guard++ < 400)
            {
                int x = rng.NextInt(8), y = 2 + rng.NextInt(6);
                if (grid[x, y] != '.') continue;
                if (!SafeToFill(grid, x, y)) continue;
                grid[x, y] = cell;
                placed++;
                int mx = 7 - x;
                if (mirror && placed < count && grid[mx, y] == '.' && SafeToFill(grid, mx, y))
                {
                    grid[mx, y] = cell;
                    placed++;
                }
            }
        }

        /// <summary>Nunca deja una fila o columna llena en el tablero inicial (se limpiaría sola).</summary>
        private static bool SafeToFill(char[,] grid, int x, int y)
        {
            int row = 0, col = 0;
            for (int i = 0; i < 8; i++)
            {
                if (grid[i, y] != '.') row++;
                if (grid[x, i] != '.') col++;
            }
            return row < 6 && col < 6;
        }

        private static int Count(string[] rows, char c)
        {
            int n = 0;
            foreach (string r in rows) foreach (char ch in r) if (ch == c) n++;
            return n;
        }

        private static bool TryTune(LevelDefinition level, float t, ScoringConfig scoring, PieceCatalog catalog, int runs)
        {
            var open = LevelValidator.Validate(level, scoring, catalog, runs, 150);
            if (open.Solvability < LevelValidator.MinSolvability + 0.05f) return false;
            // Margen generoso al principio y más justo al final; el validador manda.
            float slack = 1.55f - 0.4f * t;
            int limit = Math.Max(8, (int)Math.Ceiling(open.MedianMovesUsed * slack));
            for (int i = 0; i < 10; i++)
            {
                level.MoveLimit = limit;
                var v = LevelValidator.Validate(level, scoring, catalog, runs);
                if (v.Solvability >= LevelValidator.MinSolvability + 0.1f)
                {
                    level.Solvability = v.Solvability;
                    level.TwoStarMovesLeft = Math.Max(1, LevelValidator.Percentile(v.MovesLeftOnSuccess, 0.4f));
                    level.ThreeStarMovesLeft = Math.Max(level.TwoStarMovesLeft + 1, LevelValidator.Percentile(v.MovesLeftOnSuccess, 0.75f));
                    return true;
                }
                limit = (int)Math.Ceiling(limit * 1.12f) + 1;
            }
            return false;
        }
    }
}
