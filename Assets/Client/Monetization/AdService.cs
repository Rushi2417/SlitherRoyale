using System;
using SlitherRoyale.Client.Backend;
using UnityEngine;

namespace SlitherRoyale.Client.Monetization
{
    public static class AdService
    {
        public static bool IsInitialized { get; private set; }
        public static bool AdsRemoved { get; set; }

        public static event Action OnRewardGranted;
        public static event Action<string> OnAdFailed;

        private static int _interstitialImpressionsThisSession;
        private static int _sessionStartDay;

        public static void Initialize()
        {
            Debug.Log("[AdService] Initialize - requires AppLovin MAX SDK + mediation setup");
            AdsRemoved = PlayerPrefs.GetInt("AdsRemoved", 0) == 1;
            IsInitialized = true;
            _sessionStartDay = DateTime.UtcNow.DayOfYear;
            _interstitialImpressionsThisSession = 0;
        }

        public static void SetAdsRemoved(bool removed)
        {
            AdsRemoved = removed;
            if (removed) HideBanner();
            PlayerPrefs.SetInt("AdsRemoved", removed ? 1 : 0);
        }

        public static bool CanShowInterstitial()
        {
            if (AdsRemoved || !IsInitialized) return false;
            ResetDailyCountersIfNewDay();
            int maxPerSession = RemoteConfigService.GetInt("ad_interstitial_max_per_session", 3);
            int maxPerHour = RemoteConfigService.GetInt("ad_interstitial_max_per_hour", 5);
            return _interstitialImpressionsThisSession < maxPerSession;
        }

        public static bool CanShowBanner()
        {
            return !AdsRemoved && IsInitialized;
        }

        public static void ShowRewardedVideo(string placement, Action onRewarded, Action<string> onFailed)
        {
            if (!IsInitialized)
            {
                onFailed?.Invoke("AdService not initialized");
                return;
            }

            Debug.Log($"[AdService] Show rewarded video at placement: {placement}");
            AnalyticsService.LogAdImpression(placement, "rewarded");
            var listener = new GameObject("AdListener").AddComponent<AdListenerComponent>();
            listener.OnReward = () =>
            {
                AnalyticsService.LogAdRewardGranted(placement);
                OnRewardGranted?.Invoke();
                onRewarded?.Invoke();
                GameObject.Destroy(listener.gameObject);
            };
            listener.OnFail = (err) =>
            {
                OnAdFailed?.Invoke(err);
                onFailed?.Invoke(err);
                GameObject.Destroy(listener.gameObject);
            };

            listener.SimulateAd(1f);
        }

        public static void ShowInterstitial(string placement)
        {
            if (!CanShowInterstitial()) return;
            _interstitialImpressionsThisSession++;
            AnalyticsService.LogAdImpression(placement, "interstitial");
            Debug.Log($"[AdService] Show interstitial at: {placement} (#{_interstitialImpressionsThisSession} this session)");
        }

        public static void ShowBanner()
        {
            if (!CanShowBanner()) return;
            AnalyticsService.LogAdImpression("menu_banner", "banner");
            Debug.Log("[AdService] Show banner ad");
        }

        public static void HideBanner()
        {
            Debug.Log("[AdService] Hide banner ad");
        }

        private static void ResetDailyCountersIfNewDay()
        {
            int today = DateTime.UtcNow.DayOfYear;
            if (today != _sessionStartDay)
            {
                _sessionStartDay = today;
                _interstitialImpressionsThisSession = 0;
            }
        }

        private class AdListenerComponent : MonoBehaviour
        {
            public Action OnReward;
            public Action<string> OnFail;
            private float _timer;
            private bool _started;

            public void SimulateAd(float delay)
            {
                _timer = delay;
                _started = true;
            }

            private void Update()
            {
                if (!_started) return;
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                    _started = false;
                    if (UnityEngine.Random.value < 0.9f)
                        OnReward?.Invoke();
                    else
                        OnFail?.Invoke("Simulated ad failure");
                }
            }
        }
    }
}
