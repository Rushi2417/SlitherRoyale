using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SlitherRoyale.Client.Audio;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// In-match emote wheel — doc 02 LAUNCH feature.
    /// 6 emotes in a radial layout, shown on button hold.
    /// Emotes are purely visual (text/icon burst above worm).
    /// </summary>
    public class EmoteWheelUI : MonoBehaviour
    {
        public static EmoteWheelUI Instance { get; private set; }

        private static readonly string[] EmoteLabels  = { "😂", "👍", "💀", "🔥", "👑", "🐍" };
        private static readonly string[] EmoteNames   = { "LOL", "GG", "RIP", "LIT", "KING", "SNEK" };
        private static readonly Color[]  EmoteColors  =
        {
            new Color(1f, 0.79f, 0.3f),   // gold
            new Color(0.25f, 0.88f, 0.77f), // bio-mint
            new Color(1f, 0.42f, 0.36f),   // ember-coral
            new Color(1f, 0.6f, 0.2f),     // orange
            new Color(0.42f, 0.31f, 1f),   // arc-violet
            new Color(0.6f, 1f, 0.4f),     // green
        };

        private GameObject _wheelRoot;
        private Image[]    _sliceImages;
        private Text[]     _sliceLabels;
        private int        _hoveredSlice = -1;
        private bool       _isOpen;

        // Emote callout UI (displayed above worm in world space)
        private Text   _calloutText;
        private Canvas _calloutCanvas;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(Transform canvasRoot)
        {
            BuildWheel(canvasRoot);
        }

        private void BuildWheel(Transform canvasRoot)
        {
            // Wheel container — center screen
            _wheelRoot = new GameObject("EmoteWheel");
            _wheelRoot.transform.SetParent(canvasRoot, false);
            var rootRT = _wheelRoot.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0.5f, 0.5f);
            rootRT.anchorMax = new Vector2(0.5f, 0.5f);
            rootRT.sizeDelta = new Vector2(260f, 260f);
            rootRT.anchoredPosition = Vector2.zero;

            // Dark background disk
            var bgTex = CreateCircleTex(128, new Color(0.04f, 0.06f, 0.09f, 0.85f));
            var bgGo = new GameObject("WheelBg");
            bgGo.transform.SetParent(_wheelRoot.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.sizeDelta = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = Sprite.Create(bgTex, new Rect(0,0,128,128), new Vector2(0.5f,0.5f), 128f);

            _sliceImages = new Image[EmoteLabels.Length];
            _sliceLabels = new Text[EmoteLabels.Length];

            float radius = 80f;
            for (int i = 0; i < EmoteLabels.Length; i++)
            {
                float angle = i * (360f / EmoteLabels.Length) * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;

                var sliceGo = new GameObject($"Emote_{EmoteNames[i]}");
                sliceGo.transform.SetParent(_wheelRoot.transform, false);
                var sliceRT = sliceGo.AddComponent<RectTransform>();
                sliceRT.anchorMin = new Vector2(0.5f, 0.5f);
                sliceRT.anchorMax = new Vector2(0.5f, 0.5f);
                sliceRT.sizeDelta = new Vector2(64f, 64f);
                sliceRT.anchoredPosition = new Vector2(x, y);

                _sliceImages[i] = sliceGo.AddComponent<Image>();
                _sliceImages[i].color = EmoteColors[i] * 0.8f;

                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(sliceGo.transform, false);
                var labelRT = labelGo.AddComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one; labelRT.sizeDelta = Vector2.zero;
                _sliceLabels[i] = labelGo.AddComponent<Text>();
                _sliceLabels[i].text = EmoteLabels[i];
                _sliceLabels[i].fontSize = 22;
                _sliceLabels[i].alignment = TextAnchor.MiddleCenter;
                _sliceLabels[i].font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                _sliceLabels[i].color = Color.white;
            }

            _wheelRoot.SetActive(false);
        }

        private void Update()
        {
            // Open with Tab/hold duration
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.E))
                Open();
            if (Input.GetKeyUp(KeyCode.Tab) || Input.GetKeyUp(KeyCode.E))
                Close();

            if (!_isOpen) return;

            // Detect hovered slice from mouse position relative to wheel center
            Vector2 center = _wheelRoot.transform.position;
            Vector2 dir = (Vector2)Input.mousePosition - center;
            if (dir.magnitude > 20f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x);
                float step  = 2f * Mathf.PI / EmoteLabels.Length;
                int slice   = Mathf.RoundToInt(angle / step) % EmoteLabels.Length;
                if (slice < 0) slice += EmoteLabels.Length;
                SetHovered(slice);

                if (Input.GetMouseButtonDown(0))
                    SelectEmote(_hoveredSlice);
            }
        }

        private void SetHovered(int idx)
        {
            _hoveredSlice = idx;
            for (int i = 0; i < _sliceImages.Length; i++)
            {
                float scale = i == idx ? 1.2f : 1f;
                _sliceImages[i].transform.localScale = Vector3.one * scale;
                _sliceImages[i].color = i == idx
                    ? EmoteColors[i]
                    : EmoteColors[i] * 0.7f;
            }
        }

        public void Open()
        {
            _isOpen = true;
            _wheelRoot.SetActive(true);
            _hoveredSlice = -1;
        }

        public void Close()
        {
            _isOpen = false;
            _wheelRoot.SetActive(false);
        }

        private void SelectEmote(int idx)
        {
            if (idx < 0 || idx >= EmoteLabels.Length) return;
            AudioManager.Instance?.Play(AudioManager.SfxType.UITap);
            ShowCallout(EmoteLabels[idx], EmoteColors[idx]);
            Close();
        }

        private void ShowCallout(string emote, Color color)
        {
            StopAllCoroutines();
            StartCoroutine(CalloutAnim(emote, color));
        }

        private IEnumerator CalloutAnim(string emote, Color color)
        {
            if (_calloutCanvas == null)
            {
                var go = new GameObject("EmoteCallout");
                _calloutCanvas = go.AddComponent<Canvas>();
                _calloutCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _calloutCanvas.sortingOrder = 200;
                var txt = new GameObject("Text");
                txt.transform.SetParent(go.transform, false);
                var rt = txt.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.7f);
                rt.anchorMax = new Vector2(0.5f, 0.7f);
                rt.sizeDelta = new Vector2(200f, 60f);
                _calloutText = txt.AddComponent<Text>();
                _calloutText.fontSize = 36;
                _calloutText.alignment = TextAnchor.MiddleCenter;
                _calloutText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            _calloutText.text = emote;
            _calloutText.color = color;
            _calloutText.transform.localScale = Vector3.one * 0.5f;

            // Pop in
            for (float t = 0; t < 0.2f; t += Time.deltaTime)
            {
                float s = Mathf.Lerp(0.5f, 1.2f, t / 0.2f);
                _calloutText.transform.localScale = Vector3.one * s;
                yield return null;
            }
            // Hold
            yield return new WaitForSeconds(1.5f);
            // Fade out
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                float a = Mathf.Lerp(1f, 0f, t / 0.3f);
                var c = _calloutText.color;
                _calloutText.color = new Color(c.r, c.g, c.b, a);
                yield return null;
            }
            _calloutText.text = "";
        }

        private static Texture2D CreateCircleTex(int size, Color c)
        {
            var tex = new Texture2D(size, size);
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r, dy = y - r;
                    float d = Mathf.Sqrt(dx*dx + dy*dy);
                    tex.SetPixel(x, y, d < r ? c : Color.clear);
                }
            tex.Apply();
            return tex;
        }
    }
}
