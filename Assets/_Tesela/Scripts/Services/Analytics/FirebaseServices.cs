#if TESELA_FIREBASE
using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using Tesela.Core;

namespace Tesela.Services
{
    /// <summary>Firebase Analytics + Crashlytics. Sin verificar en editor: ver QA_PENDIENTE.md.</summary>
    public sealed class FirebaseAnalyticsService : IAnalytics
    {
        private bool _ready;
        private readonly List<KeyValuePair<string, Dictionary<string, object>>> _queue = new List<KeyValuePair<string, Dictionary<string, object>>>();

        public void Initialize(Action<bool> onDone)
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(t =>
            {
                _ready = t.Result == DependencyStatus.Available;
                if (_ready) foreach (var e in _queue) Log(e.Key, e.Value);
                _queue.Clear();
                onDone?.Invoke(_ready);
            });
        }

        public void Log(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!_ready) { _queue.Add(new KeyValuePair<string, Dictionary<string, object>>(eventName, parameters)); return; }
            if (parameters == null) { FirebaseAnalytics.LogEvent(eventName); return; }
            var list = new List<Parameter>(parameters.Count);
            foreach (var kv in parameters)
            {
                switch (kv.Value)
                {
                    case int i: list.Add(new Parameter(kv.Key, i)); break;
                    case long l: list.Add(new Parameter(kv.Key, l)); break;
                    case float f: list.Add(new Parameter(kv.Key, f)); break;
                    case double d: list.Add(new Parameter(kv.Key, d)); break;
                    default: list.Add(new Parameter(kv.Key, Convert.ToString(kv.Value))); break;
                }
            }
            FirebaseAnalytics.LogEvent(eventName, list.ToArray());
        }

        public void SetUserProperty(string name, string value)
        {
            if (_ready) FirebaseAnalytics.SetUserProperty(name, value);
        }

        public void RecordException(Exception e)
        {
            if (_ready) Firebase.Crashlytics.Crashlytics.LogException(e);
        }
    }

    /// <summary>Remote Config con valores por defecto locales (RemoteDefaults.json): sin red se juega igual.</summary>
    public sealed class FirebaseRemoteConfigService : IRemoteConfig
    {
        private readonly JsonNode _defaults;
        private readonly Dictionary<string, JsonNode> _remote = new Dictionary<string, JsonNode>();

        public FirebaseRemoteConfigService(string defaultsJson) { _defaults = Json.ParseNode(defaultsJson); }

        public string Variant { get; private set; } = "default";

        public JsonNode Get(string key) => _remote.TryGetValue(key, out var v) ? v : _defaults[key];

        public void Fetch(Action<bool> onDone)
        {
            var rc = FirebaseRemoteConfig.DefaultInstance;
            rc.FetchAndActivateAsync().ContinueWithOnMainThread(t =>
            {
                bool ok = !t.IsFaulted && !t.IsCanceled;
                if (ok)
                {
                    foreach (var kv in _defaults.Properties)
                    {
                        string s = rc.GetValue(kv.Key).StringValue;
                        if (string.IsNullOrEmpty(s)) continue;
                        try { _remote[kv.Key] = Json.ParseNode(s); }
                        catch (FormatException) { }
                    }
                    string variant = rc.GetValue("ab_variant").StringValue;
                    if (!string.IsNullOrEmpty(variant)) Variant = variant;
                }
                onDone?.Invoke(ok);
            });
        }
    }
}
#endif
