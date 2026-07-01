using System.Collections.Generic;
using SlitherRoyale.Client.Audio;
using SlitherRoyale.Client.Backend;
using SlitherRoyale.Client.UI;
using SlitherRoyale.Client.VFX;
using UnityEngine;
using UnityEngine.UI;
using WormCore;
using Random = UnityEngine.Random;

namespace SlitherRoyale.Client.Gameplay
{
    public class GameManager : MonoBehaviour
    {
    public CameraFollow cameraFollow;
    public WormRenderer wormRenderer;
    public BotManager botManager;
    public LeaderboardUI leaderboardUI;
    public ComboCalloutUI comboCalloutUI;
    public DeathBurstVFX deathBurstVFX;
    public MapMechanics mapMechanics;
    public MapConfig mapConfig;
    public Bootstrapper bootstrapper;
    // BUG-02 FIX: explicit HUD canvas reference so we don't accidentally find
    // the ScreenManager canvas via FindObjectOfType<Canvas>()
    public Canvas hudCanvas;
    public int initialPelletCount = 500;
    public int maxPellets = 800;
    public float arenaRadius = 800f;
    public Material pelletMaterial;
    public int botCount = 15;
    public MatchMode matchMode = MatchMode.FreeForAll;

        public int LocalPlayerId => 1;

        private List<WormState> _allWorms;
        private List<BotContext> _botContexts;
        private List<string> _wormNames;
        private ComboSystem _comboSystem;
        private float _gameTime;
        // BUG-16 FIX: separate lifetime kill counter (ComboSystem only tracks 10-sec window)
        private int _totalKills;

        private Transform _pelletContainer;
        private float _spawnTimer;
        private GameObject _pelletPrefab;

        private bool _localDeathHandled;
        private bool _victoryHandled;
        private float[] _spawnProtectionTimer; // per-worm spawn protection

        private List<PelletInfo> _pelletInfoCache;
        private int _pelletCacheFrame;

        private List<Transform> _wisps;
        private List<Transform> _giantPellets;
        private GameObject _darknessOverlay;
        private const float SyrupSlowFactor = 0.5f;
        private const float SpawnProtectionDuration = 3f;

        // Boost pellet trail
        private float _boostPelletTimer;
        private const float BoostPelletInterval = 0.15f;

        // Speed pad temporary boost
        private float _speedPadBoostTimer;
        private const float SpeedPadBoostDuration = 1.5f;
        private const float SpeedPadBoostMultiplier = 1.4f;

        // BUG-07 FIX: timer-based giant pellet respawn (replaces frame-rate-dependent Random check)
        private float _giantPelletRespawnTimer = 8f;

        // Danger vignette & boost button
        private DangerVignetteUI _dangerVignette;
        private Image _boostButtonImage;
        private Text _boostButtonLabel;

        // HUD references
        private Text _hudMassLabel;
        private Text _hudScoreLabel;
        private Text _hudBoostLabel;
        private Text _hudKillsLabel;
        private Image _hudBoostBar;
        private GameObject _killFeedContainer;
        private List<Text> _killFeedEntries;
        private float _killFeedTimer;
        private const float KillFeedDuration = 3f;
        private MinimapUI _minimap;

        private void Awake()
        {
            // BUG-19/23 FIX: parent pellet container to this transform so it's
            // automatically destroyed when the arena is destroyed.
            _pelletContainer = new GameObject("Pellets").transform;
            _pelletContainer.SetParent(transform);
            _pelletPrefab = CreatePelletPrefab();
            _comboSystem = new ComboSystem();
            _comboSystem.ComboWindowSeconds = 10f;
            _comboSystem.OnComboEvent += OnComboEvent;
            _pelletInfoCache = new List<PelletInfo>(1024);
            _wisps = new List<Transform>();
            _giantPellets = new List<Transform>();
            _killFeedEntries = new List<Text>();
        }

        private void Start()
        {
            InitializeArena();
        }

        private int _teamCounter;

        private void InitializeArena()
        {
            _allWorms = new List<WormState>(botCount + 2);
            _botContexts = new List<BotContext>(botCount);
            _wormNames = new List<string>(botCount + 2);
            _totalKills = 0;
            _gameTime = 0f;
            _teamCounter = 0;
            _localDeathHandled = false;
            _victoryHandled = false;

            _allWorms.Add(CreateWormState(1, 10f));
            _wormNames.Add("YOU");

            for (int i = 0; i < botCount; i++)
            {
                int id = 100 + i;
                float startMass = 5f + Random.value * 20f;
                _allWorms.Add(CreateWormState(id, startMass));
                BotSkillTier tier = BotAI.GetRandomTier(id);
                _botContexts.Add(new BotContext(tier, id));
                _wormNames.Add(BotAI.GetBotName(id));
            }

            // Spawn protection array
            _spawnProtectionTimer = new float[_allWorms.Count];
            for (int i = 0; i < _spawnProtectionTimer.Length; i++)
                _spawnProtectionTimer[i] = SpawnProtectionDuration;

            if (botManager != null)
                botManager.Initialize(botCount, transform);

            initialPelletCount = Mathf.RoundToInt(mapConfig.pelletCount * 6f);
            maxPellets = Mathf.RoundToInt(mapConfig.pelletCount * 10f);
            SpawnInitialPellets();
            if (mapConfig.hasGiantPellets) SpawnGiantPellets();
            if (mapConfig.hasLowGravity)
                MovementMath.TurnRadiusMultiplier = 2.5f;
            else
                MovementMath.TurnRadiusMultiplier = 1f;
            CreateDarknessOverlay();
            CreateHUD();

            // Start match music
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayMatchMusic();
        }

        private WormState CreateWormState(int id, float mass)
        {
            int teamId = id;
            if (matchMode == MatchMode.Duos)
            {
                teamId = _teamCounter / 2;
                _teamCounter++;
            }

            return new WormState
            {
                Id = id,
                X = Random.Range(-arenaRadius * 0.5f, arenaRadius * 0.5f),
                Y = Random.Range(-arenaRadius * 0.5f, arenaRadius * 0.5f),
                Heading = Random.Range(0f, Mathf.PI * 2f),
                Mass = mass,
                IsBoosting = false,
                IsDead = false,
                TeamId = teamId,
                Segments = new List<WormState.Segment>()
            };
        }

        private float _shrinkTimer;

        private void Update()
        {
            _gameTime += Time.deltaTime;

            // Update spawn protection timers
            if (_spawnProtectionTimer != null)
                for (int i = 0; i < _spawnProtectionTimer.Length && i < _allWorms.Count; i++)
                    if (_spawnProtectionTimer[i] > 0f)
                        _spawnProtectionTimer[i] -= Time.deltaTime;

            if (mapConfig != null && mapConfig.hasShrinkZone)
            {
                _shrinkTimer += Time.deltaTime;
                if (_shrinkTimer >= mapConfig.shrinkInterval)
                {
                    _shrinkTimer = 0f;
                    arenaRadius = Mathf.Max(arenaRadius - 30f, 30f);
                    if (mapMechanics != null)
                        mapMechanics.SetShrinkRadius(arenaRadius);
                }
            }

            Vector2 wormPos = _allWorms != null && _allWorms.Count > 0
                ? new Vector2(_allWorms[0].X, _allWorms[0].Y)
                : Vector2.zero;
            InputHandler.Update(wormPos);

            // Pre-tick bot cluster cache (compute once for all bots)
            var pellets = GetPelletInfo();
            if (_allWorms.Count > 0)
                BotAI.PreTickUpdate(pellets, _allWorms[0].X, _allWorms[0].Y);

            // Check victory condition (local player alive, all others dead)
            if (!_allWorms[0].IsDead && !_victoryHandled)
            {
                bool allOthersDead = true;
                for (int i = 1; i < _allWorms.Count; i++)
                {
                    if (!_allWorms[i].IsDead) { allOthersDead = false; break; }
                }
                if (allOthersDead)
                {
                    _victoryHandled = true;
                    _localDeathHandled = true; // prevent death-check from also firing
                    var data = new ResultsData
                    {
                        Kills = _comboSystem.GetCurrentStreak(LocalPlayerId),
                        Score = _allWorms[0].Mass * 10f,
                        DeathReason = "VICTORY — Last worm standing!",
                        Rank = 1,
                    };
                    if (bootstrapper != null) Destroy(gameObject);
                    ScreenManager.Instance.NavigateTo<ResultsScreen>(data);
                    return;
                }
            }

            // Check local player death
            if (_allWorms[0].IsDead && !_localDeathHandled)
            {
                _localDeathHandled = true;
                int aliveCount = 0;
                foreach (var w in _allWorms) { if (!w.IsDead) aliveCount++; }
                var data = new ResultsData
                {
                    Kills = _comboSystem.GetCurrentStreak(LocalPlayerId),
                    Score = _allWorms[0].Mass * 10f,
                    DeathReason = "You were eliminated",
                    Rank = aliveCount + 1,
                };
                if (bootstrapper != null) Destroy(gameObject);
                ScreenManager.Instance.NavigateTo<ResultsScreen>(data);
                return;
            }

            // Move all worms
            for (int i = 0; i < _allWorms.Count; i++)
            {
                if (_allWorms[i].IsDead) continue;

                var ws = _allWorms[i];
                bool wasBoostingBefore = ws.IsBoosting;

                if (i == 0) // local player
                {
                    Vector2 inputDir = InputHandler.GetSteerDirection();
                    bool boostHeld = InputHandler.IsBoosting();
                    float desiredHeading = Mathf.Atan2(inputDir.y, inputDir.x);

                    // BUG-12 FIX: Removed duplicate speed-pad pre-movement block.
                    // Speed pad effect is applied once inside ApplyMapMechanics.
                    MovementMath.IntegrateMovement(ref ws, desiredHeading, boostHeld, Time.deltaTime);

                    // Boost SFX trigger
                    if (boostHeld && !wasBoostingBefore)
                        AudioManager.Instance?.Play(AudioManager.SfxType.BoostStart);
                    else if (!boostHeld && wasBoostingBefore)
                        AudioManager.Instance?.Play(AudioManager.SfxType.BoostEnd);
                }
                else
                {
                    int botIdx = i - 1;
                    if (botIdx >= 0 && botIdx < _botContexts.Count)
                    {
                        BotDecision decision = BotAI.Decide(
                            ws, _botContexts[botIdx],
                            _allWorms, pellets, Time.deltaTime, _gameTime);
                        MovementMath.IntegrateMovement(ref ws, decision.DesiredHeading, decision.BoostHeld, Time.deltaTime);
                    }
                }

                _allWorms[i] = ws;
                ApplyMapMechanics(i);

                ws = _allWorms[i];
                GrowthMath.ApplyBoostDrain(ref ws, Time.deltaTime);
                _allWorms[i] = ws;
            }

            // Boost pellet drops — drop a small pellet behind boosting worms
            if (_allWorms.Count > 0 && _allWorms[0].IsBoosting && !_allWorms[0].IsDead)
            {
                _boostPelletTimer -= Time.deltaTime;
                if (_boostPelletTimer <= 0f)
                {
                    _boostPelletTimer = BoostPelletInterval;
                    var pw = _allWorms[0];
                    // Drop pellet behind the head
                    float trailX = pw.X - Mathf.Cos(pw.Heading) * 15f;
                    float trailY = pw.Y - Mathf.Sin(pw.Heading) * 15f;
                    var pellet = Instantiate(_pelletPrefab,
                        new Vector3(trailX, trailY, 0f),
                        Quaternion.identity, _pelletContainer);
                    pellet.SetActive(true);
                    pellet.transform.localScale = Vector3.one * 0.5f;
                    var sr = pellet.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.color = new Color(0.25f, 0.88f, 0.77f, 0.8f); // bio-mint boost pellet
                }
            }

            _comboSystem.Update(_gameTime);
            CheckAllPelletCollisions();
            CheckWormCollisions();
            MaintainPelletCount();

            if (!_allWorms[0].IsDead)
            {
                QuestManager.ReportProgress("SurviveTime", Mathf.RoundToInt(Time.deltaTime));
                if (_allWorms[0].IsBoosting)
                    QuestManager.ReportProgress("UseBoost", Mathf.RoundToInt(Time.deltaTime));
            }

            if (!_allWorms[0].IsDead)
            {
                wormRenderer.UpdateFromState(_allWorms[0]);
                cameraFollow.SetTarget(_allWorms[0].X, _allWorms[0].Y, _allWorms[0].Mass);
            }

            if (botManager != null)
                botManager.UpdateRenderers(_allWorms);

            if (leaderboardUI != null)
                leaderboardUI.UpdateLeaderboard(_allWorms, LocalPlayerId, _wormNames);

            UpdateHUD();
            UpdateKillFeed();
        }

        private float _wispRefreshTimer;

        private void LateUpdate()
        {
            if (mapConfig == null) return;
            if (mapConfig.hasWisps && mapMechanics != null)
            {
                _wispRefreshTimer -= Time.deltaTime;
                if (_wispRefreshTimer <= 0f)
                {
                    _wispRefreshTimer = 0.5f;
                    RefreshWispVisuals();
                }
                HandleWispCollisions();
            }
            UpdateDarknessOverlay();
            MaintainGiantPellets();
        }

        private void ApplyMapMechanics(int wormIndex)
        {
            var ws = _allWorms[wormIndex];

            // Arena boundary pushback
            float dist = Mathf.Sqrt(ws.X * ws.X + ws.Y * ws.Y);
            if (dist > arenaRadius)
            {
                float angle = Mathf.Atan2(ws.Y, ws.X);
                ws.X = Mathf.Cos(angle) * arenaRadius * 0.95f;
                ws.Y = Mathf.Sin(angle) * arenaRadius * 0.95f;
                _allWorms[wormIndex] = ws;
            }

            if (mapMechanics == null) return;

            Vector3 pos = new Vector3(ws.X, ws.Y, 0f);
            bool isProtected = _spawnProtectionTimer != null &&
                               wormIndex < _spawnProtectionTimer.Length &&
                               _spawnProtectionTimer[wormIndex] > 0f;

            if (!isProtected && mapMechanics.IsInLava(pos))
            {
                var s = _allWorms[wormIndex];
                s.Mass = Mathf.Max(s.Mass - mapConfig.shrinkDamage * Time.deltaTime, GrowthMath.MinMass);
                _allWorms[wormIndex] = s;
            }

            if (!isProtected && mapMechanics.HitLaserFence(pos,
                pos - new Vector3(Mathf.Cos(ws.Heading), Mathf.Sin(ws.Heading), 0f) * 2f))
            {
                var updated = _allWorms[wormIndex];
                updated.IsDead = true;
                _allWorms[wormIndex] = updated;
                HandleDeath(wormIndex, -1);
                return;
            }

            if (mapMechanics.IsSpeedPad(pos))
            {
                // Speed pad: boost forward without mass cost; applies a timer so the effect persists briefly
                if (wormIndex == 0) _speedPadBoostTimer = SpeedPadBoostDuration;
                var s = _allWorms[wormIndex];
                float padSpeed = MovementMath.CalculateSpeed(s.Mass, false) * SpeedPadBoostMultiplier;
                s.X += Mathf.Cos(s.Heading) * padSpeed * Time.deltaTime;
                s.Y += Mathf.Sin(s.Heading) * padSpeed * Time.deltaTime;
                _allWorms[wormIndex] = s;
            }

            if (mapMechanics.HitJellyfish(pos))
            {
                var s = _allWorms[wormIndex];
                s.Heading += Mathf.Sin(Time.time * 3f + wormIndex) * 2f * Time.deltaTime;
                _allWorms[wormIndex] = s;
            }

            // BUG-24 FIX: use GetCurrentForce() which returns a world-space directional
            // push vector. Old code used GetCurrentVelocity() (scalar) multiplied by
            // cos/sin of the worm's own heading \u2014 currents pushed in the worm's facing
            // direction, not the current's actual direction.
            Vector3 currentForce = mapMechanics.GetCurrentForce(pos);
            if (currentForce.sqrMagnitude > 0f)
            {
                var s = _allWorms[wormIndex];
                s.X += currentForce.x * Time.deltaTime;
                s.Y += currentForce.y * Time.deltaTime;
                _allWorms[wormIndex] = s;
            }

            if (mapMechanics.IsInSyrup(pos))
            {
                var s = _allWorms[wormIndex];
                // Move back then re-apply with syrup factor
                float speed = MovementMath.CalculateSpeed(s.Mass, s.IsBoosting) * Time.deltaTime * SyrupSlowFactor;
                s.X = s.X - Mathf.Cos(s.Heading) * MovementMath.CalculateSpeed(s.Mass, s.IsBoosting) * Time.deltaTime
                          + Mathf.Cos(s.Heading) * speed;
                s.Y = s.Y - Mathf.Sin(s.Heading) * MovementMath.CalculateSpeed(s.Mass, s.IsBoosting) * Time.deltaTime
                          + Mathf.Sin(s.Heading) * speed;
                _allWorms[wormIndex] = s;
            }

            Vector3? airlockTarget = mapMechanics.GetAirlockTarget(pos);
            if (airlockTarget.HasValue)
            {
                var s = _allWorms[wormIndex];
                s.X = airlockTarget.Value.x;
                s.Y = airlockTarget.Value.y;
                _allWorms[wormIndex] = s;
            }
        }

        private void CheckAllPelletCollisions()
        {
            for (int w = 0; w < _allWorms.Count; w++)
            {
                if (_allWorms[w].IsDead) continue;
                float headR = _allWorms[w].HeadRadius();
                float px = _allWorms[w].X;
                float py = _allWorms[w].Y;
                float massGain = 0f;
                int pelletsToRemove = 0;

                // Collect all pellets to destroy this frame (avoid index-shift issues)
                for (int i = _pelletContainer.childCount - 1; i >= 0; i--)
                {
                    Transform pellet = _pelletContainer.GetChild(i);
                    Vector3 pPos = pellet.position;
                    float pelletRadius = pellet.GetComponent<GiantPelletMarker>() != null ? 10f : 4f;
                    if (CollisionMath.HeadVsPellet(px, py, headR, pPos.x, pPos.y, pelletRadius))
                    {
                        // BUG-05 FIX: use GiantPelletMarker component instead of unregistered tag
                        float value = pellet.GetComponent<GiantPelletMarker>() != null ? mapConfig.giantPelletValue : GrowthMath.PelletBaseValue;
                        massGain += value;
                        if (w == 0) QuestManager.ReportProgress("EatPellet", 1);
                        Destroy(pellet.gameObject);
                        pelletsToRemove++;
                        // Allow eating up to 5 pellets per frame (not just 1)
                        if (pelletsToRemove >= 5) break;
                    }
                }

                if (massGain > 0f)
                {
                    var s = _allWorms[w];
                    s.Mass += massGain;
                    if (s.Mass > GrowthMath.MaxMass) s.Mass = GrowthMath.MaxMass;
                    _allWorms[w] = s;

                    // Audio: pellet eat
                    if (w == 0)
                    {
                        bool isLarge = pelletsToRemove > 0 && massGain > GrowthMath.PelletBaseValue * 2f;
                        AudioManager.Instance?.Play(
                            isLarge ? AudioManager.SfxType.PelletEatLarge : AudioManager.SfxType.PelletEat);
                    }
                }
            }
        }

        private void CheckWormCollisions()
        {
            for (int a = 0; a < _allWorms.Count; a++)
            {
                if (_allWorms[a].IsDead) continue;
                float headAx = _allWorms[a].X, headAy = _allWorms[a].Y;
                float massA = _allWorms[a].Mass;
                float headRadiusA = _allWorms[a].HeadRadius();

                // Skip if spawn protected
                bool aProtected = _spawnProtectionTimer != null && a < _spawnProtectionTimer.Length
                                  && _spawnProtectionTimer[a] > 0f;
                if (aProtected) continue;

                for (int b = a + 1; b < _allWorms.Count; b++)
                {
                    if (_allWorms[b].IsDead) continue;
                    if (matchMode == MatchMode.Duos && _allWorms[a].TeamId == _allWorms[b].TeamId)
                        continue;

                    bool bProtected = _spawnProtectionTimer != null && b < _spawnProtectionTimer.Length
                                     && _spawnProtectionTimer[b] > 0f;

                    float headRadiusB = _allWorms[b].HeadRadius();
                    var headVsHead = CollisionMath.HeadVsHead(
                        headAx, headAy, headRadiusA, massA,
                        _allWorms[b].X, _allWorms[b].Y, headRadiusB, _allWorms[b].Mass);

                    if (headVsHead == CollisionMath.HeadOnCollisionResult.AWins && !bProtected)
                    {
                        HandleDeath(b, a);
                    }
                    else if (headVsHead == CollisionMath.HeadOnCollisionResult.BWins && !aProtected)
                    {
                        HandleDeath(a, b);
                    }
                    else if (headVsHead == CollisionMath.HeadOnCollisionResult.BothDie && !aProtected && !bProtected)
                    {
                        HandleDeath(a, -1);
                        HandleDeath(b, -1);
                    }

                    if (_allWorms[a].IsDead) break;

                    // Head-A vs Body-B
                    float bodyRadiusB = _allWorms[b].BodyRadius();
                    if (!bProtected && _allWorms[b].Segments != null)
                    {
                        // Skip first 3 segments (too close to head for false positives)
                        for (int s = 3; s < _allWorms[b].Segments.Count; s++)
                        {
                            var seg = _allWorms[b].Segments[s];
                            if (CollisionMath.HeadVsBody(headAx, headAy, headRadiusA, seg.X, seg.Y, bodyRadiusB))
                            {
                                HandleDeath(a, b);
                                break;
                            }
                        }
                    }

                    if (_allWorms[a].IsDead) continue;
                    if (_allWorms[b].IsDead) continue;
                    if (matchMode == MatchMode.Duos && _allWorms[a].TeamId == _allWorms[b].TeamId)
                        continue;

                    // Head-B vs Body-A
                    if (!aProtected && _allWorms[a].Segments != null)
                    {
                        float bodyRadiusA = _allWorms[a].BodyRadius();
                        for (int s = 3; s < _allWorms[a].Segments.Count; s++)
                        {
                            var seg = _allWorms[a].Segments[s];
                            if (CollisionMath.HeadVsBody(
                                    _allWorms[b].X, _allWorms[b].Y, headRadiusB,
                                    seg.X, seg.Y, bodyRadiusA))
                            {
                                HandleDeath(b, a);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void HandleDeath(int deadIdx, int killerIdx)
        {
            if (deadIdx < 0 || deadIdx >= _allWorms.Count) return;
            if (_allWorms[deadIdx].IsDead) return;

            var deadState = _allWorms[deadIdx];
            var updated = deadState;
            updated.IsDead = true;
            _allWorms[deadIdx] = updated;

            var burstPellets = DeathBurstMath.GenerateBurstPellets(deadState);
            foreach (var bp in burstPellets)
            {
                var pellet = Instantiate(_pelletPrefab, new Vector3(bp.X, bp.Y, 0f), Quaternion.identity, _pelletContainer);
                pellet.SetActive(true);
                pellet.transform.localScale = Vector3.one * 0.6f;
                var sr = pellet.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = new Color(1f, 0.79f, 0.3f, 0.9f);
            }

            // Audio: own death vs nearby death
            if (deadIdx == 0)
                AudioManager.Instance?.Play(AudioManager.SfxType.Death, 0f);
            else
                AudioManager.Instance?.PlayAt(AudioManager.SfxType.DeathBurst,
                    new Vector3(deadState.X, deadState.Y, 0f));

            if (killerIdx >= 0 && killerIdx < _allWorms.Count)
            {
                _comboSystem.RegisterKill(_allWorms[killerIdx].Id, _gameTime);
                if (killerIdx == 0) // local player killed someone
                {
                    // BUG-16 FIX: increment lifetime kill counter (separate from combo window)
                    _totalKills++;
                    QuestManager.ReportProgress("KillWorm", 1);
                    AddKillFeedEntry($"You killed {_wormNames[deadIdx]}!");
                }
                else if (deadIdx == 0)
                {
                    AddKillFeedEntry($"Killed by {_wormNames[killerIdx]}");
                }
            }
            else if (deadIdx == 0)
            {
                AddKillFeedEntry("You were eliminated");
            }

            _comboSystem.Reset(deadState.Id);

            if (deathBurstVFX != null)
            {
                Color color = deadIdx == 0
                    ? new Color(0.42f, 0.31f, 1f)
                    : BotManager.GetBotColor((deadIdx - 1) % BotManager.BotColors.Length);
                deathBurstVFX.SpawnBurst(
                    new Vector3(deadState.X, deadState.Y, 0f),
                    color, burstPellets);
            }
        }

        private void MaintainPelletCount()
        {
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

        private List<PelletInfo> GetPelletInfo()
        {
            if (Time.frameCount == _pelletCacheFrame)
                return _pelletInfoCache;

            _pelletCacheFrame = Time.frameCount;
            _pelletInfoCache.Clear();

            for (int i = 0; i < _pelletContainer.childCount; i++)
            {
                Transform t = _pelletContainer.GetChild(i);
                _pelletInfoCache.Add(new PelletInfo
                {
                    X = t.position.x,
                    Y = t.position.y,
                    Value = GrowthMath.PelletBaseValue
                });
            }

            return _pelletInfoCache;
        }

        private void OnComboEvent(int wormId, string callout, int streak)
        {
            if (wormId == LocalPlayerId && comboCalloutUI != null)
                comboCalloutUI.ShowCallout(callout, streak);

            // Play escalating combo audio sting
            if (wormId == LocalPlayerId && AudioManager.Instance != null)
            {
                var sfx = streak == 2 ? AudioManager.SfxType.DoubleKill :
                           streak == 3 ? AudioManager.SfxType.TripleKill :
                           streak >= 4 ? AudioManager.SfxType.Rampage : AudioManager.SfxType.UITap;
                AudioManager.Instance.Play(sfx, 0f);
            }
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

        private void SpawnGiantPellets()
        {
            for (int i = 0; i < mapConfig.giantPelletCount; i++)
            {
                Vector2 pos = Random.insideUnitCircle * arenaRadius * 0.8f;
                var pellet = Instantiate(_pelletPrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity, _pelletContainer);
                pellet.SetActive(true);
                pellet.transform.localScale = Vector3.one * 2.5f;
                // BUG-05 FIX: GiantPelletMarker instead of unregistered string tag
                pellet.AddComponent<GiantPelletMarker>();
                var sr = pellet.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(1f, 0.79f, 0.3f, 1f);
                _giantPellets.Add(pellet.transform);
            }
        }

        // ─── HUD ──────────────────────────────────────────────────────────────

        private void CreateHUD()
        {
            // BUG-02 FIX: Use the explicitly-assigned hudCanvas field.
            // FindObjectOfType<Canvas>() returns the ScreenManager's canvas (sort 50),
            // not the HUD canvas (sort 100), making all HUD elements invisible.
            var canvas = hudCanvas != null ? hudCanvas : FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Danger vignette — initialize first so it draws behind all other HUD elements
            _dangerVignette = canvas.gameObject.AddComponent<DangerVignetteUI>();
            _dangerVignette.Initialize(canvas.transform);

            CreateHUDPanel(canvas.transform);

            // Minimap — bottom right
            _minimap = canvas.gameObject.AddComponent<MinimapUI>();
            _minimap.Initialize(arenaRadius, canvas.transform);

            // Boost button — bottom center, large thumb target
            CreateBoostButton(canvas.transform);
        }

        private void CreateBoostButton(Transform canvasRoot)
        {
            var btnGo = new GameObject("BoostButton");
            btnGo.transform.SetParent(canvasRoot, false);
            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(220f, 40f);
            rt.sizeDelta = new Vector2(120f, 120f);

            _boostButtonImage = btnGo.AddComponent<Image>();
            // Create circular button texture
            var tex = new Texture2D(64, 64);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = x - 31.5f, dy = y - 31.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = d < 28f ? new Color(0.25f, 0.88f, 0.77f, 0.85f)
                            : d < 31f ? new Color(1f, 1f, 1f, 0.5f)
                            : Color.clear;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            _boostButtonImage.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);

            _boostButtonLabel = CreateHUDText(btnGo.transform, "BOOST",
                new Color(0.04f, 0.06f, 0.09f), 14, Vector2.zero);
            _boostButtonLabel.fontStyle = FontStyle.Bold;

            // BUG-06 FIX: Use EventTrigger PointerDown/PointerUp instead of Button.onClick.
            // onClick fires for one frame only \u2014 boost would release immediately.
            // PointerDown sets held=true, PointerUp sets held=false for a proper hold pattern.
            var trigger = btnGo.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var downEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
            downEntry.callback.AddListener(_ => InputHandler.SetMobileBoostHeld(true));
            trigger.triggers.Add(downEntry);

            var upEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
            upEntry.callback.AddListener(_ => InputHandler.SetMobileBoostHeld(false));
            trigger.triggers.Add(upEntry);
        }

        private void CreateHUDPanel(Transform canvasRoot)
        {
            // Bottom status bar
            var barGo = new GameObject("HUD_StatusBar");
            barGo.transform.SetParent(canvasRoot, false);
            var barRT = barGo.AddComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0.5f, 0f);
            barRT.anchorMax = new Vector2(0.5f, 0f);
            barRT.pivot = new Vector2(0.5f, 0f);
            barRT.anchoredPosition = new Vector2(0f, 20f);
            barRT.sizeDelta = new Vector2(500f, 60f);
            var barImg = barGo.AddComponent<Image>();
            barImg.color = new Color(0.04f, 0.06f, 0.09f, 0.75f);

            _hudMassLabel = CreateHUDText(barGo.transform, "⬡ Mass: 10", new Color(0.42f, 0.31f, 1f), 16,
                new Vector2(-80f, 12f));
            _hudScoreLabel = CreateHUDText(barGo.transform, "⬆ Score: 0", new Color(1f, 0.79f, 0.3f), 16,
                new Vector2(80f, 12f));
            _hudKillsLabel = CreateHUDText(barGo.transform, "☠ Kills: 0", new Color(1f, 0.42f, 0.36f), 14,
                new Vector2(0f, -8f));

            // Boost bar background
            var boostBgGo = new GameObject("HUD_BoostBg");
            boostBgGo.transform.SetParent(canvasRoot, false);
            var boostBgRT = boostBgGo.AddComponent<RectTransform>();
            boostBgRT.anchorMin = new Vector2(0.5f, 0f);
            boostBgRT.anchorMax = new Vector2(0.5f, 0f);
            boostBgRT.pivot = new Vector2(0.5f, 0f);
            boostBgRT.anchoredPosition = new Vector2(0f, 85f);
            boostBgRT.sizeDelta = new Vector2(200f, 6f);
            var boostBgImg = boostBgGo.AddComponent<Image>();
            boostBgImg.color = new Color(0.1f, 0.1f, 0.15f);

            var boostFillGo = new GameObject("HUD_BoostFill");
            boostFillGo.transform.SetParent(canvasRoot, false);
            var boostFillRT = boostFillGo.AddComponent<RectTransform>();
            boostFillRT.anchorMin = new Vector2(0.5f, 0f);
            boostFillRT.anchorMax = new Vector2(0.5f, 0f);
            boostFillRT.pivot = new Vector2(0f, 0f);
            boostFillRT.anchoredPosition = new Vector2(-100f, 85f);
            boostFillRT.sizeDelta = new Vector2(200f, 6f);
            _hudBoostBar = boostFillGo.AddComponent<Image>();
            _hudBoostBar.color = new Color(0.25f, 0.88f, 0.77f);

            _hudBoostLabel = CreateHUDText(canvasRoot, "BOOST", new Color(0.25f, 0.88f, 0.77f), 11,
                new Vector2(0f, 96f), anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(0.5f, 0f));

            // Kill feed (top-right)
            var killFeedGo = new GameObject("KillFeed");
            killFeedGo.transform.SetParent(canvasRoot, false);
            var killFeedRT = killFeedGo.AddComponent<RectTransform>();
            killFeedRT.anchorMin = new Vector2(1f, 1f);
            killFeedRT.anchorMax = new Vector2(1f, 1f);
            killFeedRT.pivot = new Vector2(1f, 1f);
            killFeedRT.anchoredPosition = new Vector2(-15f, -15f);
            killFeedRT.sizeDelta = new Vector2(280f, 120f);
            _killFeedContainer = killFeedGo;

            for (int i = 0; i < 4; i++)
            {
                var entry = CreateHUDText(killFeedGo.transform, "", new Color(1f, 0.79f, 0.3f), 13,
                    new Vector2(0f, -i * 26f),
                    anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
                    pivot: new Vector2(1f, 1f));
                _killFeedEntries.Add(entry);
            }
        }

        private Text CreateHUDText(Transform parent, string text, Color color, int size, Vector2 pos,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            var go = new GameObject("HUDText");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
            rt.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(240f, 28f);
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.color = color;
            txt.fontSize = size;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return txt;
        }

        private void UpdateHUD()
        {
            if (_allWorms == null || _allWorms.Count == 0) return;
            var player = _allWorms[0];

            // BUG-16 FIX: show lifetime total kills, not 10-second combo window streak
            if (_hudMassLabel != null)
                _hudMassLabel.text = $"⬡ Mass: {player.Mass:0}";
            if (_hudScoreLabel != null)
                _hudScoreLabel.text = $"⬆ Score: {player.Mass * 10f:0}";
            if (_hudKillsLabel != null)
                _hudKillsLabel.text = $"☠ Kills: {_totalKills}";

            if (_hudBoostBar != null)
            {
                float massFraction = Mathf.Clamp01(player.Mass / GrowthMath.MaxMass);
                _hudBoostBar.rectTransform.sizeDelta = new Vector2(200f * massFraction, 6f);
                _hudBoostBar.color = player.IsBoosting
                    ? new Color(1f, 0.42f, 0.36f)
                    : new Color(0.25f, 0.88f, 0.77f);
            }

            // Boost button visual feedback
            if (_boostButtonImage != null)
            {
                float pulse = player.IsBoosting ? 0.8f + 0.2f * Mathf.Sin(Time.time * 10f) : 1f;
                _boostButtonImage.color = player.IsBoosting
                    ? new Color(1f, 0.42f, 0.36f, pulse)
                    : new Color(0.25f, 0.88f, 0.77f, 0.85f);
            }

            // Update minimap every frame
            if (_minimap != null)
                _minimap.UpdateMinimap(_allWorms, LocalPlayerId,
                    mapConfig.hasShrinkZone ? arenaRadius : -1f);

            // Update danger vignette
            if (_dangerVignette != null && !player.IsDead)
            {
                // BUG-14 FIX: warn about worms within 80% of player mass, not just bigger ones.
                // Even a slightly smaller worm can kill you by touching your body.
                float minDist = float.MaxValue;
                for (int i = 1; i < _allWorms.Count; i++)
                {
                    if (_allWorms[i].IsDead || _allWorms[i].Mass < player.Mass * 0.8f) continue;
                    float dx = _allWorms[i].X - player.X;
                    float dy = _allWorms[i].Y - player.Y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < minDist) minDist = d;
                }
                float dangerT = minDist < 200f ? Mathf.InverseLerp(200f, 40f, minDist) : 0f;
                _dangerVignette.SetDanger(dangerT);
            }
        }

        private List<string> _killFeedMessages = new List<string>();
        private List<float> _killFeedTimers = new List<float>();

        private void AddKillFeedEntry(string message)
        {
            _killFeedMessages.Insert(0, message);
            _killFeedTimers.Insert(0, KillFeedDuration);
            if (_killFeedMessages.Count > 4)
            {
                _killFeedMessages.RemoveAt(_killFeedMessages.Count - 1);
                _killFeedTimers.RemoveAt(_killFeedTimers.Count - 1);
            }
        }

        private void UpdateKillFeed()
        {
            for (int i = 0; i < _killFeedTimers.Count; i++)
                _killFeedTimers[i] -= Time.deltaTime;

            for (int i = _killFeedTimers.Count - 1; i >= 0; i--)
                if (_killFeedTimers[i] <= 0f)
                {
                    _killFeedTimers.RemoveAt(i);
                    _killFeedMessages.RemoveAt(i);
                }

            for (int i = 0; i < _killFeedEntries.Count; i++)
            {
                if (_killFeedEntries[i] == null) continue;
                if (i < _killFeedMessages.Count)
                {
                    _killFeedEntries[i].text = _killFeedMessages[i];
                    float alpha = Mathf.Clamp01(_killFeedTimers[i]);
                    var c = _killFeedEntries[i].color;
                    _killFeedEntries[i].color = new Color(c.r, c.g, c.b, alpha);
                }
                else
                {
                    _killFeedEntries[i].text = "";
                }
            }
        }

        // ─── Map Visuals ──────────────────────────────────────────────────────

        private void CreateDarknessOverlay()
        {
            if (!mapConfig.hasDarknessEvents) return;
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            _darknessOverlay = new GameObject("DarknessOverlay");
            _darknessOverlay.transform.SetParent(canvas.transform, false);

            var rt = _darknessOverlay.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var img = _darknessOverlay.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = false;
        }

        private void RefreshWispVisuals()
        {
            if (mapMechanics == null) return;
            var activeWisps = mapMechanics.Wisps;
            while (_wisps.Count > activeWisps.Count)
            {
                var extra = _wisps[_wisps.Count - 1];
                _wisps.RemoveAt(_wisps.Count - 1);
                if (extra != null) Destroy(extra.gameObject);
            }
            while (_wisps.Count < activeWisps.Count)
            {
                var go = new GameObject("Wisp");
                go.transform.SetParent(_pelletContainer);
                var sr = go.AddComponent<SpriteRenderer>();
                var tex = new Texture2D(16, 16);
                Color wispColor = new Color(0.6f, 0.9f, 0.3f, 0.5f);
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        float dx = x - 7.5f, dy = y - 7.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        tex.SetPixel(x, y, d < 5f ? wispColor : Color.clear);
                    }
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
                sr.material = pelletMaterial ?? new Material(Shader.Find("Sprites/Default"));
                go.transform.localScale = Vector3.one * 0.7f;
                _wisps.Add(go.transform);
            }
            for (int i = 0; i < activeWisps.Count && i < _wisps.Count; i++)
            {
                if (_wisps[i] != null)
                    _wisps[i].position = new Vector3(activeWisps[i].Position.x, activeWisps[i].Position.y, 0f);
            }
        }

        private void MaintainGiantPellets()
        {
            if (!mapConfig.hasGiantPellets) return;
            for (int i = _giantPellets.Count - 1; i >= 0; i--)
                if (_giantPellets[i] == null) _giantPellets.RemoveAt(i);

            // BUG-07 FIX: Use a timer instead of frame-rate-dependent Random.value < 0.01f.
            // At 60fps the old code triggered ~36% per second — now respawns one every 8s.
            if (_giantPellets.Count < mapConfig.giantPelletCount)
            {
                _giantPelletRespawnTimer -= Time.deltaTime;
                if (_giantPelletRespawnTimer <= 0f)
                {
                    _giantPelletRespawnTimer = 8f;
                    Vector2 pos = Random.insideUnitCircle * arenaRadius * 0.8f;
                    var pellet = Instantiate(_pelletPrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity, _pelletContainer);
                    pellet.SetActive(true);
                    pellet.transform.localScale = Vector3.one * 2.5f;
                    // BUG-05 FIX: Add GiantPelletMarker component instead of using an
                    // unregistered Unity tag that throws UnityException at runtime.
                    pellet.AddComponent<GiantPelletMarker>();
                    var sr = pellet.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = new Color(1f, 0.79f, 0.3f, 1f);
                    _giantPellets.Add(pellet.transform);
                }
            }
        }

        private void HandleWispCollisions()
        {
            if (mapMechanics == null || !mapConfig.hasWisps) return;
            if (_allWorms[0].IsDead) return;

            float headR = _allWorms[0].HeadRadius();
            float px = _allWorms[0].X;
            float py = _allWorms[0].Y;

            for (int i = _wisps.Count - 1; i >= 0; i--)
            {
                if (_wisps[i] == null) { _wisps.RemoveAt(i); continue; }
                Vector3 wPos = _wisps[i].position;
                float dx = px - wPos.x;
                float dy = py - wPos.y;
                if ((dx * dx + dy * dy) <= (headR + 4f) * (headR + 4f))
                {
                    Destroy(_wisps[i].gameObject);
                    _wisps.RemoveAt(i);
                }
            }
        }

        private void UpdateDarknessOverlay()
        {
            if (_darknessOverlay == null || mapMechanics == null) return;
            var img = _darknessOverlay.GetComponent<Image>();
            if (img == null) return;
            float targetAlpha = mapMechanics.IsDark ? 0.65f : 0f;
            float newAlpha = Mathf.Lerp(img.color.a, targetAlpha, Time.deltaTime * 2f);
            img.color = new Color(0f, 0f, 0f, newAlpha);
        }

        public Vector2 GetArenaRadius() => Vector2.one * arenaRadius;
    }
}
