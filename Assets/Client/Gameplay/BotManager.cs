using System.Collections.Generic;
using UnityEngine;
using WormCore;

namespace SlitherRoyale.Client.Gameplay
{
    public class BotManager : MonoBehaviour
    {
        public int botCount = 15;
        public float arenaRadius = 800f;

        private List<WormRenderer> _botRenderers;
        private List<GameObject> _botHeadGOs;
        private Material _botGlowMaterial;
        private Material _botHeadMaterial;
        public static readonly Color[] BotColors = new[]
        {
            new Color(1f, 0.42f, 0.36f),
            new Color(0.25f, 0.88f, 0.77f),
            new Color(1f, 0.79f, 0.3f),
            new Color(0.6f, 0.2f, 0.9f),
            new Color(0.2f, 0.7f, 1f),
            new Color(1f, 0.5f, 0.8f),
            new Color(0.8f, 0.9f, 0.2f),
            new Color(0.9f, 0.3f, 0.3f),
            new Color(0.3f, 0.9f, 0.6f),
            new Color(1f, 0.6f, 0.1f),
            new Color(0.4f, 0.5f, 1f),
            new Color(0.1f, 0.8f, 0.8f),
            new Color(0.9f, 0.4f, 0.6f),
            new Color(0.5f, 0.9f, 0.3f),
            new Color(0.7f, 0.3f, 0.7f),
            new Color(0.3f, 0.7f, 0.4f),
            new Color(1f, 0.7f, 0.5f),
            new Color(0.2f, 0.5f, 0.9f),
            new Color(0.8f, 0.2f, 0.5f),
        };

        private void Awake()
        {
            Shader shader = Shader.Find("WormCore/GlowUnlit");
            if (shader != null)
            {
                _botGlowMaterial = new Material(shader);
                _botHeadMaterial = new Material(shader);
            }
            else
            {
                _botGlowMaterial = new Material(Shader.Find("Sprites/Default"));
                _botHeadMaterial = new Material(Shader.Find("Sprites/Default"));
            }
        }

        public void Initialize(int count, Transform arenaRoot)
        {
            botCount = count;
            _botRenderers = new List<WormRenderer>(count);
            _botHeadGOs = new List<GameObject>(count);

            for (int i = 0; i < count; i++)
            {
                Color color = BotColors[i % BotColors.Length];

                var wormGo = new GameObject($"Bot_{i}");
                wormGo.transform.SetParent(arenaRoot);

                var lr = wormGo.AddComponent<LineRenderer>();
                lr.positionCount = 0;
                lr.startWidth = 6f;
                lr.endWidth = 3f;
                var mat = new Material(_botGlowMaterial);
                mat.SetColor("_GlowColor", color);
                lr.material = mat;

                var renderer = wormGo.AddComponent<WormRenderer>();
                renderer.lineRenderer = lr;
                renderer.SetColor(color);

                var headGo = new GameObject($"BotHead_{i}");
                headGo.transform.SetParent(wormGo.transform);
                var headSr = headGo.AddComponent<SpriteRenderer>();
                var headTex = new Texture2D(32, 32);
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        float dx = x - 15.5f, dy = y - 15.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        headTex.SetPixel(x, y, d < 13f ? color : Color.clear);
                    }
                headTex.Apply();
                headSr.sprite = Sprite.Create(headTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
                var headMat = new Material(_botHeadMaterial);
                headMat.SetColor("_GlowColor", color);
                headSr.material = headMat;

                renderer.headTransform = headGo.transform;
                _botRenderers.Add(renderer);
                _botHeadGOs.Add(headGo);
            }
        }

        public static Color GetBotColor(int index)
        {
            return BotColors[Mathf.Abs(index) % BotColors.Length];
        }

        public void UpdateRenderers(List<WormState> states)
        {
            // states[0] is local player — bots start at states[1]
            for (int i = 0; i < _botRenderers.Count; i++)
            {
                int stateIndex = i + 1; // skip index 0 (local player)
                if (stateIndex < states.Count)
                {
                    var state = states[stateIndex];
                    if (state.IsDead)
                    {
                        if (_botRenderers[i] != null && _botRenderers[i].lineRenderer != null)
                            _botRenderers[i].lineRenderer.positionCount = 0;
                        if (i < _botHeadGOs.Count && _botHeadGOs[i] != null)
                            _botHeadGOs[i].SetActive(false);
                    }
                    else
                    {
                        _botRenderers[i].UpdateFromState(state);
                    }
                }
                else
                {
                    // No state for this renderer — hide it
                    if (_botRenderers[i] != null && _botRenderers[i].lineRenderer != null)
                        _botRenderers[i].lineRenderer.positionCount = 0;
                }
            }
        }
    }
}
