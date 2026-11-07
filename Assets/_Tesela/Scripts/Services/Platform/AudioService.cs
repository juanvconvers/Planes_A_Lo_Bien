using System.Collections;
using System.Threading.Tasks;
using Tesela.Core;
using Tesela.Core.Audio;
using UnityEngine;

namespace Tesela.Services
{
    /// <summary>
    /// Efectos y música sintetizados. Las muestras se calculan en un hilo aparte (no bloquea el arranque) y los
    /// AudioClip se crean en el hilo principal. Música y efectos con volúmenes separados; silencio en segundo plano.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        private const int ComboNotes = 10;
        private const int MusicLayers = 3;

        private readonly AudioClip[] _sfx = new AudioClip[9];
        private readonly AudioClip[] _combo = new AudioClip[ComboNotes];
        private readonly AudioSource[] _music = new AudioSource[MusicLayers];
        private readonly float[] _layerTarget = { 1f, 0f, 0f };
        private AudioSource[] _sfxPool;
        private int _nextSfx;
        private float _musicVolume = 0.7f;
        private float _sfxVolume = 0.9f;
        private bool _paused;

        public bool Ready { get; private set; }

        public void Initialize(GameSettings settings)
        {
            ApplySettings(settings);
            _sfxPool = new AudioSource[6];
            for (int i = 0; i < _sfxPool.Length; i++)
            {
                _sfxPool[i] = gameObject.AddComponent<AudioSource>();
                _sfxPool[i].playOnAwake = false;
            }
            for (int i = 0; i < MusicLayers; i++)
            {
                _music[i] = gameObject.AddComponent<AudioSource>();
                _music[i].loop = true;
                _music[i].playOnAwake = false;
                _music[i].volume = 0f;
            }
            StartCoroutine(Build());
        }

        private IEnumerator Build()
        {
            var task = Task.Run(() =>
            {
                var sfx = new float[_sfx.Length][];
                for (int i = 0; i < sfx.Length; i++) sfx[i] = SfxSynth.Render((Sfx)i, i + 1);
                var combo = new float[ComboNotes][];
                for (int i = 0; i < ComboNotes; i++) combo[i] = SfxSynth.ComboNote(i);
                var layers = new float[MusicLayers][];
                for (int i = 0; i < MusicLayers; i++) layers[i] = MusicSynth.RenderLayer(i);
                return (sfx, combo, layers);
            });
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) yield break;
            var (sfxData, comboData, layerData) = task.Result;
            for (int i = 0; i < sfxData.Length; i++) _sfx[i] = UnityConvert.ToClip(sfxData[i], SfxSynth.SampleRate, ((Sfx)i).ToString());
            for (int i = 0; i < comboData.Length; i++) _combo[i] = UnityConvert.ToClip(comboData[i], SfxSynth.SampleRate, "combo" + i);
            double start = AudioSettings.dspTime + 0.2;
            for (int i = 0; i < MusicLayers; i++)
            {
                _music[i].clip = UnityConvert.ToClip(layerData[i], MusicSynth.SampleRate, "music" + i);
                _music[i].PlayScheduled(start);
            }
            Ready = true;
        }

        public void ApplySettings(GameSettings s)
        {
            _musicVolume = s.MusicVolume;
            _sfxVolume = s.SfxVolume;
        }

        public void Play(Sfx sfx, float volume = 1f, float pitch = 1f)
        {
            var clip = _sfx[(int)sfx];
            if (clip == null || _sfxVolume <= 0f || _paused) return;
            var src = _sfxPool[_nextSfx];
            _nextSfx = (_nextSfx + 1) % _sfxPool.Length;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume * _sfxVolume);
        }

        public void PlayCombo(int step)
        {
            var clip = _combo[Mathf.Clamp(step, 0, ComboNotes - 1)];
            if (clip == null || _sfxVolume <= 0f || _paused) return;
            var src = _sfxPool[_nextSfx];
            _nextSfx = (_nextSfx + 1) % _sfxPool.Length;
            src.pitch = 1f;
            src.PlayOneShot(clip, 0.8f * _sfxVolume);
        }

        /// <summary>Las capas de guitarra y melodía entran con la racha.</summary>
        public void SetIntensity(int streak, bool calm)
        {
            _layerTarget[0] = 1f;
            _layerTarget[1] = calm ? 0.35f : Mathf.Clamp01(streak / 2f);
            _layerTarget[2] = calm ? 0f : Mathf.Clamp01((streak - 2) / 3f);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < MusicLayers; i++)
            {
                if (_music[i] == null) continue;
                float target = _paused ? 0f : _layerTarget[i] * _musicVolume * 0.6f;
                _music[i].volume = Mathf.MoveTowards(_music[i].volume, target, dt * 0.5f);
            }
        }

        private void OnApplicationPause(bool paused) => SetPaused(paused);
        private void OnApplicationFocus(bool focus) => SetPaused(!focus);

        private void SetPaused(bool paused)
        {
            _paused = paused;
            AudioListener.pause = paused;
        }
    }
}
