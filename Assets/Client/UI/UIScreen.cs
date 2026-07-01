using UnityEngine;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Base class for all full-screen UI panels.
    /// Guarantees: fills the parent canvas, has a CanvasGroup for fade control,
    /// respects device safe-area (notch/home-bar insets), and starts hidden.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        public string ScreenId => GetType().Name;

        protected CanvasGroup _group;
        protected RectTransform _rect;

        // Bio-Arcade color tokens — available to every screen
        protected static readonly Color InkVoid   = new Color(0.043f, 0.055f, 0.078f, 1f); // #0B0E14
        protected static readonly Color ArcViolet = new Color(0.424f, 0.310f, 1.000f, 1f); // #6C4FFF
        protected static readonly Color EmberCoral= new Color(1.000f, 0.420f, 0.357f, 1f); // #FF6B5B
        protected static readonly Color BioMint   = new Color(0.247f, 0.878f, 0.773f, 1f); // #3FE0C5
        protected static readonly Color GoldYolk  = new Color(1.000f, 0.788f, 0.302f, 1f); // #FFC94D
        protected static readonly Color FogGrey   = new Color(0.663f, 0.690f, 0.765f, 1f); // #A9B0C3
        protected static readonly Color CardBg    = new Color(0.070f, 0.090f, 0.130f, 1f);
        protected static readonly Color PanelBg   = new Color(0.050f, 0.070f, 0.100f, 0.95f);
        protected static readonly Color DividerCol= new Color(0.180f, 0.200f, 0.280f, 1f);

        protected virtual void Awake()
        {
            // Stretch to fill parent canvas completely
            _rect = GetComponent<RectTransform>();
            if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin    = Vector2.zero;
            _rect.anchorMax    = Vector2.one;
            _rect.offsetMin    = Vector2.zero;
            _rect.offsetMax    = Vector2.zero;
            _rect.localScale   = Vector3.one;
            _rect.localPosition = Vector3.zero;

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

            // Start hidden
            _group.alpha          = 0f;
            _group.interactable   = false;
            _group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            _group.alpha          = 1f;
            _group.interactable   = true;
            _group.blocksRaycasts = true;
        }

        public virtual void Hide()
        {
            _group.alpha          = 0f;
            _group.interactable   = false;
            _group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public virtual void OnEnter(ScreenManager sm, object data) { Show(); }
        public virtual void OnExit() { Hide(); }

        // ── Shared builder helpers ────────────────────────────────────────────

        /// <summary>Full-screen background image, stretched to fill.</summary>
        protected Image AddFullBg(Color color)
        {
            var go  = new GameObject("BG");
            go.transform.SetParent(transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Creates an Image at an anchored position.</summary>
        protected Image AddImage(string name, Vector2 anchorMin, Vector2 anchorMax,
                                  Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        /// <summary>Creates an Image using center-anchor + size (legacy helper).</summary>
        protected Image AddImage(string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            return img;
        }

        /// <summary>Creates a Text label at an anchored position.</summary>
        protected Text AddText(string content, Color color, int size,
                                Vector2 pos, FontStyle style = FontStyle.Normal,
                                TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Text_" + content.Substring(0, Mathf.Min(8, content.Length)));
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(900f, 80f);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content;
            txt.color = color;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.alignment = anchor;
            txt.supportRichText = true;
            return txt;
        }

        /// <summary>Creates a Text label parented to a given transform.</summary>
        protected Text AddTextOn(Transform parent, string content, Color color,
                                  int size, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content;
            txt.color = color;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.supportRichText = true;
            return txt;
        }

        /// <summary>Creates a fully styled button with a solid color background.</summary>
        protected Button AddButton(string label, Vector2 pos, Vector2 size,
                                    Color bg, System.Action onClick,
                                    int fontSize = 18, FontStyle style = FontStyle.Bold)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor      = bg;
            colors.highlightedColor = new Color(Mathf.Min(bg.r + 0.15f, 1f), Mathf.Min(bg.g + 0.15f, 1f), Mathf.Min(bg.b + 0.15f, 1f));
            colors.pressedColor     = new Color(bg.r * 0.7f, bg.g * 0.7f, bg.b * 0.7f);
            colors.selectedColor    = bg;
            colors.fadeDuration     = 0.08f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());
            AddTextOn(go.transform, label, Color.white, fontSize, style);
            return btn;
        }

        /// <summary>Creates a pill/capsule button with rounded-looking corners via a procedural texture.</summary>
        protected Button AddPillButton(string label, Vector2 pos, Vector2 size,
                                        Color bg, System.Action onClick, int fontSize = 16)
        {
            var btn = AddButton(label, pos, size, bg, onClick, fontSize);
            var img = btn.GetComponent<Image>();
            img.sprite = MakeRoundRectSprite((int)size.x, (int)size.y, (int)(size.y / 2f));
            img.type = Image.Type.Sliced;
            return btn;
        }

        /// <summary>Generates a rounded-rectangle sprite procedurally.</summary>
        protected static Sprite MakeRoundRectSprite(int w, int h, int radius)
        {
            w = Mathf.Max(w, 4); h = Mathf.Max(h, 4);
            radius = Mathf.Clamp(radius, 2, Mathf.Min(w, h) / 2);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int cx = x < radius ? radius : x > w - 1 - radius ? w - 1 - radius : x;
                    int cy = y < radius ? radius : y > h - 1 - radius ? h - 1 - radius : y;
                    int dx = x - cx, dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = d <= radius ? (byte)255 : (byte)0;
                    pixels[y * w + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            int b = radius / 2;
            return Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect,
                new Vector4(b, b, b, b));
        }

        /// <summary>Creates a circle sprite (for avatars, dots, icons).</summary>
        protected static Sprite MakeCircleSprite(Color color, int res = 64)
        {
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            float r = res / 2f;
            var pixels = new Color32[res * res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float dx = x - r + 0.5f, dy = y - r + 0.5f;
                    float d  = Mathf.Sqrt(dx * dx + dy * dy);
                    float a  = Mathf.Clamp01(r - d);
                    pixels[y * res + x] = new Color32(
                        (byte)(color.r * 255), (byte)(color.g * 255),
                        (byte)(color.b * 255), (byte)(a * 255));
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        }

        /// <summary>Returns the current safe-area inset in canvas space.</summary>
        protected Vector4 GetSafeInsets(Canvas canvas)
        {
            var sa    = Screen.safeArea;
            float sw  = Screen.width;
            float sh  = Screen.height;
            var ref2  = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            float scaleX = ref2 != null ? ref2.referenceResolution.x / sw : 1f;
            float scaleY = ref2 != null ? ref2.referenceResolution.y / sh : 1f;
            // left, bottom, right, top insets in reference pixels
            return new Vector4(
                sa.xMin * scaleX,
                (sh - sa.yMax) * scaleY,
                (sw - sa.xMax) * scaleX,
                sa.yMin * scaleY);
        }
    }
}
