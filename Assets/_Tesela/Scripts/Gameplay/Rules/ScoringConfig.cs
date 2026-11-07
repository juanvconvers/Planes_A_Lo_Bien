using System;
using Tesela.Core;

namespace Tesela.Gameplay
{
    public sealed class ScoringConfig
    {
        public int PointsPerCell = 1;
        public int[] LineBonus = { 0, 10, 30, 60, 100 };
        public int ExtraLineBonus = 50;
        public float[] StreakMultipliers = { 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f };
        public int StreakBreakAfterMoves = 3;
        public int EmptyBoardBonus = 300;
        public int GoldCellBonus = 25;

        public static ScoringConfig FromJson(string json)
        {
            var n = Json.ParseNode(json);
            var c = new ScoringConfig
            {
                PointsPerCell = n["pointsPerCell"].AsInt(1),
                ExtraLineBonus = n["extraLineBonus"].AsInt(50),
                StreakBreakAfterMoves = n["streakBreakAfterMoves"].AsInt(3),
                EmptyBoardBonus = n["emptyBoardBonus"].AsInt(300),
                GoldCellBonus = n["goldCellBonus"].AsInt(25)
            };
            int[] lb = n["lineBonus"].AsIntArray();
            if (lb.Length > 1) c.LineBonus = lb;
            float[] sm = n["streakMultipliers"].AsFloatArray();
            if (sm.Length > 0) c.StreakMultipliers = sm;
            if (c.StreakBreakAfterMoves < 1) throw new FormatException("streakBreakAfterMoves debe ser ≥ 1");
            return c;
        }

        /// <summary>Bono por líneas simultáneas: tabla hasta 4 y +extra por cada línea adicional.</summary>
        public int LineBonusFor(int lines)
        {
            if (lines <= 0) return 0;
            int last = LineBonus.Length - 1;
            if (lines <= last) return LineBonus[lines];
            return LineBonus[last] + (lines - last) * ExtraLineBonus;
        }

        /// <summary>Multiplicador para una racha de N jugadas que limpian (N ≥ 1).</summary>
        public float MultiplierFor(int streak)
        {
            if (streak <= 0) return 1f;
            int idx = Math.Min(streak - 1, StreakMultipliers.Length - 1);
            return StreakMultipliers[idx];
        }
    }

    /// <summary>Estado de la racha: sube con cada jugada que limpia, se rompe tras N jugadas seguidas sin limpiar.</summary>
    public struct StreakState
    {
        public int Streak;
        public int MovesWithoutClear;

        public void Register(bool cleared, int breakAfter)
        {
            if (cleared)
            {
                Streak++;
                MovesWithoutClear = 0;
                return;
            }
            MovesWithoutClear++;
            if (MovesWithoutClear >= breakAfter)
            {
                Streak = 0;
                MovesWithoutClear = 0;
            }
        }
    }

    public static class Scoring
    {
        /// <summary>Puntos de una jugada. La racha ya incluye esta jugada (Register se llama antes).</summary>
        public static int ScoreMove(ScoringConfig cfg, int cellsPlaced, LineClearInfo clear, int streakAfter, bool boardEmptyAfter)
        {
            int points = cellsPlaced * cfg.PointsPerCell;
            int lines = clear.Lines;
            if (lines > 0)
            {
                points += (int)Math.Round(cfg.LineBonusFor(lines) * cfg.MultiplierFor(streakAfter), MidpointRounding.AwayFromZero);
                points += clear.GoldCleared * cfg.GoldCellBonus;
                if (boardEmptyAfter) points += cfg.EmptyBoardBonus;
            }
            return points;
        }
    }
}
