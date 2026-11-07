using System;
using System.Collections.Generic;
using System.Text;

namespace Tesela.Gameplay
{
    public readonly struct CellOffset
    {
        public readonly sbyte X;
        public readonly sbyte Y;
        public CellOffset(int x, int y) { X = (sbyte)x; Y = (sbyte)y; }
    }

    /// <summary>Forma inmutable de un poliominó normalizado a su esquina superior izquierda.</summary>
    public sealed class PieceShape
    {
        public readonly string Id;
        public readonly string Family;
        public readonly int Width;
        public readonly int Height;
        public readonly CellOffset[] Cells;
        public readonly ushort[] RowMasks;
        public readonly float Weight;

        public int CellCount => Cells.Length;

        public PieceShape(string id, string family, IReadOnlyList<CellOffset> cells, float weight)
        {
            if (cells == null || cells.Count == 0) throw new ArgumentException("Pieza sin celdas: " + id);
            Id = id;
            Family = string.IsNullOrEmpty(family) ? id : family;
            Weight = weight;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var c in cells)
            {
                minX = Math.Min(minX, c.X); minY = Math.Min(minY, c.Y);
                maxX = Math.Max(maxX, c.X); maxY = Math.Max(maxY, c.Y);
            }
            Width = maxX - minX + 1;
            Height = maxY - minY + 1;
            var sorted = new List<CellOffset>(cells.Count);
            foreach (var c in cells) sorted.Add(new CellOffset(c.X - minX, c.Y - minY));
            sorted.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
            for (int i = 1; i < sorted.Count; i++)
                if (sorted[i].X == sorted[i - 1].X && sorted[i].Y == sorted[i - 1].Y) throw new ArgumentException("Celda repetida en " + id);
            Cells = sorted.ToArray();
            RowMasks = new ushort[Height];
            foreach (var c in Cells) RowMasks[c.Y] |= (ushort)(1 << c.X);
        }

        /// <summary>Crea la forma desde filas de texto ("X." / "XX"); 'X' o '#' marcan celda.</summary>
        public static PieceShape FromRows(string id, string family, string[] rows, float weight = 1f)
        {
            var cells = new List<CellOffset>();
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (rows[y][x] == 'X' || rows[y][x] == '#') cells.Add(new CellOffset(x, y));
            return new PieceShape(id, family, cells, weight);
        }

        /// <summary>Rotación 90° en sentido horario. El id indica la rotación para poder guardarla y restaurarla.</summary>
        public PieceShape RotatedClockwise()
        {
            var cells = new List<CellOffset>(Cells.Length);
            foreach (var c in Cells) cells.Add(new CellOffset(Height - 1 - c.Y, c.X));
            return new PieceShape(NextRotationId(Id), Family, cells, Weight);
        }

        public static string BaseId(string id)
        {
            int at = id.IndexOf('@');
            return at < 0 ? id : id.Substring(0, at);
        }

        public static int RotationOf(string id)
        {
            int at = id.IndexOf('@');
            return at < 0 ? 0 : id[at + 1] - '0';
        }

        private static string NextRotationId(string id)
        {
            int r = (RotationOf(id) + 1) % 4;
            return r == 0 ? BaseId(id) : BaseId(id) + "@" + r;
        }

        public bool SameCellsAs(PieceShape other)
        {
            if (other.Width != Width || other.Height != Height || other.Cells.Length != Cells.Length) return false;
            for (int r = 0; r < Height; r++) if (RowMasks[r] != other.RowMasks[r]) return false;
            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Id).Append(' ');
            for (int r = 0; r < Height; r++)
            {
                for (int x = 0; x < Width; x++) sb.Append((RowMasks[r] & (1 << x)) != 0 ? 'X' : '.');
                if (r < Height - 1) sb.Append('/');
            }
            return sb.ToString();
        }
    }
}
