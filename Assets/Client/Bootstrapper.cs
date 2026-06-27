using SlitherRoyale.Client.Backend;
using SlitherRoyale.Client.Gameplay;
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

            StartArena();
        }

        private void StartArena()
        {
            var arena = new GameObject("Arena");

            var camGo = new GameObject("GameCamera");
            camGo.transform.SetParent(arena.transform);
            var camComp = camGo.AddComponent<Camera>();
            camComp.orthographic = true;
            camComp.orthographicSize = 60f;
            camComp.clearFlags = CameraClearFlags.SolidColor;
            camComp.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
            camComp.nearClipPlane = 0.1f;
            camComp.farClipPlane = 1000f;
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<CameraFollow>();

            var wormGo = new GameObject("PlayerWorm");
            wormGo.transform.SetParent(arena.transform);
            var lineRenderer = wormGo.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = 6f;
            lineRenderer.endWidth = 3f;
            var wormRenderer = wormGo.AddComponent<WormRenderer>();
            wormRenderer.lineRenderer = lineRenderer;

            var headGo = new GameObject("Head");
            headGo.transform.SetParent(wormGo.transform);
            var headSprite = headGo.AddComponent<SpriteRenderer>();
            var headTex = new Texture2D(32, 32);
            Color violet = new Color(0.42f, 0.31f, 1f);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float dx = x - 15.5f, dy = y - 15.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    headTex.SetPixel(x, y, d < 13f ? violet : Color.clear);
                }
            headTex.Apply();
            headSprite.sprite = Sprite.Create(headTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            wormRenderer.headTransform = headGo.transform;

            var mgr = arena.AddComponent<GameManager>();
            mgr.wormRenderer = wormRenderer;
            mgr.cameraFollow = camGo.GetComponent<CameraFollow>();
        }
    }
}
