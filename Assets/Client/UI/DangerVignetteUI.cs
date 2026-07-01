using UnityEngine;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Full-screen edge vignette that intensifies in ember-coral (#FF6B5B)
    /// as a bigger worm gets close — the danger readable signal from doc 05 §3.5.
    /// Uses soft procedural gradient textures for a premium, glowing overlay.
    /// </summary>
    public class DangerVignetteUI : MonoBehaviour
    {
        private Image _topEdge;
        private Image _bottomEdge;
        private Image _leftEdge;
        private Image _rightEdge;

        private float _currentDanger;
        private float _targetDanger;

        private static readonly Color EmberCoralFull = new Color(1.000f, 0.420f, 0.357f, 0.65f); // #FF6B5B

        public void Initialize(Transform canvasRoot)
        {
            // Procedural gradient sprite fading from fully opaque white to transparent
            var gradSprite = MakeVignetteGradientSprite(64);

            _topEdge    = CreateEdge(canvasRoot, "Vignette_Top",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 120f), new Vector2(0.5f, 1f), gradSprite, 180f);

            _bottomEdge = CreateEdge(canvasRoot, "Vignette_Bot",
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 120f), new Vector2(0.5f, 0f), gradSprite, 0f);

            _leftEdge   = CreateEdge(canvasRoot, "Vignette_Left",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(120f, 0f), new Vector2(0f, 0.5f), gradSprite, 90f);

            _rightEdge  = CreateEdge(canvasRoot, "Vignette_Right",
                new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(120f, 0f), new Vector2(1f, 0.5f), gradSprite, 270f);

            SetAlpha(0f);
        }

        public void SetDanger(float t)
        {
            _targetDanger = Mathf.Clamp01(t);
        }

        private void Update()
        {
            // Smooth transition
            _currentDanger = Mathf.Lerp(_currentDanger, _targetDanger, Time.deltaTime * 6f);

            // Subtle pulsing at high danger levels to capture player attention
            float pulse = _currentDanger > 0.4f
                ? _currentDanger * (0.85f + 0.15f * Mathf.Sin(Time.time * 8f))
                : _currentDanger;

            SetAlpha(pulse * EmberCoralFull.a);
        }

        private void SetAlpha(float alpha)
        {
            Color c = new Color(EmberCoralFull.r, EmberCoralFull.g, EmberCoralFull.b, alpha);
            if (_topEdge    != null) _topEdge.color    = c;
            if (_bottomEdge != null) _bottomEdge.color = c;
            if (_leftEdge   != null) _leftEdge.color   = c;
            if (_rightEdge  != null) _rightEdge.color  = c;
        }

        private Image CreateEdge(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 sizeDelta, Vector2 pivot, Sprite gradSprite, float rotationZ)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot     = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            var img = go.AddComponent<Image>();
            img.sprite = gradSprite;
            img.type   = Image.Type.Simple;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// Generates a 1D gradient sprite that goes from white (opaque) to transparent.
        /// When rotated, this creates a soft border shadow/vignette.
        /// </summary>
        private static Sprite MakeVignetteGradientSprite(int height)
        {
            var tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                // Inverse quadratic curve for smooth transition
                float alpha = Mathf.Pow(1f - t, 2.5f);
                tex.SetPixel(0, y, new Color(1f, 1f, 1f, alpha));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, height), new Vector2(0.5f, 0f), 100f);
        }
    }
}
