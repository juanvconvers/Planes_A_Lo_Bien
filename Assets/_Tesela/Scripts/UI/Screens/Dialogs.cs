using System;
using System.Collections.Generic;
using Tesela.Core;
using Tesela.Core.Audio;
using Tesela.Core.Raster;
using Tesela.Meta;
using Tesela.Monetization;
using Tesela.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Tesela.UI
{
    /// <summary>Tamaño de referencia del lienzo (se calcula en el arranque según el área segura).</summary>
    public static class Layout
    {
        public static float Width = 1080f;
        public static float Height = 1920f;
    }

    /// <summary>
    /// Diálogo con fondo atenuado. Reglas anti patrones oscuros: el botón de cerrar es visible desde el primer
    /// instante, del mismo tamaño que las demás opciones y sin cuentas atrás; nada de frases de culpa.
    /// </summary>
    public abstract class DialogBase : ScreenBase
    {
        protected RectTransform Panel;
        protected RectTransform Content;
        private float _cursor;
        private float _height;

        public override bool IsDialog => true;
        public override bool IsMenu => false;

        protected abstract string TitleKey { get; }
        protected virtual float PanelHeight => 900;
        protected virtual bool ShowClose => true;

        protected override void Build()
        {
            var dim = Ui.Image(Root, "dim", Ui.Art.White, Palette.Indigo.ToColor(0.55f), true);
            dim.rectTransform.Stretch();
            _height = PanelHeight;
            var panel = Ui.Panel(Root, "panel");
            Panel = panel.rectTransform;
            Panel.Place(0.5f, 0.5f, Mathf.Min(940, Layout.Width - 60), _height, 6, 0);
            panel.raycastTarget = true;
            var title = Ui.Title(Panel, Loc.T(TitleKey), 60);
            title.Rect.Band(true, 40, 90, 40);
            if (ShowClose)
            {
                var close = Ui.IconButton(Panel, "close", Close, 96, FrameStyle.ButtonSecondary);
                ((RectTransform)close.transform).Place(1, 1, 96, 96, -64, -64);
            }
            Content = Ui.Rect(Panel, "content").Stretch(50, 40, 50, 150);
            _cursor = 0;
            BuildContent();
            Motion.Instance.Scale(Panel, Vector3.one * 0.92f, Vector3.one, 0.2f, Ease.Settle);
        }

        protected abstract void BuildContent();

        protected virtual void Close() => Nav.Remove(this);

        public override bool OnBack()
        {
            if (ShowClose) Close();
            return true;
        }

        protected UiText Line(string text, float size = 40, float height = 70, Color? color = null, TextRole role = TextRole.Body)
        {
            var t = Ui.Text(Content, text, size, color ?? Ui.Ink, TextAnchor.MiddleCenter, role);
            t.Rect.Band(true, _cursor, height);
            _cursor += height + 10;
            return t;
        }

        protected TeselaButton Action(string text, FrameStyle style, Action onClick, string icon = null, float height = 120)
        {
            var b = Ui.Button(Content, text, style, onClick, icon, 44);
            ((RectTransform)b.transform).Band(true, _cursor, height, 20);
            _cursor += height + 18;
            return b;
        }

        protected void Space(float h) => _cursor += h;
    }

    public sealed class PauseDialog : DialogBase
    {
        private readonly GameController _game;
        private readonly Action _onResume;

        public PauseDialog(GameController game, Action onResume)
        {
            _game = game;
            _onResume = onResume;
        }

        protected override string TitleKey => "pause.title";
        protected override float PanelHeight => 1000;

        protected override void BuildContent()
        {
            Line(Loc.T("results.seed", _game.Session.SeedCode), 34, 60, Ui.InkSoft);
            Action(Loc.T("pause.resume"), FrameStyle.ButtonPrimary, Close, "play");
            Action(Loc.T("pause.restart"), FrameStyle.ButtonSecondary, Restart, "restore");
            Action(Loc.T("pause.settings"), FrameStyle.ButtonSecondary, () => Nav.Push(new SettingsScreen()), "settings");
            Action(Loc.T("pause.home"), FrameStyle.ButtonSecondary, Home, "home");
            Line(Loc.T("pause.saved"), 30, 60, Ui.InkSoft);
        }

        protected override void Close()
        {
            base.Close();
            _onResume?.Invoke();
        }

        private void Restart()
        {
            // Reiniciar abandona la partida sin entregar recompensas, igual para todos los modos.
            Ctx.Saves.ClearRun();
            var level = _game.Level;
            var mode = _game.ModeKey;
            var next = new GameController(Ctx);
            if (level != null) next.StartLevel(level);
            else if (mode == "daily") next.StartDaily();
            else if (mode == "zen") next.StartZen();
            else if (mode == "event" && Ctx.ActiveEventRules() != null) next.StartEvent(Ctx.ActiveEventRules());
            else next.StartClassic();
            Nav.Reset(new HomeScreen());
            Nav.Push(new GameScreen(next));
        }

        private void Home()
        {
            _game.Save();
            Nav.Reset(new HomeScreen());
        }
    }

    /// <summary>Revivir una vez por partida: con anuncio (opcional) o con monedas. Sin cuenta atrás.</summary>
    public sealed class ReviveDialog : DialogBase
    {
        private readonly GameController _game;
        private readonly Action _onDecline;
        private readonly Action _onRevived;

        public ReviveDialog(GameController game, Action onDecline, Action onRevived)
        {
            _game = game;
            _onDecline = onDecline;
            _onRevived = onRevived;
        }

        protected override string TitleKey => "revive.title";
        protected override bool ShowClose => false;
        protected override float PanelHeight => 860;

        protected override void BuildContent()
        {
            Line(Loc.T("revive.body"), 38, 120);
            bool adAllowed = AdPolicy.CanOfferRewarded(Ctx.AdConfig, Ctx.AdState, RewardedPlacement.Revive, Ctx.Today) == AdDecision.Allowed && Ctx.Ads.IsRewardedReady;
            if (adAllowed) Action(Loc.T("revive.ad"), FrameStyle.ButtonPrimary, WatchAd, "ad");
            int price = Ctx.Data.Economy.RevivePrice;
            var coins = Action(Loc.T("revive.coins", price), adAllowed ? FrameStyle.ButtonSecondary : FrameStyle.ButtonPrimary, PayCoins, "coin");
            coins.SetInteractable(Ctx.Profile.Coins >= price);
            if (!adAllowed && Ctx.AdState.RewardedToday < Ctx.AdConfig.RewardedDailyCap && !Ctx.AdFree)
                Line(Loc.T("ads.unavailable"), 30, 60, Ui.InkSoft);
            Action(Loc.T("revive.decline"), FrameStyle.ButtonSecondary, Decline);
        }

        private void WatchAd()
        {
            Ctx.Log("ad_offer", new Dictionary<string, object> { { "placement", "revive" } });
            Ctx.Ads.ShowRewarded(RewardedPlacement.Revive, result =>
            {
                Ctx.Log("ad_show", new Dictionary<string, object> { { "placement", "revive" }, { "result", result.ToString() } });
                if (result == AdResult.Completed)
                {
                    AdPolicy.OnRewardedCompleted(Ctx.AdState, RewardedPlacement.Revive, Time.realtimeSinceStartup, Ctx.Today);
                    Ctx.Log("ad_reward", new Dictionary<string, object> { { "placement", "revive" } });
                    _game.ReviveFree();
                    Nav.Remove(this);
                    _onRevived?.Invoke();
                }
            });
        }

        private void PayCoins()
        {
            if (!_game.ReviveWithCoins()) return;
            Ctx.Audio?.Play(Sfx.Reward);
            Nav.Remove(this);
            _onRevived?.Invoke();
        }

        private void Decline()
        {
            Nav.Remove(this);
            _onDecline?.Invoke();
        }
    }

    /// <summary>Sin cargas ni monedas: ofrece un potenciador gratis por anuncio (tope diario) o ir a la tienda.</summary>
    public sealed class BoosterOfferDialog : DialogBase
    {
        private readonly GameController _game;
        private readonly BoosterType _type;
        private readonly Action _onDone;

        public BoosterOfferDialog(GameController game, BoosterType type, Action onDone)
        {
            _game = game;
            _type = type;
            _onDone = onDone;
        }

        protected override string TitleKey => "booster." + Boosters.Key(_type) + ".name";
        protected override float PanelHeight => 760;

        protected override void BuildContent()
        {
            Line(Loc.T("booster." + Boosters.Key(_type) + ".desc"), 36, 110);
            bool ad = AdPolicy.CanOfferRewarded(Ctx.AdConfig, Ctx.AdState, RewardedPlacement.FreeBooster, Ctx.Today) == AdDecision.Allowed && Ctx.Ads.IsRewardedReady;
            if (ad) Action(Loc.T("booster.free_ad"), FrameStyle.ButtonPrimary, WatchAd, "ad");
            else Line(Loc.T("ads.unavailable_short"), 30, 60, Ui.InkSoft);
            Action(Loc.T("booster.go_shop"), FrameStyle.ButtonSecondary, () => { Nav.Remove(this); Nav.Push(new ShopScreen()); }, "shop");
        }

        private void WatchAd()
        {
            Ctx.Ads.ShowRewarded(RewardedPlacement.FreeBooster, result =>
            {
                if (result != AdResult.Completed) return;
                AdPolicy.OnRewardedCompleted(Ctx.AdState, RewardedPlacement.FreeBooster, Time.realtimeSinceStartup, Ctx.Today);
                Ctx.Log("ad_reward", new Dictionary<string, object> { { "placement", "booster" } });
                _game.GrantBooster(_type);
                Ctx.Saves.SaveProfile(Ctx.Profile);
                Nav.Remove(this);
                _onDone?.Invoke();
            });
        }
    }

    /// <summary>Mensaje simple con un botón (también usado para "no hay video disponible").</summary>
    public sealed class MessageDialog : DialogBase
    {
        private readonly string _titleKey;
        private readonly string _body;

        public MessageDialog(string titleKey, string body)
        {
            _titleKey = titleKey;
            _body = body;
        }

        protected override string TitleKey => _titleKey;
        protected override float PanelHeight => 640;

        protected override void BuildContent()
        {
            Line(_body, 38, 220);
            Action(Loc.T("common.ok"), FrameStyle.ButtonPrimary, Close);
        }
    }

    /// <summary>Oferta por segmento en un momento de logro. Precio y contenido claros; cerrar es tan fácil como comprar.</summary>
    public sealed class OfferDialog : DialogBase
    {
        private readonly OfferRule _rule;

        public OfferDialog(OfferRule rule) { _rule = rule; }

        protected override string TitleKey => "offer." + _rule.Id + ".title";
        protected override float PanelHeight => 820;

        protected override void BuildContent()
        {
            Line(Loc.T("offer." + _rule.Id + ".body"), 36, 160);
            if (!string.IsNullOrEmpty(_rule.Gift))
            {
                Action(Loc.T("offer.claim_gift"), FrameStyle.ButtonPrimary, ClaimGift, "gift");
                return;
            }
            var product = Ctx.Data.Products.ByKey(_rule.ProductKey);
            string price = Ctx.Store.TryGetPrice(product.Id, out var p) ? p.PriceText : product.BasePriceUsd.ToString("0.00");
            Action(Loc.T("shop.buy_for", price), FrameStyle.ButtonPrimary, () => Shop.Purchase(Ctx, product, ok => { if (ok) Close(); }), "shop");
            Action(Loc.T("common.not_now"), FrameStyle.ButtonSecondary, Dismiss);
            Ctx.Log("offer_show", new Dictionary<string, object> { { "offer", _rule.Id } });
        }

        private void ClaimGift()
        {
            var gift = Reward.FromJson(Ctx.Remote.Get("returnGift"));
            Ctx.Profile.Grant(gift, "gift");
            Ctx.Saves.SaveProfile(Ctx.Profile);
            Ctx.Log("offer_accept", new Dictionary<string, object> { { "offer", _rule.Id } });
            Ctx.Audio?.Play(Sfx.Reward);
            Close();
        }

        private void Dismiss()
        {
            Ctx.Log("offer_dismiss", new Dictionary<string, object> { { "offer", _rule.Id } });
            Close();
        }
    }
}
