using UnityEngine;
using WormCore;
using Random = UnityEngine.Random;

namespace SlitherRoyale.Client.Gameplay
{
    public class GameManager : MonoBehaviour
    {
    public CameraFollow cameraFollow;
    public WormRenderer wormRenderer;
    public int initialPelletCount = 500;
    public int maxPellets = 800;
    public float arenaRadius = 800f;
    public Material pelletMaterial;

        public WormState PlayerState;

        private Transform _pelletContainer;
        private float _spawnTimer;
        private GameObject _pelletPrefab;

        private void Awake()
        {
            _pelletContainer = new GameObject("Pellets").transform;
            _pelletPrefab = CreatePelletPrefab();
        }

        private GameObject CreatePelletPrefab()
        {
            var obj = new GameObject("Pellet");
            obj.SetActive(false);

            var renderer = obj.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(16, 16);
            Color c = new Color(1f, 0.79f, 0.3f, 0.8f);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float dx = x - 7.5f, dy = y - 7.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    tex.SetPixel(x, y, d < 6f ? c : Color.clear);
                }
            tex.Apply();
            renderer.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            renderer.material = pelletMaterial ?? new Material(Shader.Find("Sprites/Default"));

            obj.transform.localScale = Vector3.one * 0.8f;
            return obj;
        }

        private void Start()
        {
            SpawnPlayer();
            SpawnInitialPellets();
        }

        private void SpawnPlayer()
        {
            PlayerState = new WormState
            {
                Id = 1,
                X = Random.Range(-arenaRadius * 0.5f, arenaRadius * 0.5f),
                Y = Random.Range(-arenaRadius * 0.5f, arenaRadius * 0.5f),
                Heading = Random.Range(0f, Mathf.PI * 2f),
                Mass = 10f,
                IsBoosting = false,
                IsDead = false,
                Segments = new System.Collections.Generic.List<WormState.Segment>()
            };
        }

        private void SpawnInitialPellets()
        {
            for (int i = 0; i < initialPelletCount; i++)
                SpawnPellet();
        }

        private void SpawnPellet()
        {
            Vector2 pos = Random.insideUnitCircle * arenaRadius;
            var pellet = Instantiate(_pelletPrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity, _pelletContainer);
            pellet.SetActive(true);
        }

        private void Update()
        {
            InputHandler.Update();
            if (PlayerState.IsDead) return;

            Vector2 inputDir = InputHandler.GetSteerDirection();
            bool boostHeld = InputHandler.IsBoosting();

            float desiredHeading = Mathf.Atan2(inputDir.y, inputDir.x);
            MovementMath.IntegrateMovement(ref PlayerState, desiredHeading, boostHeld, Time.deltaTime);
            GrowthMath.ApplyBoostDrain(ref PlayerState, Time.deltaTime);

            CheckPelletCollisions();
            wormRenderer.UpdateFromState(PlayerState);
            cameraFollow.SetTarget(PlayerState.X, PlayerState.Y);

            if (_pelletContainer.childCount < maxPellets)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f)
                {
                    SpawnPellet();
                    _spawnTimer = 0.05f;
                }
            }
        }

        private void CheckPelletCollisions()
        {
            float headR = PlayerState.HeadRadius();
            for (int i = _pelletContainer.childCount - 1; i >= 0; i--)
            {
                Transform pellet = _pelletContainer.GetChild(i);
                Vector3 pPos = pellet.position;
                if (CollisionMath.HeadVsPellet(PlayerState.X, PlayerState.Y, headR, pPos.x, pPos.y, 3f))
                {
                    GrowthMath.ApplyPelletGain(ref PlayerState, GrowthMath.PelletBaseValue);
                    Destroy(pellet.gameObject);
                }
            }
        }
    }
}
