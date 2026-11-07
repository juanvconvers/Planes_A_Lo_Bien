using System;
using System.Linq;
using NUnit.Framework;
using Tesela.Core;
using Tesela.Gameplay;
using Tesela.Gameplay.Bots;

namespace Tesela.Tests
{
    public class LevelDefinitionTests
    {
        private const string Sample = @"{""levels"":[{""id"":""t1"",""chapter"":1,""index"":1,""seed"":""123"",""moves"":20,""stars"":[3,6],
            ""objectives"":[{""type"":""collect"",""glaze"":1,""target"":5},{""type"":""cracked"",""target"":1},{""type"":""mud"",""target"":1},{""type"":""score"",""target"":50},{""type"":""lines"",""target"":2}],
            ""rows"":[""........"",""........"",""........"",""........"",""........"",""........"",""Cm......"",""1111.111""]}]}";

        [Test]
        public void ParsesAndSerializesLevels()
        {
            var levels = LevelDefinition.ListFromJson(Sample);
            Assert.AreEqual(1, levels.Count);
            var l = levels[0];
            Assert.AreEqual(5, l.Objectives.Count);
            Assert.AreEqual(ObjectiveType.FreeMud, l.Objectives[2].Type);
            Assert.AreEqual(123UL, l.Seed);
            var again = LevelDefinition.FromJson(new JsonNode(Json.Parse(Json.Serialize(l.ToJson()))));
            Assert.AreEqual(Json.Serialize(l.ToJson()), Json.Serialize(again.ToJson()));
            foreach (ObjectiveType t in Enum.GetValues(typeof(ObjectiveType)))
                Assert.AreEqual(t, ObjectiveDef.ParseType(ObjectiveDef.TypeKey(t)));
        }

        [Test]
        public void RejectsInvalidLevels()
        {
            Assert.Throws<FormatException>(() => LevelDefinition.FromJson(Json.ParseNode("{\"moves\":5,\"objectives\":[{\"type\":\"lines\"}]}")));
            Assert.Throws<FormatException>(() => LevelDefinition.FromJson(Json.ParseNode("{\"id\":\"x\",\"moves\":5}")));
            Assert.Throws<FormatException>(() => LevelDefinition.FromJson(Json.ParseNode("{\"id\":\"x\",\"moves\":0,\"objectives\":[{\"type\":\"lines\"}]}")));
            Assert.Throws<FormatException>(() => LevelDefinition.FromJson(Json.ParseNode("{\"id\":\"x\",\"moves\":5,\"objectives\":[{\"type\":\"???\"}]}")));
            var empty = LevelDefinition.FromJson(Json.ParseNode("{\"id\":\"x\",\"moves\":5,\"objectives\":[{\"type\":\"lines\"}]}"));
            Assert.AreEqual(8, empty.Rows.Length);
        }

        [Test]
        public void TrackerCountsEveryObjectiveType()
        {
            var l = LevelDefinition.ListFromJson(Sample)[0];
            var s = new GameSession(ModeRules.Restoration(), TestData.Scoring, TestData.Catalog, l.Seed, l);
            Assert.IsTrue(s.IsLevel);
            Assert.AreEqual(20, s.MovesLeft);
            var tracker = s.Objectives;
            var cleared = new System.Collections.Generic.List<ClearedCell>
            {
                new ClearedCell { Glaze = 1 }, new ClearedCell { Glaze = 1 }, new ClearedCell { Glaze = 1, Survived = true }, new ClearedCell { Glaze = 2 }
            };
            tracker.OnMove(new LineClearInfo { Rows = 1, CrackedBroken = 1, MudFreed = 3 }, cleared, 40);
            CollectionAssert.AreEqual(new[] { 2, 1, 1, 40, 1 }, tracker.Progress);
            Assert.IsFalse(tracker.IsComplete);
            Assert.That(tracker.Completion, Is.InRange(0.5f, 0.9f));
            tracker.OnMove(new LineClearInfo { Rows = 5 }, cleared, 999);
            tracker.OnMove(default, cleared, 999);
            Assert.AreEqual(50, tracker.Progress[3]);
            Assert.IsTrue(tracker.IsComplete);
            Assert.AreEqual(1f, tracker.Completion);
            tracker.CopyFrom(null);
            tracker.CopyFrom(new[] { 0, 0 });
            Assert.AreEqual(0, tracker.Progress[0]);
        }

        [Test]
        public void LevelEndsWhenCompleteOrOutOfMoves()
        {
            var easy = LevelDefinition.FromJson(Json.ParseNode("{\"id\":\"e\",\"seed\":5,\"moves\":40,\"objectives\":[{\"type\":\"score\",\"target\":10}]}"));
            var s = new GameSession(ModeRules.Restoration(), TestData.Scoring, TestData.Catalog, easy.Seed, easy);
            var bot = new GreedyBot(1);
            while (!s.IsOver)
            {
                bot.Choose(s, out int sl, out int x, out int y);
                s.Place(sl, x, y);
            }
            Assert.AreEqual(GameOverReason.LevelComplete, s.OverReason);
            Assert.IsFalse(s.CanRevive);

            var hard = LevelDefinition.FromJson(Json.ParseNode("{\"id\":\"h\",\"seed\":5,\"moves\":2,\"objectives\":[{\"type\":\"score\",\"target\":100000}]}"));
            var h = new GameSession(ModeRules.Restoration(), TestData.Scoring, TestData.Catalog, hard.Seed, hard);
            for (int i = 0; i < 2; i++)
            {
                bot.Choose(h, out int sl, out int x, out int y);
                h.Place(sl, x, y);
            }
            Assert.AreEqual(GameOverReason.OutOfMoves, h.OverReason);
            Assert.AreEqual(0, h.MovesLeft);
        }

        [Test]
        public void LevelSessionSaveRestore()
        {
            var l = LevelDefinition.ListFromJson(Sample)[0];
            var s = new GameSession(ModeRules.Restoration(), TestData.Scoring, TestData.Catalog, l.Seed, l);
            var bot = new GreedyBot(2);
            for (int i = 0; i < 4 && !s.IsOver; i++) { bot.Choose(s, out int a, out int b, out int c); s.Place(a, b, c); }
            var state = s.ToState();
            var restored = GameSession.FromState(new JsonNode(Json.Parse(Json.Serialize(state))), TestData.Scoring, TestData.Catalog, id => id == l.Id ? l : null);
            Assert.AreEqual(Json.Serialize(state), Json.Serialize(restored.ToState()));
            Assert.Throws<FormatException>(() => GameSession.FromState(new JsonNode(Json.Parse(Json.Serialize(state))), TestData.Scoring, TestData.Catalog, id => null));
            Assert.AreEqual(GameMode.Restoration, GameSession.RulesFromState(new JsonNode(state)).Mode);
        }

        [Test]
        public void ModeRulesFactories()
        {
            Assert.IsTrue(ModeRules.Zen().ZenSweep);
            Assert.IsFalse(ModeRules.Zen().Competitive);
            Assert.AreEqual(GameMode.Daily, ModeRules.Daily().Mode);
            var ev = ModeRules.EventFromJson(Json.ParseNode("{\"id\":\"x\",\"boardWidth\":9,\"boardHeight\":9,\"regions3x3\":true,\"competitive\":false}"));
            var copy = ev.Copy();
            Assert.AreNotSame(ev, copy);
            Assert.AreEqual(9, copy.BoardWidth);
            Assert.IsTrue(copy.ClearRegions3x3);
            Assert.IsFalse(copy.Competitive);
        }
    }
}

namespace Tesela.Tests
{
    public class ShippedLevelsTests
    {
        private static System.Collections.Generic.List<LevelDefinition> All()
        {
            var list = new System.Collections.Generic.List<LevelDefinition>();
            for (int ch = 1; ch <= 3; ch++) list.AddRange(LevelDefinition.ListFromJson(TestData.Read("Assets/_Tesela/Data/Levels/Resources/Levels/chapter" + ch + ".json")));
            return list;
        }

        [Test]
        public void SixtyLevelsInThreeChaptersWithUniqueIds()
        {
            var all = All();
            Assert.AreEqual(60, all.Count);
            Assert.AreEqual(60, all.Select(l => l.Id).Distinct().Count());
            for (int ch = 1; ch <= 3; ch++) Assert.AreEqual(20, all.Count(l => l.Chapter == ch));
            foreach (var l in all)
            {
                var b = new Board(l.Width, l.Height);
                b.LoadRows(l.Rows);
                Assert.AreEqual(0, b.ClearCompletedLines(null).Lines, l.Id + ": el tablero inicial no debe tener líneas llenas");
                Assert.That(l.ThreeStarMovesLeft, Is.GreaterThan(l.TwoStarMovesLeft), l.Id);
            }
        }

        /// <summary>Criterio de cierre de la Fase 5: los 60 niveles superan el validador (≥ 70 % resolubles).</summary>
        [Test]
        public void AllShippedLevelsPassTheValidator()
        {
            foreach (var l in All())
            {
                var v = LevelValidator.Validate(l, TestData.Scoring, TestData.Catalog, 60);
                Assert.That(v.Solvability, Is.GreaterThanOrEqualTo(LevelValidator.MinSolvability), l.Id);
            }
        }

        [Test]
        public void GeneratorProducesValidLevelAndPercentileHelper()
        {
            var l = LevelGenerator.Generate(2, 3, TestData.Scoring, TestData.Catalog);
            Assert.AreEqual("c2_l03", l.Id);
            Assert.That(l.Solvability, Is.GreaterThanOrEqualTo(LevelValidator.MinSolvability));
            Assert.AreEqual(0, LevelValidator.Percentile(null, 0.5f));
            Assert.AreEqual(5, LevelValidator.Percentile(new[] { 1, 5, 9 }, 0.5f));
            var hopeless = LevelDefinition.FromJson(Json.ParseNode("{\"id\":\"x\",\"seed\":1,\"moves\":1,\"objectives\":[{\"type\":\"score\",\"target\":999999}]}"));
            var v = LevelValidator.Validate(hopeless, TestData.Scoring, TestData.Catalog, 5);
            Assert.AreEqual(0f, v.Solvability);
            Assert.IsFalse(v.GreedySolves);
            Assert.AreEqual(1, hopeless.MoveLimit, "el validador restaura el límite");
        }

        [Test]
        public void ObjectiveAwareBotPrefersObjectiveLines()
        {
            var b = new Board();
            for (int x = 0; x < 7; x++) b.SetCell(x, 7, 1, x == 0 ? CellModifier.Cracked : CellModifier.None, 1);
            var mono = TestData.Shape("X");
            Assert.That(Tesela.Gameplay.Bots.GreedyPlacement.ObjectiveBonus(b, mono, 7, 7, 0), Is.GreaterThanOrEqualTo(30));
            Assert.AreEqual(0, Tesela.Gameplay.Bots.GreedyPlacement.ObjectiveBonus(b, mono, 0, 0, 0));
            Assert.That(Tesela.Gameplay.Bots.GreedyPlacement.ObjectiveBonus(b, mono, 7, 7, 1), Is.GreaterThan(30));
        }
    }
}
