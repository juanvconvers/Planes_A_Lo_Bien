using System;
using System.Collections.Generic;

namespace Tesela.Monetization
{
    public enum AdResult
    {
        Completed = 0,
        Skipped = 1,
        NotAvailable = 2,
        Failed = 3,
        TimedOut = 4
    }

    /// <summary>Anuncios. Toda implementación debe llamar al callback exactamente una vez, aunque el SDK falle.</summary>
    public interface IAdService
    {
        bool IsInitialized { get; }
        void Initialize(bool personalizedAllowed);
        bool IsRewardedReady { get; }
        bool IsInterstitialReady { get; }
        void ShowRewarded(RewardedPlacement placement, Action<AdResult> onDone);
        void ShowInterstitial(Action<AdResult> onDone);
        void ShowBanner();
        void HideBanner();
    }

    public enum PurchaseStatus
    {
        Success = 0,
        Cancelled = 1,
        Failed = 2,
        Pending = 3,
        AlreadyOwned = 4
    }

    public struct LocalizedPrice
    {
        public string ProductId;
        public string PriceText;
        public decimal Price;
        public string IsoCurrency;
    }

    public interface IStoreService
    {
        bool IsInitialized { get; }
        void Initialize(IReadOnlyList<StoreProduct> products, Action<bool> onReady);
        bool TryGetPrice(string productId, out LocalizedPrice price);
        void Purchase(string productId, Action<string, PurchaseStatus> onDone);
        /// <summary>Restaurar compras (no consumibles y suscripciones). Llama a onRestored por cada producto con recibo válido.</summary>
        void Restore(Action<string> onRestored, Action<bool> onDone);
        /// <summary>Vencimiento de la suscripción según el recibo de Play (UTC). null si no hay suscripción activa.</summary>
        DateTime? SubscriptionExpiryUtc(string productId);
        /// <summary>Compras que llegan sin una solicitud en curso (pendientes aprobadas más tarde, reinicios a mitad de compra). Hay que entregarlas igual.</summary>
        event Action<string> DeferredPurchase;
    }

    public enum ConsentStatus
    {
        Unknown = 0,
        NotRequired = 1,
        Obtained = 2
    }

    /// <summary>CMP certificada (IAB TCF v2.3). Se pide antes de cargar anuncios y nunca bloquea el tutorial.</summary>
    public interface IConsentService
    {
        ConsentStatus Status { get; }
        bool PersonalizedAdsAllowed { get; }
        void RequestIfNeeded(Action<ConsentStatus> onDone);
        void ShowPrivacyOptions(Action onClosed);
    }

    /// <summary>Anuncios simulados para el editor y para jugar sin IDs: completa al instante y nunca castiga.</summary>
    public sealed class FakeAdService : IAdService
    {
        public AdResult NextRewardedResult = AdResult.Completed;
        public AdResult NextInterstitialResult = AdResult.Completed;
        public int RewardedShown { get; private set; }
        public int InterstitialsShown { get; private set; }
        public bool BannerVisible { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool PersonalizedAllowed { get; private set; }

        public void Initialize(bool personalizedAllowed)
        {
            IsInitialized = true;
            PersonalizedAllowed = personalizedAllowed;
        }

        public bool IsRewardedReady => IsInitialized && NextRewardedResult != AdResult.NotAvailable;
        public bool IsInterstitialReady => IsInitialized;

        public void ShowRewarded(RewardedPlacement placement, Action<AdResult> onDone)
        {
            if (NextRewardedResult == AdResult.Completed) RewardedShown++;
            onDone?.Invoke(IsInitialized ? NextRewardedResult : AdResult.NotAvailable);
        }

        public void ShowInterstitial(Action<AdResult> onDone)
        {
            InterstitialsShown++;
            onDone?.Invoke(NextInterstitialResult);
        }

        public void ShowBanner() => BannerVisible = true;
        public void HideBanner() => BannerVisible = false;
    }

    public sealed class FakeStoreService : IStoreService
    {
        private readonly Dictionary<string, StoreProduct> _products = new Dictionary<string, StoreProduct>();
        private readonly HashSet<string> _owned = new HashSet<string>();
        private readonly Dictionary<string, DateTime> _subscriptions = new Dictionary<string, DateTime>();
        public PurchaseStatus NextResult = PurchaseStatus.Success;
        public Func<DateTime> Now = () => DateTime.UtcNow;
        public bool IsInitialized { get; private set; }
        public event Action<string> DeferredPurchase;

        /// <summary>Simula una compra pendiente que se aprueba más tarde.</summary>
        public void SimulateDeferred(string productId) => DeferredPurchase?.Invoke(productId);

        public void Initialize(IReadOnlyList<StoreProduct> products, Action<bool> onReady)
        {
            foreach (var p in products) _products[p.Id] = p;
            IsInitialized = true;
            onReady?.Invoke(true);
        }

        public bool TryGetPrice(string productId, out LocalizedPrice price)
        {
            price = default;
            if (!_products.TryGetValue(productId, out var p)) return false;
            price = new LocalizedPrice
            {
                ProductId = productId,
                Price = p.BasePriceUsd,
                IsoCurrency = "USD",
                PriceText = "US$" + p.BasePriceUsd.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            };
            return true;
        }

        public void Purchase(string productId, Action<string, PurchaseStatus> onDone)
        {
            if (!_products.TryGetValue(productId, out var p)) { onDone?.Invoke(productId, PurchaseStatus.Failed); return; }
            if (p.Type == ProductType.NonConsumable && _owned.Contains(productId)) { onDone?.Invoke(productId, PurchaseStatus.AlreadyOwned); return; }
            var result = NextResult;
            if (result == PurchaseStatus.Success)
            {
                if (p.Type == ProductType.NonConsumable) _owned.Add(productId);
                if (p.Type == ProductType.Subscription) _subscriptions[productId] = Now().AddDays(Math.Max(1, p.PeriodDays));
            }
            onDone?.Invoke(productId, result);
        }

        public void Restore(Action<string> onRestored, Action<bool> onDone)
        {
            foreach (string id in _owned) onRestored?.Invoke(id);
            foreach (var kv in _subscriptions) if (kv.Value > Now()) onRestored?.Invoke(kv.Key);
            onDone?.Invoke(true);
        }

        public DateTime? SubscriptionExpiryUtc(string productId) =>
            _subscriptions.TryGetValue(productId, out var d) && d > Now() ? d : (DateTime?)null;
    }

    public sealed class FakeConsentService : IConsentService
    {
        public ConsentStatus Status { get; private set; } = ConsentStatus.Unknown;
        public bool PersonalizedAdsAllowed { get; set; }

        public void RequestIfNeeded(Action<ConsentStatus> onDone)
        {
            if (Status == ConsentStatus.Unknown) Status = ConsentStatus.Obtained;
            onDone?.Invoke(Status);
        }

        public void ShowPrivacyOptions(Action onClosed) => onClosed?.Invoke();
    }
}
