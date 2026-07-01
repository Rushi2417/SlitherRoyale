using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using UnityEngine;

namespace SlitherRoyale.Client.Backend
{
    public static class FirebaseBootstrap
    {
        public static bool IsInitialized { get; private set; }

        public static async System.Threading.Tasks.Task<bool> InitializeAsync()
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                Crashlytics.IsCrashlyticsCollectionEnabled = true;
                IsInitialized = true;
                Debug.Log("[Firebase] Initialized successfully");
                LogTestEvent();
                return true;
            }
            else
            {
                Debug.LogError($"[Firebase] Dependency error: {dependencyStatus}");
                return false;
            }
        }

        public static void LogTestEvent()
        {
            FirebaseAnalytics.LogEvent(
                FirebaseAnalytics.EventAppOpen,
                new Parameter("source", "phase0_bootstrap"),
                new Parameter("build_version", Application.version));
        }
    }
}
