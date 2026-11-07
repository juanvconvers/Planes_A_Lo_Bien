using System;
using System.Collections.Generic;
using Tesela.Core;
using Tesela.Meta;

namespace Tesela.Monetization
{
    public enum ProductType
    {
        Consumable = 0,
        NonConsumable = 1,
        Subscription = 2
    }

    public sealed class StoreProduct
    {
        public string Id;
        public string Key;
        public ProductType Type;
        public decimal BasePriceUsd;
        public int PeriodDays;
        public Reward Content;
        public bool RemovesAds;
        public bool StarterPack;
        public bool Piggy;
        public bool Pass;
        public bool PassPlus;
        public bool Club;
        public int BonusPercent;
        public int OfferHours;
    }

    /// <summary>Products.json: IDs de Play Console y contenido de cada producto.</summary>
    public sealed class ProductCatalog
    {
        public readonly List<StoreProduct> Products = new List<StoreProduct>();
        public int PiggyCapacity = 2500;

        public static ProductCatalog FromJson(string json)
        {
            var n = Json.ParseNode(json);
            var c = new ProductCatalog { PiggyCapacity = n["piggyCapacity"].AsInt(2500) };
            foreach (var p in n["products"].Items)
            {
                c.Products.Add(new StoreProduct
                {
                    Id = p["id"].AsString(),
                    Key = p["key"].AsString(),
                    Type = ParseType(p["type"].AsString("consumable")),
                    BasePriceUsd = (decimal)p["priceUsd"].AsDouble(),
                    PeriodDays = p["periodDays"].AsInt(0),
                    Content = Reward.FromJson(p["content"]),
                    RemovesAds = p["removesAds"].AsBool(),
                    StarterPack = p["starterPack"].AsBool(),
                    Piggy = p["piggy"].AsBool(),
                    Pass = p["pass"].AsBool(),
                    PassPlus = p["passPlus"].AsBool(),
                    Club = p["club"].AsBool(),
                    BonusPercent = p["bonusPercent"].AsInt(0),
                    OfferHours = p["offerHours"].AsInt(0)
                });
            }
            var ids = new HashSet<string>();
            foreach (var p in c.Products)
            {
                if (string.IsNullOrEmpty(p.Id) || !ids.Add(p.Id)) throw new FormatException("ID de producto vacío o repetido: " + p.Id);
                if (p.BasePriceUsd <= 0) throw new FormatException("Precio inválido: " + p.Id);
            }
            return c;
        }

        private static ProductType ParseType(string s) => s == "nonconsumable" ? ProductType.NonConsumable : s == "subscription" ? ProductType.Subscription : ProductType.Consumable;

        public StoreProduct Get(string id) => Products.Find(p => p.Id == id);
        public StoreProduct ByKey(string key) => Products.Find(p => p.Key == key);
    }

    /// <summary>Entrega de compras. Idempotente para no consumibles; registra el gasto para los segmentos.</summary>
    public static class Fulfillment
    {
        public static bool Apply(StoreProduct p, PlayerProfile profile, ProductCatalog catalog, DateTime utcNow, DateTime? subscriptionExpiry = null)
        {
            if (p == null) return false;
            profile.Grant(p.Content, "iap");
            if (p.RemovesAds) profile.NoAds = true;
            if (p.StarterPack) profile.StarterPackBought = true;
            if (p.Pass) profile.PassPremium = true;
            if (p.PassPlus)
            {
                profile.PassPremium = true;
                profile.PassPlus = true;
            }
            if (p.Club)
            {
                DateTime until = subscriptionExpiry ?? utcNow.AddDays(Math.Max(1, p.PeriodDays));
                profile.ClubUntilUtcTicks = Math.Max(profile.ClubUntilUtcTicks, until.Ticks);
            }
            if (p.Piggy)
            {
                profile.AddCoins(Math.Min(profile.PiggyCoins, catalog.PiggyCapacity), "piggy");
                profile.PiggyCoins = 0;
            }
            profile.TotalSpentUsd += (double)p.BasePriceUsd;
            profile.PurchaseCount++;
            profile.LastPurchaseUtcTicks = utcNow.Ticks;
            return true;
        }

        /// <summary>Restauración: solo reaplica derechos (sin volver a sumar gasto ni monedas).</summary>
        public static void Restore(StoreProduct p, PlayerProfile profile, DateTime utcNow, DateTime? subscriptionExpiry)
        {
            if (p == null) return;
            if (p.RemovesAds) profile.NoAds = true;
            if (p.Club && subscriptionExpiry.HasValue) profile.ClubUntilUtcTicks = Math.Max(profile.ClubUntilUtcTicks, subscriptionExpiry.Value.Ticks);
        }

        /// <summary>La hucha se ve llenarse; se abre pagando. Muestra cuánto contiene (hasta la capacidad).</summary>
        public static int PiggyValue(PlayerProfile profile, ProductCatalog catalog) => Math.Min(profile.PiggyCoins, catalog.PiggyCapacity);
    }
}
