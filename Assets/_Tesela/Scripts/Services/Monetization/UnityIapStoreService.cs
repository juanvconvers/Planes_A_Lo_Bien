#if TESELA_IAP
using System;
using System.Collections.Generic;
using Tesela.Monetization;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using IapProductType = UnityEngine.Purchasing.ProductType;
using StoreProduct = Tesela.Monetization.StoreProduct;

namespace Tesela.Services
{
    /// <summary>Adaptador Unity IAP sobre Google Play Billing. Sin verificar en editor: ver QA_PENDIENTE.md.</summary>
    public sealed class UnityIapStoreService : IStoreService, IDetailedStoreListener
    {
        private IStoreController _controller;
        private IExtensionProvider _extensions;
        private Action<bool> _onReady;
        private Action<string, PurchaseStatus> _onPurchase;

        public bool IsInitialized => _controller != null;
        public event Action<string> DeferredPurchase;

        public void Initialize(IReadOnlyList<StoreProduct> products, Action<bool> onReady)
        {
            _onReady = onReady;
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var p in products)
            {
                var type = p.Type == Tesela.Monetization.ProductType.Subscription ? IapProductType.Subscription
                         : p.Type == Tesela.Monetization.ProductType.NonConsumable ? IapProductType.NonConsumable
                         : IapProductType.Consumable;
                builder.AddProduct(p.Id, type);
            }
            UnityPurchasing.Initialize(this, builder);
        }

        public bool TryGetPrice(string productId, out LocalizedPrice price)
        {
            price = default;
            var p = _controller?.products.WithID(productId);
            if (p == null || !p.availableToPurchase) return false;
            price = new LocalizedPrice
            {
                ProductId = productId,
                PriceText = p.metadata.localizedPriceString,
                Price = p.metadata.localizedPrice,
                IsoCurrency = p.metadata.isoCurrencyCode
            };
            return true;
        }

        public void Purchase(string productId, Action<string, PurchaseStatus> onDone)
        {
            if (_controller == null) { onDone?.Invoke(productId, PurchaseStatus.Failed); return; }
            _onPurchase = onDone;
            _controller.InitiatePurchase(productId);
        }

        public void Restore(Action<string> onRestored, Action<bool> onDone)
        {
            if (_extensions == null) { onDone?.Invoke(false); return; }
            _extensions.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions((ok, _) =>
            {
                if (ok && _controller != null)
                    foreach (var p in _controller.products.all)
                        if (p.hasReceipt && p.definition.type != IapProductType.Consumable) onRestored?.Invoke(p.definition.id);
                onDone?.Invoke(ok);
            });
        }

        public DateTime? SubscriptionExpiryUtc(string productId)
        {
            var p = _controller?.products.WithID(productId);
            if (p == null || !p.hasReceipt) return null;
            try
            {
                var info = new SubscriptionManager(p, null).getSubscriptionInfo();
                return info.isSubscribed() == Result.True ? info.getExpireDate() : (DateTime?)null;
            }
            catch (Exception) { return null; }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            _onReady?.Invoke(true);
        }

        public void OnInitializeFailed(InitializationFailureReason error) => _onReady?.Invoke(false);
        public void OnInitializeFailed(InitializationFailureReason error, string message) => _onReady?.Invoke(false);

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            var cb = _onPurchase;
            _onPurchase = null;
            if (cb != null) cb(e.purchasedProduct.definition.id, PurchaseStatus.Success);
            else DeferredPurchase?.Invoke(e.purchasedProduct.definition.id);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) => FailPurchase(product, reason == PurchaseFailureReason.UserCancelled);
        public void OnPurchaseFailed(Product product, PurchaseFailureDescription d) => FailPurchase(product, d.reason == PurchaseFailureReason.UserCancelled);

        private void FailPurchase(Product product, bool cancelled)
        {
            var cb = _onPurchase;
            _onPurchase = null;
            cb?.Invoke(product.definition.id, cancelled ? PurchaseStatus.Cancelled : PurchaseStatus.Failed);
        }
    }
}
#endif
