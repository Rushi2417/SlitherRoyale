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
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true,
                TitleId = PlayFabSettings.TitleId
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
    }
}
