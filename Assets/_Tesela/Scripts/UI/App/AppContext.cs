using System;
using System.Collections.Generic;
using Tesela.Core;
using Tesela.Gameplay;
using Tesela.Meta;
using Tesela.Monetization;
using Tesela.Services;
using UnityEngine;

namespace Tesela.UI
{
    /// <summary>Datos de juego cargados desde Resources (JSON actualizable por Remote Config/Addressables).</summary>
    public sealed class GameData
    {
        public PieceCatalog Pieces;
        public ScoringConfig Scoring;
        public EconomyConfig Economy;
        public ProductCatalog Products;
        public readonly List<LevelDefinition> Levels = new List<LevelDefinition>();
        public readonly Dictionary<string, LevelDefinition> LevelById = new Dictionary<string, LevelDefinition>();
        public readonly List<string> Credits = new List<string>();

        public static string Text(string resource)
        {
            var asset = Resources.Load<TextAsset>(resource);
            if (asset == null) throw new InvalidOperationException("Falta el recurso " + resource);
            return asset.text;
        }

        public static GameData Load()
        {
            var d = new GameData
            {
                Pieces = PieceCatalog.FromJson(Text("Pieces")),
                Scoring = ScoringConfig.FromJson(Text("ScoringConfig")),
                Economy = EconomyConfig.FromJson(Text("Economy")),
                Products = ProductCatalog.FromJson(Text("Products"))
            };
            for (int ch = 1; ch <= 9; ch++)
            {
                var asset = Resources.Load<TextAsset>("Levels/chapter" + ch);
                if (asset == null) break;
                foreach (var l in LevelDefinition.ListFromJson(asset.text))
                {
                    d.Levels.Add(l);
                    d.LevelById[l.Id] = l;
                }
            }
            foreach (var c in Json.ParseNode(Text("Credits"))["credits"].Items) d.Credits.Add(c.AsString());
            return d;
        }
    }

    /// <summary>Servicios y estado compartidos de la app (se registran también en el ServiceLocator).</summary>
    public sealed class AppContext
    {
        public GameData Data;
        public SaveSystem Saves;
        public GameSettings Settings;
        public MetaService Meta;
        public PlayerProfile Profile => Meta.Profile;
        public AdState AdState;
        public AdConfig AdConfig;
        public OfferEngine Offers;
        public SafeAdService Ads;
        public IStoreService Store;
        public IConsentService Consent;
        public IAnalytics Analytics;
        public IRemoteConfig Remote;
        public IPlatformGames Games;
        public INotificationScheduler Notifications;
        public IReviewService Review;
        public AudioService Audio;
        public IClock Clock;
        public int GamesThisSession;
        public int PreviousActiveDay = -1;

        public DateTime UtcNow => Clock.UtcNow;
        public int Today => DayKey.FromUtc(Clock.UtcNow);
        public bool AdFree => Profile.AdFree(UtcNow);

        public void SaveAll()
        {
            Saves.SaveProfile(Profile);
            Saves.SaveSettings(Settings);
            Saves.SaveAds(AdState);
        }

        public void Log(string eventName, Dictionary<string, object> p = null) => Analytics?.Log(eventName, p);

        public Segment Segment => Segmentation.Compute(Profile, Today, PreviousActiveDay);

        public ModeRules ActiveEventRules()
        {
            var events = Remote.Get("events");
            string active = events["active"].AsString();
            foreach (var e in events["list"].Items)
                if (e["id"].AsString() == active) return ModeRules.EventFromJson(e);
            return null;
        }

        public string ActiveEventNameKey()
        {
            var events = Remote.Get("events");
            string active = events["active"].AsString();
            foreach (var e in events["list"].Items)
                if (e["id"].AsString() == active) return e["nameKey"].AsString("event.title");
            return "event.title";
        }
    }
}
