using System;
using System.Linq;
using NUnit.Framework;
using Tesela.Core;
using Tesela.Meta;
using Tesela.Monetization;

namespace Tesela.Tests
{
    public class AdPolicyTests
    {
        private static readonly DateTime Now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);
        private static AdConfig Cfg => AdConfig.FromJson(Json.ParseNode(TestData.Config("RemoteDefaults.json"))["ads"]);

        /// <summary>Estado de un jugador veterano listo para ver un intersticial.</summary>
        private static AdState Ready()
        {
            var s = new AdState();
            s.StartSession(0);
            s.StartSession(0);
            for (int i = 0; i < 5; i++) s.OnRunFinished();
            return s;
        }

        private static AdDecision Check(AdState s, double t, InterstitialPlacement p = InterstitialPlacement.ResultsToMenu, bool adFree = false, long purchase = 0)
            => AdPolicy.CanShowInterstitial(Cfg, s, p, adFree, t, Now, purchase);

        [Test]
        public void DefaultsMatchSpec()
        {
            var c = Cfg;
            Assert.AreEqual(20, c.RewardedDailyCap);
            Assert.AreEqual(5, c.FreeBoosterDailyCap);
            Assert.AreEqual(3, c.MinGamesBeforeFirstInterstitial);
            Assert.AreEqual(150, c.MinSecondsBetweenInterstitials);
            Assert.AreEqual(2, c.MinGamesBetweenInterstitials);
            Assert.AreEqual(120, c.CooldownAfterRewardedSeconds);
            Assert.AreEqual(24, c.NoInterstitialHoursAfterPurchase);
            Assert.AreEqual(5, c.SdkTimeoutSeconds);
            Assert.AreEqual(20, AdConfig.FromJson(default).RewardedDailyCap);
        }

        [Test]
        public void AllowedOnlyAtResultsTransitions()
        {
            var s = Ready();
            Assert.AreEqual(AdDecision.Allowed, Check(s, 200));
            Assert.AreEqual(AdDecision.Allowed, Check(s, 200, InterstitialPlacement.ResultsToNewGame));
            Assert.AreEqual(AdDecision.WrongPlacement, Check(s, 200, InterstitialPlacement.LevelStart));
            Assert.AreEqual(AdDecision.WrongPlacement, Check(s, 200, InterstitialPlacement.MidGame));
            Assert.AreEqual(AdDecision.WrongPlacement, Check(s, 200, InterstitialPlacement.AppOpen));
        }

        [Test]
        public void NeverInFirstSession()
        {
            var s = new AdState();
            s.StartSession(0);
            for (int i = 0; i < 10; i++) s.OnRunFinished();
            Assert.AreEqual(AdDecision.FirstSession, Check(s, 10000));
        }

        [Test]
        public void NeverInFirstThreeGames()
        {
            var s = new AdState();
            s.StartSession(0);
            s.StartSession(0);
            s.OnRunFinished(); s.OnRunFinished();
            Assert.AreEqual(AdDecision.TooFewGames, Check(s, 10000));
            s.OnRunFinished();
            Assert.AreEqual(AdDecision.Allowed, Check(s, 10000));
        }

        [Test]
        public void InitialWaitIs150SecondsFromSessionStart()
        {
            var s = Ready();
            s.SessionStartRealtime = 100;
            Assert.AreEqual(AdDecision.TooSoon, Check(s, 249));
            Assert.AreEqual(AdDecision.Allowed, Check(s, 250));
        }

        [Test]
        public void WaitAndGamesBetweenInterstitials()
        {
            var s = Ready();
            AdPolicy.OnInterstitialShown(s, 300);
            s.OnRunFinished();
            Assert.AreEqual(AdDecision.TooSoon, Check(s, 400));
            Assert.AreEqual(AdDecision.TooFewGames, Check(s, 500));
            s.OnRunFinished();
            Assert.AreEqual(AdDecision.Allowed, Check(s, 500));
        }

        [Test]
        public void NotWithin120SecondsOfRewarded()
        {
            var s = Ready();
            AdPolicy.OnRewardedCompleted(s, RewardedPlacement.DoubleCoins, 1000, 1);
            Assert.AreEqual(AdDecision.AfterRewarded, Check(s, 1119));
            Assert.AreEqual(AdDecision.Allowed, Check(s, 1120));
        }

        [Test]
        public void NoneFor24HoursAfterPurchase()
        {
            var s = Ready();
            Assert.AreEqual(AdDecision.AfterPurchase, Check(s, 500, purchase: Now.AddHours(-23).Ticks));
            Assert.AreEqual(AdDecision.Allowed, Check(s, 500, purchase: Now.AddHours(-25).Ticks));
        }

        [Test]
        public void NoAdsBuyersNeverSeeInterstitialsOrBanners()
        {
            var s = Ready();
            Assert.AreEqual(AdDecision.AdFree, Check(s, 10000, adFree: true));
            Assert.IsFalse(AdPolicy.ShowBanner(Cfg, true, false, true));
            Assert.IsTrue(AdPolicy.ShowBanner(Cfg, true, false, false));
            Assert.IsFalse(AdPolicy.ShowBanner(Cfg, false, false, false), "nunca sobre el tablero");
            Assert.IsFalse(AdPolicy.ShowBanner(Cfg, true, true, false), "nunca en Zen");
        }

        [Test]
        public void RewardedCapsAndOncePerRunRevive()
        {
            var c = Cfg;
            var s = new AdState();
            s.OnRunStarted();
            Assert.AreEqual(AdDecision.Allowed, AdPolicy.CanOfferRewarded(c, s, RewardedPlacement.Revive, 1));
            AdPolicy.OnRewardedCompleted(s, RewardedPlacement.Revive, 0, 1);
            Assert.AreEqual(AdDecision.AlreadyUsed, AdPolicy.CanOfferRewarded(c, s, RewardedPlacement.Revive, 1));
            s.OnRunStarted();
            Assert.AreEqual(AdDecision.Allowed, AdPolicy.CanOfferRewarded(c, s, RewardedPlacement.Revive, 1));

            for (int i = 0; i < 5; i++) AdPolicy.OnRewardedCompleted(s, RewardedPlacement.FreeBooster, 0, 1);
            Assert.AreEqual(AdDecision.DailyCap, AdPolicy.CanOfferRewarded(c, s, RewardedPlacement.FreeBooster, 1));
            AdPolicy.OnRewardedCompleted(s, RewardedPlacement.DoubleDailyChest, 0, 1);
            Assert.AreEqual(AdDecision.AlreadyUsed, AdPolicy.CanOfferRewarded(c, s, RewardedPlacement.DoubleDailyChest, 1));
            while (s.RewardedToday < c.RewardedDailyCap) AdPolicy.OnRewardedCompleted(s, RewardedPlacement.DoubleCoins, 0, 1);
            Assert.AreEqual(AdDecision.DailyCap, AdPolicy.CanOfferRewarded(c, s, RewardedPlacement.DoubleCoins, 1));
            Assert.AreEqual(AdDecision.Allowed, AdPolicy.CanOfferRewarded(c, s, RewardedPlacement.DoubleCoins, 2), "el tope se reinicia al día siguiente");
        }

        [Test]
        public void AdStateRoundTrips()
        {
            var s = Ready();
            AdPolicy.OnInterstitialShown(s, 10);
            string a = Json.Serialize(s.ToJson());
            Assert.AreEqual(a, Json.Serialize(AdState.FromJson(Json.ParseNode(a)).ToJson()));
        }

        [Test]
        public void FakeAdServiceAlwaysCallsBack()
        {
            var ads = new FakeAdService();
            AdResult result = AdResult.Failed;
            ads.ShowRewarded(RewardedPlacement.Revive, r => result = r);
            Assert.AreEqual(AdResult.NotAvailable, result, "sin inicializar: se dice claramente que no hay video");
            ads.Initialize(false);
            ads.ShowRewarded(RewardedPlacement.Revive, r => result = r);
            Assert.AreEqual(AdResult.Completed, result);
            ads.ShowInterstitial(r => result = r);
            Assert.AreEqual(1, ads.InterstitialsShown);
            ads.ShowBanner();
            Assert.IsTrue(ads.BannerVisible);
            ads.HideBanner();
            Assert.IsFalse(ads.BannerVisible);
            Assert.IsTrue(ads.IsRewardedReady && ads.IsInterstitialReady && !ads.PersonalizedAllowed);
            var consent = new FakeConsentService();
            consent.RequestIfNeeded(st => Assert.AreEqual(ConsentStatus.Obtained, st));
            bool closed = false;
            consent.ShowPrivacyOptions(() => closed = true);
            Assert.IsTrue(closed);
        }
    }

    public class StoreTests
    {
        private static readonly DateTime Now = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);
        private static ProductCatalog Catalog => ProductCatalog.FromJson(TestData.Config("Products.json"));

        [Test]
        public void CatalogMatchesSpec()
        {
            var c = Catalog;
            Assert.AreEqual(3.99m, c.ByKey("no_ads").BasePriceUsd);
            Assert.AreEqual(ProductType.NonConsumable, c.ByKey("no_ads").Type);
            Assert.AreEqual(1.99m, c.ByKey("starter").BasePriceUsd);
            Assert.AreEqual(48, c.ByKey("starter").OfferHours);
            Assert.AreEqual(2.99m, c.ByKey("piggy").BasePriceUsd);
            Assert.AreEqual(4.99m, c.ByKey("pass").BasePriceUsd);
            Assert.AreEqual(9.99m, c.ByKey("pass_plus").BasePriceUsd);
            Assert.AreEqual(ProductType.Subscription, c.ByKey("club").Type);
            var coins = c.Products.Where(p => p.Key.StartsWith("coins_")).ToList();
            Assert.AreEqual(6, coins.Count);
            Assert.AreEqual(0.99m, coins.Min(p => p.BasePriceUsd));
            Assert.AreEqual(99.99m, coins.Max(p => p.BasePriceUsd));
            Assert.AreEqual(50, coins.Max(p => p.BonusPercent));
            Assert.IsNotNull(c.Get("tesela_no_ads"));
            Assert.Throws<FormatException>(() => ProductCatalog.FromJson("{\"products\":[{\"id\":\"a\",\"priceUsd\":1},{\"id\":\"a\",\"priceUsd\":1}]}"));
            Assert.Throws<FormatException>(() => ProductCatalog.FromJson("{\"products\":[{\"id\":\"a\",\"priceUsd\":0}]}"));
        }

        [Test]
        public void EachProductDeliversItsContent()
        {
            var c = Catalog;
            var p = new PlayerProfile();
            Assert.IsTrue(Fulfillment.Apply(c.ByKey("no_ads"), p, c, Now));
            Assert.IsTrue(p.NoAds);
            Assert.AreEqual(3, p.Protectors);
            Assert.IsTrue(p.AdFree(Now));

            Fulfillment.Apply(c.ByKey("starter"), p, c, Now);
            Assert.AreEqual(500, p.Coins);
            Assert.IsTrue(p.StarterPackBought);
            Assert.IsTrue(p.OwnedCosmetics.Contains("theme_cobalto_antiguo"));
            Assert.AreEqual(1, p.Boosters[(int)BoosterType.Broom]);

            p.PiggyCoins = 9999;
            Assert.AreEqual(c.PiggyCapacity, Fulfillment.PiggyValue(p, c));
            Fulfillment.Apply(c.ByKey("piggy"), p, c, Now);
            Assert.AreEqual(500 + c.PiggyCapacity, p.Coins);
            Assert.AreEqual(0, p.PiggyCoins);

            Fulfillment.Apply(c.ByKey("pass"), p, c, Now);
            Assert.IsTrue(p.PassPremium);
            Assert.IsFalse(p.PassPlusActive);
            Fulfillment.Apply(c.ByKey("pass_plus"), p, c, Now);
            Assert.IsTrue(p.PassPlusActive);
            Assert.AreEqual(4500, p.PassXp);

            Fulfillment.Apply(c.ByKey("club"), p, c, Now);
            Assert.IsTrue(p.ClubActive(Now.AddDays(29)));
            Fulfillment.Apply(c.ByKey("coins_3"), p, c, Now);
            Fulfillment.Apply(c.ByKey("boosters_large"), p, c, Now);
            Assert.AreEqual(1 + 8, p.Boosters[(int)BoosterType.Undo]);
            Assert.AreEqual(8, p.PurchaseCount);
            Assert.AreEqual(Now.Ticks, p.LastPurchaseUtcTicks);
            Assert.That(p.TotalSpentUsd, Is.GreaterThan(30));
            Assert.IsFalse(Fulfillment.Apply(null, p, c, Now));
        }

        [Test]
        public void FakeStorePurchaseRestoreAndSubscriptions()
        {
            var c = Catalog;
            var store = new FakeStoreService { Now = () => Now };
            bool ready = false;
            store.Initialize(c.Products, ok => ready = ok);
            Assert.IsTrue(ready && store.IsInitialized);
            Assert.IsTrue(store.TryGetPrice("tesela_no_ads", out var price));
            StringAssert.Contains("3.99", price.PriceText);
            Assert.IsFalse(store.TryGetPrice("nope", out _));

            PurchaseStatus status = PurchaseStatus.Failed;
            store.Purchase("tesela_no_ads", (id, st) => status = st);
            Assert.AreEqual(PurchaseStatus.Success, status);
            store.Purchase("tesela_no_ads", (id, st) => status = st);
            Assert.AreEqual(PurchaseStatus.AlreadyOwned, status);
            store.Purchase("nope", (id, st) => status = st);
            Assert.AreEqual(PurchaseStatus.Failed, status);
            store.NextResult = PurchaseStatus.Cancelled;
            store.Purchase("tesela_coins_1", (id, st) => status = st);
            Assert.AreEqual(PurchaseStatus.Cancelled, status);
            store.NextResult = PurchaseStatus.Success;
            store.Purchase("tesela_club_monthly", (id, st) => status = st);
            Assert.IsTrue(store.SubscriptionExpiryUtc("tesela_club_monthly").HasValue);

            var restored = new System.Collections.Generic.List<string>();
            store.Restore(id => restored.Add(id), ok => Assert.IsTrue(ok));
            CollectionAssert.AreEquivalent(new[] { "tesela_no_ads", "tesela_club_monthly" }, restored);

            var fresh = new PlayerProfile();
            foreach (string id in restored) Fulfillment.Restore(c.Get(id), fresh, Now, store.SubscriptionExpiryUtc(id));
            Assert.IsTrue(fresh.NoAds);
            Assert.IsTrue(fresh.ClubActive(Now));
            Assert.AreEqual(0, fresh.PurchaseCount, "restaurar no cuenta como compra nueva");
            Fulfillment.Restore(null, fresh, Now, null);
            string deferred = null;
            store.DeferredPurchase += id => deferred = id;
            store.SimulateDeferred("tesela_coins_2");
            Assert.AreEqual("tesela_coins_2", deferred);
        }
    }

    public class OfferTests
    {
        private static OfferEngine Engine() => OfferEngine.FromJson(Json.ParseNode(TestData.Config("RemoteDefaults.json"))["offers"]);

        [Test]
        public void SegmentsComputedOnDevice()
        {
            var p = new PlayerProfile { FirstDay = 100 };
            Assert.AreEqual(Segment.New, Segmentation.Compute(p, 102, 101));
            Assert.AreEqual(Segment.EngagedNonPayer, Segmentation.Compute(p, 110, 109));
            Assert.AreEqual(Segment.AtRisk, Segmentation.Compute(p, 110, 104));
            p.PurchaseCount = 1;
            Assert.AreEqual(Segment.FirstPayer, Segmentation.Compute(p, 110, 109));
            p.PurchaseCount = 3;
            Assert.AreEqual(Segment.Repeat, Segmentation.Compute(p, 110, 109));
            p.TotalSpentUsd = 60;
            Assert.AreEqual(Segment.Whale, Segmentation.Compute(p, 110, 109));
            foreach (Segment s in Enum.GetValues(typeof(Segment))) Assert.IsNotEmpty(Segmentation.Key(s));
        }

        [Test]
        public void OneOfferPerSessionNeverBeforeSecondGameNeverOnDefeat()
        {
            var e = Engine();
            var p = new PlayerProfile { FirstDay = 0 };
            e.StartSession();
            Assert.IsNull(e.Pick(OfferMoment.NewBest, Segment.EngagedNonPayer, p, 1, 10, null));
            Assert.IsNull(e.Pick(OfferMoment.GameOver, Segment.EngagedNonPayer, p, 5, 10, null));
            var rule = e.Pick(OfferMoment.NewBest, Segment.EngagedNonPayer, p, 2, 10, null);
            Assert.AreEqual("starter", rule.ProductKey);
            e.MarkShown();
            Assert.IsNull(e.Pick(OfferMoment.NewBest, Segment.EngagedNonPayer, p, 3, 10, null));
            e.StartSession();
            Assert.IsNull(e.Pick(OfferMoment.NewBest, Segment.EngagedNonPayer, p, 3, 10, key => key != "starter"));
            Assert.AreEqual("return_gift", e.Pick(OfferMoment.ReturnAfterAbsence, Segment.AtRisk, p, 0, 10, null).Gift);
            Assert.IsNull(e.Pick(OfferMoment.NewBest, Segment.EngagedNonPayer, new PlayerProfile { FirstDay = 9 }, 3, 10, null), "edad mínima");
            foreach (OfferMoment m in Enum.GetValues(typeof(OfferMoment))) Assert.IsNotEmpty(OfferEngine.MomentKey(m));
        }
    }
}
