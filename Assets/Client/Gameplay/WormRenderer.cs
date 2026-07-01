using UnityEngine;
using UnityEngine.UI;
using WormCore;

namespace SlitherRoyale.Client.Gameplay
{
    public class WormRenderer : MonoBehaviour
    {
    public LineRenderer lineRenderer;
    public Transform headTransform;
    public ParticleSystem boostTrail;

        private static readonly Color ArcViolet  = new Color(0.42f, 0.31f, 1f);
        private static readonly Color BioMint     = new Color(0.25f, 0.88f, 0.77f);
        private static readonly Color EmberCoral  = new Color(1f,   0.42f, 0.36f);

        // Cached gradients — rebuilt only when boost state changes (not every frame)
        private Gradient _normalGradient;
        private Gradient _boostGradient;
        private bool _lastBoostState = false;

        // Optional: worm name label above head
        private Text _nameLabel;
        private string _wormName;

        private void Awake()
        {
            _normalGradient = BuildGradient(ArcViolet);
            _boostGradient  = BuildGradient(EmberCoral);
        }

        private static Gradient BuildGradient(Color headColor)
        {
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(headColor, 0f),
                    new GradientColorKey(BioMint,   1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f,  0f),
                    new GradientAlphaKey(0.3f, 1f)
                }
            );
            return g;
        }

        public void SetWormName(string name)
        {
            _wormName = name;
            if (_nameLabel != null) _nameLabel.text = name;
        }

        public void AttachNameLabel(Canvas hudCanvas)
        {
            if (_nameLabel != null || hudCanvas == null) return;
            var go = new GameObject("WormNameLabel");
            go.transform.SetParent(hudCanvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120f, 20f);
            _nameLabel = go.AddComponent<Text>();
            _nameLabel.text = _wormName ?? "";
            _nameLabel.fontSize = 11;
            _nameLabel.color = new Color(1f, 1f, 1f, 0.85f);
            _nameLabel.alignment = TextAnchor.MiddleCenter;
            _nameLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        public void UpdateFromState(WormState state)
        {
            // ── Boost trail ──────────────────────────────────────────────────
            if (boostTrail != null)
            {
                var emission = boostTrail.emission;
                emission.rateOverTime = state.IsBoosting ? 30f : 0f;
                if (state.IsBoosting != _lastBoostState)
                {
                    var main = boostTrail.main;
                    main.startColor = state.IsBoosting ? EmberCoral : ArcViolet;
                    _lastBoostState = state.IsBoosting;
                }
            }

            // ── Early out if no segments ──────────────────────────────────────
            if (state.Segments == null || state.Segments.Count == 0)
            {
                lineRenderer.positionCount = 0;
                if (headTransform) headTransform.gameObject.SetActive(false);
                return;
            }

            // ── Body width scales with mass ───────────────────────────────────
            float bodyWidth = Mathf.Lerp(4f, 14f, state.Mass / 300f);
            lineRenderer.startWidth = bodyWidth;
            lineRenderer.endWidth   = bodyWidth * 0.45f;

            // ── Set segment positions ─────────────────────────────────────────
            int maxPoints = Mathf.Min(state.Segments.Count, 200);
            lineRenderer.positionCount = maxPoints + 1;
            lineRenderer.SetPosition(0, new Vector3(state.X, state.Y, 0f));
            for (int i = 0; i < maxPoints; i++)
                lineRenderer.SetPosition(i + 1, new Vector3(state.Segments[i].X, state.Segments[i].Y, 0f));

            // ── Apply cached gradient (only swap when boost changes) ──────────
            lineRenderer.colorGradient = state.IsBoosting ? _boostGradient : _normalGradient;

            // ── Head transform ────────────────────────────────────────────────
            if (headTransform)
            {
                headTransform.gameObject.SetActive(true);
                headTransform.position = new Vector3(state.X, state.Y, 0f);
                float angle = state.Heading * Mathf.Rad2Deg;
                headTransform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
                float scale = 1f + state.Mass * 0.003f;
                headTransform.localScale = new Vector3(scale, scale, 1f);
            }

            // ── Name label follows head in world-space ────────────────────────
            if (_nameLabel != null && Camera.main != null)
            {
                Vector3 worldPos = new Vector3(state.X, state.Y + state.HeadRadius() * 2f, 0f);
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                _nameLabel.rectTransform.position = screenPos;
                _nameLabel.enabled = screenPos.z > 0f;
            }
        }

        public void SetColor(Color color)
        {
            _normalGradient = BuildGradient(color);
            _boostGradient  = BuildGradient(Color.Lerp(color, EmberCoral, 0.6f));
            if (lineRenderer != null)
                lineRenderer.colorGradient = _normalGradient;
        }
    }
}
