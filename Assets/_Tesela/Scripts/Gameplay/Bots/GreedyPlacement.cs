using Tesela.Core;

namespace Tesela.Gameplay.Bots
{
    /// <summary>Heurística barata (sin copiar el tablero): líneas completadas, contacto con bordes/piezas y penalización por huecos creados.</summary>
    public static class GreedyPlacement
    {
        public static int Evaluate(Board board, PieceShape shape, int x, int y)
        {
            int lines = board.CountLinesCompletedBy(shape, x, y);
            int contacts = 0;
            int holes = 0;
            for (int i = 0; i < shape.Cells.Length; i++)
            {
                int cx = x + shape.Cells[i].X, cy = y + shape.Cells[i].Y;
                contacts += Solid(board, shape, x, y, cx - 1, cy) + Solid(board, shape, x, y, cx + 1, cy)
                          + Solid(board, shape, x, y, cx, cy - 1) + Solid(board, shape, x, y, cx, cy + 1);
                holes += CreatesHole(board, shape, x, y, cx - 1, cy) + CreatesHole(board, shape, x, y, cx + 1, cy)
                       + CreatesHole(board, shape, x, y, cx, cy - 1) + CreatesHole(board, shape, x, y, cx, cy + 1);
            }
            return lines * 40 + contacts * 2 - holes * 9 + shape.CellCount;
        }

        private static bool InShape(PieceShape s, int ox, int oy, int x, int y)
        {
            int sx = x - ox, sy = y - oy;
            return sx >= 0 && sy >= 0 && sx < s.Width && sy < s.Height && (s.RowMasks[sy] & (1 << sx)) != 0;
        }

        private static int Solid(Board b, PieceShape s, int ox, int oy, int x, int y)
        {
            if (!b.InBounds(x, y)) return 1;
            if (InShape(s, ox, oy, x, y)) return 0;
            return b.IsOccupied(x, y) ? 1 : 0;
        }

        private static bool SolidAfter(Board b, PieceShape s, int ox, int oy, int x, int y)
        {
            if (!b.InBounds(x, y)) return true;
            return b.IsOccupied(x, y) || InShape(s, ox, oy, x, y);
        }

        /// <summary>1 si la celda vecina queda vacía y completamente encerrada tras colocar la pieza.</summary>
        private static int CreatesHole(Board b, PieceShape s, int ox, int oy, int x, int y)
        {
            if (!b.InBounds(x, y) || b.IsOccupied(x, y) || InShape(s, ox, oy, x, y)) return 0;
            bool enclosed = SolidAfter(b, s, ox, oy, x - 1, y) && SolidAfter(b, s, ox, oy, x + 1, y)
                         && SolidAfter(b, s, ox, oy, x, y - 1) && SolidAfter(b, s, ox, oy, x, y + 1);
            return enclosed ? 1 : 0;
        }

        public static bool TryFindBest(Board board, PieceShape shape, out int bestX, out int bestY)
        {
            bestX = bestY = -1;
            int best = int.MinValue;
            for (int y = 0; y <= board.Height - shape.Height; y++)
                for (int x = 0; x <= board.Width - shape.Width; x++)
                {
                    if (!board.CanPlace(shape, x, y)) continue;
                    int v = Evaluate(board, shape, x, y);
                    if (v > best) { best = v; bestX = x; bestY = y; }
                }
            return bestX >= 0;
        }
    }

    public interface IBot
    {
        /// <summary>Elige jugada; false si no hay ninguna.</summary>
        bool Choose(GameSession session, out int slot, out int x, out int y);
    }

    public sealed class RandomBot : IBot
    {
        private Pcg32 _rng;
        public RandomBot(ulong seed) { _rng = new Pcg32(seed, 0xB07UL); }

        public bool Choose(GameSession s, out int slot, out int x, out int y)
        {
            slot = x = y = -1;
            int total = 0;
            for (int i = 0; i < s.Tray.Length; i++)
                if (!s.Tray[i].IsEmpty) total += s.Board.CountPlacements(s.Tray[i].Shape);
            if (total == 0) return false;
            int pick = _rng.NextInt(total);
            for (int i = 0; i < s.Tray.Length; i++)
            {
                if (s.Tray[i].IsEmpty) continue;
                var shape = s.Tray[i].Shape;
                for (int py = 0; py <= s.Board.Height - shape.Height; py++)
                    for (int px = 0; px <= s.Board.Width - shape.Width; px++)
                    {
                        if (!s.Board.CanPlace(shape, px, py)) continue;
                        if (pick-- == 0) { slot = i; x = px; y = py; return true; }
                    }
            }
            return false;
        }
    }

    /// <summary>Bot codicioso. Con <c>noise</c> &gt; 0 elige al azar entre jugadas casi tan buenas como la mejor (Monte Carlo del validador).</summary>
    public sealed class GreedyBot : IBot
    {
        private Pcg32 _rng;
        private readonly int _noise;

        public GreedyBot(ulong seed = 1, int noise = 0)
        {
            _rng = new Pcg32(seed, 0x6EEDUL);
            _noise = noise;
        }

        public bool Choose(GameSession s, out int slot, out int x, out int y)
        {
            slot = x = y = -1;
            int best = int.MinValue;
            int ties = 0;
            for (int i = 0; i < s.Tray.Length; i++)
            {
                if (s.Tray[i].IsEmpty) continue;
                var shape = s.Tray[i].Shape;
                for (int py = 0; py <= s.Board.Height - shape.Height; py++)
                    for (int px = 0; px <= s.Board.Width - shape.Width; px++)
                    {
                        if (!s.Board.CanPlace(shape, px, py)) continue;
                        int v = GreedyPlacement.Evaluate(s.Board, shape, px, py);
                        if (_noise > 0) v += _rng.NextInt(_noise + 1);
                        if (v > best)
                        {
                            best = v; slot = i; x = px; y = py; ties = 1;
                        }
                        else if (v == best && _rng.NextInt(++ties) == 0)
                        {
                            slot = i; x = px; y = py;
                        }
                    }
            }
            return slot >= 0;
        }
    }
}
