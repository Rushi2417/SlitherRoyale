using System.Collections.Generic;
using UnityEngine;
using WormCore;

namespace SlitherRoyale.Client.VFX
{
    public class DeathBurstVFX : MonoBehaviour
    {
        [SerializeField] private float burstDuration = 0.8f;
        [SerializeField] private float shockwaveSpeed = 400f;
        [SerializeField] private int ringSegments = 40;

        private List<BurstInstance> _activeBursts;
        private Material _lineMat;

        private struct BurstInstance
        {
            public Vector3 Position;
            public float Timer;
            public float Duration;
            public Color Color;
            public LineRenderer RingRenderer;
            public float RingStartWidth;
        }

        private void Awake()
        {
            _activeBursts = new List<BurstInstance>(8);
            var shader = Shader.Find("Sprites/Default");
            _lineMat = new Material(shader);
            _lineMat.color = Color.white;
        }

        public void SpawnBurst(Vector3 position, Color color, List<BurstPellet> pellets)
        {
            var ringGo = new GameObject($"Shockwave_{Time.frameCount}");
            ringGo.transform.SetParent(transform);
            var lr = ringGo.AddComponent<LineRenderer>();
            lr.positionCount = ringSegments;
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.material = new Material(_lineMat);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            _activeBursts.Add(new BurstInstance
            {
                Position = position,
                Timer = 0f,
                Duration = burstDuration,
                Color = color,
                RingRenderer = lr,
                RingStartWidth = 4f
            });
        }

        private void Update()
        {
            for (int i = _activeBursts.Count - 1; i >= 0; i--)
            {
                var burst = _activeBursts[i];
                burst.Timer += Time.deltaTime;

                if (burst.Timer >= burst.Duration)
                {
                    if (burst.RingRenderer != null)
                        Destroy(burst.RingRenderer.gameObject);
                    _activeBursts.RemoveAt(i);
                    continue;
                }

                float t = burst.Timer / burst.Duration;
                float radius = shockwaveSpeed * burst.Timer;
                float alpha = Mathf.Lerp(0.6f, 0f, t);
                float width = Mathf.Lerp(burst.RingStartWidth, 0.5f, t);

                var lr = burst.RingRenderer;
                if (lr != null)
                {
                    lr.startWidth = width;
                    lr.endWidth = width * 0.3f;

                    Color ringColor = new Color(burst.Color.r, burst.Color.g, burst.Color.b, alpha);
                    var gradient = new Gradient();
                    gradient.SetKeys(
                        new GradientColorKey[] {
                            new GradientColorKey(burst.Color, 0f),
                            new GradientColorKey(burst.Color, 0.5f),
                            new GradientColorKey(Color.white, 1f)
                        },
                        new GradientAlphaKey[] {
                            new GradientAlphaKey(alpha, 0f),
                            new GradientAlphaKey(alpha * 0.5f, 0.5f),
                            new GradientAlphaKey(0f, 1f)
                        }
                    );
                    lr.colorGradient = gradient;

                    for (int j = 0; j < ringSegments; j++)
                    {
                        float angle = (float)j / ringSegments * Mathf.PI * 2f;
                        float wobble = 1f + Mathf.Sin(t * 20f + j * 0.5f) * 0.05f * t;
                        float r = radius * wobble;
                        Vector3 pos = burst.Position + new Vector3(
                            Mathf.Cos(angle) * r,
                            Mathf.Sin(angle) * r,
                            0f);
                        lr.SetPosition(j, pos);
                    }
                }

                _activeBursts[i] = burst;
            }
        }
    }
}
