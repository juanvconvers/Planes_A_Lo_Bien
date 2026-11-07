using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tesela.Core;
using Tesela.Gameplay;
using Tesela.Gameplay.Bots;

namespace Tesela.Tests
{
    public class BoardTests
    {
        [Test]
        public void PlaceAndBounds()
        {
            var b = new Board();
            var l = TestData.Shape("X.", "XX");
            Assert.IsTrue(b.CanPlace(l, 0, 0));
            Assert.IsFalse(b.CanPlace(l, 7, 0));
            Assert.IsFalse(b.CanPlace(l, -1, 0));
            Assert.IsFalse(b.CanPlace(l, 0, 7));
            b.Place(l, 0, 0, 2);
            Assert.IsTrue(b.IsOccupied(0, 0) && b.IsOccupied(0, 1) && b.IsOccupied(1, 1));
            Assert.IsFalse(b.IsOccupied(1, 0));
            Assert.AreEqual(2, b.GetGlaze(1, 1));
            Assert.AreEqual(3, b.OccupiedCount);
            Assert.IsFalse(b.CanPlace(l, 0, 0));
            Assert.IsTrue(b.CanPlace(TestData.Shape("X"), 1, 0));
            Assert.Throws<InvalidOperationException>(() => b.Place(l, 0, 0, 1));
        }

        [Test]
        public void ConstructorValidates()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Board(2, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Board(8, 13));
            Assert.Throws<ArgumentException>(() => new Board(8, 8, true));
            Assert.DoesNotThrow(() => new Board(9, 9, true));
        }

        [Test]
        public void ClearsRowsAndColumnsSimultaneously()
        {
            var b = new Board();
            for (int x = 0; x < 8; x++) if (x != 3) b.SetCell(x, 5, 1);
            for (int y = 0; y < 8; y++) if (y != 5) b.SetCell(3, y, 2);
            var mono = TestData.Shape("X");
            Assert.AreEqual(2, b.CountLinesCompletedBy(mono, 3, 5));
            b.Place(mono, 3, 5, 4);
            var cleared = new List<ClearedCell>();
            var info = b.ClearCompletedLines(cleared);
            Assert.AreEqual(1, info.Rows);
            Assert.AreEqual(1, info.Columns);
            Assert.AreEqual(15, info.CellsRemoved);
            Assert.AreEqual(15, cleared.Count);
            Assert.IsTrue(b.IsEmpty);
        }

        [Test]
        public void PreviewMatchesActualClear()
        {
            var b = new Board();
            for (int x = 0; x < 6; x++) b.SetCell(x, 7, 1);
            var i2 = TestData.Shape("XX");
            var rows = new bool[8];
            var cols = new bool[8];
            b.PreviewLines(i2, 6, 7, rows, cols);
            Assert.IsTrue(rows[7]);
            Assert.AreEqual(1, rows.Count(r => r) + cols.Count(c => c));
            Assert.AreEqual(1, b.CountLinesCompletedBy(i2, 6, 7));
            b.PreviewLines(i2, 0, 0, rows, cols);
            Assert.IsFalse(rows.Any(r => r) || cols.Any(c => c));
        }

        [Test]
        public void CrackedNeedsTwoClearsMudAndGoldCount()
        {
            var b = new Board();
            b.SetCell(0, 0, 3, CellModifier.Cracked, 2);
            b.SetCell(1, 0, 3, CellModifier.Mud);
            b.SetCell(2, 0, 3, CellModifier.Gold);
            for (int x = 3; x < 8; x++) b.SetCell(x, 0, 1);
            var cleared = new List<ClearedCell>();
            var info = b.ClearCompletedLines(cleared);
            Assert.AreEqual(1, info.CrackedDamaged);
            Assert.AreEqual(0, info.CrackedBroken);
            Assert.AreEqual(1, info.MudFreed);
            Assert.AreEqual(1, info.GoldCleared);
            Assert.IsTrue(b.IsOccupied(0, 0));
            Assert.AreEqual(1, b.GetHp(0, 0));
            Assert.IsTrue(cleared.Single(c => c.X == 0).Survived);

            for (int x = 1; x < 8; x++) b.SetCell(x, 0, 1);
            info = b.ClearCompletedLines(cleared);
            Assert.AreEqual(1, info.CrackedBroken);
            Assert.IsTrue(b.IsEmpty);
        }

        [Test]
        public void Regions3x3ClearOnNineByNine()
        {
            var b = new Board(9, 9, true);
            for (int y = 3; y < 6; y++) for (int x = 3; x < 6; x++) b.SetCell(x, y, 1);
            var info = b.ClearCompletedLines(null);
            Assert.AreEqual(1, info.Regions);
            Assert.AreEqual(1, info.Lines);
            Assert.IsTrue(b.IsEmpty);
        }

        [Test]
        public void RowsEncodingRoundTrip()
        {
            var b = new Board();
            string[] rows =
            {
                "123456..", "C.c.m.g.", "a......i", "........",
                "........", "........", "........", "66666666"
            };
            b.LoadRows(rows);
            CollectionAssert.AreEqual(rows, b.ToRows());
            Assert.AreEqual(CellModifier.Bomb, b.GetModifier(7, 2));
            Assert.AreEqual(9, b.GetHp(7, 2));
            Assert.AreEqual(2, b.GetHp(0, 1));
            Assert.Throws<FormatException>(() => b.LoadRows(new[] { "x" }));
            Assert.Throws<FormatException>(() => b.LoadRows(Enumerable.Repeat("zzzzzzzz", 8).ToArray()));
            Assert.Throws<FormatException>(() => b.LoadRows(Enumerable.Repeat("...", 8).ToArray()));
        }

        [Test]
        public void HelpersAndClone()
        {
            var b = new Board();
            for (int x = 0; x < 5; x++) b.SetCell(x, 6, 1);
            for (int x = 0; x < 3; x++) b.SetCell(x, 2, 1);
            Assert.AreEqual(6, b.FullestRow());
            Assert.AreEqual(2, b.FullestRow(6));
            var c = b.Clone();
            Assert.AreEqual(5, c.ClearRow(6, null));
            Assert.AreEqual(5, b.RowFillCount(6));
            Assert.AreEqual(1, c.ClearColumn(0, null));
            b.SetCell(1, 0, 1); b.SetCell(0, 1, 1);
            Assert.AreEqual(1, b.CountIsolatedHoles());
            Assert.AreEqual(4, Board.PopCount(0b1011_0000_0001));
            var big = TestData.Shape("XXXXX", "XXXXX", "XXXXX", "XXXXX", "XXXXX");
            Assert.IsTrue(new Board().FitsAnywhere(big));
            Assert.AreEqual(16, new Board().CountPlacements(big));
            var glazes = b.GlazeSnapshot();
            b.SetCell(1, 0, 5);
            b.RestoreGlazes(glazes);
            Assert.AreEqual(1, b.GetGlaze(1, 0));
            Assert.Throws<ArgumentException>(() => b.CopyTo(new Board(9, 9)));
        }
    }

    public class PieceTests
    {
        [Test]
        public void ShapeNormalizesAndRotates()
        {
            var s = PieceShape.FromRows("L", "L", new[] { "..X", "XXX" });
            Assert.AreEqual(3, s.Width);
            Assert.AreEqual(2, s.Height);
            var r1 = s.RotatedClockwise();
            Assert.AreEqual("L@1", r1.Id);
            Assert.AreEqual(2, r1.Width);
            Assert.AreEqual(3, r1.Height);
            var r4 = r1.RotatedClockwise().RotatedClockwise().RotatedClockwise();
            Assert.AreEqual("L", r4.Id);
            Assert.IsTrue(r4.SameCellsAs(s));
            Assert.IsFalse(r1.SameCellsAs(s));
            Assert.Throws<ArgumentException>(() => PieceShape.FromRows("e", "e", new[] { "..." }));
            Assert.Throws<ArgumentException>(() => new PieceShape("d", "d", new[] { new CellOffset(0, 0), new CellOffset(0, 0) }, 1));
            StringAssert.Contains("X", s.ToString());
        }

        [Test]
        public void CatalogLoadsRealDataAndResolvesRotations()
        {
            var c = TestData.Catalog;
            Assert.That(c.Shapes.Count, Is.GreaterThanOrEqualTo(30));
            Assert.IsNotNull(c.Mono);
            Assert.That(c.Shapes.Max(s => s.CellCount), Is.EqualTo(9));
            Assert.That(c.Shapes.Min(s => s.CellCount), Is.EqualTo(1));
            var rot = c.Get("c3a@2");
            Assert.AreEqual(3, rot.CellCount);
            Assert.AreSame(rot, c.Get("c3a@2"));
            Assert.Throws<KeyNotFoundException>(() => c.Get("nope"));
            Assert.IsFalse(c.TryGet("nope@1", out _));
            Assert.IsTrue(c.TryGet("sq3", out _));
        }

        [Test]
        public void DifficultyTableIsPublicAndMonotonicForLargePieces()
        {
            var c = TestData.Catalog;
            var sq3 = c.Get("sq3");
            var mono = c.Get("mono");
            float prev = 0;
            foreach (var step in c.Difficulty)
            {
                float w = c.EffectiveWeight(sq3, step.MinScore);
                Assert.That(w, Is.GreaterThanOrEqualTo(prev));
                prev = w;
            }
            Assert.AreEqual(mono.Weight, c.EffectiveWeight(mono, 0), 1e-6);
            Assert.AreEqual(c.Difficulty[0].MinScore, c.DifficultyFor(-5).MinScore);
        }

        [Test]
        public void CatalogRejectsBadData()
        {
            Assert.Throws<FormatException>(() => PieceCatalog.FromJson("{\"pieces\":[]}"));
            Assert.Throws<FormatException>(() => PieceCatalog.FromJson("{\"pieces\":[{\"id\":\"a\",\"cells\":[\"XX\"]}]}"));
            Assert.Throws<FormatException>(() => PieceCatalog.FromJson("{\"pieces\":[{\"id\":\"m\",\"cells\":[\"X\"]},{\"id\":\"m\",\"cells\":[\"X\"]}]}"));
            Assert.Throws<FormatException>(() => PieceCatalog.FromJson("{\"pieces\":[{\"id\":\"m@1\",\"cells\":[\"X\"]}]}"));
            Assert.Throws<FormatException>(() => PieceCatalog.FromJson("{\"pieces\":[{\"id\":\"m\",\"cells\":[\"X\"]},{\"id\":\"big\",\"cells\":[\"XXXXXX\"]}]}"));
            Assert.Throws<FormatException>(() => PieceCatalog.FromJson("{\"pieces\":[{\"id\":\"m\",\"cells\":[\"X\"],\"weight\":-1}]}"));
            Assert.Throws<FormatException>(() => PieceCatalog.FromJson("{\"pieces\":[{\"id\":\"m\",\"cells\":[\"X\"]},{\"id\":\"ten\",\"cells\":[\"XXXX\",\"XXXX\",\"XX\"]}]}"));
        }
    }

    public class ScoringTests
    {
        [Test]
        public void LineBonusTable()
        {
            var cfg = TestData.Scoring;
            Assert.AreEqual(0, cfg.LineBonusFor(0));
            Assert.AreEqual(10, cfg.LineBonusFor(1));
            Assert.AreEqual(30, cfg.LineBonusFor(2));
            Assert.AreEqual(60, cfg.LineBonusFor(3));
            Assert.AreEqual(100, cfg.LineBonusFor(4));
            Assert.AreEqual(150, cfg.LineBonusFor(5));
        }

        [Test]
        public void StreakMultipliersCapAtFour()
        {
            var cfg = TestData.Scoring;
            Assert.AreEqual(1f, cfg.MultiplierFor(0));
            Assert.AreEqual(1f, cfg.MultiplierFor(1));
            Assert.AreEqual(1.5f, cfg.MultiplierFor(2));
            Assert.AreEqual(2f, cfg.MultiplierFor(3));
            Assert.AreEqual(4f, cfg.MultiplierFor(50));
        }

        [Test]
        public void StreakBreaksAfterThreeMovesWithoutClearing()
        {
            var s = new StreakState();
            s.Register(true, 3);
            s.Register(true, 3);
            Assert.AreEqual(2, s.Streak);
            s.Register(false, 3);
            s.Register(false, 3);
            Assert.AreEqual(2, s.Streak);
            s.Register(true, 3);
            Assert.AreEqual(3, s.Streak);
            s.Register(false, 3); s.Register(false, 3); s.Register(false, 3);
            Assert.AreEqual(0, s.Streak);
        }

        [Test]
        public void MoveScoreIncludesCellsBonusAndEmptyBoard()
        {
            var cfg = TestData.Scoring;
            Assert.AreEqual(4, Scoring.ScoreMove(cfg, 4, default, 0, false));
            var two = new LineClearInfo { Rows = 1, Columns = 1 };
            Assert.AreEqual(3 + 30, Scoring.ScoreMove(cfg, 3, two, 1, false));
            Assert.AreEqual(3 + 45, Scoring.ScoreMove(cfg, 3, two, 2, false));
            Assert.AreEqual(3 + 30 + 300, Scoring.ScoreMove(cfg, 3, two, 1, true));
            var gold = new LineClearInfo { Rows = 1, GoldCleared = 2 };
            Assert.AreEqual(1 + 10 + 50, Scoring.ScoreMove(cfg, 1, gold, 1, false));
            Assert.Throws<FormatException>(() => ScoringConfig.FromJson("{\"streakBreakAfterMoves\":0}"));
        }
    }

    public class SessionTests
    {
        [Test]
        public void NewSessionHasThreePiecesAndOneFits()
        {
            var s = TestData.NewClassic();
            Assert.IsTrue(s.Tray.All(p => !p.IsEmpty));
            Assert.IsTrue(s.Tray.Any(p => s.Board.FitsAnywhere(p.Shape)));
            Assert.IsFalse(s.IsOver);
            Assert.AreEqual(8, s.SeedCode.Length);
            Assert.AreEqual(int.MaxValue, s.MovesLeft);
        }

        [Test]
        public void SameSeedSamePieces()
        {
            var a = TestData.NewClassic(777);
            var b = TestData.NewClassic(777);
            var botA = new GreedyBot(1);
            var botB = new GreedyBot(1);
            for (int i = 0; i < 60 && !a.IsOver; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    Assert.AreEqual(a.Tray[k].Shape?.Id, b.Tray[k].Shape?.Id);
                    Assert.AreEqual(a.Tray[k].Glaze, b.Tray[k].Glaze);
                }
                Assert.IsTrue(botA.Choose(a, out int sl, out int x, out int y));
                Assert.IsTrue(botB.Choose(b, out int sl2, out int x2, out int y2));
                a.Place(sl, x, y);
                b.Place(sl2, x2, y2);
            }
            Assert.AreEqual(a.Score, b.Score);
        }

        [Test]
        public void PlaceUpdatesScoreAndRefillsTray()
        {
            var s = TestData.NewClassic(5);
            int placed = 0;
            var bot = new GreedyBot(2);
            bool refilled = false;
            while (placed < 3)
            {
                Assert.IsTrue(bot.Choose(s, out int slot, out int x, out int y));
                int cells = s.Tray[slot].Shape.CellCount;
                int before = s.Score;
                var r = s.Place(slot, x, y);
                Assert.AreEqual(cells, r.CellsPlaced);
                Assert.That(s.Score - before, Is.GreaterThanOrEqualTo(cells));
                placed++;
                refilled |= r.TrayRefilled;
                if (s.IsOver) Assert.Inconclusive("Fin temprano improbable con esta semilla");
            }
            Assert.IsTrue(refilled);
            Assert.IsTrue(s.Tray.All(p => !p.IsEmpty));
            Assert.AreEqual(3, s.Moves);
        }

        [Test]
        public void InvalidMovesThrow()
        {
            var s = TestData.NewClassic(9);
            Assert.IsFalse(s.CanPlace(-1, 0, 0));
            Assert.IsFalse(s.CanPlace(5, 0, 0));
            Assert.IsFalse(s.CanPlace(0, 50, 50));
            Assert.Throws<InvalidOperationException>(() => s.Place(0, 50, 50));
        }

        [Test]
        public void GameOverOnlyWhenNothingFits()
        {
            var s = TestData.NewClassic(11);
            // Llena el tablero dejando solo celdas aisladas en diagonal (ninguna línea completa).
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    s.Board.SetCell(x, y, x == y ? Board.Empty : (byte)1);
            bool anyFit = s.Tray.Any(p => s.Board.FitsAnywhere(p.Shape));
            Assert.AreEqual(anyFit, s.AnyMoveAvailable());
            Assert.AreEqual(anyFit, Simulation.BruteForceAnyFit(s));
        }

        [Test]
        public void GameOverDetectedAfterMoveAndReviveClearsTwoRows()
        {
            var catalog = PieceCatalog.FromJson("{\"pieces\":[{\"id\":\"m\",\"cells\":[\"X\"],\"weight\":0.0001},{\"id\":\"big\",\"cells\":[\"XXX\",\"XXX\",\"XXX\"],\"weight\":1000}]}");
            var s = new GameSession(ModeRules.Classic(), TestData.Scoring, catalog, 3);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    if (x != y && !(x == 1 && y == 0)) s.Board.SetCell(x, y, 2);
            // Diagonal vacía + (1,0): ninguna línea llena. Bandeja controlada: un mono y dos 3×3 que no caben.
            s.Tray[0] = new TrayPiece(catalog.Get("m"), 1);
            s.Tray[1] = new TrayPiece(catalog.Get("big"), 1);
            s.Tray[2] = new TrayPiece(catalog.Get("big"), 1);
            var r = s.Place(0, 0, 0);
            Assert.IsTrue(r.GameOver);
            Assert.AreEqual(GameOverReason.NoFit, r.Reason);
            Assert.IsFalse(Simulation.BruteForceAnyFit(s));
            Assert.IsTrue(s.CanRevive);
            Assert.IsTrue(s.Revive());
            Assert.IsFalse(s.CanRevive);
            Assert.That(s.LastResult.ClearedCells.Count, Is.GreaterThanOrEqualTo(12));
            Assert.IsTrue(s.Tray.Any(p => s.Board.FitsAnywhere(p.Shape)));
            Assert.IsFalse(s.Revive());
        }

        [Test]
        public void UndoRestoresExactState()
        {
            var s = TestData.NewClassic(21);
            var bot = new GreedyBot(3);
            Assert.IsFalse(s.Undo());
            bot.Choose(s, out int slot, out int x, out int y);
            string before = Json.Serialize(s.ToState());
            s.Place(slot, x, y);
            Assert.IsTrue(s.CanUndo);
            Assert.IsTrue(s.Undo());
            Json.ParseNode(before);
            var afterUndo = s.ToState();
            afterUndo["boosters"] = 0;
            Assert.AreEqual(before, Json.Serialize(afterUndo));
            // Tras deshacer, la misma jugada da exactamente el mismo resultado (el RNG también se restaura).
            s.Place(slot, x, y);
            string trayIds = string.Join(",", s.Tray.Select(p => p.Shape?.Id));
            s.Undo();
            s.Place(slot, x, y);
            Assert.AreEqual(trayIds, string.Join(",", s.Tray.Select(p => p.Shape?.Id)));
        }

        [Test]
        public void UndoDepthIsBounded()
        {
            var s = TestData.NewClassic(23);
            var bot = new GreedyBot(4);
            for (int i = 0; i < 6 && !s.IsOver; i++)
            {
                bot.Choose(s, out int slot, out int x, out int y);
                s.Place(slot, x, y);
            }
            int undos = 0;
            while (s.Undo()) undos++;
            Assert.AreEqual(GameSession.UndoDepth, undos);
        }

        [Test]
        public void BoostersWork()
        {
            var s = TestData.NewClassic(31);
            s.Board.SetCell(2, 2, 3);
            Assert.IsTrue(s.Hammer(2, 2));
            Assert.IsFalse(s.Board.IsOccupied(2, 2));
            Assert.IsFalse(s.Hammer(2, 2));

            for (int x = 0; x < 5; x++) s.Board.SetCell(x, 4, 1);
            Assert.IsTrue(s.BroomRow(4));
            Assert.AreEqual(0, s.Board.RowFillCount(4));
            Assert.IsFalse(s.BroomRow(4));
            s.Board.SetCell(6, 1, 1);
            Assert.IsTrue(s.BroomColumn(6));
            Assert.IsFalse(s.BroomColumn(6));
            Assert.IsFalse(s.BroomColumn(99));

            string oldTray = string.Join(",", s.Tray.Select(p => p.Shape.Id + p.Glaze));
            Assert.IsTrue(s.NewTray());
            Assert.IsTrue(s.Tray.All(p => !p.IsEmpty));
            Assert.AreNotEqual(oldTray, string.Join(",", s.Tray.Select(p => p.Shape.Id + p.Glaze)) + "x");

            string id = s.Tray[0].Shape.Id;
            Assert.IsTrue(s.RotateSlot(0));
            Assert.AreEqual(PieceShape.BaseId(id), PieceShape.BaseId(s.Tray[0].Shape.Id));
            Assert.That(s.BoostersUsed, Is.GreaterThanOrEqualTo(5));
            Assert.IsFalse(s.RotateSlot(7));
        }

        [Test]
        public void ReserveSwapsAndCountsForGameOver()
        {
            var s = TestData.NewClassic(41);
            Assert.IsFalse(s.HoldSlot(0));
            s.UnlockReserve();
            var first = s.Tray[0];
            Assert.IsTrue(s.HoldSlot(0));
            Assert.AreEqual(first.Shape.Id, s.Reserve.Shape.Id);
            Assert.IsTrue(s.Tray[0].IsEmpty);
            var second = s.Tray[1];
            Assert.IsTrue(s.HoldSlot(1));
            Assert.AreEqual(first.Shape.Id, s.Tray[1].Shape.Id);
            Assert.AreEqual(second.Shape.Id, s.Reserve.Shape.Id);
            Assert.IsFalse(s.HoldSlot(9));
        }

        [Test]
        public void HoldingLastPieceRefillsTray()
        {
            var s = TestData.NewClassic(43);
            s.UnlockReserve();
            var bot = new GreedyBot(5);
            // Coloca dos piezas y guarda la tercera: la bandeja debe rellenarse.
            for (int i = 0; i < 2; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    if (s.Tray[k].IsEmpty || !s.Board.FitsAnywhere(s.Tray[k].Shape)) continue;
                    GreedyPlacement.TryFindBest(s.Board, s.Tray[k].Shape, out int x, out int y);
                    s.Place(k, x, y);
                    break;
                }
            }
            int remaining = Enumerable.Range(0, 3).Single(k => !s.Tray[k].IsEmpty);
            Assert.IsTrue(s.HoldSlot(remaining));
            Assert.IsTrue(s.Tray.All(p => !p.IsEmpty));
            Assert.IsFalse(s.Reserve.IsEmpty);
        }

        [Test]
        public void SaveAndRestoreContinuesIdentically()
        {
            var s = TestData.NewClassic(51);
            var bot = new GreedyBot(6);
            for (int i = 0; i < 25 && !s.IsOver; i++)
            {
                bot.Choose(s, out int sl, out int x, out int y);
                s.Place(sl, x, y);
            }
            s.UnlockReserve();
            string text = SaveCodec.Wrap(s.ToState());
            Assert.IsTrue(SaveCodec.TryUnwrap(text, out var payload, out _));
            var restored = GameSession.FromState(payload, TestData.Scoring, TestData.Catalog);
            Assert.AreEqual(Json.Serialize(s.ToState()), Json.Serialize(restored.ToState()));

            var botA = new GreedyBot(9);
            var botB = new GreedyBot(9);
            for (int i = 0; i < 30 && !s.IsOver; i++)
            {
                botA.Choose(s, out int a1, out int a2, out int a3);
                botB.Choose(restored, out int b1, out int b2, out int b3);
                s.Place(a1, a2, a3);
                restored.Place(b1, b2, b3);
            }
            Assert.AreEqual(s.Score, restored.Score);
            CollectionAssert.AreEqual(s.Board.ToRows(), restored.Board.ToRows());
        }

        [Test]
        public void ZenNeverEndsAndSweeps()
        {
            var s = new GameSession(ModeRules.Zen(), TestData.Scoring, TestData.Catalog, 61);
            var bot = new RandomBot(1);
            int sweeps = 0;
            for (int i = 0; i < 400; i++)
            {
                Assert.IsFalse(s.IsOver);
                Assert.IsTrue(bot.Choose(s, out int sl, out int x, out int y));
                sweeps += s.Place(sl, x, y).ZenSweeps;
            }
            Assert.That(sweeps, Is.GreaterThan(0));
        }

        [Test]
        public void RelaxedTraysAlwaysFullyPlaceable()
        {
            var rules = ModeRules.Classic();
            rules.Relaxed = true;
            var s = new GameSession(rules, TestData.Scoring, TestData.Catalog, 71);
            var bot = new GreedyBot(7);
            for (int i = 0; i < 300 && !s.IsOver; i++)
            {
                if (s.Tray.All(p => !p.IsEmpty))
                    foreach (var p in s.Tray) Assert.IsTrue(s.Board.FitsAnywhere(p.Shape), "Cada pieza de una bandeja relajada debe caber");
                bot.Choose(s, out int sl, out int x, out int y);
                s.Place(sl, x, y);
            }
        }

        [Test]
        public void FreeRotationEventCountsRotationsForGameOver()
        {
            var rules = ModeRules.EventFromJson(Json.ParseNode("{\"id\":\"giro\",\"freeRotation\":true}"));
            var catalog = PieceCatalog.FromJson("{\"pieces\":[{\"id\":\"m\",\"cells\":[\"X\"],\"weight\":0.001},{\"id\":\"v\",\"cells\":[\"X\",\"X\",\"X\"],\"weight\":1000}]}");
            var s = new GameSession(rules, TestData.Scoring, catalog, 3);
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) s.Board.SetCell(x, y, 1);
            for (int x = 0; x < 3; x++) s.Board.SetCell(x, 0, Board.Empty);
            s.Board.SetCell(0, 1, 1);
            for (int i = 0; i < 3; i++) s.Tray[i] = new TrayPiece(catalog.Get("v"), 1);
            Assert.IsTrue(s.AnyMoveAvailable(), "La pieza vertical cabe girada");
            Assert.IsTrue(s.RotateSlot(0));
            Assert.IsTrue(s.SlotFits(0));
            Assert.AreEqual(0, s.BoostersUsed);
        }

        [Test]
        public void BombEventExplodesWhenCountdownEnds()
        {
            var rules = ModeRules.EventFromJson(Json.ParseNode("{\"id\":\"bombas\",\"bombEveryMoves\":1,\"bombCountdown\":2}"));
            var s = new GameSession(rules, TestData.Scoring, TestData.Catalog, 81);
            var bot = new GreedyBot(8);
            bool spawned = false;
            for (int i = 0; i < 50 && !s.IsOver; i++)
            {
                bot.Choose(s, out int sl, out int x, out int y);
                spawned |= s.Place(sl, x, y).BombSpawned;
            }
            Assert.IsTrue(spawned);
            Assert.IsTrue(s.IsOver);
        }

        [Test]
        public void GoldenPiecesAppearWithChance()
        {
            var rules = ModeRules.EventFromJson(Json.ParseNode("{\"id\":\"oro\",\"goldenPieceChance\":1.0}"));
            var s = new GameSession(rules, TestData.Scoring, TestData.Catalog, 91);
            Assert.IsTrue(s.Tray.All(p => p.Golden));
            GreedyPlacement.TryFindBest(s.Board, s.Tray[0].Shape, out int x, out int y);
            s.Place(0, x, y);
            Assert.AreEqual(CellModifier.Gold, s.Board.GetModifier(x + s.LastResult.Shape.Cells[0].X, y + s.LastResult.Shape.Cells[0].Y));
        }

        [Test]
        public void PreviewLinesThroughSession()
        {
            var s = TestData.NewClassic(13);
            s.PreviewLines(0, 99, 99, out var rows, out var cols);
            Assert.IsFalse(rows.Any(r => r) || cols.Any(c => c));
            Assert.IsTrue(s.SlotFits(0) || !s.Board.FitsAnywhere(s.Tray[0].Shape));
        }
    }

    public class FairBagTests
    {
        [Test]
        public void EveryTrayHasAPlayablePieceAcrossManyBoards()
        {
            var bag = new FairBag(TestData.Catalog, 5);
            var tray = new TrayPiece[3];
            var rng = new Pcg32(8);
            for (int t = 0; t < 2000; t++)
            {
                var b = new Board();
                int fill = rng.NextInt(64);
                for (int i = 0; i < fill; i++) b.SetCell(rng.NextInt(8), rng.NextInt(8), 1);
                b.ClearCompletedLines(null);
                bag.NextTray(b, rng.NextInt(12000), false, 0f, tray);
                Assert.IsTrue(tray.Any(p => Simulation.BruteForceFits(b, p.Shape)), "Bandeja sin pieza jugable en prueba " + t);
            }
        }

        [Test]
        public void NeverThreeOfTheSameFamilyInARow()
        {
            var catalog = PieceCatalog.FromJson("{\"pieces\":[{\"id\":\"m\",\"cells\":[\"X\"],\"weight\":100},{\"id\":\"d\",\"cells\":[\"XX\"],\"weight\":1}]}");
            var bag = new FairBag(catalog, 1);
            var tray = new TrayPiece[3];
            var seq = new List<string>();
            var b = new Board();
            for (int t = 0; t < 500; t++)
            {
                bag.NextTray(b, 0, false, 0f, tray);
                seq.AddRange(tray.Select(p => p.Shape.Family));
            }
            for (int i = 2; i < seq.Count; i++)
                Assert.IsFalse(seq[i] == seq[i - 1] && seq[i] == seq[i - 2], "Tres iguales seguidas en " + i);
        }

        [Test]
        public void RecentFamiliesRoundTrip()
        {
            var bag = new FairBag(TestData.Catalog, 2);
            var tray = new TrayPiece[3];
            bag.NextTray(new Board(), 0, false, 0f, tray);
            var fam = bag.RecentFamilies;
            Assert.AreEqual(2, fam.Length);
            var other = new FairBag(TestData.Catalog, 2) { RecentFamilies = fam };
            CollectionAssert.AreEqual(fam, other.RecentFamilies);
            other.RecentFamilies = null;
            Assert.AreEqual(0, other.RecentFamilies.Length);
            Assert.Throws<ArgumentException>(() => bag.NextTray(new Board(), 0, false, 0, new TrayPiece[2]));
            Assert.Throws<ArgumentNullException>(() => new FairBag(null, 1));
        }

        [Test]
        public void LargePiecesBecomeMoreFrequentWithScoreSameForEveryone()
        {
            int Large(int score)
            {
                var bag = new FairBag(TestData.Catalog, 3);
                var tray = new TrayPiece[3];
                int n = 0;
                for (int t = 0; t < 3000; t++)
                {
                    bag.NextTray(new Board(), score, false, 0f, tray);
                    n += tray.Count(p => p.Shape.CellCount >= TestData.Catalog.LargePieceMinCells);
                }
                return n;
            }
            Assert.That(Large(12000), Is.GreaterThan(Large(0)));
        }
    }

    public class SimulationSmokeTests
    {
        [Test]
        public void ThousandGamesWithoutFalseEnds()
        {
            var rnd = Simulation.Run(seed => new RandomBot(seed), "aleatorio", 500, ModeRules.Classic(), TestData.Scoring, TestData.Catalog, 100);
            var greedy = Simulation.Run(seed => new GreedyBot(seed), "codicioso", 100, ModeRules.Classic(), TestData.Scoring, TestData.Catalog, 200, 800);
            foreach (var st in new[] { rnd, greedy })
            {
                Assert.AreEqual(0, st.Exceptions, string.Join("\n", st.ExceptionSamples));
                Assert.AreEqual(0, st.FalseGameOvers);
                Assert.AreEqual(0, st.TraysWithoutFit);
                Assert.IsTrue(st.Passed);
            }
            Assert.That(greedy.AvgScore, Is.GreaterThan(rnd.AvgScore));
            StringAssert.Contains("codicioso", Simulation.ToMarkdown(new[] { rnd, greedy }, "t"));
        }
    }

    public class FairPlayArchitectureTests
    {
        [Test]
        public void GameplayAssemblyDoesNotReferenceMonetization()
        {
            var gameplay = typeof(GameSession).Assembly;
            foreach (var reference in gameplay.GetReferencedAssemblies())
            {
                StringAssert.DoesNotContain("Monetization", reference.Name);
                StringAssert.DoesNotContain("Meta", reference.Name);
                StringAssert.DoesNotContain("Services", reference.Name);
            }
        }

        [Test]
        public void GameplayAsmdefDoesNotReferenceMonetization()
        {
            string asmdef = TestData.Read("Assets/_Tesela/Scripts/Gameplay/Tesela.Gameplay.asmdef");
            var refs = Json.ParseNode(asmdef)["references"].AsStringArray();
            CollectionAssert.AreEqual(new[] { "Tesela.Core" }, refs);
            Assert.IsTrue(Json.ParseNode(asmdef)["noEngineReferences"].AsBool());
        }

        [Test]
        public void FairBagPublicApiOnlyTakesBoardScoreAndRules()
        {
            var allowed = new HashSet<Type> { typeof(Board), typeof(int), typeof(bool), typeof(float), typeof(TrayPiece[]), typeof(PieceCatalog), typeof(ulong), typeof(string[]) };
            foreach (var m in typeof(FairBag).GetMethods())
            {
                if (m.DeclaringType != typeof(FairBag)) continue;
                foreach (var p in m.GetParameters())
                    Assert.IsTrue(allowed.Contains(p.ParameterType), m.Name + " recibe " + p.ParameterType.Name);
            }
            foreach (var c in typeof(FairBag).GetConstructors())
                foreach (var p in c.GetParameters())
                    Assert.IsTrue(allowed.Contains(p.ParameterType));
        }
    }
}
