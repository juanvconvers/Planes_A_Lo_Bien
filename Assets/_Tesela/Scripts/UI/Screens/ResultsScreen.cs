using System.Collections.Generic;
using Tesela.Core;
using Tesela.Core.Audio;
using Tesela.Core.Raster;
using Tesela.Gameplay;
using Tesela.Monetization;
using Tesela.Services;
using UnityEngine;

namespace Tesela.UI
{
    /// <summary>
    /// Resultados con la semilla visible. Los intersticiales solo pueden aparecer en la transición desde aquí
    /// (a Inicio o a una partida nueva), y solo si AdPolicy lo permite.
    /// </summary>
    public sealed class ResultsScreen : ScreenBase
    {
        private readonly GameController _game;
        private bool _doubled;
        private TeselaButton _doubleButton;
        private UiText _coinsText;

        public ResultsScreen(GameController game) { _game = game; }

        public override bool IsMenu => false;

        protected override void Build()
        {
            var s = _game.Session;
            var r = _game.LastRewards;
            bool level = s.IsLevel;
            bool won = s.OverReason == GameOverReason.LevelComplete;

            var panel = Ui.Panel(Root, "panel");
            panel.rectTransform.Place(0.5f, 0.52f, Mathf.Min(960, Layout.Width - 60), Mathf.Min(1500, Layout.Height - 200));
            var p = panel.rectTransform;
            string titleKey = level ? (won ? "results.level_won" : "results.level_lost") : _game.ModeKey == "zen" ? "results.zen" : "results.title";
            Ui.Title(p, Loc.T(titleKey), 64).Rect.Band(true, 50, 90, 40);

            if (level && won)
            {
                for (int i = 0; i < 3; i++)
                {
                    var star = Ui.Icon(p, "star", 120, i < _game.Stars ? Ui.Gold : Palette.SandDeep.ToColor());
                    star.rectTransform.Place(0.5f, 1, 120, 120, (i - 1) * 150, -230 - (i == 1 ? 20 : 0));
                    Motion.Instance.Scale(star.rectTransform, Vector3.zero, Vector3.one, 0.3f, Ease.Settle, 0.15f + i * 0.12f);
                }
            }

            var score = Ui.Text(p, "0", 150, Ui.Ink, TextAnchor.MiddleCenter, TextRole.Display);
            score.Rect.Band(true, level && won ? 330 : 180, 180);
            Motion.Instance.Value(0, s.Score, 0.8f, v => score.Text = Loc.Number(Mathf.RoundToInt(v)), Ease.Out, 0.1f);

            string sub = r.NewBest ? Loc.T("results.new_best") : s.Rules.Competitive ? Loc.T("game.best", Loc.Number(Ctx.Profile.Best(_game.ModeKey))) : "";
            var subText = Ui.Text(p, sub, 40, r.NewBest ? Palette.Coral.ToColor() : Ui.InkSoft, TextAnchor.MiddleCenter, TextRole.Display);
            subText.Rect.Band(true, level && won ? 510 : 370, 60);

            // Semilla visible: el jugador puede comprobar que la partida fue la misma para todos.
            var seed = Ui.Text(p, Loc.T("results.seed", s.SeedCode), 32, Ui.InkSoft);
            seed.Rect.Band(true, level && won ? 580 : 440, 50);

            var stats = Ui.Rect(p, "stats").Band(true, level && won ? 650 : 520, 160, 40);
            StatChip(stats, 0, "coin", "+" + r.Coins, out _coinsText);
            StatChip(stats, 1, "tile", "+" + r.Tiles, out _);
            StatChip(stats, 2, "crown", Loc.T("results.level", Ctx.Profile.Level), out _);

            var info = new List<string>();
            if (r.LevelsGained > 0) info.Add(Loc.T("results.level_up", Ctx.Profile.Level));
            if (r.MissionsReady.Count > 0) info.Add(Loc.T("results.missions_ready", r.MissionsReady.Count));
            if (r.StreakChest) info.Add(Loc.T("results.streak_chest"));
            if (r.TileSetsCompleted > 0) info.Add(Loc.T("results.tileset"));
            if (r.AchievementsUnlocked.Count > 0) info.Add(Loc.T("results.achievement"));
            var infoText = Ui.Text(p, string.Join("\n", info), 34, Ui.Ink);
            infoText.Rect.Band(true, level && won ? 830 : 700, 170, 40);

            var buttons = Ui.Rect(p, "buttons").Band(false, 50, 420, 50);
            bool adOk = r.Coins > 0 && AdPolicy.CanOfferRewarded(Ctx.AdConfig, Ctx.AdState, RewardedPlacement.DoubleCoins, Ctx.Today) == AdDecision.Allowed && Ctx.Ads.IsRewardedReady;
            if (adOk)
            {
                _doubleButton = Ui.Button(buttons, Loc.T("results.double_coins"), FrameStyle.ButtonGold, DoubleCoins, "ad", 42);
                ((RectTransform)_doubleButton.transform).Band(true, 0, 120);
            }
            string againKey = level ? (won ? "results.next_level" : "results.retry") : "results.again";
            var again = Ui.Button(buttons, Loc.T(againKey), FrameStyle.ButtonPrimary, PlayAgain, "play", 46);
            ((RectTransform)again.transform).Band(true, 140, 130);
            var home = Ui.Button(buttons, Loc.T("results.home"), FrameStyle.ButtonSecondary, GoHome, "home", 42);
            ((RectTransform)home.transform).Band(true, 290, 120);

            Ctx.Audio?.Play(won || r.NewBest ? Sfx.LevelUp : Sfx.GameOver, 0.8f);
        }

        private void StatChip(RectTransform parent, int index, string icon, string text, out UiText label)
        {
            var chip = Ui.Panel(parent, "stat" + index, FrameStyle.Chip);
            chip.rectTransform.anchorMin = new Vector2(index / 3f, 0);
            chip.rectTransform.anchorMax = new Vector2((index + 1) / 3f, 1);
            chip.rectTransform.offsetMin = new Vector2(10, 0);
            chip.rectTransform.offsetMax = new Vector2(-10, 0);
            var ic = icon == "tile" ? Ui.Image(chip.transform, "icon", Ui.Art.Tile(2), Color.white) : Ui.Icon(chip.transform, icon, 64, Ui.Ink);
            ic.rectTransform.Place(0.5f, 1, 64, 64, 0, -48);
            label = Ui.Text(chip.transform, text, 38, Ui.Ink, TextAnchor.MiddleCenter, TextRole.Display);
            label.Rect.Band(false, 14, 60, 8);
        }

        public override bool OnBack()
        {
            GoHome();
            return true;
        }

        private void DoubleCoins()
        {
            if (_doubled) return;
            Ctx.Log("ad_offer", new Dictionary<string, object> { { "placement", "double_coins" } });
            Ctx.Ads.ShowRewarded(RewardedPlacement.DoubleCoins, result =>
            {
                if (result != AdResult.Completed)
                {
                    if (result == AdResult.NotAvailable || result == AdResult.TimedOut) Nav.Push(new MessageDialog("ads.title", Loc.T("ads.unavailable")));
                    return;
                }
                _doubled = true;
                AdPolicy.OnRewardedCompleted(Ctx.AdState, RewardedPlacement.DoubleCoins, Time.realtimeSinceStartup, Ctx.Today);
                Ctx.Log("ad_reward", new Dictionary<string, object> { { "placement", "double_coins" } });
                _game.DoubleCoins();
                _coinsText.Text = "+" + (_game.LastRewards.Coins * 2);
                _doubleButton.SetInteractable(false);
                Ctx.Audio?.Play(Sfx.Coin);
            });
        }

        private void PlayAgain() => Transition(InterstitialPlacement.ResultsToNewGame, StartNext);

        private void GoHome() => Transition(InterstitialPlacement.ResultsToMenu, () => Nav.Reset(new HomeScreen()));

        private void StartNext()
        {
            if (!Ctx.Profile.ConsentAsked)
            {
                Nav.Reset(new HomeScreen());
                Nav.Push(new ConsentScreen());
                return;
            }
            var next = new GameController(Ctx);
            var level = _game.Level;
            if (level != null)
            {
                bool won = _game.Session.OverReason == GameOverReason.LevelComplete;
                var target = won ? NextLevel(level) : level;
                if (target == null) { Nav.Reset(new HomeScreen()); Nav.Push(new ChapterScreen()); return; }
                next.StartLevel(target);
            }
            else if (_game.ModeKey == "daily") next.StartDaily();
            else if (_game.ModeKey == "zen") next.StartZen();
            else if (_game.ModeKey == "event" && Ctx.ActiveEventRules() != null) next.StartEvent(Ctx.ActiveEventRules());
            else next.StartClassic();
            Nav.Reset(new HomeScreen());
            Nav.Push(new GameScreen(next));
        }

        private LevelDefinition NextLevel(LevelDefinition current)
        {
            int idx = Ctx.Data.Levels.IndexOf(current);
            return idx >= 0 && idx + 1 < Ctx.Data.Levels.Count ? Ctx.Data.Levels[idx + 1] : null;
        }

        /// <summary>Muestra (o no) el intersticial según las reglas, y luego continúa. Antes revisa ofertas de logro.</summary>
        private void Transition(InterstitialPlacement placement, System.Action next)
        {
            var r = _game.LastRewards;
            OfferMoment? moment = _game.ChapterJustCompleted() ? OfferMoment.ChapterComplete : r.NewBest ? OfferMoment.NewBest : r.PassTiersGained > 0 ? OfferMoment.PassTierUp : (OfferMoment?)null;
            if (moment.HasValue)
            {
                var rule = Ctx.Offers.Pick(moment.Value, Ctx.Segment, Ctx.Profile, Ctx.GamesThisSession, Ctx.Today, key =>
                {
                    var prod = Ctx.Data.Products.ByKey(key);
                    return prod != null && !(prod.StarterPack && Ctx.Profile.StarterPackBought) && !(prod.RemovesAds && Ctx.Profile.NoAds);
                });
                if (rule != null)
                {
                    Ctx.Offers.MarkShown();
                    next();
                    Nav.Push(new OfferDialog(rule));
                    return;
                }
                MaybeAskReview();
            }

            var decision = AdPolicy.CanShowInterstitial(Ctx.AdConfig, Ctx.AdState, placement, Ctx.AdFree, Time.realtimeSinceStartup, Ctx.UtcNow, Ctx.Profile.LastPurchaseUtcTicks);
            if (decision != AdDecision.Allowed || !Ctx.Ads.IsInterstitialReady)
            {
                next();
                return;
            }
            Ctx.Ads.ShowInterstitial(result =>
            {
                if (result == AdResult.Completed) AdPolicy.OnInterstitialShown(Ctx.AdState, Time.realtimeSinceStartup);
                Ctx.Log("ad_show", new Dictionary<string, object> { { "placement", "interstitial" }, { "result", result.ToString() } });
                Ctx.Saves.SaveAds(Ctx.AdState);
                next();
            });
        }

        /// <summary>Reseña solo tras un momento positivo, nunca tras perder, y una sola vez.</summary>
        private void MaybeAskReview()
        {
            int after = Ctx.Remote.Get("features")["reviewAfterGames"].AsInt(8);
            if (Ctx.Profile.GamesPlayed < after || Ctx.Profile.Stat("review_asked") > 0) return;
            Ctx.Profile.AddStat("review_asked", 1);
            Ctx.Review.RequestReview();
        }
    }
}
