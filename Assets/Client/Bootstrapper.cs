using System;
using System.Collections.Generic;
using SlitherRoyale.Client.Audio;
using SlitherRoyale.Client.Backend;
using SlitherRoyale.Client.Gameplay;
using SlitherRoyale.Client.Networking;
using SlitherRoyale.Client.UI;
using SlitherRoyale.Client.VFX;
using SlitherRoyale.Server;
using UnityEngine;
using UnityEngine.UI;
using WormCore;

namespace SlitherRoyale.Client
{
    public class Bootstrapper : MonoBehaviour
    {
        public static Bootstrapper Instance { get; private set; }

        // Default to offline mode — avoids UDP server startup issues for solo play.
        // Set to true before calling StartArena to use the in-process networked server.
        public static bool UseNetworking = false;

        private Material _glowMaterial;
        private ScreenManager _screenManager;
        private GameObject _arena;
        private MapConfig _currentMapConfig;

#if !UNITY_SERVER
        // AfterSceneLoad ensures the Init scene's Camera and lighting are ready
        // BEFORE we create the ScreenManager canvas and navigate to SplashScreen.
        // BeforeSceneLoad caused a blank frame because Awake() hadn't run yet.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Application.isBatchMode) return;

            var go = new GameObject("Bootstrapper");
            DontDestroyOnLoad(go);
            var instance = go.AddComponent<Bootstrapper>();
            // StartInit is called from Awake so all AddComponent Awake() calls
            // are guaranteed to have already fired by the time we run.
        }
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(InitRoutine());
        }

        private System.Collections.IEnumerator InitRoutine()
        {
            // Wait one frame so every AddComponent's Awake() has fired,
            // guaranteeing Canvas and CanvasGroup exist before we navigate.
            yield return null;

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            try { AccessibilityService.Initialize(); AccessibilityService.LoadSavedMode(); }
            catch (System.Exception ex) { Debug.LogError($"[Bootstrapper] AccessibilityService failed: {ex.Message}"); }

            // Paint the scene camera InkVoid so there is no white flash
            var sceneCam = Camera.main;
            if (sceneCam != null)
            {
                sceneCam.clearFlags      = CameraClearFlags.SolidColor;
                sceneCam.backgroundColor = new Color(0.043f, 0.055f, 0.078f, 1f);
            }

            string overrideHost = ParseArg("-connect");
            if (!string.IsNullOrEmpty(overrideHost))
            {
                UseNetworking = true;
                MatchmakerClient.SetOverrideHost(overrideHost);
            }

            // Initialize AdService early so banner guards in ScreenManager work
            try { AdService.Initialize(); }
            catch (System.Exception ex) { Debug.LogWarning($"[Bootstrapper] AdService init failed: {ex.Message}"); }

            Debug.Log("[Bootstrapper] Phase 0 init started");

            // Build ScreenManager — its Awake() runs immediately inside AddComponent
            var smGo = new GameObject("ScreenManager");
            smGo.transform.SetParent(transform);
            _screenManager = smGo.AddComponent<ScreenManager>();

            // Register all screens individually so one bad screen cannot
            // kill the entire init coroutine and leave a blank black screen.
            SafeRegister<SplashScreen>(_screenManager);
            SafeRegister<HomeScreen>(_screenManager);
            SafeRegister<ModeSelectScreen>(_screenManager);
            SafeRegister<MatchmakingScreen>(_screenManager);
            SafeRegister<ResultsScreen>(_screenManager);
            SafeRegister<CustomizeScreen>(_screenManager);
            SafeRegister<ShopScreen>(_screenManager);
            SafeRegister<BattlePassScreen>(_screenManager);
            SafeRegister<SettingsScreen>(_screenManager);
            SafeRegister<LeaderboardScreen>(_screenManager);
            SafeRegister<FriendsListScreen>(_screenManager);

            Debug.Log("[Bootstrapper] All screens registered. Navigating to SplashScreen...");

            // Wait one more frame so RectTransform layout has been calculated
            // before OnEnter tries to position children.
            yield return null;

            // Now safe to show the first screen
            _screenManager.NavigateTo<SplashScreen>();

            // Persistent singletons
            SpawnAudioManager();
            SpawnLoginRewardService();
            SpawnDebugConsole();  // always-on log overlay; triple-tap to toggle on device

            InitBackendAsync();
        }

        /// <summary>
        /// Wraps screen registration in try-catch so any single screen's
        /// Awake() throwing does NOT crash the entire init coroutine.
        /// </summary>
        private static void SafeRegister<T>(ScreenManager sm) where T : UIScreen
        {
            try
            {
                sm.Register<T>();
                Debug.Log($"[Bootstrapper] Registered: {typeof(T).Name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bootstrapper] FAILED to register {typeof(T).Name}: {ex}");
            }
        }

        private void SpawnAudioManager()
        {
            var go = new GameObject("AudioManager");
            go.transform.SetParent(transform);
            go.AddComponent<AudioManager>();
            // Plays menu music automatically after Awake
        }

        private void SpawnLoginRewardService()
        {
            var go = new GameObject("LoginRewardService");
            go.transform.SetParent(transform);
            go.AddComponent<LoginRewardService>();
        }

        private void SpawnDebugConsole()
        {
            // InGameDebugConsole is stripped in release builds via #if DEVELOPMENT_BUILD
            // but is always active in the editor. Triple-tap anywhere to show/hide on device.
            var go = new GameObject("DebugConsole");
            go.transform.SetParent(transform);
            go.AddComponent<InGameDebugConsole>();
        }

        private async void InitBackendAsync()
        {
            try
            {
                bool firebaseOk = await FirebaseBootstrap.InitializeAsync().ConfigureAwait(true);
                AnalyticsService.Initialize();
                Debug.Log($"[Bootstrapper] Firebase init: {firebaseOk}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Bootstrapper] Firebase init failed: {e.Message}");
            }

            try
            {
                bool playfabOk = await PlayFabBootstrap.LoginAsGuestAsync().ConfigureAwait(true);
                Debug.Log($"[Bootstrapper] PlayFab guest login: {playfabOk}");

                if (playfabOk)
                {
                    try { await PlayFabEconomy.InitializeAsync().ConfigureAwait(true); } catch (Exception e) { Debug.LogError($"[Bootstrapper] Economy init failed: {e.Message}"); }
                    try { await RemoteConfigService.LoadAsync().ConfigureAwait(true); } catch (Exception e) { Debug.LogError($"[Bootstrapper] RemoteConfig failed: {e.Message}"); }
                    try { await QuestManager.LoadProgressAsync().ConfigureAwait(true); } catch (Exception e) { Debug.LogError($"[Bootstrapper] QuestManager failed: {e.Message}"); }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Bootstrapper] PlayFab init failed: {e.Message}");
            }

            Debug.Log("[Bootstrapper] Backend init complete");
        }

        public void StartArena(int mapIndex, MatchMode mode = MatchMode.FreeForAll, string connectHost = null, int connectPort = 12345)
        {
            AnalyticsService.LogMatchStart(mode.ToString(), mapIndex);

            if (_arena != null)
            {
                Destroy(_arena);
                _arena = null;
            }

            _currentMapConfig = mapIndex switch
            {
                1 => MapConfig.CoralReef(),
                2 => MapConfig.MagmaCore(),
                3 => MapConfig.CandyKingdom(),
                4 => MapConfig.SpaceStation(),
                5 => MapConfig.HauntedForest(),
                _ => MapConfig.NeonGrid()
            };

            _arena = new GameObject("Arena");

            if (UseNetworking)
            {
                StartNetworkedArena(mode, connectHost, connectPort);
                return;
            }

            StartOfflineArena(mode);
        }

        private void StartOfflineArena(MatchMode mode)
        {
            var camGo = new GameObject("GameCamera");
            camGo.transform.SetParent(_arena.transform);
            var camComp = camGo.AddComponent<Camera>();
            camComp.orthographic = true;
            camComp.orthographicSize = Mathf.Min(_currentMapConfig.arenaRadius * 0.075f, 70f);
            camComp.clearFlags = CameraClearFlags.SolidColor;
            camComp.backgroundColor = _currentMapConfig.bgColor;
            camComp.nearClipPlane = 0.1f;
            camComp.farClipPlane = 1000f;
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<CameraFollow>();

            LoadGlowMaterial();
            BuildMap(_arena.transform);

            var mapMechGo = new GameObject("MapMechanics");
            mapMechGo.transform.SetParent(_arena.transform);
            var mapMech = mapMechGo.AddComponent<MapMechanics>();
            mapMech.config = _currentMapConfig;

            var wormGo = new GameObject("PlayerWorm");
            wormGo.transform.SetParent(_arena.transform);
            var lineRenderer = wormGo.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = 6f;
            lineRenderer.endWidth = 3f;
            lineRenderer.material = _glowMaterial;
            var wormRenderer = wormGo.AddComponent<WormRenderer>();
            wormRenderer.lineRenderer = lineRenderer;

            var headGo = new GameObject("Head");
            headGo.transform.SetParent(wormGo.transform);
            var headSprite = headGo.AddComponent<SpriteRenderer>();
            headSprite.sprite = CreateCircleSprite(new Color(0.42f, 0.31f, 1f), 32);
            headSprite.material = _glowMaterial;
            wormRenderer.headTransform = headGo.transform;
            wormRenderer.boostTrail = CreateBoostTrail(wormGo.transform);

            var botMgrGo = new GameObject("BotManager");
            botMgrGo.transform.SetParent(_arena.transform);
            var botManager = botMgrGo.AddComponent<BotManager>();
            botManager.arenaRadius = _currentMapConfig.arenaRadius;

            var vfxGo = new GameObject("DeathBurstVFX");
            vfxGo.transform.SetParent(_arena.transform);
            var deathBurstVFX = vfxGo.AddComponent<DeathBurstVFX>();

            var canvasGo = new GameObject("HUDCanvas");
            canvasGo.transform.SetParent(_arena.transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var leaderboardGo = new GameObject("Leaderboard");
            leaderboardGo.transform.SetParent(canvasGo.transform);
            var rect = leaderboardGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(10f, 0f);
            rect.sizeDelta = new Vector2(280f, 400f);
            var leaderboardUI = leaderboardGo.AddComponent<LeaderboardUI>();

            var comboGo = new GameObject("ComboCallout");
            comboGo.transform.SetParent(canvasGo.transform);
            var comboRect = comboGo.AddComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(0.5f, 0.5f);
            comboRect.anchorMax = new Vector2(0.5f, 0.5f);
            comboRect.pivot = new Vector2(0.5f, 0.5f);
            comboRect.anchoredPosition = new Vector2(0f, 150f);
            comboRect.sizeDelta = new Vector2(400f, 80f);
            var comboCalloutUI = comboGo.AddComponent<ComboCalloutUI>();

            var mgr = _arena.AddComponent<GameManager>();
            mgr.wormRenderer = wormRenderer;
            mgr.botManager = botManager;
            mgr.matchMode = mode;
            mgr.cameraFollow = camGo.GetComponent<CameraFollow>();
            mgr.leaderboardUI = leaderboardUI;
            mgr.comboCalloutUI = comboCalloutUI;
            mgr.deathBurstVFX = deathBurstVFX;
            mgr.mapMechanics = mapMech;
            mgr.mapConfig = _currentMapConfig;
            mgr.arenaRadius = _currentMapConfig.arenaRadius;
            mgr.botCount = 15;
            mgr.bootstrapper = this;
            // BUG-02 FIX: explicitly assign the HUD canvas so GameManager.CreateHUD()
            // doesn't fall back to FindObjectOfType<Canvas>() which returns the
            // ScreenManager canvas (sort order 50) and hides all HUD elements behind the game world.
            mgr.hudCanvas = canvas;

            // Biome ambient VFX — per-map particle atmosphere
            BiomeAmbientVFX.SpawnForMap(_currentMapConfig.mapName, _arena.transform);

            // Emote wheel — initialized on the HUD canvas
            var emoteWheelGo = new GameObject("EmoteWheel");
            emoteWheelGo.transform.SetParent(_arena.transform);
            var emoteWheel = emoteWheelGo.AddComponent<EmoteWheelUI>();
            emoteWheel.Initialize(canvasGo.transform);
        }

        private void StartNetworkedArena(MatchMode mode, string connectHost = null, int connectPort = 12345)
        {
            bool isLocalServer = connectHost == null || connectHost == "127.0.0.1";
            var modeConfig = ModeConfig.GetDefault(mode);
            var camGo = new GameObject("GameCamera");
            camGo.transform.SetParent(_arena.transform);
            var camComp = camGo.AddComponent<Camera>();
            camComp.orthographic = true;
            camComp.orthographicSize = Mathf.Min(_currentMapConfig.arenaRadius * 0.075f, 70f);
            camComp.clearFlags = CameraClearFlags.SolidColor;
            camComp.backgroundColor = _currentMapConfig.bgColor;
            camComp.nearClipPlane = 0.1f;
            camComp.farClipPlane = 1000f;
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<CameraFollow>();

            LoadGlowMaterial();
            BuildMap(_arena.transform);

            var wormGo = new GameObject("PlayerWorm");
            wormGo.transform.SetParent(_arena.transform);
            var lineRenderer = wormGo.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = 6f;
            lineRenderer.endWidth = 3f;
            lineRenderer.material = _glowMaterial;
            var wormRenderer = wormGo.AddComponent<WormRenderer>();
            wormRenderer.lineRenderer = lineRenderer;

            var headGo = new GameObject("Head");
            headGo.transform.SetParent(wormGo.transform);
            var headSprite = headGo.AddComponent<SpriteRenderer>();
            headSprite.sprite = CreateCircleSprite(new Color(0.42f, 0.31f, 1f), 32);
            headSprite.material = _glowMaterial;
            wormRenderer.headTransform = headGo.transform;
            wormRenderer.boostTrail = CreateBoostTrail(wormGo.transform);

            var botMgrGo = new GameObject("BotManager");
            botMgrGo.transform.SetParent(_arena.transform);
            var botManager = botMgrGo.AddComponent<BotManager>();
            botManager.arenaRadius = _currentMapConfig.arenaRadius;

            var canvasGo = new GameObject("HUDCanvas");
            canvasGo.transform.SetParent(_arena.transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var leaderboardGo = new GameObject("Leaderboard");
            leaderboardGo.transform.SetParent(canvasGo.transform);
            var rect = leaderboardGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(10f, 0f);
            rect.sizeDelta = new Vector2(280f, 400f);
            var leaderboardUI = leaderboardGo.AddComponent<LeaderboardUI>();

            var comboGo = new GameObject("ComboCallout");
            comboGo.transform.SetParent(canvasGo.transform);
            var comboRect = comboGo.AddComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(0.5f, 0.5f);
            comboRect.anchorMax = new Vector2(0.5f, 0.5f);
            comboRect.pivot = new Vector2(0.5f, 0.5f);
            comboRect.anchoredPosition = new Vector2(0f, 150f);
            comboRect.sizeDelta = new Vector2(400f, 80f);
            var comboCalloutUI = comboGo.AddComponent<ComboCalloutUI>();

            if (isLocalServer)
            {
                var serverMgr = _arena.AddComponent<ServerNetworkManager>();
                serverMgr.SetModeConfig(modeConfig);
            }
            var netMgr = _arena.AddComponent<ClientNetworkManager>();
            netMgr.ConnectTo(connectHost ?? "127.0.0.1", connectPort);
            var netGameMgr = _arena.AddComponent<NetworkedGameManager>();
            netGameMgr.wormRenderer = wormRenderer;
            netGameMgr.botManager = botManager;
            netGameMgr.cameraFollow = camGo.GetComponent<CameraFollow>();
            netGameMgr.leaderboardUI = leaderboardUI;
            netGameMgr.comboCalloutUI = comboCalloutUI;
            netGameMgr.bootstrapper = this;
            netGameMgr.mapConfig = _currentMapConfig;
        }

        private void LoadGlowMaterial()
        {
            Shader shader = Shader.Find("WormCore/GlowUnlit");
            if (shader != null)
            {
                _glowMaterial = new Material(shader);
                _glowMaterial.SetColor("_GlowColor", new Color(0.42f, 0.31f, 1f));
                _glowMaterial.SetFloat("_GlowIntensity", 1.5f);
                _glowMaterial.SetFloat("_PulseSpeed", 1.2f);
            }
            else
            {
                _glowMaterial = new Material(Shader.Find("Sprites/Default"));
            }
        }

        private void BuildMap(Transform parent)
        {
            float hs = _currentMapConfig.arenaRadius;
            float spacing = 100f;

            var gc = _currentMapConfig.gridColor;
            Material gridMat = new Material(_glowMaterial != null ? _glowMaterial.shader : Shader.Find("Sprites/Default"));
            gridMat.color = new Color(gc.r, gc.g, gc.b, 0.06f);

            for (float x = -hs; x <= hs + 0.01f; x += spacing)
                DrawLine(parent, new Vector3(x, -hs, 0.1f), new Vector3(x, hs, 0.1f), gridMat, 0.3f);
            for (float y = -hs; y <= hs + 0.01f; y += spacing)
                DrawLine(parent, new Vector3(-hs, y, 0.1f), new Vector3(hs, y, 0.1f), gridMat, 0.3f);

            // Arena boundary — circle drawn as 64-segment polygon
            var wc = _currentMapConfig.wallColor;
            Material wallMat = new Material(_glowMaterial != null ? _glowMaterial.shader : Shader.Find("Sprites/Default"));
            wallMat.color = new Color(wc.r, wc.g, wc.b, 0.6f);
            DrawCircle(parent, hs, 64, wallMat, 3f);

            // Danger zone ring just inside boundary
            Material dangerMat = new Material(Shader.Find("Sprites/Default"));
            dangerMat.color = new Color(1f, 0.3f, 0.2f, 0.12f);
            DrawCircle(parent, hs * 0.97f, 48, dangerMat, 12f);
        }

        private static void DrawLine(Transform parent, Vector3 start, Vector3 end, Material mat, float width)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(parent);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.startWidth = width;
            lr.endWidth = width;
            lr.material = mat;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }

        private static void DrawCircle(Transform parent, float radius, int segments, Material mat, float width)
        {
            var go = new GameObject("ArenaBorder");
            go.transform.SetParent(parent);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = segments + 1;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.material = mat;
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        /// <summary>Creates a procedural circle sprite. Replaces per-class texture generation.</summary>
        public static Sprite CreateCircleSprite(Color color, int resolution = 32)
        {
            var tex = new Texture2D(resolution, resolution);
            float half = resolution / 2f;
            float r = half * 0.85f;
            for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                {
                    float dx = x - half, dy = y - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 1f - Mathf.Clamp01((d - r) / 2f);
                    tex.SetPixel(x, y, d < r ? color : new Color(color.r, color.g, color.b, alpha * 0.3f));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
                new Vector2(0.5f, 0.5f), resolution);
        }

        private static ParticleSystem CreateBoostTrail(Transform parent)
        {
            var go = new GameObject("BoostTrail");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.42f, 0.31f, 1f, 0.5f),
                new Color(0.25f, 0.88f, 0.77f, 0.2f));
            main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startLifetime = 0.6f;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = false;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null) particleShader = Shader.Find("Sprites/Default");
            renderer.material = new Material(particleShader);
            renderer.material.color = new Color(0.42f, 0.31f, 1f, 0.4f);

            return ps;
        }

        private static string ParseArg(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }
    }
}
