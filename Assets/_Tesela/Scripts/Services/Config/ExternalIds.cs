using UnityEngine;

namespace Tesela.Services
{
    /// <summary>
    /// IDs de servicios externos. Mientras valgan "TODO_ID_*" el juego usa las implementaciones falsas
    /// (anuncios simulados, tienda simulada, sin Firebase): todo se puede jugar sin cuentas ni red.
    /// </summary>
    [CreateAssetMenu(menuName = "Tesela/External Ids", fileName = "ExternalIds")]
    public sealed class ExternalIds : ScriptableObject
    {
        public const string TodoPrefix = "TODO_ID_";

        [Header("Android")]
        public string androidPackageName = "com.jdvelasquez.tesela";
        public string companyName = "TODO_ID_COMPANY_NAME";

        [Header("Unity LevelPlay + AdMob")]
        public string levelPlayAppKey = "TODO_ID_LEVELPLAY_APP_KEY";
        public string admobAppId = "TODO_ID_ADMOB_APP_ID";
        public string rewardedAdUnitId = "TODO_ID_LEVELPLAY_REWARDED_AD_UNIT";
        public string interstitialAdUnitId = "TODO_ID_LEVELPLAY_INTERSTITIAL_AD_UNIT";
        public string bannerAdUnitId = "TODO_ID_LEVELPLAY_BANNER_AD_UNIT";

        [Header("Google Play Games")]
        public string playGamesAppId = "TODO_ID_PLAY_GAMES_APP_ID";
        public string leaderboardClassic = "TODO_ID_LEADERBOARD_CLASSIC";
        public string leaderboardDaily = "TODO_ID_LEADERBOARD_DAILY";
        [Tooltip("Pares id_interno=id_de_Play, uno por línea (ej. first_line=CgkI...)")]
        [TextArea(3, 12)] public string achievementIds = "TODO_ID_ACHIEVEMENTS";

        [Header("Firebase")]
        [Tooltip("Coloca google-services.json en Assets/ (lo descargas de la consola de Firebase).")]
        public string firebaseProjectId = "TODO_ID_FIREBASE_PROJECT";

        [Header("Legal y soporte")]
        public string privacyPolicyUrl = "TODO_ID_PRIVACY_POLICY_URL";
        public string supportEmail = "TODO_ID_SUPPORT_EMAIL";
        public string dataDeletionUrl = "TODO_ID_DATA_DELETION_URL";

        public static bool IsSet(string id) => !string.IsNullOrEmpty(id) && !id.StartsWith(TodoPrefix, System.StringComparison.Ordinal);

        public bool AdsConfigured => IsSet(levelPlayAppKey) && IsSet(rewardedAdUnitId);
        public bool PlayGamesConfigured => IsSet(playGamesAppId);

        public string AchievementId(string internalId)
        {
            if (!IsSet(achievementIds)) return null;
            foreach (string line in achievementIds.Split('\n'))
            {
                int eq = line.IndexOf('=');
                if (eq > 0 && line.Substring(0, eq).Trim() == internalId) return line.Substring(eq + 1).Trim();
            }
            return null;
        }

        private static ExternalIds _instance;

        public static ExternalIds Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = Resources.Load<ExternalIds>("ExternalIds");
                if (_instance == null) _instance = CreateInstance<ExternalIds>();
                return _instance;
            }
        }
    }
}
