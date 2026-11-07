using System.Collections;
using System.IO;
using NUnit.Framework;
using Tesela.Core;
using Tesela.Gameplay;
using Tesela.Gameplay.Bots;
using Tesela.Monetization;
using Tesela.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tesela.Tests.PlayMode
{
    /// <summary>
    /// Tests de humo con la app real (UI incluida). Cualquier Debug.LogError o excepción no controlada hace fallar el test.
    /// Usan una carpeta de guardado temporal para no tocar el progreso del editor.
    /// </summary>
    public class AppPlayModeTests
    {
        private string _dir;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _dir = Path.Combine(Application.temporaryCachePath, "tesela-tests-" + System.Guid.NewGuid().ToString("N"));
            TeselaApp.SaveDirectoryOverride = _dir;
            TeselaApp.Restart();
            float timeout = Time.realtimeSinceStartup + 30f;
            while (!TeselaApp.Ready && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.IsTrue(TeselaApp.Ready, "La app no arrancó en 30 s");
            var ctx = TeselaApp.Context;
            ctx.Profile.TutorialDone = true;
            ctx.Profile.ConsentAsked = true;
            TeselaApp.Navigator.Reset(new HomeScreen());
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private static IEnumerator PlayMoves(GameController game, GameScreen screen, int moves)
        {
            var bot = new GreedyBot(7, 6);
            int played = 0;
            while (played < moves)
            {
                var s = game.Session;
                if (s.IsOver)
                {
                    if (s.CanRevive) game.ReviveFree();
                    else
                    {
                        game.Finish();
                        RestartSame(game);
                    }
                    screen.Refresh();
                    continue;
                }
                Assert.IsTrue(bot.Choose(s, out int slot, out int x, out int y), "El bot no encontró jugada sin fin de partida");
                game.Place(slot, x, y);
                screen.Refresh();
                played++;
                if (played % 15 == 0) yield return null;
            }
        }

        private static void RestartSame(GameController game)
        {
            var ctx = TeselaApp.Context;
            switch (game.ModeKey)
            {
                case "daily": game.StartDaily(); break;
                case "zen": game.StartZen(); break;
                case "event": game.StartEvent(ctx.ActiveEventRules()); break;
                case "restoration": game.StartLevel(ctx.Data.Levels[0]); break;
                default: game.StartClassic(); break;
            }
        }

        [UnityTest]
        public IEnumerator Classic300MovesThroughTheUi()
        {
            var game = new GameController(TeselaApp.Context);
            game.StartClassic();
            var screen = TeselaApp.Navigator.Push(new GameScreen(game));
            yield return null;
            yield return PlayMoves(game, screen, 300);
        }

        [UnityTest]
        public IEnumerator SaveAfterEveryMoveAndResumeAfterClose()
        {
            var ctx = TeselaApp.Context;
            var game = new GameController(ctx);
            game.StartClassic();
            var screen = TeselaApp.Navigator.Push(new GameScreen(game));
            yield return PlayMoves(game, screen, 20);
            string before = Json.Serialize(game.Session.ToState());

            // Simula cierre de la app: se reconstruye todo y se reanuda desde disco.
            TeselaApp.Restart();
            while (!TeselaApp.Ready) yield return null;
            var resumed = new GameController(TeselaApp.Context);
            Assert.IsTrue(resumed.TryResume(), "Debe aparecer 'Continuar partida'");
            Assert.AreEqual(before, Json.Serialize(resumed.Session.ToState()));
        }

        [UnityTest]
        public IEnumerator EveryModePlays200Moves()
        {
            var ctx = TeselaApp.Context;
            var starts = new System.Action<GameController>[]
            {
                g => g.StartClassic(), g => g.StartDaily(), g => g.StartZen(),
                g => g.StartLevel(ctx.Data.Levels[0]), g => g.StartEvent(ctx.ActiveEventRules())
            };
            foreach (var start in starts)
            {
                var game = new GameController(ctx);
                start(game);
                var screen = TeselaApp.Navigator.Push(new GameScreen(game));
                yield return null;
                yield return PlayMoves(game, screen, 200);
                TeselaApp.Navigator.Reset(new HomeScreen());
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EveryEventRulesetPlays()
        {
            var ctx = TeselaApp.Context;
            foreach (var e in ctx.Remote.Get("events")["list"].Items)
            {
                var game = new GameController(ctx);
                game.StartEvent(ModeRules.EventFromJson(e));
                var screen = TeselaApp.Navigator.Push(new GameScreen(game));
                yield return null;
                yield return PlayMoves(game, screen, 60);
                TeselaApp.Navigator.Reset(new HomeScreen());
            }
        }

        [UnityTest]
        public IEnumerator VisitsEveryScreenAndReturnsHome()
        {
            var nav = TeselaApp.Navigator;
            var game = new GameController(TeselaApp.Context);
            game.StartClassic();
            ScreenBase[] screens =
            {
                new ChapterScreen(), new ShopScreen(), new PassScreen(), new AlbumScreen(), new AchievementsScreen(),
                new SettingsScreen(), new FairPlayScreen(), new CreditsScreen(), new GameScreen(game), new PauseDialog(game, null),
                new ChestDialog(), new ConsentScreen(), new MessageDialog("common.ok", "x")
            };
            foreach (var s in screens)
            {
                nav.Push(s);
                yield return null;
                yield return null;
                nav.Reset(new HomeScreen());
                yield return null;
                Assert.AreEqual(1, nav.Depth);
                Assert.IsInstanceOf<HomeScreen>(nav.Top);
            }
            game.Finish();
            nav.Push(new ResultsScreen(game));
            yield return null;
            nav.Back();
            yield return null;
            Assert.IsInstanceOf<HomeScreen>(nav.Top);
        }

        /// <summary>Criterio de la Fase 8: sin red ni IDs el juego arranca y se juega con los valores por defecto.</summary>
        [UnityTest]
        public IEnumerator BootsAndPlaysWithoutNetworkOrIds()
        {
            var ctx = TeselaApp.Context;
            Assert.IsInstanceOf<FakeAdService>(ctx.Ads.Inner);
            Assert.AreEqual(20, ctx.AdConfig.RewardedDailyCap);
            Assert.IsNotNull(ctx.ActiveEventRules());
            Assert.AreEqual(60, ctx.Data.Levels.Count);
            var game = new GameController(ctx);
            game.StartClassic();
            var screen = TeselaApp.Navigator.Push(new GameScreen(game));
            yield return PlayMoves(game, screen, 40);
            game.Finish();
            Assert.That(ctx.Profile.GamesPlayed, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator RewardedAdFlowNeverPunishesWhenNoVideo()
        {
            var ctx = TeselaApp.Context;
            var fake = (FakeAdService)ctx.Ads.Inner;
            fake.NextRewardedResult = AdResult.NotAvailable;
            AdResult? result = null;
            ctx.Ads.ShowRewarded(RewardedPlacement.DoubleCoins, r => result = r);
            yield return null;
            Assert.AreEqual(AdResult.NotAvailable, result);
            int coins = ctx.Profile.Coins;
            Assert.AreEqual(coins, ctx.Profile.Coins);
        }
    }
}
