#if TESELA_LEVELPLAY
using System;
using Tesela.Monetization;
using Unity.Services.LevelPlay;

namespace Tesela.Services
{
    /// <summary>Adaptador Unity LevelPlay (mediación) con AdMob como red. Sin verificar en editor: ver QA_PENDIENTE.md.</summary>
    public sealed class LevelPlayAdService : IAdService
    {
        private readonly ExternalIds _ids;
        private LevelPlayRewardedAd _rewarded;
        private LevelPlayInterstitialAd _interstitial;
        private LevelPlayBannerAd _banner;
        private Action<AdResult> _rewardedCb;
        private Action<AdResult> _interstitialCb;
        private bool _rewardEarned;

        public LevelPlayAdService(ExternalIds ids) { _ids = ids; }

        public bool IsInitialized { get; private set; }
        public bool IsRewardedReady => _rewarded != null && _rewarded.IsAdReady();
        public bool IsInterstitialReady => _interstitial != null && _interstitial.IsAdReady();

        public void Initialize(bool personalizedAllowed)
        {
            LevelPlay.SetConsent(personalizedAllowed);
            LevelPlay.OnInitSuccess += _ => OnInit();
            LevelPlay.Init(_ids.levelPlayAppKey);
        }

        private void OnInit()
        {
            IsInitialized = true;
            _rewarded = new LevelPlayRewardedAd(_ids.rewardedAdUnitId);
            _rewarded.OnAdRewarded += (_, __) => _rewardEarned = true;
            _rewarded.OnAdClosed += _ => { CompleteRewarded(_rewardEarned ? AdResult.Completed : AdResult.Skipped); _rewarded.LoadAd(); };
            _rewarded.OnAdDisplayFailed += _ => { CompleteRewarded(AdResult.Failed); _rewarded.LoadAd(); };
            _rewarded.LoadAd();

            _interstitial = new LevelPlayInterstitialAd(_ids.interstitialAdUnitId);
            _interstitial.OnAdClosed += _ => { CompleteInterstitial(AdResult.Completed); _interstitial.LoadAd(); };
            _interstitial.OnAdDisplayFailed += _ => { CompleteInterstitial(AdResult.Failed); _interstitial.LoadAd(); };
            _interstitial.LoadAd();

            _banner = new LevelPlayBannerAd(_ids.bannerAdUnitId, LevelPlayAdSize.CreateAdaptiveAdSize(), LevelPlayBannerPosition.BottomCenter);
            _banner.LoadAd();
            _banner.HideAd();
        }

        public void ShowRewarded(RewardedPlacement placement, Action<AdResult> onDone)
        {
            if (!IsRewardedReady) { onDone?.Invoke(AdResult.NotAvailable); return; }
            _rewardEarned = false;
            _rewardedCb = onDone;
            _rewarded.ShowAd(placement.ToString());
        }

        public void ShowInterstitial(Action<AdResult> onDone)
        {
            if (!IsInterstitialReady) { onDone?.Invoke(AdResult.NotAvailable); return; }
            _interstitialCb = onDone;
            _interstitial.ShowAd();
        }

        public void ShowBanner() => _banner?.ShowAd();
        public void HideBanner() => _banner?.HideAd();

        private void CompleteRewarded(AdResult r) { var cb = _rewardedCb; _rewardedCb = null; cb?.Invoke(r); }
        private void CompleteInterstitial(AdResult r) { var cb = _interstitialCb; _interstitialCb = null; cb?.Invoke(r); }
    }
}
#endif
