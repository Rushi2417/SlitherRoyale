using SlitherRoyale.Client.Backend;
using UnityEngine;

namespace SlitherRoyale.Client
{
    public class Bootstrapper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var go = new GameObject("Bootstrapper");
            go.AddComponent<Bootstrapper>();

            var instance = go.GetComponent<Bootstrapper>();
            instance.StartInit();
        }

        private async void StartInit()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;

            Debug.Log("[Bootstrapper] Phase 0 init started");

            bool firebaseOk = await FirebaseBootstrap.InitializeAsync();
            Debug.Log($"[Bootstrapper] Firebase init: {firebaseOk}");

            bool playfabOk = await PlayFabBootstrap.LoginAsGuestAsync();
            Debug.Log($"[Bootstrapper] PlayFab guest login: {playfabOk}");

            if (firebaseOk && playfabOk)
            {
                Debug.Log("[Bootstrapper] Exit criteria achieved: Firebase analytics + PlayFab guest auth OK");
            }
        }
    }
}
