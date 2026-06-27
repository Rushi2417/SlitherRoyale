using UnityEngine;
using WormCore;

namespace SlitherRoyale.Client.Gameplay
{
    public class WormRenderer : MonoBehaviour
    {
    public LineRenderer lineRenderer;
    public Transform headTransform;
        [SerializeField] private Color bodyColor = new Color(0.42f, 0.31f, 1f);
        [SerializeField] private float headRadius = 8f;
        [SerializeField] private float bodyWidthMin = 4f;
        [SerializeField] private float bodyWidthMax = 12f;

        private static readonly Color ArcViolet = new Color(0.42f, 0.31f, 1f);
        private static readonly Color BioMint = new Color(0.25f, 0.88f, 0.77f);
        private static readonly Color EmberCoral = new Color(1f, 0.42f, 0.36f);

        public void UpdateFromState(WormState state)
        {
            if (state.Segments == null || state.Segments.Count == 0)
            {
                lineRenderer.positionCount = 0;
                if (headTransform) headTransform.gameObject.SetActive(false);
                return;
            }

            float bodyWidth = Mathf.Lerp(bodyWidthMin, bodyWidthMax, state.Mass / 200f);
            lineRenderer.startWidth = bodyWidth;
            lineRenderer.endWidth = bodyWidth * 0.5f;

            int maxPoints = Mathf.Min(state.Segments.Count, 200);
            lineRenderer.positionCount = maxPoints + 1;
            lineRenderer.SetPosition(0, new Vector3(state.X, state.Y, 0f));

            for (int i = 0; i < maxPoints; i++)
            {
                lineRenderer.SetPosition(i + 1, new Vector3(state.Segments[i].X, state.Segments[i].Y, 0f));
            }

            Color color = state.IsBoosting ? EmberCoral : ArcViolet;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(BioMint, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.3f, 1f)
                }
            );
            lineRenderer.colorGradient = gradient;

            if (headTransform)
            {
                headTransform.gameObject.SetActive(true);
                headTransform.position = new Vector3(state.X, state.Y, 0f);
                float angle = state.Heading * Mathf.Rad2Deg;
                headTransform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
                float scale = 1f + state.Mass * 0.003f;
                headTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public void SetColor(Color color)
        {
            bodyColor = color;
        }
    }
}
