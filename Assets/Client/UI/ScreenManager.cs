using System.Collections.Generic;
using SlitherRoyale.Client.Monetization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Manages all UI screens. Owns the master Canvas and EventSystem.
    /// Screens register themselves; NavigateTo shows one and hides all others.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        private Dictionary<string, UIScreen> _screens = new();
        private UIScreen _currentScreen;
        private Canvas   _canvas;

        private static ScreenManager _instance;
        public  static ScreenManager Instance => _instance;

        private void Awake()
        {
            _instance = this;
            _screens  = new Dictionary<string, UIScreen>();
            SetupCanvas();
            SetupEventSystem();
        }

        private void SetupCanvas()
        {
            // Master canvas — ScreenSpaceOverlay, high sort order
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder  = 50; // above arena content

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight  = 0.4f; // bias toward width for wide phones

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        private void SetupEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        /// <summary>
        /// Instantiates and registers a screen. The screen's RectTransform is
        /// stretched to fill the canvas by UIScreen.Awake().
        /// </summary>
        public T Register<T>() where T : UIScreen
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(transform, false);

            // Pre-stretch before Awake runs so children position correctly
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin    = Vector2.zero;
            rt.anchorMax    = Vector2.one;
            rt.offsetMin    = Vector2.zero;
            rt.offsetMax    = Vector2.zero;
            rt.localScale   = Vector3.one;
            rt.localPosition = Vector3.zero;

            go.SetActive(false);
            var screen = go.AddComponent<T>();
            _screens[screen.ScreenId] = screen;
            return screen;
        }

        // Screens that suppress banner ads
        public static readonly HashSet<string> NoBannerScreens = new HashSet<string>()
        {
            "SplashScreen", "MatchmakingScreen", "ResultsScreen"
        };

        public void NavigateTo<T>(object data = null) where T : UIScreen
            => NavigateTo(typeof(T).Name, data);

        public void NavigateTo(string screenId, object data = null)
        {
            if (_currentScreen != null)
            {
                _currentScreen.OnExit();
                if (!NoBannerScreens.Contains(_currentScreen.ScreenId))
                    AdService.HideBanner();
            }

            if (_screens.TryGetValue(screenId, out var screen))
            {
                _currentScreen = screen;
                _currentScreen.OnEnter(this, data);
                if (!NoBannerScreens.Contains(screenId))
                    AdService.ShowBanner();
            }
            else
            {
                Debug.LogError($"[ScreenManager] Screen not registered: {screenId}");
            }
        }

        /// <summary>
        /// Hides all active screens (e.g. when entering gameplay).
        /// </summary>
        public void HideAll()
        {
            if (_currentScreen != null)
            {
                _currentScreen.OnExit();
                if (!NoBannerScreens.Contains(_currentScreen.ScreenId))
                    AdService.HideBanner();
                _currentScreen = null;
            }
        }

        public T Get<T>() where T : UIScreen
        {
            string id = typeof(T).Name;
            return _screens.TryGetValue(id, out var s) ? (T)s : null;
        }

        public void Clear()
        {
            foreach (var kv in _screens) Destroy(kv.Value.gameObject);
            _screens.Clear();
            _currentScreen = null;
        }
    }
}
