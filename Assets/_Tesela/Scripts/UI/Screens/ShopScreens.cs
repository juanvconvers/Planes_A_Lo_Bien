using System;
using System.Collections.Generic;
using Tesela.Core;
using Tesela.Core.Audio;
using Tesela.Core.Raster;
using Tesela.Meta;
using Tesela.Monetization;
using Tesela.Services;
using UnityEngine;

namespace Tesela.UI
{
    /// <summary>Compra y restauración de productos (lógica de entrega en Monetization.Fulfillment).</summary>
    public static class Shop
    {
        public static void Purchase(AppContext ctx, StoreProduct product, Action<bool> done)
        {
            ctx.Log("iap_view", new Dictionary<string, object> { { "product", product.Id } });
            ctx.Store.Purchase(product.Id, (id, status) =>
            {
                bool ok = status == PurchaseStatus.Success;
                if (ok) Deliver(ctx, id);
                else if (status == PurchaseStatus.AlreadyOwned) Fulfillment.Restore(product, ctx.Profile, ctx.UtcNow, ctx.Store.SubscriptionExpiryUtc(id));
                done?.Invoke(ok);
            });
        }

        public static void Deliver(AppContext ctx, string productId)
        {
            var p = ctx.Data.Products.Get(productId);
            if (p == null) return;
            Fulfillment.Apply(p, ctx.Profile, ctx.Data.Products, ctx.UtcNow, ctx.Store.SubscriptionExpiryUtc(productId));
            ctx.Log("iap_purchase", new Dictionary<string, object> { { "product", productId }, { "value_usd", (double)p.BasePriceUsd } });
            ctx.Audio?.Play(Sfx.Reward);
            if (ctx.AdFree) ctx.Ads.HideBanner();
            ctx.SaveAll();
        }

        public static void Restore(AppContext ctx, Action<bool> done)
        {
            ctx.Store.Restore(id => Fulfillment.Restore(ctx.Data.Products.Get(id), ctx.Profile, ctx.UtcNow, ctx.Store.SubscriptionExpiryUtc(id)), ok =>
            {
                ctx.SaveAll();
                done?.Invoke(ok);
            });
        }

        public static string PriceText(AppContext ctx, StoreProduct p) =>
            ctx.Store.TryGetPrice(p.Id, out var price) ? price.PriceText : "US$" + p.BasePriceUsd.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    public sealed class ShopScreen : ScreenBase
    {
        private RectTransform _content;
        private UiText _coins;

        protected override void Build()
        {
            ScreenChrome.Header(this, "nav.shop");
            _coins = Ui.Text(Root, "", 36, Ui.Ink, TextAnchor.MiddleRight, TextRole.Display);
            _coins.Rect.Band(true, 160, 50, 50);
            _content = ScreenChrome.Scroll(Root, 220, 20);
            Fill();
        }

        public override void Refresh()
        {
            for (int i = _content.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_content.GetChild(i).gameObject);
            Fill();
        }

        private void Fill()
        {
            _coins.Text = Loc.T("shop.balance", Loc.Number(Ctx.Profile.Coins));
            float y = 10;
            var products = Ctx.Data.Products;

            if (!Ctx.Profile.NoAds) y = Card(y, products.ByKey("no_ads"), "shop.no_ads", "ad");
            if (!Ctx.Profile.StarterPackBought) y = Card(y, products.ByKey("starter"), "shop.starter", "gift");
            y = Card(y, products.ByKey("piggy"), "shop.piggy", "chest", Loc.T("shop.piggy_value", Loc.Number(Fulfillment.PiggyValue(Ctx.Profile, products))));
            y = Card(y, products.ByKey("club"), "shop.club", "crown", Loc.T("shop.club_terms", Shop.PriceText(Ctx, products.ByKey("club"))));

            y = Section(y, "shop.coins");
            for (int i = 1; i <= 6; i++)
            {
                var p = products.ByKey("coins_" + i);
                string bonus = p.BonusPercent > 0 ? Loc.T("shop.bonus", p.BonusPercent) : null;
                y = Card(y, p, null, "coin", bonus, Loc.T("shop.coins_amount", Loc.Number(p.Content.Coins)));
            }
            y = Section(y, "shop.boosters");
            foreach (string key in new[] { "boosters_small", "boosters_medium", "boosters_large" }) y = Card(y, products.ByKey(key), "shop." + key, "hammer");

            y = Section(y, "shop.coin_spend");
            foreach (BoosterType t in Enum.GetValues(typeof(BoosterType)))
            {
                var type = t;
                y = CoinRow(y, "booster." + Boosters.Key(t) + ".name", Ctx.Data.Economy.BoosterPrice(t), () => Ctx.Meta.BuyBooster(type));
            }
            y = CoinRow(y, "shop.protector", Ctx.Data.Economy.ProtectorPrice, Ctx.Meta.BuyProtector);
            y = CoinRow(y, "shop.chest", Ctx.Data.Economy.ChestPriceCoins, () => { Nav.Push(new ChestDialog()); return false; });
            foreach (var theme in Ctx.Data.Economy.Themes)
            {
                if (theme.PriceCoins <= 0 || theme.ClubOnly || Ctx.Profile.OwnedCosmetics.Contains(theme.Id)) continue;
                string id = theme.Id;
                y = CoinRow(y, "theme." + theme.Id, theme.PriceCoins, () => Ctx.Meta.BuyTheme(id, Ctx.UtcNow));
            }

            var restore = Ui.Button(_content, Loc.T("shop.restore"), FrameStyle.ButtonSecondary, () => Shop.Restore(Ctx, ok =>
                Nav.Push(new MessageDialog("shop.restore", Loc.T(ok ? "shop.restore_ok" : "shop.restore_fail")))), "restore", 38);
            ((RectTransform)restore.transform).Band(true, y + 20, 110, 60);
            var manage = Ui.Button(_content, Loc.T("shop.manage_subs"), FrameStyle.ButtonSecondary,
                () => Application.OpenURL("https://play.google.com/store/account/subscriptions?package=" + ExternalIds.Instance.androidPackageName), "info", 34);
            ((RectTransform)manage.transform).Band(true, y + 150, 110, 60);
            _content.sizeDelta = new Vector2(0, y + 320);
        }

        private float Section(float y, string key)
        {
            var t = Ui.Text(_content, Loc.T(key), 44, Ui.Ink, TextAnchor.MiddleLeft, TextRole.Display);
            t.Rect.Band(true, y + 20, 70, 50);
            return y + 100;
        }

        private float Card(float y, StoreProduct p, string titleKey, string icon, string note = null, string titleOverride = null)
        {
            if (p == null) return y;
            var card = Ui.Panel(_content, p.Id);
            card.rectTransform.Band(true, y, 190, 40);
            Ui.Icon(card.transform, icon, 80, Ui.Ink).rectTransform.Place(0, 0.5f, 80, 80, 80, 0);
            var title = Ui.Text(card.transform, titleOverride ?? Loc.T(titleKey), 38, Ui.Ink, TextAnchor.UpperLeft, TextRole.Display);
            title.Rect.Stretch(150, 70, 300, 30);
            if (titleKey != null || note != null)
            {
                var desc = Ui.Text(card.transform, note ?? Loc.T(titleKey + ".desc"), 28, Ui.InkSoft, TextAnchor.UpperLeft);
                desc.Rect.Stretch(150, 14, 300, 90);
            }
            var product = p;
            var buy = Ui.Button(card.transform, Shop.PriceText(Ctx, p), FrameStyle.ButtonPrimary, () => Shop.Purchase(Ctx, product, ok => Refresh()), null, 36);
            ((RectTransform)buy.transform).Place(1, 0.5f, 240, 110, -150, 0);
            return y + 210;
        }

        private float CoinRow(float y, string key, int price, Func<bool> buy)
        {
            var row = Ui.Panel(_content, key, FrameStyle.Chip);
            row.rectTransform.Band(true, y, 120, 40);
            var t = Ui.Text(row.transform, Loc.T(key), 34, Ui.Ink, TextAnchor.MiddleLeft);
            t.Rect.Stretch(40, 0, 300, 0);
            var b = Ui.Button(row.transform, Loc.Number(price), FrameStyle.ButtonSecondary, () =>
            {
                if (buy())
                {
                    Ctx.Audio?.Play(Sfx.Coin);
                    Ctx.Saves.SaveProfile(Ctx.Profile);
                    Refresh();
                }
            }, "coin", 32);
            ((RectTransform)b.transform).Place(1, 0.5f, 240, 90, -150, 0);
            b.SetInteractable(Ctx.Profile.Coins >= price);
            return y + 136;
        }
    }

    /// <summary>Cofre: solo con monedas ganadas jugando. Las probabilidades se muestran antes de abrir.</summary>
    public sealed class ChestDialog : DialogBase
    {
        protected override string TitleKey => "chest.title";
        protected override float PanelHeight => 1180;

        protected override void BuildContent()
        {
            Line(Loc.T("chest.odds"), 34, 60, Ui.InkSoft);
            foreach (var e in Ctx.Data.Economy.Chest)
            {
                float pct = Ctx.Data.Economy.ChestProbability(e) * 100f;
                Line(Loc.T("chest." + e.Id) + " — " + pct.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " %", 32, 56);
            }
            Space(20);
            var open = Action(Loc.T("chest.open", Ctx.Data.Economy.ChestPriceCoins), FrameStyle.ButtonPrimary, Open, "chest");
            open.SetInteractable(Ctx.Profile.Coins >= Ctx.Data.Economy.ChestPriceCoins);
        }

        private void Open()
        {
            var rng = new Pcg32((ulong)Ctx.UtcNow.Ticks);
            var entry = Ctx.Meta.OpenChest(ref rng, true);
            if (entry == null) return;
            Ctx.Saves.SaveProfile(Ctx.Profile);
            Ctx.Audio?.Play(Sfx.Reward);
            Close();
            Nav.Push(new MessageDialog("chest.title", Loc.T("chest.got", Loc.T("chest." + entry.Id))));
        }
    }

    /// <summary>Pase de temporada: carril gratis y premium, días restantes visibles.</summary>
    public sealed class PassScreen : ScreenBase
    {
        private RectTransform _content;

        protected override void Build()
        {
            ScreenChrome.Header(this, "nav.pass");
            _content = ScreenChrome.Scroll(Root, 200, 20);
            Fill();
        }

        public override void Refresh()
        {
            for (int i = _content.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_content.GetChild(i).gameObject);
            Fill();
        }

        private void Fill()
        {
            var meta = Ctx.Meta;
            int tier = meta.PassTier();
            var info = Ui.Text(_content, Loc.T("pass.info", tier, Ctx.Data.Economy.PassTiers.Count, meta.SeasonDaysLeft(Ctx.Today)), 36, Ui.Ink);
            info.Rect.Band(true, 10, 70, 40);
            float y = 100;
            if (!Ctx.Profile.PassPremium)
            {
                var pass = Ctx.Data.Products.ByKey("pass");
                var plus = Ctx.Data.Products.ByKey("pass_plus");
                var b1 = Ui.Button(_content, Loc.T("pass.buy", Shop.PriceText(Ctx, pass)), FrameStyle.ButtonPrimary, () => Shop.Purchase(Ctx, pass, _ => Refresh()), "pass", 38);
                ((RectTransform)b1.transform).Band(true, y, 120, 50);
                var b2 = Ui.Button(_content, Loc.T("pass.buy_plus", Shop.PriceText(Ctx, plus)), FrameStyle.ButtonGold, () => Shop.Purchase(Ctx, plus, _ => Refresh()), "crown", 34);
                ((RectTransform)b2.transform).Band(true, y + 140, 120, 50);
                y += 290;
            }
            for (int t = 0; t < Ctx.Data.Economy.PassTiers.Count; t++)
            {
                var row = Ui.Panel(_content, "tier" + t, t < tier ? FrameStyle.Panel : FrameStyle.Chip);
                row.rectTransform.Band(true, y, 120, 40);
                var num = Ui.Text(row.transform, (t + 1).ToString(), 44, Ui.Ink, TextAnchor.MiddleCenter, TextRole.Display);
                num.Rect.Place(0, 0.5f, 100, 100, 70, 0);
                Lane(row.rectTransform, t, false, 0.18f);
                Lane(row.rectTransform, t, true, 0.58f);
                y += 132;
            }
            _content.sizeDelta = new Vector2(0, y + 100);
        }

        private void Lane(RectTransform row, int tier, bool premium, float anchorX)
        {
            var def = Ctx.Data.Economy.PassTiers[tier];
            var reward = premium ? def.Premium : def.Free;
            string text = RewardText(reward);
            bool can = Ctx.Meta.CanClaimPass(tier, premium);
            bool claimed = premium ? Ctx.Profile.PassClaimedPremium.Contains(tier) : Ctx.Profile.PassClaimedFree.Contains(tier);
            var b = Ui.Button(row, claimed ? Loc.T("pass.claimed") : text, can ? FrameStyle.ButtonGold : FrameStyle.ButtonSecondary, () =>
            {
                if (Ctx.Meta.ClaimPass(tier, premium, out _)) { Ctx.Audio?.Play(Sfx.Reward); Ctx.Saves.SaveProfile(Ctx.Profile); Refresh(); }
            }, premium && !Ctx.Profile.PassPremium ? "lock" : null, 28);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(anchorX, 0.1f);
            rt.anchorMax = new Vector2(anchorX + 0.38f, 0.9f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            b.SetInteractable(can);
        }

        public static string RewardText(Reward r)
        {
            var parts = new List<string>();
            if (r.Coins > 0) parts.Add(Loc.T("reward.coins", r.Coins));
            if (r.Protectors > 0) parts.Add(Loc.T("reward.protectors", r.Protectors));
            if (r.Tiles > 0) parts.Add(Loc.T("reward.tiles", r.Tiles));
            if (r.Boosters != null)
                for (int i = 0; i < r.Boosters.Length; i++)
                    if (r.Boosters[i] > 0) parts.Add(r.Boosters[i] + "× " + Loc.T("booster." + Boosters.Key((BoosterType)i) + ".name"));
            if (!string.IsNullOrEmpty(r.Cosmetic)) parts.Add(Loc.T("reward.cosmetic"));
            return string.Join(" · ", parts);
        }
    }
}
