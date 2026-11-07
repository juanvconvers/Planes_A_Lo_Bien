using Tesela.Core;
using Tesela.Meta;
using Tesela.Monetization;

namespace Tesela.Services
{
    /// <summary>Archivos de guardado del juego, todos con checksum (SaveCodec) y escritura atómica.</summary>
    public sealed class SaveSystem
    {
        public const string ProfileKey = "profile";
        public const string SettingsKey = "settings";
        public const string AdsKey = "ads";
        public const string RunKey = "run";

        private readonly FileStore _store;

        public SaveSystem(FileStore store) { _store = store; }

        public PlayerProfile LoadProfile() =>
            _store.TryReadVerified(ProfileKey, out var n) ? PlayerProfile.FromJson(n) : new PlayerProfile();

        public GameSettings LoadSettings() =>
            _store.TryReadVerified(SettingsKey, out var n) ? GameSettings.FromJson(n) : new GameSettings();

        public AdState LoadAds() =>
            _store.TryReadVerified(AdsKey, out var n) ? AdState.FromJson(n) : new AdState();

        public bool TryLoadRun(out JsonNode run) => _store.TryReadVerified(RunKey, out run);

        public void SaveProfile(PlayerProfile p) => _store.Write(ProfileKey, SaveCodec.Wrap(p.ToJson()));
        public void SaveSettings(GameSettings s) => _store.Write(SettingsKey, SaveCodec.Wrap(s.ToJson()));
        public void SaveAds(AdState s) => _store.Write(AdsKey, SaveCodec.Wrap(s.ToJson()));
        public void SaveRun(object state) => _store.Write(RunKey, SaveCodec.Wrap(state));
        public void ClearRun() => _store.Delete(RunKey);

        /// <summary>Borrado de datos desde Ajustes (requisito de Google Play).</summary>
        public void DeleteEverything() => _store.DeleteAll();
    }
}
