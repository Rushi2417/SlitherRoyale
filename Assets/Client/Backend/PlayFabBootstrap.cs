using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace SlitherRoyale.Client.Backend
{
    public static class PlayFabBootstrap
    {
        public static event Action OnLoginSuccess;
        public static event Action<string> OnLoginFailed;

        public static string PlayFabId { get; private set; }

        public static async System.Threading.Tasks.Task<bool> LoginAsGuestAsync()
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

            var request = new LoginWithCustomIDRequest
            {
                // BUG-18 FIX: SystemInfo.deviceUniqueIdentifier is deprecated on iOS 16+
                // and returns a different value each install, losing all player progress.
                // We generate a stable UUID once and persist it in PlayerPrefs.
                CustomId    = GetOrCreateGuestId(),
                CreateAccount = true,
                TitleId     = PlayFabSettings.TitleId
            };

            PlayFabClientAPI.LoginWithCustomID(request, result =>
            {
                PlayFabId = result.PlayFabId;
                Debug.Log($"[PlayFab] Guest login OK. PlayFabId: {PlayFabId}");
                OnLoginSuccess?.Invoke();
                tcs.TrySetResult(true);
            }, error =>
            {
                Debug.LogError($"[PlayFab] Login failed: {error.GenerateErrorReport()}");
                OnLoginFailed?.Invoke(error.GenerateErrorReport());
                tcs.TrySetResult(false);
            });

            return await tcs.Task;
        }

        /// <summary>
        /// Returns a stable guest ID that survives app restarts.
        /// Generated once as a GUID and persisted in PlayerPrefs.
        /// BUG-18 FIX: replaces SystemInfo.deviceUniqueIdentifier which is deprecated
        /// on iOS 16+ and randomizes on each install (all progress lost on reinstall).
        /// </summary>
        private static string GetOrCreateGuestId()
        {
            const string key = "SlitherRoyale_GuestId";
            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetString(key, Guid.NewGuid().ToString("N"));
                PlayerPrefs.Save();
            }
            return PlayerPrefs.GetString(key);
        }
    }
}
