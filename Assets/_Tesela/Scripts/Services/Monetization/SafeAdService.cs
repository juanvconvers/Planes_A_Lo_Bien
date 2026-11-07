using System;
using Tesela.Monetization;
using UnityEngine;

namespace Tesela.Services
{
    /// <summary>
    /// Envoltorio "a prueba de fallos": toda llamada al SDK va en try/catch y con límite de tiempo (5 s por defecto).
    /// Si el SDK no responde o lanza, el callback se llama igual con TimedOut/Failed: el juego nunca se queda colgado.
    /// </summary>
    public sealed class SafeAdService : IAdService
    {
        private readonly IAdService _inner;
        private readonly Func<double> _timeoutSeconds;
        private Action<AdResult> _pending;
        private float _deadline;
        private bool _showingFullscreen;

        public SafeAdService(IAdService inner, Func<double> timeoutSeconds)
        {
            _inner = inner;
            _timeoutSeconds = timeoutSeconds;
        }

        public IAdService Inner => _inner;
        public bool IsInitialized => Safe(() => _inner.IsInitialized);
        public bool IsRewardedReady => Safe(() => _inner.IsRewardedReady);
        public bool IsInterstitialReady => Safe(() => _inner.IsInterstitialReady);

        public void Initialize(bool personalizedAllowed) => Try(() => _inner.Initialize(personalizedAllowed));

        public void ShowRewarded(RewardedPlacement placement, Action<AdResult> onDone)
        {
            if (!Begin(onDone)) return;
            Try(() => _inner.ShowRewarded(placement, Finish), () => Finish(AdResult.Failed));
        }

        public void ShowInterstitial(Action<AdResult> onDone)
        {
            if (!Begin(onDone)) return;
            Try(() => _inner.ShowInterstitial(Finish), () => Finish(AdResult.Failed));
        }

        public void ShowBanner() => Try(_inner.ShowBanner);
        public void HideBanner() => Try(_inner.HideBanner);

        private bool Begin(Action<AdResult> onDone)
        {
            if (_pending != null)
            {
                onDone?.Invoke(AdResult.NotAvailable);
                return false;
            }
            _pending = onDone;
            _showingFullscreen = false;
            _deadline = Time.realtimeSinceStartup + (float)_timeoutSeconds();
            return true;
        }

        /// <summary>El límite de tiempo cubre la carga; una vez que el anuncio está en pantalla no se corta.</summary>
        public void MarkFullscreenShown() => _showingFullscreen = true;

        private void Finish(AdResult result)
        {
            var cb = _pending;
            _pending = null;
            cb?.Invoke(result);
        }

        /// <summary>Lo llama TeselaApp en cada frame.</summary>
        public void Tick()
        {
            if (_pending != null && !_showingFullscreen && Time.realtimeSinceStartup > _deadline) Finish(AdResult.TimedOut);
        }

        private static void Try(Action a, Action onError = null)
        {
            try { a(); }
            catch (Exception e)
            {
                Debug.LogWarning("[Tesela] SDK de anuncios: " + e.Message);
                onError?.Invoke();
            }
        }

        private static bool Safe(Func<bool> f)
        {
            try { return f(); }
            catch (Exception) { return false; }
        }
    }
}
