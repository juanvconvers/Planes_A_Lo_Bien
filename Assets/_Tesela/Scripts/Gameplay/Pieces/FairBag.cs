using System;
using System.Collections.Generic;
using Tesela.Core;

namespace Tesela.Gameplay
{
    /// <summary>
    /// Generador justo de bandejas. Solo recibe: catálogo público, tablero, puntuación de la partida y su RNG con
    /// semilla. No existe ningún camino para pasarle datos de anuncios, gasto, país, dispositivo o valor del jugador
    /// (y Tesela.Gameplay no referencia Tesela.Monetization; lo verifica un test).
    /// </summary>
    public sealed class FairBag
    {
        public const int TraySize = 3;
        public const int MaxSameFamilyInARow = 2;

        private readonly PieceCatalog _catalog;
        private readonly float[] _weights;
        private readonly string[] _recentFamilies = new string[MaxSameFamilyInARow];
        private int _recentCount;
        private Board _scratch;
        private readonly List<ClearedCell> _scratchCleared = new List<ClearedCell>(32);

        public Pcg32 Rng;

        public FairBag(PieceCatalog catalog, ulong seed)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _weights = new float[catalog.Shapes.Count];
            Rng = new Pcg32(seed);
        }

        public PieceCatalog Catalog => _catalog;

        /// <summary>Historial de familias recientes (para guardado/restauración exacta).</summary>
        public string[] RecentFamilies
        {
            get
            {
                var r = new string[_recentCount];
                Array.Copy(_recentFamilies, r, _recentCount);
                return r;
            }
            set
            {
                _recentCount = 0;
                if (value == null) return;
                int start = Math.Max(0, value.Length - MaxSameFamilyInARow);
                for (int i = start; i < value.Length; i++) _recentFamilies[_recentCount++] = value[i];
            }
        }

        /// <summary>Copia sin asignar memoria (para deshacer). Las posiciones sin dato quedan en null.</summary>
        public void CopyRecentFamilies(string[] dst)
        {
            for (int i = 0; i < dst.Length; i++) dst[i] = i < _recentCount ? _recentFamilies[i] : null;
        }

        public void RestoreRecentFamilies(string[] src)
        {
            _recentCount = 0;
            for (int i = 0; i < src.Length && i < MaxSameFamilyInARow; i++)
                if (src[i] != null) _recentFamilies[_recentCount++] = src[i];
        }

        /// <summary>Llena <paramref name="tray"/> con una bandeja nueva para el tablero actual.</summary>
        public void NextTray(Board board, int score, bool relaxed, float goldenChance, TrayPiece[] tray)
        {
            if (tray == null || tray.Length != TraySize) throw new ArgumentException("La bandeja debe tener 3 casillas");
            if (relaxed)
            {
                FillRelaxed(board, score, goldenChance, tray);
                return;
            }

            // Se elige en orden; si las dos primeras no caben, la tercera se elige entre las que caben.
            // Así la secuencia entregada respeta la regla de variedad y siempre llega al menos una pieza jugable.
            bool anyFits = false;
            for (int i = 0; i < TraySize; i++)
            {
                bool last = i == TraySize - 1;
                PieceShape shape = PickShape(score, last && !anyFits ? board : null);
                tray[i] = MakePiece(shape, goldenChance);
                if (!anyFits) anyFits = board.FitsAnywhere(shape);
            }
        }

        private void FillRelaxed(Board board, int score, float goldenChance, TrayPiece[] tray)
        {
            if (_scratch == null || _scratch.Width != board.Width || _scratch.Height != board.Height || _scratch.ClearRegions3x3 != board.ClearRegions3x3)
                _scratch = new Board(board.Width, board.Height, board.ClearRegions3x3);
            board.CopyTo(_scratch);
            for (int i = 0; i < TraySize; i++)
            {
                PieceShape shape = PickShape(score, _scratch, board);
                tray[i] = MakePiece(shape, goldenChance);
                if (Bots.GreedyPlacement.TryFindBest(_scratch, shape, out int bx, out int by))
                {
                    _scratch.Place(shape, bx, by, 1);
                    _scratchCleared.Clear();
                    _scratch.ClearCompletedLines(_scratchCleared);
                }
            }
        }

        private TrayPiece MakePiece(PieceShape shape, float goldenChance)
        {
            byte glaze = (byte)(1 + Rng.NextInt(Palette.GlazeCount));
            bool golden = goldenChance > 0f && Rng.NextDouble() < goldenChance;
            return new TrayPiece(shape, glaze, golden);
        }

        /// <summary>Elige forma por pesos. Con <paramref name="mustFit"/> (y <paramref name="alsoMustFit"/>) solo considera formas que caben en esos tableros.</summary>
        private PieceShape PickShape(int score, Board mustFit, Board alsoMustFit = null)
        {
            var shapes = _catalog.Shapes;
            string blocked = _recentCount == MaxSameFamilyInARow && _recentFamilies[0] == _recentFamilies[1] ? _recentFamilies[0] : null;
            float total = 0f;
            for (int i = 0; i < shapes.Count; i++)
            {
                var s = shapes[i];
                float w = 0f;
                if ((blocked == null || s.Family != blocked) && (mustFit == null || mustFit.FitsAnywhere(s)) && (alsoMustFit == null || alsoMustFit.FitsAnywhere(s)))
                    w = _catalog.EffectiveWeight(s, score);
                _weights[i] = w;
                total += w;
            }

            PieceShape chosen;
            if (total <= 0f)
            {
                // Sin candidatas (tablero casi lleno y la única que cabe está bloqueada por la regla de variedad):
                // la garantía "siempre cabe al menos una" pesa más que la variedad.
                chosen = _catalog.Mono;
            }
            else
            {
                double r = Rng.NextDouble() * total;
                chosen = null;
                for (int i = 0; i < shapes.Count && chosen == null; i++)
                {
                    if (_weights[i] <= 0f) continue;
                    r -= _weights[i];
                    if (r < 0) chosen = shapes[i];
                }
                chosen ??= LastPositive(shapes);
            }
            Remember(chosen.Family);
            return chosen;
        }

        private PieceShape LastPositive(IReadOnlyList<PieceShape> shapes)
        {
            for (int i = shapes.Count - 1; i >= 0; i--) if (_weights[i] > 0f) return shapes[i];
            return _catalog.Mono;
        }

        private void Remember(string family)
        {
            if (_recentCount < MaxSameFamilyInARow)
            {
                _recentFamilies[_recentCount++] = family;
                return;
            }
            for (int i = 1; i < MaxSameFamilyInARow; i++) _recentFamilies[i - 1] = _recentFamilies[i];
            _recentFamilies[MaxSameFamilyInARow - 1] = family;
        }
    }
}
