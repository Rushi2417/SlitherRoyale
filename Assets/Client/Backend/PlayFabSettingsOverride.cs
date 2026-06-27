using PlayFab;
using UnityEngine;

namespace SlitherRoyale.Client.Backend
{
    public static class PlayFabSettingsOverride
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SetTitleId()
        {
            PlayFabSettings.TitleId = "16F553";
        }
    }
}
