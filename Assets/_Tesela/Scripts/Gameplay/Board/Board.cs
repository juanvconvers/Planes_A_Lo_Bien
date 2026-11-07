using System;
using System.Collections.Generic;

namespace Tesela.Gameplay
{
    /// <summary>
    /// Tablero rectangular (8×8 por defecto, 9×9 en eventos). Mantiene en paralelo un arreglo de esmaltes y
    /// una máscara de bits por fila: comprobar si una pieza cabe es un AND por fila, sin asignaciones.
    /// </summary>
    public sealed class Board
    {
        public const int MaxSize = 12;
        public const byte Empty = 0;

        public readonly int Width;
        public readonly int Height;
        public readonly bool ClearRegions3x3;

        private readonly byte[] _glaze;
        private readonly byte[] _mod;
        private readonly byte[] _hp;
        private readonly ushort[] _rowMask;
        private readonly ushort _fullRow;

        private readonly bool[] _rowFull;
        private readonly bool[] _colFull;
        private readonly bool[] _regionFull;
        private readonly bool[] _clearMark;

        public Board(int width = 8, int height = 8, bool clearRegions3x3 = false)
        {
            if (width < 3 || height < 3 || width > MaxSize || height > MaxSize) throw new ArgumentOutOfRangeException(nameof(width));
            if (clearRegions3x3 && (width % 3 != 0 || height % 3 != 0)) throw new ArgumentException("Regiones 3×3 requieren lados múltiplos de 3");
            Width = width;
            Height = height;
            ClearRegions3x3 = clearRegions3x3;
            _glaze = new byte[width * height];
            _mod = new byte[width * height];
            _hp = new byte[width * height];
            _rowMask = new ushort[height];
            _fullRow = (ushort)((1 << width) - 1);
            _rowFull = new bool[height];
            _colFull = new bool[width];
            _regionFull = new bool[(width / 3) * (height / 3)];
            _clearMark = new bool[width * height];
        }

        public int CellCount => Width * Height;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool IsOccupied(int x, int y) => (_rowMask[y] & (1 << x)) != 0;

        public byte GetGlaze(int x, int y) => _glaze[y * Width + x];

        public CellModifier GetModifier(int x, int y) => (CellModifier)_mod[y * Width + x];

        public int GetHp(int x, int y) => _hp[y * Width + x];

        public ushort RowMask(int y) => _rowMask[y];

        public int OccupiedCount
        {
            get
            {
                int n = 0;
                for (int y = 0; y < Height; y++) n += PopCount(_rowMask[y]);
                return n;
            }
        }

        public bool IsEmpty
        {
            get
            {
                for (int y = 0; y < Height; y++) if (_rowMask[y] != 0) return false;
                return true;
            }
        }

        public void SetCell(int x, int y, byte glaze, CellModifier modifier = CellModifier.None, int hp = 0)
        {
            int i = y * Width + x;
            _glaze[i] = glaze;
            if (glaze == Empty)
            {
                _mod[i] = 0;
                _hp[i] = 0;
                _rowMask[y] &= (ushort)~(1 << x);
            }
            else
            {
                _mod[i] = (byte)modifier;
                _hp[i] = (byte)hp;
                _rowMask[y] |= (ushort)(1 << x);
            }
        }

        public void SetHp(int x, int y, int hp) => _hp[y * Width + x] = (byte)hp;

        public void Clear()
        {
            Array.Clear(_glaze, 0, _glaze.Length);
            Array.Clear(_mod, 0, _mod.Length);
            Array.Clear(_hp, 0, _hp.Length);
            Array.Clear(_rowMask, 0, _rowMask.Length);
        }

        public bool CanPlace(PieceShape shape, int x, int y)
        {
            if (x < 0 || y < 0 || x + shape.Width > Width || y + shape.Height > Height) return false;
            for (int r = 0; r < shape.Height; r++)
            {
                if (((shape.RowMasks[r] << x) & _rowMask[y + r]) != 0) return false;
            }
            return true;
        }

        public bool FitsAnywhere(PieceShape shape)
        {
            int maxX = Width - shape.Width, maxY = Height - shape.Height;
            for (int y = 0; y <= maxY; y++)
                for (int x = 0; x <= maxX; x++)
                    if (CanPlace(shape, x, y)) return true;
            return false;
        }

        public int CountPlacements(PieceShape shape)
        {
            int n = 0;
            int maxX = Width - shape.Width, maxY = Height - shape.Height;
            for (int y = 0; y <= maxY; y++)
                for (int x = 0; x <= maxX; x++)
                    if (CanPlace(shape, x, y)) n++;
            return n;
        }

        public void Place(PieceShape shape, int x, int y, byte glaze, CellModifier modifier = CellModifier.None)
        {
            if (!CanPlace(shape, x, y)) throw new InvalidOperationException("La pieza no cabe en " + x + "," + y);
            for (int c = 0; c < shape.Cells.Length; c++)
            {
                var cell = shape.Cells[c];
                SetCell(x + cell.X, y + cell.Y, glaze, modifier);
            }
        }

        /// <summary>Cuántas filas y columnas se completarían al colocar la pieza (para resaltar antes de soltar y para los bots).</summary>
        public int CountLinesCompletedBy(PieceShape shape, int x, int y)
        {
            int lines = 0;
            for (int r = 0; r < shape.Height; r++)
            {
                if ((_rowMask[y + r] | (shape.RowMasks[r] << x)) == _fullRow) lines++;
            }
            for (int cx = 0; cx < shape.Width; cx++)
            {
                int col = x + cx;
                bool full = true;
                for (int row = 0; row < Height && full; row++)
                {
                    bool occ = (_rowMask[row] & (1 << col)) != 0;
                    if (!occ)
                    {
                        int sr = row - y;
                        occ = sr >= 0 && sr < shape.Height && (shape.RowMasks[sr] & (1 << cx)) != 0;
                    }
                    full = occ;
                }
                if (full) lines++;
            }
            return lines;
        }

        /// <summary>Marca en los arreglos dados qué filas/columnas quedarían llenas con la pieza colocada (resaltado en la UI).</summary>
        public void PreviewLines(PieceShape shape, int x, int y, bool[] rowsOut, bool[] colsOut)
        {
            for (int row = 0; row < Height; row++)
            {
                int sr = row - y;
                ushort m = _rowMask[row];
                if (sr >= 0 && sr < shape.Height) m |= (ushort)(shape.RowMasks[sr] << x);
                rowsOut[row] = m == _fullRow;
            }
            for (int col = 0; col < Width; col++)
            {
                bool full = true;
                for (int row = 0; row < Height && full; row++)
                {
                    int sr = row - y;
                    ushort m = _rowMask[row];
                    if (sr >= 0 && sr < shape.Height) m |= (ushort)(shape.RowMasks[sr] << x);
                    full = (m & (1 << col)) != 0;
                }
                colsOut[col] = full;
            }
        }

        /// <summary>Limpia filas, columnas (y regiones 3×3 si aplica). Escribe las celdas afectadas en <paramref name="cleared"/>.</summary>
        public LineClearInfo ClearCompletedLines(List<ClearedCell> cleared)
        {
            var info = new LineClearInfo();
            for (int y = 0; y < Height; y++)
            {
                _rowFull[y] = _rowMask[y] == _fullRow;
                if (_rowFull[y]) info.Rows++;
            }
            for (int x = 0; x < Width; x++)
            {
                bool full = true;
                ushort bit = (ushort)(1 << x);
                for (int y = 0; y < Height && full; y++) full = (_rowMask[y] & bit) != 0;
                _colFull[x] = full;
                if (full) info.Columns++;
            }
            if (ClearRegions3x3)
            {
                int rw = Width / 3;
                for (int ry = 0; ry < Height / 3; ry++)
                    for (int rx = 0; rx < rw; rx++)
                    {
                        bool full = true;
                        for (int y = ry * 3; y < ry * 3 + 3 && full; y++)
                            for (int x = rx * 3; x < rx * 3 + 3 && full; x++)
                                full = IsOccupied(x, y);
                        _regionFull[ry * rw + rx] = full;
                        if (full) info.Regions++;
                    }
            }
            if (info.Lines == 0) return info;

            Array.Clear(_clearMark, 0, _clearMark.Length);
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    bool mark = _rowFull[y] || _colFull[x];
                    if (!mark && ClearRegions3x3) mark = _regionFull[(y / 3) * (Width / 3) + (x / 3)];
                    _clearMark[y * Width + x] = mark;
                }

            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    if (!_clearMark[i]) continue;
                    var cell = new ClearedCell { X = x, Y = y, Glaze = _glaze[i], Modifier = (CellModifier)_mod[i] };
                    if (cell.Modifier == CellModifier.Cracked && _hp[i] > 1)
                    {
                        _hp[i]--;
                        cell.Survived = true;
                        info.CrackedDamaged++;
                    }
                    else
                    {
                        if (cell.Modifier == CellModifier.Cracked) info.CrackedBroken++;
                        else if (cell.Modifier == CellModifier.Mud) info.MudFreed++;
                        else if (cell.Modifier == CellModifier.Gold) info.GoldCleared++;
                        else if (cell.Modifier == CellModifier.Bomb) info.BombsDefused++;
                        SetCell(x, y, Empty);
                        info.CellsRemoved++;
                    }
                    cleared?.Add(cell);
                }
            return info;
        }

        /// <summary>Vacía una fila completa sin puntuar (Escoba, Revivir, barrido Zen).</summary>
        public int ClearRow(int y, List<ClearedCell> cleared)
        {
            int n = 0;
            for (int x = 0; x < Width; x++)
            {
                if (!IsOccupied(x, y)) continue;
                cleared?.Add(new ClearedCell { X = x, Y = y, Glaze = GetGlaze(x, y), Modifier = GetModifier(x, y) });
                SetCell(x, y, Empty);
                n++;
            }
            return n;
        }

        public int ClearColumn(int x, List<ClearedCell> cleared)
        {
            int n = 0;
            for (int y = 0; y < Height; y++)
            {
                if (!IsOccupied(x, y)) continue;
                cleared?.Add(new ClearedCell { X = x, Y = y, Glaze = GetGlaze(x, y), Modifier = GetModifier(x, y) });
                SetCell(x, y, Empty);
                n++;
            }
            return n;
        }

        public int RowFillCount(int y) => PopCount(_rowMask[y]);

        /// <summary>Filas ordenadas de más llena a menos (desempate: la de más abajo). Para Revivir y Zen.</summary>
        public int FullestRow(int excludeRow = -1)
        {
            int best = -1, bestCount = -1;
            for (int y = Height - 1; y >= 0; y--)
            {
                if (y == excludeRow) continue;
                int c = RowFillCount(y);
                if (c > bestCount) { best = y; bestCount = c; }
            }
            return best;
        }

        public Board Clone()
        {
            var b = new Board(Width, Height, ClearRegions3x3);
            CopyTo(b);
            return b;
        }

        public void CopyTo(Board other)
        {
            if (other.Width != Width || other.Height != Height) throw new ArgumentException("Tamaños distintos");
            Array.Copy(_glaze, other._glaze, _glaze.Length);
            Array.Copy(_mod, other._mod, _mod.Length);
            Array.Copy(_hp, other._hp, _hp.Length);
            Array.Copy(_rowMask, other._rowMask, _rowMask.Length);
        }

        /// <summary>Huecos aislados (celdas vacías rodeadas de ocupadas o bordes). Heurística de los bots y del validador.</summary>
        public int CountIsolatedHoles()
        {
            int holes = 0;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    if (IsOccupied(x, y)) continue;
                    bool l = x == 0 || IsOccupied(x - 1, y);
                    bool r = x == Width - 1 || IsOccupied(x + 1, y);
                    bool u = y == 0 || IsOccupied(x, y - 1);
                    bool d = y == Height - 1 || IsOccupied(x, y + 1);
                    if (l && r && u && d) holes++;
                }
            return holes;
        }

        public static int PopCount(int v)
        {
            v = v - ((v >> 1) & 0x5555);
            v = (v & 0x3333) + ((v >> 2) & 0x3333);
            v = (v + (v >> 4)) & 0x0F0F;
            return (v + (v >> 8)) & 0x1F;
        }

        public string[] ToRows()
        {
            var rows = new string[Height];
            var chars = new char[Width];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    chars[x] = EncodeCell(_glaze[i], (CellModifier)_mod[i], _hp[i]);
                }
                rows[y] = new string(chars);
            }
            return rows;
        }

        /// <summary>
        /// Formato de texto de los niveles y del guardado: '.' vacío, '1'..'6' esmalte,
        /// 'c'/'C' agrietada con 1/2 vidas, 'm' barro, 'g' dorado, 'a'..'i' bomba con cuenta 1..9.
        /// </summary>
        public void LoadRows(string[] rows, Func<int, int, byte> glazeForSpecial = null)
        {
            if (rows == null || rows.Length != Height) throw new FormatException("Filas esperadas: " + Height);
            Clear();
            for (int y = 0; y < Height; y++)
            {
                if (rows[y].Length != Width) throw new FormatException("Fila " + y + " debe tener " + Width + " columnas");
                for (int x = 0; x < Width; x++)
                {
                    char ch = rows[y][x];
                    if (ch == '.') continue;
                    byte specialGlaze = glazeForSpecial?.Invoke(x, y) ?? (byte)(1 + ((x * 7 + y * 3) % 6));
                    if (ch >= '1' && ch <= '6') SetCell(x, y, (byte)(ch - '0'));
                    else if (ch == 'C') SetCell(x, y, specialGlaze, CellModifier.Cracked, 2);
                    else if (ch == 'c') SetCell(x, y, specialGlaze, CellModifier.Cracked, 1);
                    else if (ch == 'm') SetCell(x, y, specialGlaze, CellModifier.Mud);
                    else if (ch == 'g') SetCell(x, y, specialGlaze, CellModifier.Gold);
                    else if (ch >= 'a' && ch <= 'i') SetCell(x, y, specialGlaze, CellModifier.Bomb, ch - 'a' + 1);
                    else throw new FormatException("Celda desconocida '" + ch + "'");
                }
            }
        }

        private static char EncodeCell(byte glaze, CellModifier mod, byte hp)
        {
            if (glaze == Empty) return '.';
            switch (mod)
            {
                case CellModifier.Cracked: return hp >= 2 ? 'C' : 'c';
                case CellModifier.Mud: return 'm';
                case CellModifier.Gold: return 'g';
                case CellModifier.Bomb: return (char)('a' + Math.Max(0, Math.Min(8, hp - 1)));
                default: return (char)('0' + glaze);
            }
        }

        /// <summary>Esmaltes de las celdas especiales (no se pueden deducir del formato de filas).</summary>
        public byte[] GlazeSnapshot() => (byte[])_glaze.Clone();

        public void RestoreGlazes(byte[] glazes)
        {
            if (glazes == null || glazes.Length != _glaze.Length) return;
            for (int i = 0; i < glazes.Length; i++)
                if (_glaze[i] != Empty && glazes[i] != Empty) _glaze[i] = glazes[i];
        }
    }

    public struct LineClearInfo
    {
        public int Rows;
        public int Columns;
        public int Regions;
        public int CellsRemoved;
        public int CrackedDamaged;
        public int CrackedBroken;
        public int MudFreed;
        public int GoldCleared;
        public int BombsDefused;

        public int Lines => Rows + Columns + Regions;
    }
}
