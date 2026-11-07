#if TESELA_UMP
using System;
using GoogleMobileAds.Ump.Api;
using Tesela.Monetization;

namespace Tesela.Services
{
    /// <summary>CMP certificada por Google (UMP, IAB TCF v2.3). Sin verificar en editor: ver QA_PENDIENTE.md.</summary>
    public sealed class UmpConsentService : IConsentService
    {
        public ConsentStatus Status { get; private set; }
        public bool PersonalizedAdsAllowed => ConsentInformation.CanRequestAds();

        public void RequestIfNeeded(Action<ConsentStatus> onDone)
        {
            var request = new ConsentRequestParameters { TagForUnderAgeOfConsent = false };
            ConsentInformation.Update(request, error =>
            {
                if (error != null) { Status = ConsentStatus.Unknown; onDone?.Invoke(Status); return; }
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    Status = ConsentInformation.CanRequestAds() ? ConsentStatus.Obtained : ConsentStatus.Unknown;
                    onDone?.Invoke(Status);
                });
            });
        }

        public void ShowPrivacyOptions(Action onClosed) => ConsentForm.ShowPrivacyOptionsForm(_ => onClosed?.Invoke());
    }
}
#endif
