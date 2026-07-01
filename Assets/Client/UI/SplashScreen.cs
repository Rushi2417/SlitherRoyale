using System.Collections;
using SlitherRoyale.Client.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace SlitherRoyale.Client.UI
{
    /// <summary>
    /// Bio-Arcade branded splash — shown for ~2s after the Unity logo.
    /// Doc 05 §3.1: "Logo animates in under 1s, straight into Home."
    /// </summary>
    public class SplashScreen : UIScreen
    {
        private Image _bg;
        private Text  _title;
        private Text  _subtitle;
        private Image _glowRing;
        private Image[] _particles;

        protected override void Awake()
        {
            base.Awake(); // stretches RectTransform

            // Full-screen dark background
            _bg = AddFullBg(InkVoid);

            // Ambient background particles (static dots for now, animated in coroutine)
            _particles = new Image[24];
            for (int i = 0; i < _particles.Length; i++)
            {
                float angle  = i * (360f / _particles.Length) * Mathf.Deg2Rad;
                float radius = 320f + (i % 3) * 80f;
                var pos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                _particles[i] = AddImage($"Dot{i}", pos, new Vector2(4f, 4f));
                _particles[i].color = new Color(ArcViolet.r, ArcViolet.g, ArcViolet.b, 0f);
                _particles[i].sprite = MakeCircleSprite(Color.white, 16);
            }

            // Glow ring behind title
            _glowRing = AddImage("GlowRing", Vector2.zero, new Vector2(340f, 340f));
            _glowRing.sprite = MakeRingSprite(256, 0.70f, 0.85f);
            _glowRing.color  = new Color(ArcViolet.r, ArcViolet.g, ArcViolet.b, 0f);

            // "SLITHER ROYALE" — display face, 64pt, arc-violet
            _title = AddText("SLITHER ROYALE", ArcViolet, 64, new Vector2(0f, 30f), FontStyle.Bold);
            _title.GetComponent<RectTransform>().sizeDelta = new Vector2(950f, 100f);
            _title.gameObject.SetActive(false);

            // "BIO-ARCADE ARENA" — subtitle, fog-grey
            _subtitle = AddText("BIO-ARCADE ARENA", FogGrey, 22, new Vector2(0f, -38f));
            _subtitle.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 60f);
            _subtitle.gameObject.SetActive(false);
        }

        public override void OnEnter(ScreenManager sm, object data)
        {
            Show();
            _glowRing.color = new Color(ArcViolet.r, ArcViolet.g, ArcViolet.b, 0f);
            _title.gameObject.SetActive(false);
            _subtitle.gameObject.SetActive(false);
            foreach (var p in _particles) p.color = new Color(ArcViolet.r, ArcViolet.g, ArcViolet.b, 0f);
            StartCoroutine(AnimateIn(sm));
        }

        private IEnumerator AnimateIn(ScreenManager sm)
        {
            yield return new WaitForSeconds(0.15f);

            // 1 — Fade in ambient particles
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float a = Mathf.SmoothStep(0f, 0.25f, t / 0.4f);
                for (int i = 0; i < _particles.Length; i++)
                    _particles[i].color = new Color(ArcViolet.r, ArcViolet.g, ArcViolet.b, a);
                yield return null;
            }

            // 2 — Glow ring expands in
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float p    = Mathf.SmoothStep(0f, 1f, t / 0.4f);
                float a    = Mathf.Lerp(0f, 0.5f, p);
                float s    = Mathf.Lerp(0.4f, 1f, p);
                _glowRing.color = new Color(ArcViolet.r, ArcViolet.g, ArcViolet.b, a);
                _glowRing.rectTransform.localScale = Vector3.one * s;
                yield return null;
            }

            // 3 — Title pops in (scale 0.5 → 1, ease-out cubic)
            _title.gameObject.SetActive(true);
            _title.color = new Color(ArcViolet.r, ArcViolet.g, ArcViolet.b, 0f);
            _title.transform.localScale = Vector3.one * 0.5f;
            t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float p     = t / 0.35f;
                float eased = 1f - Mathf.Pow(1f - p, 3f);
                _title.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, eased);
                _title.color = new Color(ArcViolet.r, ArcViolet.g, ArcViolet.b, eased);
                yield return null;
            }
            _title.transform.localScale = Vector3.one;
            _title.color = ArcViolet;

            // 4 — Subtitle fades in
            _subtitle.gameObject.SetActive(true);
            _subtitle.color = new Color(FogGrey.r, FogGrey.g, FogGrey.b, 0f);
            t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                float a = t / 0.25f;
                _subtitle.color = new Color(FogGrey.r, FogGrey.g, FogGrey.b, a);
                yield return null;
            }

            // 5 — Pulse ring + particles while hold
            float hold = 0f;
            while (hold < 0.8f)
            {
                hold += Time.deltaTime;
                float pulse = 1f + Mathf.Sin(hold * 5f) * 0.02f;
                _glowRing.rectTransform.localScale = Vector3.one * pulse;
                // Orbit particles slowly
                for (int i = 0; i < _particles.Length; i++)
                {
                    float angle  = (i * (360f / _particles.Length) + hold * 20f) * Mathf.Deg2Rad;
                    float radius = 320f + (i % 3) * 80f + Mathf.Sin(hold * 2f + i) * 10f;
                    _particles[i].rectTransform.anchoredPosition =
                        new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                }
                yield return null;
            }

            // 6 — Fade out and go to Home
            t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float a  = 1f - (t / 0.2f);
                _group.alpha = a;
                yield return null;
            }

            sm.NavigateTo<HomeScreen>();
        }

        private static Sprite MakeRingSprite(int res, float innerFrac, float outerFrac)
        {
            var tex  = new Texture2D(res, res, TextureFormat.RGBA32, false);
            float h  = res / 2f;
            float inner = h * innerFrac, outer = h * outerFrac;
            var pix = new Color32[res * res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float dx = x - h, dy = y - h;
                    float d  = Mathf.Sqrt(dx * dx + dy * dy);
                    float a  = 0f;
                    if (d >= inner && d <= outer)
                    {
                        float tt = (d - inner) / (outer - inner);
                        a  = 1f - Mathf.Abs(tt * 2f - 1f);
                        a  = Mathf.Pow(a, 0.5f);
                    }
                    pix[y * res + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(pix);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), (float)res);
        }
    }
}
