using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Tesela.Core;
using Tesela.Core.Audio;
using Tesela.Core.Raster;

namespace Tesela.Tests
{
    public class LocalizationTests
    {
        private static Dictionary<string, Dictionary<string, string>> _tables;

        private static Dictionary<string, Dictionary<string, string>> Tables
        {
            get
            {
                if (_tables != null) return _tables;
                _tables = new Dictionary<string, Dictionary<string, string>>();
                foreach (string lang in Loc.Languages)
                {
                    var d = new Dictionary<string, string>();
                    foreach (var kv in Json.ParseNode(TestData.Config("Loc/" + lang + ".json")).Properties)
                        if (!kv.Key.StartsWith("_")) d[kv.Key] = kv.Value.AsString();
                    _tables[lang] = d;
                    Loc.Load(lang, TestData.Config("Loc/" + lang + ".json"));
                }
                return _tables;
            }
        }

        private static IEnumerable<string> UiSources() =>
            Directory.GetFiles(TestData.Path("Assets/_Tesela/Scripts/UI"), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText);

        [Test]
        public void AllLanguagesHaveTheSameKeysAndNoEmptyValues()
        {
            var es = Tables["es"];
            foreach (string lang in Loc.Languages)
            {
                CollectionAssert.AreEquivalent(es.Keys, Tables[lang].Keys, lang);
                foreach (var kv in Tables[lang]) Assert.IsNotEmpty(kv.Value.Trim(), lang + ":" + kv.Key);
            }
        }

        [Test]
        public void PlaceholdersMatchAcrossLanguages()
        {
            var rx = new Regex(@"\{\d\}");
            foreach (var kv in Tables["es"])
                foreach (string lang in Loc.Languages)
                {
                    var a = rx.Matches(kv.Value).Cast<Match>().Select(m => m.Value).Distinct().OrderBy(x => x);
                    var b = rx.Matches(Tables[lang][kv.Key]).Cast<Match>().Select(m => m.Value).Distinct().OrderBy(x => x);
                    CollectionAssert.AreEqual(a, b, lang + ":" + kv.Key);
                }
        }

        /// <summary>Ningún texto queda sin traducir: toda clave literal usada en la interfaz existe en las tres tablas.</summary>
        [Test]
        public void EveryKeyUsedInUiExists()
        {
            var literal = new Regex("\"((?:app|mode|event|home|nav|game|booster|pause|revive|ads|results|missions|mission|chapter|album|ach|shop|theme|chest|pass|reward|offer|settings|lang|common|delete|fair|consent|tutorial|reminder|notif)\\.[a-z0-9_.]+)\"(?!\\s*\\+)");
            var missing = new List<string>();
            foreach (string src in UiSources())
                foreach (Match m in literal.Matches(src))
                {
                    string key = m.Groups[1].Value;
                    if (key.EndsWith(".")) continue;
                    if (!Tables["es"].ContainsKey(key)) missing.Add(key);
                }
            CollectionAssert.IsEmpty(missing.Distinct());
        }

        [Test]
        public void DynamicKeyFamiliesExist()
        {
            var es = Tables["es"];
            var economy = Tesela.Meta.EconomyConfig.FromJson(TestData.Config("Economy.json"));
            var keys = new List<string>();
            foreach (var m in economy.DailyMissions.Concat(economy.WeeklyMissions)) keys.Add("mission." + m.Metric);
            foreach (var c in economy.Chest) keys.Add("chest." + c.Id);
            foreach (var a in economy.Achievements) keys.Add("ach." + a.Id);
            foreach (var t in economy.Themes.Where(t => t.PriceCoins > 0 && !t.ClubOnly)) keys.Add("theme." + t.Id);
            for (int i = 0; i < Tesela.Meta.Boosters.Count; i++)
            {
                string b = Tesela.Meta.Boosters.Key((Tesela.Meta.BoosterType)i);
                keys.Add("booster." + b + ".name");
                keys.Add("booster." + b + ".desc");
            }
            var remote = Json.ParseNode(TestData.Config("RemoteDefaults.json"));
            foreach (var r in remote["offers"]["rules"].Items) { keys.Add("offer." + r["id"].AsString() + ".title"); keys.Add("offer." + r["id"].AsString() + ".body"); }
            foreach (var e in remote["events"]["list"].Items) keys.Add(e["nameKey"].AsString());
            foreach (string mode in new[] { "classic", "daily", "zen", "restoration", "event" }) keys.Add("mode." + mode);
            for (int i = 1; i <= 3; i++) { keys.Add("chapter." + i); keys.Add("tutorial.step" + i); }
            for (int i = 1; i <= 7; i++) keys.Add("fair.rule" + i);
            foreach (string l in Loc.Languages) keys.Add("lang." + l);
            foreach (string key in new[] { "boosters_small", "boosters_medium", "boosters_large", "no_ads", "starter" }) { keys.Add("shop." + key); keys.Add("shop." + key + ".desc"); }
            keys.Add("notif.daily_ready"); keys.Add("notif.streak_risk");
            CollectionAssert.IsEmpty(keys.Where(k => !es.ContainsKey(k)).Distinct());
        }

        [Test]
        public void NoCompetitorNamesOrBannedFontsAnywhereInTextOrCode()
        {
            var banned = new[] { "block blast", "hungry studio", "tetris", "1010!", "woodoku", "blockudoku" };
            var files = Directory.GetFiles(TestData.Path("Assets/_Tesela"), "*.*", SearchOption.AllDirectories)
                .Where(f => (f.EndsWith(".cs") || f.EndsWith(".json") || f.EndsWith(".asset")) && !f.EndsWith("LocAndArtTests.cs"));
            foreach (string f in files)
            {
                string text = File.ReadAllText(f).ToLowerInvariant();
                foreach (string b in banned) Assert.IsFalse(text.Contains(b), b + " en " + f);
            }
            var fonts = Directory.GetFiles(TestData.Path("Assets/_Tesela/Fonts"), "*.ttf", SearchOption.AllDirectories).Select(Path.GetFileName).ToList();
            CollectionAssert.AreEquivalent(new[] { "Fraunces.ttf", "BricolageGrotesque.ttf" }, fonts);
        }

        [Test]
        public void LocRuntimeFallbacksAndFormatting()
        {
            var _ = Tables;
            Loc.SetLanguage("pt");
            Assert.AreEqual("Pausa", Loc.T("pause.title"));
            Assert.AreEqual("missing.key", Loc.T("missing.key"));
            StringAssert.Contains("7", Loc.T("game.moves_left", 7));
            Loc.SetLanguage("xx");
            Assert.AreEqual("es", Loc.Language);
            Assert.AreEqual("12.345", Loc.Number(12345));
            Loc.SetLanguage("en");
            Assert.AreEqual("12,345", Loc.Number(12345));
            Assert.AreEqual("en", Loc.FromSystem("de"));
            Assert.AreEqual("pt", Loc.FromSystem("PT"));
            Assert.AreEqual("es", Loc.FromSystem(null));
            Assert.IsTrue(Loc.Has("app.name"));
            Assert.AreEqual(Loc.T("game.best"), Loc.T("game.best", new object[0]));
            Loc.SetLanguage("es");
        }
    }

    public class ContrastTests
    {
        private static void AssertAA(Rgba text, Rgba background, bool largeText, string what)
        {
            double ratio = Rgba.ContrastRatio(text, background);
            Assert.That(ratio, Is.GreaterThanOrEqualTo(largeText ? 3.0 : 4.5), what + " = " + ratio.ToString("F2"));
        }

        /// <summary>Todas las combinaciones de texto que usa la interfaz cumplen WCAG AA.</summary>
        [Test]
        public void TextColorsMeetWcagAA()
        {
            var onPrimary = new Rgba(255, 250, 240);
            foreach (var bg in new[] { Palette.Sand, Palette.SandLight, Palette.SandDeep })
            {
                AssertAA(Palette.Indigo, bg, false, "índigo sobre arena");
                AssertAA(Palette.IndigoSoft, bg, false, "índigo suave sobre arena");
            }
            AssertAA(onPrimary, Palette.Cobalt, false, "texto claro sobre botón cobalto");
            AssertAA(Palette.Indigo, Palette.Gold, false, "índigo sobre oro");
            AssertAA(Palette.Coral, Palette.SandLight, true, "récord (texto grande) coral sobre arena");
            AssertAA(Palette.Olive, Palette.Sand, true, "objetivo cumplido (texto grande) oliva sobre arena");
        }

        [Test]
        public void GlazesAreDistinguishableBeyondColor()
        {
            var patterns = Enumerable.Range(1, 6).Select(Palette.Pattern).ToList();
            Assert.AreEqual(6, patterns.Distinct().Count(), "cada esmalte tiene su propio patrón");
            Assert.IsFalse(patterns.Contains(GlazePattern.None));
            Assert.AreEqual(Palette.Grout, Palette.Glaze(0));
        }
    }

    public class RasterAndAudioTests
    {
        [Test]
        public void TilesAreOpaqueInsideTransparentOutsideAndVaryByPattern()
        {
            var a = TileRasterizer.Render(64, Palette.Cobalt, GlazePattern.Star, 1);
            Assert.AreEqual(0, a.Get(0, 0).A, "esquina transparente");
            Assert.AreEqual(255, a.Get(32, 10).A, "interior opaco");
            var b = TileRasterizer.Render(64, Palette.Cobalt, GlazePattern.Cross, 1);
            Assert.IsFalse(a.Pixels.SequenceEqual(b.Pixels));
            var rotated = TileRasterizer.Render(64, Palette.Cobalt, GlazePattern.Wave, 1, 1);
            var plain = TileRasterizer.Render(64, Palette.Cobalt, GlazePattern.Wave, 1, 0);
            Assert.IsFalse(rotated.Pixels.SequenceEqual(plain.Pixels), "el giro del patrón cambia la imagen");
            var again = TileRasterizer.Render(64, Palette.Cobalt, GlazePattern.Star, 1);
            Assert.IsTrue(a.Pixels.SequenceEqual(again.Pixels), "determinista");
            foreach (GlazePattern p in Enum.GetValues(typeof(GlazePattern)))
                if (p != GlazePattern.None) Assert.Less(TileRasterizer.PatternSdf(p, 0.5f, 0.5f, 20, 64), 1e5f);
        }

        [Test]
        public void OverlaysSocketsFramesGrainAndIconsRender()
        {
            foreach (TileOverlay o in Enum.GetValues(typeof(TileOverlay)))
            {
                var c = TileRasterizer.RenderOverlay(32, o, 2);
                if (o != TileOverlay.None) Assert.IsTrue(c.Pixels.Any(p => p.A > 0), o.ToString());
            }
            Assert.IsTrue(TileRasterizer.RenderSocket(32, 1).Pixels.Any(p => p.A == 255));
            foreach (FrameStyle f in Enum.GetValues(typeof(FrameStyle)))
            {
                var c = UiRasterizer.RenderFrame(f, 3);
                Assert.AreEqual(255, c.Get(64, 64).A, f + " centro opaco");
                Assert.That(UiRasterizer.FrameBorder(f), Is.InRange(16, 64));
            }
            var grain = UiRasterizer.RenderGrain(64, 1);
            double meanAlpha = grain.Pixels.Average(p => p.A) / 255.0;
            Assert.That(meanAlpha, Is.InRange(0.01, 0.08), "grano sutil (3–5 %)");
            foreach (string id in IconRasterizer.Ids)
            {
                var icon = IconRasterizer.Render(id);
                int ink = icon.Pixels.Count(p => p.A > 128);
                Assert.That(ink, Is.GreaterThan(150), id);
            }
            Assert.Throws<ArgumentException>(() => IconRasterizer.Render("nope"));
            Assert.AreEqual(IconRasterizer.Ids.Length, IconRasterizer.Ids.Distinct().Count());
        }

        [Test]
        public void CanvasBlendAndSdfHelpers()
        {
            var c = new RasterCanvas(4, 4);
            c.Blend(1, 1, new Rgba(255, 0, 0), 1f);
            c.Blend(1, 1, new Rgba(0, 0, 255, 128), 1f);
            Assert.That(c.Get(1, 1).B, Is.InRange(120, 135));
            c.Blend(-1, 9, new Rgba(1, 1, 1), 1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => new RasterCanvas(0, 1));
            Assert.Less(Sdf.Circle(0, 0, 0, 0, 1), 0);
            Assert.Greater(Sdf.RoundedBox(10, 10, 0, 0, 1, 1, 0.2f), 0);
            Assert.AreEqual(1f, Sdf.Arc(2, 0, 0, 0, 1, -0.5f, 0.5f), 1e-4);
            Assert.Less(Sdf.Polygon(0.1f, 0.1f, new float[] { -1, -1, 1, -1, 0, 1 }), 0);
            Assert.That(Sdf.ValueNoise(3.3f, 1.7f, 5), Is.InRange(0f, 1f));
            Assert.AreEqual(Sdf.Hash(1, 2, 3, 8), Sdf.Hash(9, 10, 3, 8), 1e-6, "periódico");
            Assert.That(Sdf.VoronoiEdge(1.5f, 2.5f, 1), Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void SynthesizedAudioIsBoundedAndPentatonicRises()
        {
            foreach (Sfx s in Enum.GetValues(typeof(Sfx)))
            {
                var buf = SfxSynth.Render(s);
                Assert.That(buf.Length, Is.GreaterThan(100), s.ToString());
                Assert.That(buf.Max(Math.Abs), Is.LessThanOrEqualTo(1f));
                Assert.IsFalse(buf.Any(float.IsNaN));
            }
            Assert.Less(SfxSynth.MidiToHz(60), SfxSynth.MidiToHz(64));
            Assert.AreEqual(440f, SfxSynth.MidiToHz(69), 0.01f);
            Assert.That(SfxSynth.ComboNote(7).Length, Is.GreaterThan(1000));
            var layer = MusicSynth.RenderLayer(1);
            Assert.AreEqual(MusicSynth.LoopSamples, layer.Length, "las capas duran lo mismo: bucle sincronizado");
            Assert.That(layer.Max(Math.Abs), Is.LessThanOrEqualTo(0.51f));
        }
    }

    public class SettingsAndServicesTests
    {
        [Test]
        public void SettingsRoundTripAndClamp()
        {
            var s = new GameSettings { LiftCells = 3, TextScale = 1.3f, LeftHanded = true, Language = "pt" };
            var back = GameSettings.FromJson(Json.ParseNode(Json.Serialize(s.ToJson())));
            Assert.AreEqual(Json.Serialize(s.ToJson()), Json.Serialize(back.ToJson()));
            var clamped = GameSettings.FromJson(Json.ParseNode("{\"lift\":9,\"textScale\":5,\"music\":-1}"));
            Assert.AreEqual(3f, clamped.LiftCells);
            Assert.AreEqual(1.3f, clamped.TextScale);
            Assert.AreEqual(0f, clamped.MusicVolume);
            Assert.AreEqual(0.7f, GameSettings.FromJson(default).MusicVolume);
        }

        [Test]
        public void OfflineFallbacksAlwaysAnswer()
        {
            var store = new MemoryStore();
            store.Write("a", "1");
            Assert.AreEqual("1", store.Read("a"));
            store.Delete("a");
            Assert.IsNull(store.Read("a"));
            var rc = new LocalRemoteConfig(TestData.Config("RemoteDefaults.json"));
            Assert.AreEqual(20, rc.Get("ads")["rewardedDailyCap"].AsInt());
            bool? fetched = null;
            rc.Fetch(ok => fetched = ok);
            Assert.AreEqual(false, fetched);
            Assert.AreEqual("local", rc.Variant);
            var games = new OfflineGames();
            games.SignIn(ok => Assert.IsFalse(ok));
            games.CloudLoad(Assert.IsNull);
            games.CloudSave("x", Assert.IsFalse);
            games.SubmitScore("classic", 1); games.UnlockAchievement("a"); games.ShowLeaderboard("x");
            Assert.IsFalse(games.IsSignedIn);
            var n = new NoNotifications();
            n.RequestPermission(Assert.IsFalse); n.ScheduleAt(DateTime.Now, "t", "b"); n.CancelAll();
            var review = new NoReview(); review.RequestReview(); review.CheckForUpdate();
            var analytics = new NullAnalytics();
            analytics.Log("run_start"); analytics.SetUserProperty("a", "b"); analytics.RecordException(new Exception());
            CollectionAssert.AreEqual(new[] { "run_start" }, analytics.Events);
        }

        [Test]
        public void SeedsAreSharedForDailyAndFitInSeedCode()
        {
            Assert.AreEqual(Tesela.Gameplay.Seeds.Daily(9000), Tesela.Gameplay.Seeds.Daily(9000));
            Assert.AreNotEqual(Tesela.Gameplay.Seeds.Daily(9000), Tesela.Gameplay.Seeds.Daily(9001));
            foreach (ulong s in new[] { Tesela.Gameplay.Seeds.Daily(1), Tesela.Gameplay.Seeds.Random(DateTime.UtcNow.Ticks), Tesela.Gameplay.Seeds.Event("x", 3) })
            {
                Assert.IsTrue(Hashing.TryCodeToSeed(Hashing.SeedToCode(s), out ulong back));
                Assert.AreEqual(s, back);
            }
        }
    }
}
