using System;
using System.Collections.Generic;
using Tesela.Core;

namespace Tesela.Gameplay
{
    public enum GameOverReason
    {
        None = 0,
        NoFit = 1,
        OutOfMoves = 2,
        Bomb = 3,
        LevelComplete = 4
    }

    /// <summary>Resultado de la última acción. La sesión reutiliza la instancia y sus listas: cero asignaciones por jugada.</summary>
    public sealed class MoveResult
    {
        public int Slot;
        public int X;
        public int Y;
        public PieceShape Shape;
        public byte Glaze;
        public int CellsPlaced;
        public LineClearInfo Clear;
        public readonly List<ClearedCell> ClearedCells = new List<ClearedCell>(64);
        public int Points;
        public int Streak;
        public float Multiplier;
        public bool BoardEmptied;
        public bool TrayRefilled;
        public int ZenSweeps;
        public bool BombSpawned;
        public int BombX;
        public int BombY;
        public bool GameOver;
        public GameOverReason Reason;

        internal void Reset()
        {
            Slot = -1; X = Y = 0; Shape = null; Glaze = 0; CellsPlaced = 0; Clear = default;
            ClearedCells.Clear(); Points = 0; Streak = 0; Multiplier = 1f; BoardEmptied = false;
            TrayRefilled = false; ZenSweeps = 0; BombSpawned = false; BombX = BombY = -1; GameOver = false; Reason = GameOverReason.None;
        }
    }

    /// <summary>
    /// Una partida de cualquier modo. Lógica pura: la UI llama a Place/potenciadores y lee el resultado.
    /// El coste de los potenciadores (cargas, monedas) se cobra fuera; aquí solo se aplican las reglas.
    /// </summary>
    public sealed class GameSession
    {
        public const int UndoDepth = 3;

        public readonly ModeRules Rules;
        public readonly ScoringConfig Scoring;
        public readonly PieceCatalog Catalog;
        public readonly ulong Seed;
        public readonly LevelDefinition Level;

        public Board Board { get; }
        public FairBag Bag { get; }
        public ObjectiveTracker Objectives { get; }
        public TrayPiece[] Tray { get; } = new TrayPiece[FairBag.TraySize];
        public TrayPiece Reserve { get; private set; }
        public bool ReserveUnlocked { get; private set; }

        public int Score { get; private set; }
        public StreakState Streak;
        public int Moves { get; private set; }
        public int LinesCleared { get; private set; }
        public int BestStreak { get; private set; }
        public bool IsOver { get; private set; }
        public GameOverReason OverReason { get; private set; }
        public bool Revived { get; private set; }
        public int BoostersUsed { get; private set; }

        public readonly MoveResult LastResult = new MoveResult();

        private Pcg32 _eventRng;
        private readonly Snapshot[] _undo = new Snapshot[UndoDepth];
        private int _undoCount;
        private int _undoHead;
        private readonly bool[] _previewRows = new bool[Board.MaxSize];
        private readonly bool[] _previewCols = new bool[Board.MaxSize];

        public GameSession(ModeRules rules, ScoringConfig scoring, PieceCatalog catalog, ulong seed, LevelDefinition level = null)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Scoring = scoring ?? throw new ArgumentNullException(nameof(scoring));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Seed = seed;
            Level = level;
            int w = level?.Width ?? rules.BoardWidth;
            int h = level?.Height ?? rules.BoardHeight;
            Board = new Board(w, h, rules.ClearRegions3x3);
            Bag = new FairBag(catalog, seed);
            _eventRng = new Pcg32(Hashing.Mix(seed ^ 0x7E5E1AUL), 0x51ED27UL);
            if (level != null)
            {
                Board.LoadRows(level.Rows);
                Objectives = new ObjectiveTracker(level.Objectives);
            }
            for (int i = 0; i < UndoDepth; i++) _undo[i] = new Snapshot(Board.Width, Board.Height, Board.ClearRegions3x3, level?.Objectives.Count ?? 0);
            Bag.NextTray(Board, Score, Rules.Relaxed, Rules.GoldenPieceChance, Tray);
            EvaluateGameOver(LastResult);
        }

        public bool IsLevel => Level != null;
        public int MovesLeft => Level == null ? int.MaxValue : Math.Max(0, Level.MoveLimit - Moves);
        public bool CanUndo => _undoCount > 0;
        public string SeedCode => Hashing.SeedToCode(Seed);

        public bool CanPlace(int slot, int x, int y)
        {
            if (IsOver || slot < 0 || slot >= Tray.Length || Tray[slot].IsEmpty) return false;
            return Board.CanPlace(Tray[slot].Shape, x, y);
        }

        public bool SlotFits(int slot) => !Tray[slot].IsEmpty && Board.FitsAnywhere(Tray[slot].Shape);

        /// <summary>Resalta las líneas que se completarían. Devuelve arreglos internos (no guardar la referencia).</summary>
        public void PreviewLines(int slot, int x, int y, out bool[] rows, out bool[] cols)
        {
            rows = _previewRows;
            cols = _previewCols;
            Array.Clear(_previewRows, 0, _previewRows.Length);
            Array.Clear(_previewCols, 0, _previewCols.Length);
            if (!CanPlace(slot, x, y)) return;
            Board.PreviewLines(Tray[slot].Shape, x, y, _previewRows, _previewCols);
        }

        public MoveResult Place(int slot, int x, int y)
        {
            if (!CanPlace(slot, x, y)) throw new InvalidOperationException("Jugada inválida: casilla " + slot + " en " + x + "," + y);
            var r = LastResult;
            r.Reset();
            PushUndo();

            TrayPiece piece = Tray[slot];
            r.Slot = slot; r.X = x; r.Y = y; r.Shape = piece.Shape; r.Glaze = piece.Glaze; r.CellsPlaced = piece.Shape.CellCount;
            Board.Place(piece.Shape, x, y, piece.Glaze, piece.Golden ? CellModifier.Gold : CellModifier.None);
            Tray[slot] = TrayPiece.None;

            r.Clear = Board.ClearCompletedLines(r.ClearedCells);
            Streak.Register(r.Clear.Lines > 0, Scoring.StreakBreakAfterMoves);
            BestStreak = Math.Max(BestStreak, Streak.Streak);
            r.Streak = Streak.Streak;
            r.Multiplier = r.Clear.Lines > 0 ? Scoring.MultiplierFor(Streak.Streak) : 1f;
            r.BoardEmptied = r.Clear.Lines > 0 && Board.IsEmpty;
            r.Points = Gameplay.Scoring.ScoreMove(Scoring, r.CellsPlaced, r.Clear, Streak.Streak, r.BoardEmptied);
            Score += r.Points;
            LinesCleared += r.Clear.Lines;
            Moves++;
            Objectives?.OnMove(r.Clear, r.ClearedCells, Score);

            TickBombs();
            if (!IsOver) MaybeSpawnBomb(r);

            if (TrayIsEmpty())
            {
                Bag.NextTray(Board, Score, Rules.Relaxed, Rules.GoldenPieceChance, Tray);
                r.TrayRefilled = true;
            }

            if (!IsOver) EvaluateGameOver(r);
            r.GameOver = IsOver;
            r.Reason = OverReason;
            return r;
        }

        private bool TrayIsEmpty()
        {
            for (int i = 0; i < Tray.Length; i++) if (!Tray[i].IsEmpty) return false;
            return true;
        }

        /// <summary>¿Queda alguna jugada? Considera la reserva desbloqueada y, con giro libre, todas las rotaciones.</summary>
        public bool AnyMoveAvailable()
        {
            for (int i = 0; i < Tray.Length; i++)
                if (!Tray[i].IsEmpty && ShapeFits(Tray[i].Shape)) return true;
            if (ReserveUnlocked && !Reserve.IsEmpty && ShapeFits(Reserve.Shape)) return true;
            return false;
        }

        private bool ShapeFits(PieceShape shape)
        {
            if (Board.FitsAnywhere(shape)) return true;
            if (!Rules.FreeRotation) return false;
            PieceShape s = shape;
            for (int i = 0; i < 3; i++)
            {
                s = Catalog.Get(NextRotation(s.Id));
                if (Board.FitsAnywhere(s)) return true;
            }
            return false;
        }

        private static string NextRotation(string id)
        {
            int r = (PieceShape.RotationOf(id) + 1) % 4;
            string b = PieceShape.BaseId(id);
            return r == 0 ? b : b + "@" + r;
        }

        private void EvaluateGameOver(MoveResult r)
        {
            if (Level != null && Objectives.IsComplete)
            {
                End(GameOverReason.LevelComplete);
                return;
            }
            if (Level != null && Moves >= Level.MoveLimit)
            {
                End(GameOverReason.OutOfMoves);
                return;
            }
            if (AnyMoveAvailable()) return;
            if (Rules.ZenSweep)
            {
                int guard = Board.Height;
                while (!AnyMoveAvailable() && guard-- > 0)
                {
                    int row = Board.FullestRow();
                    Board.ClearRow(row, r.ClearedCells);
                    r.ZenSweeps++;
                }
                if (AnyMoveAvailable()) return;
            }
            End(GameOverReason.NoFit);
        }

        private void End(GameOverReason reason)
        {
            IsOver = true;
            OverReason = reason;
        }

        private void TickBombs()
        {
            if (Rules.BombEveryMoves <= 0) return;
            for (int y = 0; y < Board.Height; y++)
                for (int x = 0; x < Board.Width; x++)
                {
                    if (!Board.IsOccupied(x, y) || Board.GetModifier(x, y) != CellModifier.Bomb) continue;
                    int hp = Board.GetHp(x, y) - 1;
                    Board.SetHp(x, y, hp);
                    if (hp <= 0) End(GameOverReason.Bomb);
                }
        }

        private void MaybeSpawnBomb(MoveResult r)
        {
            if (Rules.BombEveryMoves <= 0 || Moves % Rules.BombEveryMoves != 0) return;
            int empty = Board.CellCount - Board.OccupiedCount;
            if (empty <= 1) return;
            int pick = _eventRng.NextInt(empty);
            for (int y = 0; y < Board.Height; y++)
                for (int x = 0; x < Board.Width; x++)
                {
                    if (Board.IsOccupied(x, y)) continue;
                    if (pick-- != 0) continue;
                    Board.SetCell(x, y, (byte)(1 + _eventRng.NextInt(Palette.GlazeCount)), CellModifier.Bomb, Rules.BombCountdown);
                    r.BombSpawned = true; r.BombX = x; r.BombY = y;
                    // La bomba puede completar una línea: se limpia de inmediato (y cuenta como desactivada).
                    var info = Board.ClearCompletedLines(r.ClearedCells);
                    LinesCleared += info.Lines;
                    return;
                }
        }

        // ───────────── Potenciadores ─────────────

        public bool Undo()
        {
            if (_undoCount == 0) return false;
            _undoHead = (_undoHead - 1 + UndoDepth) % UndoDepth;
            _undoCount--;
            _undo[_undoHead].RestoreTo(this);
            BoostersUsed++;
            return true;
        }

        public bool CanRotate(int slot) => !IsOver && slot >= 0 && slot < Tray.Length && !Tray[slot].IsEmpty;

        public bool RotateSlot(int slot)
        {
            if (!CanRotate(slot)) return false;
            var p = Tray[slot];
            p.Shape = Catalog.Get(NextRotation(p.Shape.Id));
            Tray[slot] = p;
            if (!Rules.FreeRotation) BoostersUsed++;
            return true;
        }

        public void UnlockReserve() => ReserveUnlocked = true;

        /// <summary>Guarda la pieza de la casilla en la reserva (o la intercambia). Gratis una vez desbloqueada en la partida.</summary>
        public bool HoldSlot(int slot)
        {
            if (!ReserveUnlocked || IsOver || slot < 0 || slot >= Tray.Length) return false;
            var current = Tray[slot];
            if (current.IsEmpty && Reserve.IsEmpty) return false;
            Tray[slot] = Reserve;
            Reserve = current;
            ClearUndo();
            if (TrayIsEmpty()) Bag.NextTray(Board, Score, Rules.Relaxed, Rules.GoldenPieceChance, Tray);
            return true;
        }

        public bool Hammer(int x, int y)
        {
            if (IsOver || !Board.InBounds(x, y) || !Board.IsOccupied(x, y)) return false;
            LastResult.Reset();
            LastResult.ClearedCells.Add(new ClearedCell { X = x, Y = y, Glaze = Board.GetGlaze(x, y), Modifier = Board.GetModifier(x, y) });
            Board.SetCell(x, y, Board.Empty);
            AfterBooster();
            return true;
        }

        public bool BroomRow(int y)
        {
            if (IsOver || y < 0 || y >= Board.Height || Board.RowFillCount(y) == 0) return false;
            LastResult.Reset();
            Board.ClearRow(y, LastResult.ClearedCells);
            AfterBooster();
            return true;
        }

        public bool BroomColumn(int x)
        {
            if (IsOver || x < 0 || x >= Board.Width) return false;
            LastResult.Reset();
            if (Board.ClearColumn(x, LastResult.ClearedCells) == 0) return false;
            AfterBooster();
            return true;
        }

        public bool NewTray()
        {
            if (IsOver) return false;
            Bag.NextTray(Board, Score, Rules.Relaxed, Rules.GoldenPieceChance, Tray);
            LastResult.Reset();
            LastResult.TrayRefilled = true;
            AfterBooster();
            return true;
        }

        public bool CanRevive => IsOver && !Revived && OverReason == GameOverReason.NoFit;

        /// <summary>Revivir (una vez por partida): barre las 2 filas más llenas y entrega bandeja nueva.</summary>
        public bool Revive()
        {
            if (!CanRevive) return false;
            Revived = true;
            IsOver = false;
            OverReason = GameOverReason.None;
            LastResult.Reset();
            int first = Board.FullestRow();
            Board.ClearRow(first, LastResult.ClearedCells);
            int second = Board.FullestRow(first);
            if (second >= 0) Board.ClearRow(second, LastResult.ClearedCells);
            Bag.NextTray(Board, Score, Rules.Relaxed, Rules.GoldenPieceChance, Tray);
            LastResult.TrayRefilled = true;
            ClearUndo();
            EvaluateGameOver(LastResult);
            return true;
        }

        private void AfterBooster()
        {
            BoostersUsed++;
            ClearUndo();
            EvaluateGameOver(LastResult);
            LastResult.GameOver = IsOver;
            LastResult.Reason = OverReason;
        }

        // ───────────── Deshacer ─────────────

        private void PushUndo()
        {
            _undo[_undoHead].Capture(this);
            _undoHead = (_undoHead + 1) % UndoDepth;
            if (_undoCount < UndoDepth) _undoCount++;
        }

        private void ClearUndo()
        {
            _undoCount = 0;
        }

        private sealed class Snapshot
        {
            private readonly Board _board;
            private readonly TrayPiece[] _tray = new TrayPiece[FairBag.TraySize];
            private readonly int[] _objectives;
            private readonly string[] _families = new string[FairBag.MaxSameFamilyInARow];
            private TrayPiece _reserve;
            private int _score, _moves, _lines, _bestStreak;
            private StreakState _streak;
            private Pcg32 _bagRng, _eventRng;
            private bool _isOver;
            private GameOverReason _reason;

            public Snapshot(int w, int h, bool regions, int objectiveCount)
            {
                _board = new Board(w, h, regions);
                _objectives = new int[objectiveCount];
            }

            public void Capture(GameSession s)
            {
                s.Board.CopyTo(_board);
                Array.Copy(s.Tray, _tray, _tray.Length);
                _reserve = s.Reserve;
                _score = s.Score; _moves = s.Moves; _lines = s.LinesCleared; _bestStreak = s.BestStreak;
                _streak = s.Streak;
                _bagRng = s.Bag.Rng;
                _eventRng = s._eventRng;
                _isOver = s.IsOver;
                _reason = s.OverReason;
                s.Bag.CopyRecentFamilies(_families);
                if (s.Objectives != null) Array.Copy(s.Objectives.Progress, _objectives, _objectives.Length);
            }

            public void RestoreTo(GameSession s)
            {
                _board.CopyTo(s.Board);
                Array.Copy(_tray, s.Tray, _tray.Length);
                s.Reserve = _reserve;
                s.Score = _score; s.Moves = _moves; s.LinesCleared = _lines; s.BestStreak = _bestStreak;
                s.Streak = _streak;
                s.Bag.Rng = _bagRng;
                s._eventRng = _eventRng;
                s.IsOver = _isOver;
                s.OverReason = _reason;
                s.Bag.RestoreRecentFamilies(_families);
                if (s.Objectives != null) Array.Copy(_objectives, s.Objectives.Progress, _objectives.Length);
            }
        }

        // ───────────── Guardado ─────────────

        public Dictionary<string, object> ToState()
        {
            var tray = new List<object>();
            foreach (var p in Tray) tray.Add(PieceToState(p));
            var glazes = Board.GlazeSnapshot();
            var glazeList = new List<object>(glazes.Length);
            foreach (byte g in glazes) glazeList.Add((int)g);
            var state = new Dictionary<string, object>
            {
                { "mode", (int)Rules.Mode },
                { "seed", Seed.ToString() },
                { "w", Board.Width }, { "h", Board.Height },
                { "rows", new List<object>(Board.ToRows()) },
                { "glazes", glazeList },
                { "tray", tray },
                { "reserve", PieceToState(Reserve) },
                { "reserveUnlocked", ReserveUnlocked },
                { "score", Score }, { "moves", Moves }, { "lines", LinesCleared }, { "bestStreak", BestStreak },
                { "streak", Streak.Streak }, { "noClear", Streak.MovesWithoutClear },
                { "rngS", Bag.Rng.State.ToString() }, { "rngI", Bag.Rng.Increment.ToString() },
                { "evS", _eventRng.State.ToString() }, { "evI", _eventRng.Increment.ToString() },
                { "recent", new List<object>(Bag.RecentFamilies) },
                { "revived", Revived }, { "over", IsOver }, { "reason", (int)OverReason }, { "boosters", BoostersUsed },
                { "rules", RulesToState(Rules) }
            };
            if (Level != null)
            {
                state["level"] = Level.Id;
                state["objectives"] = new List<object>(Array.ConvertAll(Objectives.Progress, v => (object)v));
            }
            return state;
        }

        private static object PieceToState(TrayPiece p)
        {
            if (p.IsEmpty) return null;
            return new Dictionary<string, object> { { "id", p.Shape.Id }, { "g", (int)p.Glaze }, { "gold", p.Golden } };
        }

        private static Dictionary<string, object> RulesToState(ModeRules r) => new Dictionary<string, object>
        {
            { "w", r.BoardWidth }, { "h", r.BoardHeight }, { "regions", r.ClearRegions3x3 }, { "freeRot", r.FreeRotation },
            { "relaxed", r.Relaxed }, { "zen", r.ZenSweep }, { "competitive", r.Competitive }, { "bombEvery", r.BombEveryMoves },
            { "bombCount", r.BombCountdown }, { "gold", r.GoldenPieceChance }, { "event", r.EventId }
        };

        public static ModeRules RulesFromState(JsonNode n)
        {
            var rules = new ModeRules
            {
                Mode = (GameMode)n["mode"].AsInt(),
                BoardWidth = n["rules"]["w"].AsInt(8),
                BoardHeight = n["rules"]["h"].AsInt(8),
                ClearRegions3x3 = n["rules"]["regions"].AsBool(),
                FreeRotation = n["rules"]["freeRot"].AsBool(),
                Relaxed = n["rules"]["relaxed"].AsBool(),
                ZenSweep = n["rules"]["zen"].AsBool(),
                Competitive = n["rules"]["competitive"].AsBool(true),
                BombEveryMoves = n["rules"]["bombEvery"].AsInt(),
                BombCountdown = n["rules"]["bombCount"].AsInt(9),
                GoldenPieceChance = n["rules"]["gold"].AsFloat(),
                EventId = n["rules"]["event"].AsString()
            };
            return rules;
        }

        /// <summary>Reconstruye una partida guardada. <paramref name="levelResolver"/> devuelve el nivel por id (Restauración).</summary>
        public static GameSession FromState(JsonNode n, ScoringConfig scoring, PieceCatalog catalog, Func<string, LevelDefinition> levelResolver = null)
        {
            ModeRules rules = RulesFromState(n);
            LevelDefinition level = null;
            string levelId = n["level"].AsString();
            if (!string.IsNullOrEmpty(levelId))
            {
                level = levelResolver?.Invoke(levelId);
                if (level == null) throw new FormatException("Nivel guardado no encontrado: " + levelId);
            }
            var s = new GameSession(rules, scoring, catalog, n["seed"].AsULong(), level);
            s.Board.LoadRows(n["rows"].AsStringArray());
            s.Board.RestoreGlazes(Array.ConvertAll(n["glazes"].AsIntArray(), v => (byte)v));
            for (int i = 0; i < s.Tray.Length; i++) s.Tray[i] = PieceFromState(n["tray"][i], catalog);
            s.Reserve = PieceFromState(n["reserve"], catalog);
            s.ReserveUnlocked = n["reserveUnlocked"].AsBool();
            s.Score = n["score"].AsInt();
            s.Moves = n["moves"].AsInt();
            s.LinesCleared = n["lines"].AsInt();
            s.BestStreak = n["bestStreak"].AsInt();
            s.Streak = new StreakState { Streak = n["streak"].AsInt(), MovesWithoutClear = n["noClear"].AsInt() };
            s.Bag.Rng = new Pcg32 { State = n["rngS"].AsULong(), Increment = n["rngI"].AsULong() };
            s._eventRng = new Pcg32 { State = n["evS"].AsULong(), Increment = n["evI"].AsULong() };
            s.Bag.RecentFamilies = n["recent"].AsStringArray();
            s.Revived = n["revived"].AsBool();
            s.IsOver = n["over"].AsBool();
            s.OverReason = (GameOverReason)n["reason"].AsInt();
            s.BoostersUsed = n["boosters"].AsInt();
            if (level != null) s.Objectives.CopyFrom(n["objectives"].AsIntArray());
            s.ClearUndo();
            s.LastResult.Reset();
            return s;
        }

        private static TrayPiece PieceFromState(JsonNode n, PieceCatalog catalog)
        {
            if (n.IsNull) return TrayPiece.None;
            return new TrayPiece(catalog.Get(n["id"].AsString()), (byte)n["g"].AsInt(1), n["gold"].AsBool());
        }
    }
}
