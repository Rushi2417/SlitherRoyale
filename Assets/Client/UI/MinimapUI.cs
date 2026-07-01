using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WormCore;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// In-arena minimap overlay. Attach to the HUD Canvas; call Initialize() once,
    /// then UpdateMinimap() every frame from GameManager.
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        private RawImage _mapTexture;
        private Texture2D _mapTex;
        private List<Image> _dotPool;
        private Image _playerDot;
        private Image _borderRing;
        private float _arenaRadius;
        private const int TexSize = 128;
        private const int MaxDots = 24;

        // BUG-21 FIX: pre-allocated clear buffer so ClearTexture is a single native
        // buffer copy instead of 16,384 managed SetPixel calls every frame.
        private byte[] _clearPixels;

        private static readonly Color BgColor      = new Color(0.04f, 0.06f, 0.10f, 0.85f);
        private static readonly Color PlayerColor   = new Color(0.42f, 0.31f, 1f, 1f);
        private static readonly Color BotColor      = new Color(0.25f, 0.88f, 0.77f, 0.8f);
        private static readonly Color BorderColor   = new Color(0.42f, 0.31f, 1f, 0.4f);
        private static readonly Color ShrinkColor   = new Color(1f, 0.42f, 0.36f, 0.5f);

        public void Initialize(float arenaRadius, Transform parentCanvas)
        {
            _arenaRadius = arenaRadius;
            _dotPool = new List<Image>(MaxDots);

            // Container
            var containerGo = new GameObject("MinimapContainer");
            containerGo.transform.SetParent(parentCanvas, false);
            var containerRT = containerGo.AddComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(1f, 0f);
            containerRT.anchorMax = new Vector2(1f, 0f);
            containerRT.pivot = new Vector2(1f, 0f);
            containerRT.anchoredPosition = new Vector2(-16f, 16f);
            containerRT.sizeDelta = new Vector2(150f, 150f);

            // Background circle
            var bgGo = new GameObject("MinimapBG");
            bgGo.transform.SetParent(containerGo.transform, false);
            var bgRT = bgGo.AddComponent<RectTransform>();
            bgRT.anchorMin = bgRT.anchorMax = new Vector2(0.5f, 0.5f);
            bgRT.pivot = new Vector2(0.5f, 0.5f);
            bgRT.anchoredPosition = Vector2.zero;
            bgRT.sizeDelta = new Vector2(150f, 150f);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = BgColor;
            bgImg.sprite = CreateCircleSprite(Color.white, 64);
            bgImg.type = Image.Type.Simple;

            // BUG-21 FIX: initialise the pre-cleared pixel buffer once.
            // Each pixel is RGBA32: R=10, G=15, B=25, A=0 (transparent ink-void tint).
            _clearPixels = new byte[TexSize * TexSize * 4];
            for (int i = 0; i < _clearPixels.Length; i += 4)
            {
                _clearPixels[i]     = 10;  // R
                _clearPixels[i + 1] = 15;  // G
                _clearPixels[i + 2] = 25;  // B
                _clearPixels[i + 3] = 0;   // A (fully transparent)
            }

            // Minimap texture
            _mapTex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            ClearTexture();
            _mapTex.Apply();

            var rawGo = new GameObject("MinimapTex");
            rawGo.transform.SetParent(containerGo.transform, false);
            var rawRT = rawGo.AddComponent<RectTransform>();
            rawRT.anchorMin = rawRT.anchorMax = new Vector2(0.5f, 0.5f);
            rawRT.pivot = new Vector2(0.5f, 0.5f);
            rawRT.anchoredPosition = Vector2.zero;
            rawRT.sizeDelta = new Vector2(140f, 140f);
            _mapTexture = rawGo.AddComponent<RawImage>();
            _mapTexture.texture = _mapTex;

            // Border ring
            var borderGo = new GameObject("MinimapBorder");
            borderGo.transform.SetParent(containerGo.transform, false);
            var borderRT = borderGo.AddComponent<RectTransform>();
            borderRT.anchorMin = borderRT.anchorMax = new Vector2(0.5f, 0.5f);
            borderRT.pivot = new Vector2(0.5f, 0.5f);
            borderRT.anchoredPosition = Vector2.zero;
            borderRT.sizeDelta = new Vector2(150f, 150f);
            _borderRing = borderGo.AddComponent<Image>();
            _borderRing.color = BorderColor;
            _borderRing.sprite = CreateRingSprite(64, 0.88f, 0.96f);

            // Player dot (always on top)
            _playerDot = CreateDot(containerGo.transform, PlayerColor, 10f);

            // Pre-allocate bot dots
            for (int i = 0; i < MaxDots; i++)
                _dotPool.Add(CreateDot(containerGo.transform, BotColor, 6f));

            // Label
            var labelGo = new GameObject("MinimapLabel");
            labelGo.transform.SetParent(containerGo.transform, false);
            var labelRT = labelGo.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0.5f, 1f);
            labelRT.anchorMax = new Vector2(0.5f, 1f);
            labelRT.pivot = new Vector2(0.5f, 1f);
            labelRT.anchoredPosition = new Vector2(0, 4f);
            labelRT.sizeDelta = new Vector2(100f, 16f);
            var label = labelGo.AddComponent<Text>();
            label.text = "MINIMAP";
            label.color = new Color(0.66f, 0.69f, 0.76f, 0.5f);
            label.fontSize = 9;
            label.alignment = TextAnchor.UpperCenter;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public void UpdateMinimap(List<WormState> worms, int localPlayerId, float shrinkRadius = -1f)
        {
            if (_mapTex == null || worms == null) return;

            ClearTexture();

            // Draw shrink zone
            if (shrinkRadius > 0f && shrinkRadius < _arenaRadius)
            {
                float ratio = shrinkRadius / _arenaRadius;
                int cx = TexSize / 2, cy = TexSize / 2;
                int r = Mathf.RoundToInt(ratio * TexSize * 0.5f);
                DrawCircleOnTex(cx, cy, r, ShrinkColor);
            }

            _mapTex.Apply();

            // Position player dot
            if (worms.Count > 0)
            {
                var player = worms[0];
                Vector2 mapPos = WorldToMinimap(player.X, player.Y);
                _playerDot.rectTransform.anchoredPosition = mapPos;
                _playerDot.enabled = !player.IsDead;
            }

            // Position bot dots
            int dotIndex = 0;
            for (int i = 1; i < worms.Count && dotIndex < _dotPool.Count; i++)
            {
                var worm = worms[i];
                var dot = _dotPool[dotIndex++];
                if (worm.IsDead)
                {
                    dot.enabled = false;
                    continue;
                }
                Vector2 mapPos = WorldToMinimap(worm.X, worm.Y);
                dot.rectTransform.anchoredPosition = mapPos;
                dot.enabled = true;
                // Color by mass relative to player
                float massRatio = worms.Count > 0 ? worm.Mass / (worms[0].Mass + 0.001f) : 1f;
                dot.color = massRatio > 1.3f
                    ? new Color(1f, 0.42f, 0.36f, 0.9f)   // bigger â€” red = danger
                    : massRatio < 0.7f
                        ? new Color(0.25f, 0.88f, 0.77f, 0.7f)  // smaller â€” mint = easy prey
                        : new Color(1f, 0.79f, 0.3f, 0.8f);     // similar â€” gold = caution
            }
            // Hide unused dots
            for (int i = dotIndex; i < _dotPool.Count; i++)
                _dotPool[i].enabled = false;
        }

        private Vector2 WorldToMinimap(float wx, float wy)
        {
            float nx = Mathf.Clamp01((wx / _arenaRadius + 1f) * 0.5f);
            float ny = Mathf.Clamp01((wy / _arenaRadius + 1f) * 0.5f);
            return new Vector2((nx - 0.5f) * 140f, (ny - 0.5f) * 140f);
        }

        private void ClearTexture()
        {
            // BUG-21 FIX: single native buffer copy instead of 16,384 SetPixel calls.
            // Pre-allocated _clearPixels buffer is filled once at Initialize().
            _mapTex.LoadRawTextureData(_clearPixels);
        }

        private void DrawCircleOnTex(int cx, int cy, int radius, Color color)
        {
            int r2 = radius * radius;
            int x0 = Mathf.Max(0, cx - radius), x1 = Mathf.Min(TexSize - 1, cx + radius);
            int y0 = Mathf.Max(0, cy - radius), y1 = Mathf.Min(TexSize - 1, cy + radius);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    int dist2 = dx * dx + dy * dy;
                    if (dist2 <= r2)
                    {
                        float edge = 1f - Mathf.Clamp01((Mathf.Sqrt(dist2) - radius * 0.85f) / (radius * 0.15f));
                        var c = _mapTex.GetPixel(x, y);
                        _mapTex.SetPixel(x, y, Color.Lerp(c, color, edge * color.a));
                    }
                }
        }

        private static Image CreateDot(Transform parent, Color color, float size)
        {
            var go = new GameObject("Dot");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.sprite = CreateCircleSprite(Color.white, 16);
            return img;
        }

        private static Sprite CreateCircleSprite(Color color, int res)
        {
            var tex = new Texture2D(res, res);
            float half = res / 2f;
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float dx = x - half, dy = y - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 1f - Mathf.Clamp01((d - half * 0.8f) / (half * 0.2f));
                    tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        }

        private static Sprite CreateRingSprite(int res, float innerFrac, float outerFrac)
        {
            var tex = new Texture2D(res, res);
            float half = res / 2f;
            float inner = half * innerFrac, outer = half * outerFrac;
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float dx = x - half, dy = y - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 0f;
                    if (d >= inner && d <= outer)
                    {
                        float t = (d - inner) / (outer - inner);
                        alpha = 1f - Mathf.Abs(t * 2f - 1f);
                    }
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        }
    }
}
