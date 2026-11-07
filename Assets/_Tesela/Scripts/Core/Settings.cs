using System;
using System.Collections.Generic;

namespace Tesela.Core
{
    /// <summary>Ajustes persistentes del jugador (accesibilidad, audio, control).</summary>
    public sealed class GameSettings
    {
        public float MusicVolume = 0.7f;
        public float SfxVolume = 0.9f;
        public bool Vibration = true;
        public bool ReduceMotion;
        /// <summary>Patrones de esmalte más grandes y con más contraste (los patrones siempre se muestran).</summary>
        public bool StrongPatterns;
        /// <summary>Escala de texto: 1, 1.15 o 1.3.</summary>
        public float TextScale = 1f;
        public bool LeftHanded;
        /// <summary>Elevación de la pieza sobre el dedo, en celdas (0–3).</summary>
        public float LiftCells = 2f;
        public string Language;
        public bool RelaxedMode;
        public bool SessionReminder = true;
        public bool HighContrast;

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "music", MusicVolume }, { "sfx", SfxVolume }, { "vibration", Vibration }, { "reduceMotion", ReduceMotion },
            { "strongPatterns", StrongPatterns }, { "textScale", TextScale }, { "leftHanded", LeftHanded }, { "lift", LiftCells },
            { "lang", Language }, { "relaxed", RelaxedMode }, { "reminder", SessionReminder }, { "contrast", HighContrast }
        };

        public static GameSettings FromJson(JsonNode n)
        {
            var d = new GameSettings();
            if (n.IsNull) return d;
            return new GameSettings
            {
                MusicVolume = Clamp(n["music"].AsFloat(d.MusicVolume), 0f, 1f),
                SfxVolume = Clamp(n["sfx"].AsFloat(d.SfxVolume), 0f, 1f),
                Vibration = n["vibration"].AsBool(d.Vibration),
                ReduceMotion = n["reduceMotion"].AsBool(),
                StrongPatterns = n["strongPatterns"].AsBool(),
                TextScale = Clamp(n["textScale"].AsFloat(1f), 1f, 1.3f),
                LeftHanded = n["leftHanded"].AsBool(),
                LiftCells = Clamp(n["lift"].AsFloat(d.LiftCells), 0f, 3f),
                Language = n["lang"].AsString(),
                RelaxedMode = n["relaxed"].AsBool(),
                SessionReminder = n["reminder"].AsBool(true),
                HighContrast = n["contrast"].AsBool()
            };
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }

    /// <summary>Almacén clave → texto (archivo atómico en el dispositivo; memoria en tests).</summary>
    public interface IKeyValueStore
    {
        string Read(string key);
        void Write(string key, string value);
        void Delete(string key);
    }

    public sealed class MemoryStore : IKeyValueStore
    {
        private readonly Dictionary<string, string> _data = new Dictionary<string, string>();
        public string Read(string key) => _data.TryGetValue(key, out string v) ? v : null;
        public void Write(string key, string value) => _data[key] = value;
        public void Delete(string key) => _data.Remove(key);
    }

    public interface IAnalytics
    {
        void Log(string eventName, Dictionary<string, object> parameters = null);
        void SetUserProperty(string name, string value);
        void RecordException(Exception e);
    }

    public sealed class NullAnalytics : IAnalytics
    {
        public readonly List<string> Events = new List<string>();
        public void Log(string eventName, Dictionary<string, object> parameters = null) => Events.Add(eventName);
        public void SetUserProperty(string name, string value) { }
        public void RecordException(Exception e) { }
    }

    /// <summary>Remote Config: siempre devuelve algo (valores por defecto locales si no hay red).</summary>
    public interface IRemoteConfig
    {
        JsonNode Get(string key);
        void Fetch(Action<bool> onDone);
        string Variant { get; }
    }

    public sealed class LocalRemoteConfig : IRemoteConfig
    {
        private readonly JsonNode _root;
        public LocalRemoteConfig(string defaultsJson) { _root = Json.ParseNode(defaultsJson); }
        public JsonNode Get(string key) => _root[key];
        public void Fetch(Action<bool> onDone) => onDone?.Invoke(false);
        public string Variant => "local";
    }

    public interface IPlatformGames
    {
        bool IsSignedIn { get; }
        void SignIn(Action<bool> onDone);
        void SubmitScore(string leaderboardKey, long score);
        void UnlockAchievement(string internalId);
        void ShowLeaderboard(string leaderboardKey);
        void CloudSave(string data, Action<bool> onDone);
        void CloudLoad(Action<string> onLoaded);
    }

    public sealed class OfflineGames : IPlatformGames
    {
        public bool IsSignedIn => false;
        public void SignIn(Action<bool> onDone) => onDone?.Invoke(false);
        public void SubmitScore(string leaderboardKey, long score) { }
        public void UnlockAchievement(string internalId) { }
        public void ShowLeaderboard(string leaderboardKey) { }
        public void CloudSave(string data, Action<bool> onDone) => onDone?.Invoke(false);
        public void CloudLoad(Action<string> onLoaded) => onLoaded?.Invoke(null);
    }

    public interface INotificationScheduler
    {
        void RequestPermission(Action<bool> onDone);
        void ScheduleAt(DateTime localTime, string title, string body);
        void CancelAll();
    }

    public sealed class NoNotifications : INotificationScheduler
    {
        public void RequestPermission(Action<bool> onDone) => onDone?.Invoke(false);
        public void ScheduleAt(DateTime localTime, string title, string body) { }
        public void CancelAll() { }
    }

    public interface IReviewService
    {
        void RequestReview();
        void CheckForUpdate();
    }

    public sealed class NoReview : IReviewService
    {
        public void RequestReview() { }
        public void CheckForUpdate() { }
    }
}
