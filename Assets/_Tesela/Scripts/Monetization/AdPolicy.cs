using System;
using Tesela.Core;

namespace Tesela.Monetization
{
    public enum InterstitialPlacement
    {
        /// <summary>Resultados → menú. Único momento permitido junto con ResultsToNewGame.</summary>
        ResultsToMenu = 0,
        ResultsToNewGame = 1,
        LevelStart = 2,
        MidGame = 3,
        AppOpen = 4
    }

    public enum RewardedPlacement
    {
        Revive = 0,
        DoubleCoins = 1,
        FreeBooster = 2,
        DoubleDailyChest = 3
    }

    /// <summary>Parámetros de anuncios. Viven en Remote Config (con estos valores por defecto) y se pueden probar con A/B.</summary>
    public sealed class AdConfig
    {
        public int RewardedDailyCap = 20;
        public int FreeBoosterDailyCap = 5;
        public bool BlockFirstSession = true;
        public int MinGamesBeforeFirstInterstitial = 3;
        public double MinSecondsBetweenInterstitials = 150;
        public int MinGamesBetweenInterstitials = 2;
        public double CooldownAfterRewardedSeconds = 120;
        public double NoInterstitialHoursAfterPurchase = 24;
        public double SdkTimeoutSeconds = 5;
        public bool BannerEnabled = true;
        public int ReviveCoinPrice = 300;

        public static AdConfig FromJson(JsonNode n)
        {
            var d = new AdConfig();
            if (n.IsNull) return d;
            return new AdConfig
            {
                RewardedDailyCap = n["rewardedDailyCap"].AsInt(d.RewardedDailyCap),
                FreeBoosterDailyCap = n["freeBoosterDailyCap"].AsInt(d.FreeBoosterDailyCap),
                BlockFirstSession = n["blockFirstSession"].AsBool(d.BlockFirstSession),
                MinGamesBeforeFirstInterstitial = n["minGamesBeforeFirstInterstitial"].AsInt(d.MinGamesBeforeFirstInterstitial),
                MinSecondsBetweenInterstitials = n["minSecondsBetweenInterstitials"].AsDouble(d.MinSecondsBetweenInterstitials),
                MinGamesBetweenInterstitials = n["minGamesBetweenInterstitials"].AsInt(d.MinGamesBetweenInterstitials),
                CooldownAfterRewardedSeconds = n["cooldownAfterRewardedSeconds"].AsDouble(d.CooldownAfterRewardedSeconds),
                NoInterstitialHoursAfterPurchase = n["noInterstitialHoursAfterPurchase"].AsDouble(d.NoInterstitialHoursAfterPurchase),
                SdkTimeoutSeconds = n["sdkTimeoutSeconds"].AsDouble(d.SdkTimeoutSeconds),
                BannerEnabled = n["bannerEnabled"].AsBool(d.BannerEnabled),
                ReviveCoinPrice = n["reviveCoinPrice"].AsInt(d.ReviveCoinPrice)
            };
        }
    }

    /// <summary>Estado de anuncios del jugador (se guarda con el perfil de monetización).</summary>
    public sealed class AdState
    {
        public int SessionNumber;
        public int TotalGames;
        public int GamesSinceInterstitial;
        public bool AnyInterstitialShown;
        public double LastInterstitialRealtime = double.NegativeInfinity;
        public double LastRewardedRealtime = double.NegativeInfinity;
        public double SessionStartRealtime;
        public int RewardedDay = -1;
        public int RewardedToday;
        public int FreeBoosterToday;
        public int DoubleChestDay = -1;
        public bool ReviveUsedThisRun;

        public void StartSession(double realtime)
        {
            SessionNumber++;
            SessionStartRealtime = realtime;
            // Los tiempos son monótonos por sesión: al reiniciar la app las esperas empiezan de nuevo desde el arranque.
            LastInterstitialRealtime = double.NegativeInfinity;
            LastRewardedRealtime = double.NegativeInfinity;
        }

        public void OnRunStarted() => ReviveUsedThisRun = false;

        public void OnRunFinished()
        {
            TotalGames++;
            GamesSinceInterstitial++;
        }

        public void RollDay(int day)
        {
            if (RewardedDay == day) return;
            RewardedDay = day;
            RewardedToday = 0;
            FreeBoosterToday = 0;
        }

        public System.Collections.Generic.Dictionary<string, object> ToJson() => new System.Collections.Generic.Dictionary<string, object>
        {
            { "session", SessionNumber }, { "games", TotalGames }, { "sinceInt", GamesSinceInterstitial }, { "anyInt", AnyInterstitialShown },
            { "rDay", RewardedDay }, { "rToday", RewardedToday }, { "bToday", FreeBoosterToday }, { "chestDay", DoubleChestDay }
        };

        public static AdState FromJson(JsonNode n) => new AdState
        {
            SessionNumber = n["session"].AsInt(), TotalGames = n["games"].AsInt(), GamesSinceInterstitial = n["sinceInt"].AsInt(),
            AnyInterstitialShown = n["anyInt"].AsBool(), RewardedDay = n["rDay"].AsInt(-1), RewardedToday = n["rToday"].AsInt(),
            FreeBoosterToday = n["bToday"].AsInt(), DoubleChestDay = n["chestDay"].AsInt(-1)
        };
    }

    public enum AdDecision
    {
        Allowed = 0,
        AdFree,
        WrongPlacement,
        FirstSession,
        TooFewGames,
        TooSoon,
        AfterRewarded,
        AfterPurchase,
        DailyCap,
        AlreadyUsed
    }

    /// <summary>Reglas de anuncios de la Parte 3. Puras y testeadas una por una.</summary>
    public static class AdPolicy
    {
        public static AdDecision CanShowInterstitial(AdConfig c, AdState s, InterstitialPlacement placement, bool adFree,
                                                     double realtime, DateTime utcNow, long lastPurchaseUtcTicks)
        {
            if (adFree) return AdDecision.AdFree;
            if (placement != InterstitialPlacement.ResultsToMenu && placement != InterstitialPlacement.ResultsToNewGame) return AdDecision.WrongPlacement;
            if (c.BlockFirstSession && s.SessionNumber <= 1) return AdDecision.FirstSession;
            if (s.TotalGames < c.MinGamesBeforeFirstInterstitial) return AdDecision.TooFewGames;
            if (lastPurchaseUtcTicks > 0 && (utcNow - new DateTime(lastPurchaseUtcTicks, DateTimeKind.Utc)).TotalHours < c.NoInterstitialHoursAfterPurchase)
                return AdDecision.AfterPurchase;
            if (realtime - s.LastRewardedRealtime < c.CooldownAfterRewardedSeconds) return AdDecision.AfterRewarded;
            // La primera espera cuenta desde el inicio de la sesión: nunca un intersticial nada más abrir.
            double since = s.AnyInterstitialShown && !double.IsNegativeInfinity(s.LastInterstitialRealtime)
                ? realtime - s.LastInterstitialRealtime
                : realtime - s.SessionStartRealtime;
            if (since < c.MinSecondsBetweenInterstitials) return AdDecision.TooSoon;
            if (s.AnyInterstitialShown && s.GamesSinceInterstitial < c.MinGamesBetweenInterstitials) return AdDecision.TooFewGames;
            return AdDecision.Allowed;
        }

        public static void OnInterstitialShown(AdState s, double realtime)
        {
            s.AnyInterstitialShown = true;
            s.LastInterstitialRealtime = realtime;
            s.GamesSinceInterstitial = 0;
        }

        public static AdDecision CanOfferRewarded(AdConfig c, AdState s, RewardedPlacement placement, int day)
        {
            s.RollDay(day);
            if (s.RewardedToday >= c.RewardedDailyCap) return AdDecision.DailyCap;
            switch (placement)
            {
                case RewardedPlacement.Revive when s.ReviveUsedThisRun: return AdDecision.AlreadyUsed;
                case RewardedPlacement.FreeBooster when s.FreeBoosterToday >= c.FreeBoosterDailyCap: return AdDecision.DailyCap;
                case RewardedPlacement.DoubleDailyChest when s.DoubleChestDay == day: return AdDecision.AlreadyUsed;
            }
            return AdDecision.Allowed;
        }

        public static void OnRewardedCompleted(AdState s, RewardedPlacement placement, double realtime, int day)
        {
            s.RollDay(day);
            s.RewardedToday++;
            s.LastRewardedRealtime = realtime;
            if (placement == RewardedPlacement.Revive) s.ReviveUsedThisRun = true;
            if (placement == RewardedPlacement.FreeBooster) s.FreeBoosterToday++;
            if (placement == RewardedPlacement.DoubleDailyChest) s.DoubleChestDay = day;
        }

        /// <summary>El banner solo se muestra en menús, nunca sobre el tablero ni en Zen, y nunca a quien no tiene anuncios.</summary>
        public static bool ShowBanner(AdConfig c, bool inMenu, bool zen, bool adFree) => c.BannerEnabled && inMenu && !zen && !adFree;
    }
}
