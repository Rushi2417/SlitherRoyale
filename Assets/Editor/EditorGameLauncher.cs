#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlitherRoyale.Editor
{
    /// <summary>
    /// Adds a "SlitherRoyale" menu to the Unity Editor toolbar.
    /// Lets you start the game from any scene without manually switching scenes.
    ///
    /// USAGE:
    ///   Top menu bar → SlitherRoyale → ▶ Play from Init Scene
    ///   Or press  Ctrl+Shift+P
    ///
    /// The Console window shows all logs in real time while in Play mode.
    /// The InGameDebugConsole overlay (F1 to toggle) is also active.
    /// </summary>
    public static class EditorGameLauncher
    {
        private const string InitScenePath = "Assets/Scenes/Init.unity";

        // ── Menu items ─────────────────────────────────────────────────────────

        [MenuItem("SlitherRoyale/▶  Play from Init Scene  _F5", false, 0)]
        public static void PlayFromInitScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            // Save any dirty scenes first
            bool saved = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            if (!saved) return;

            // Open the Init scene
            var scene = EditorSceneManager.OpenScene(InitScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[EditorGameLauncher] Could not open Init scene at: {InitScenePath}");
                return;
            }

            // Enter Play mode
            EditorApplication.isPlaying = true;
            Debug.Log("[EditorGameLauncher] Entering Play mode from Init scene. " +
                      "Open Window > General > Console for live logs. Press F1 to toggle on-screen overlay.");
        }

        [MenuItem("SlitherRoyale/▶  Play from Init Scene  _F5", true)]
        private static bool ValidatePlay() => true;

        // ──────────────────────────────────────────────────────────────────────

        [MenuItem("SlitherRoyale/⏹  Stop", false, 1)]
        public static void Stop()
        {
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
        }

        [MenuItem("SlitherRoyale/⏹  Stop", true)]
        private static bool ValidateStop() => EditorApplication.isPlaying;

        // ──────────────────────────────────────────────────────────────────────

        [MenuItem("SlitherRoyale/🗄  Open Console Window", false, 20)]
        public static void OpenConsole()
        {
            // Reflect into internal EditorWindow type to open Console
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(EditorWindow));
            var type = assembly.GetType("UnityEditor.ConsoleWindow");
            if (type != null)
                EditorWindow.GetWindow(type, false, "Console", true);
        }

        // ──────────────────────────────────────────────────────────────────────

        [MenuItem("SlitherRoyale/📦  Build Android APK", false, 40)]
        public static void BuildAndroid()
        {
            BuildScript.BuildAndroid();
        }

        [MenuItem("SlitherRoyale/📦  Build Android APK", true)]
        private static bool ValidateBuildAndroid() => !EditorApplication.isPlaying && !EditorApplication.isCompiling;

        // ──────────────────────────────────────────────────────────────────────

        [MenuItem("SlitherRoyale/🔄  Clear Console", false, 60)]
        public static void ClearConsole()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(EditorWindow));
            var logEntries = assembly.GetType("UnityEditor.LogEntries");
            if (logEntries != null)
                logEntries.GetMethod("Clear")?.Invoke(null, null);
        }

        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws a status bar at the bottom of the Editor when in Play mode
        /// showing the current screen and frame rate.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RegisterStatusBar()
        {
            EditorApplication.update += UpdateStatusBar;
        }

        private static double _lastFpsTime;
        private static int    _frameCount;
        private static float  _fps;

        private static void UpdateStatusBar()
        {
            _frameCount++;
            if (EditorApplication.timeSinceStartup - _lastFpsTime >= 0.5)
            {
                _fps = (float)(_frameCount / (EditorApplication.timeSinceStartup - _lastFpsTime));
                _frameCount = 0;
                _lastFpsTime = EditorApplication.timeSinceStartup;
            }
        }
    }
}
#endif
