using System.Collections.Generic;
using UnityEngine;

namespace SlitherRoyale.Client
{
    /// <summary>
    /// In-game debug log overlay. Shows the last 20 Unity log messages on screen.
    /// 
    /// HOW TO USE:
    ///   - In Development builds: triple-tap anywhere to toggle the overlay
    ///   - In Editor: always visible; press F1 to toggle
    ///   - Attaches to the Bootstrapper GameObject automatically via InitRoutine
    ///   - Color coding: Red = errors, Yellow = warnings, White = logs
    /// 
    /// The overlay writes nothing to the UI Canvas — it uses OnGUI so it always
    /// renders on top of everything else, even during blank-screen conditions.
    /// </summary>
    public class InGameDebugConsole : MonoBehaviour
    {
        private struct LogEntry
        {
            public string Message;
            public LogType Type;
            public float Time;
        }

        private readonly List<LogEntry> _logs   = new List<LogEntry>(64);
        private Vector2                 _scroll  = Vector2.zero;
        private bool                    _visible = false;
        private GUIStyle                _boxStyle;
        private GUIStyle                _labelStyle;
        private GUIStyle                _errorStyle;
        private GUIStyle                _warnStyle;
        private float                   _tapTimer;
        private int                     _tapCount;
        private const int               MaxLogs  = 40;
        private const float             TapWindow = 0.5f;

        private void OnEnable()  => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            // In release builds, start hidden and only show after triple-tap
            _visible = false;
#else
            _visible = true;
#endif
            Application.logMessageReceived += OnLog;
            Debug.Log("[DebugConsole] Ready. Triple-tap (or F1) to toggle.");
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (_logs.Count >= MaxLogs) _logs.RemoveAt(0);
            _logs.Add(new LogEntry
            {
                Message = message,
                Type    = type,
                Time    = Time.realtimeSinceStartup,
            });
        }

        private void Update()
        {
            // Toggle with F1 in Editor / PC
            if (Input.GetKeyDown(KeyCode.F1)) _visible = !_visible;

            // Triple-tap toggle for device
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                if (Time.realtimeSinceStartup - _tapTimer > TapWindow)
                {
                    _tapCount = 0;
                    _tapTimer = Time.realtimeSinceStartup;
                }
                _tapCount++;
                if (_tapCount >= 3) { _visible = !_visible; _tapCount = 0; }
            }
        }

        private void InitStyles()
        {
            if (_boxStyle != null) return;

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.88f)) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = Mathf.Clamp(Screen.height / 70, 11, 22),
                wordWrap  = true,
                normal    = { textColor = Color.white },
            };

            _errorStyle = new GUIStyle(_labelStyle) { normal = { textColor = new Color(1f, 0.35f, 0.35f) } };
            _warnStyle  = new GUIStyle(_labelStyle) { normal = { textColor = new Color(1f, 0.8f, 0.2f) } };
        }

        private void OnGUI()
        {
            if (!_visible) return;

            InitStyles();

            float w = Screen.width;
            float h = Screen.height;
            float consoleH = h * 0.45f;
            float consoleY = h - consoleH - 10f;

            // Semi-transparent background panel
            GUI.Box(new Rect(5, consoleY, w - 10, consoleH), "", _boxStyle);

            // Header bar
            GUI.color = new Color(0.42f, 0.31f, 1f, 0.9f);
            GUI.Box(new Rect(5, consoleY, w - 10, 26), "");
            GUI.color = Color.white;
            GUI.Label(new Rect(10, consoleY + 4, w - 100, 22),
                      $"  ● SLITHER ROYALE DEBUG CONSOLE ({_logs.Count} msgs)", _labelStyle);

            // Clear button
            if (GUI.Button(new Rect(w - 75, consoleY + 3, 68, 20), "Clear"))
                _logs.Clear();

            // Scrollable log area
            Rect scrollArea = new Rect(5, consoleY + 28, w - 10, consoleH - 30);
            float lineH = _labelStyle.fontSize + 4;
            float totalH = _logs.Count * lineH;
            _scroll = GUI.BeginScrollView(scrollArea,
                                          _scroll,
                                          new Rect(0, 0, scrollArea.width - 16, Mathf.Max(totalH, scrollArea.height)));

            for (int i = _logs.Count - 1; i >= 0; i--)
            {
                var entry  = _logs[i];
                var style  = entry.Type == LogType.Error || entry.Type == LogType.Exception
                             ? _errorStyle
                             : entry.Type == LogType.Warning ? _warnStyle : _labelStyle;
                float y    = (_logs.Count - 1 - i) * lineH;
                string prefix = entry.Type == LogType.Error || entry.Type == LogType.Exception ? "✖ "
                              : entry.Type == LogType.Warning ? "⚠ " : "  ";
                GUI.Label(new Rect(4, y, scrollArea.width - 20, lineH * 2), prefix + entry.Message, style);
            }

            GUI.EndScrollView();
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
