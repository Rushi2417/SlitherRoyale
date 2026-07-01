using System;
using System.Collections.Generic;
using SlitherRoyale.Client.Networking;
using UnityEngine;
using WormCore;

namespace SlitherRoyale.Server
{
    public class ServerGameLoop
    {
        private List<WormState> _allWorms;
        private List<BotContext> _botContexts;
        private List<ServerPlayerState> _players;
        private ComboSystem _comboSystem;
        private float _gameTime;
        private List<ServerPellet> _pellets;
        private int _maxPellets = 800;
        private const float TickRate = 1f / 25f;

        private ModeConfig _modeConfig;
        private float _matchArenaRadius = 800f;
        private float _brShrinkTimer;
        private float _brCurrentRadius;
        private int _currentRound = 1;
        private int[] _roundWins;
        private System.Random _rng = new System.Random();
        private bool _matchEnded;
        private Dictionary<int, int> _teamScores;

        public delegate void OnEventDelegate(ServerEventType type, byte[] payload);
        public event OnEventDelegate OnEvent;

        public List<WormState> AllWorms => _allWorms;
        public List<ServerPlayerState> Players => _players;
        public float GameTime => _gameTime;
        public float CurrentShrinkRadius => _brCurrentRadius;

        public class ServerPlayerState
        {
            public int PlayerId;
            public string Name;
            public float Score;
            public int Kills;
            public bool Connected;
            public double LastInputTime;
            public bool IsBot;
            public BotSkillTier BotTier;
        }

        private struct ServerPellet
        {
            public float X, Y;
            public float Value;
        }

        public void Initialize(ModeConfig mode = default)
        {
            _modeConfig = mode.Mode == 0 ? ModeConfig.GetDefault(MatchMode.FreeForAll) : mode;
            _matchArenaRadius = 800f * _modeConfig.ArenaRadiusMultiplier;
            _brCurrentRadius = _matchArenaRadius;
            _currentRound = 1;
            _matchEnded = false;
            _roundWins = new int[32];
            _teamScores = new Dictionary<int, int>();

            _allWorms = new List<WormState>(32);
            _players = new List<ServerPlayerState>(32);
            _botContexts = new List<BotContext>(20);
            _comboSystem = new ComboSystem();
            _comboSystem.ComboWindowSeconds = 10f;
            _gameTime = 0f;
            _pellets = new List<ServerPellet>(_maxPellets);
            SpawnInitialPellets(500);
        }

        private void SpawnInitialPellets(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(_rng.NextDouble() * Mathf.PI * 2f);
                float r = (float)(_rng.NextDouble() * _matchArenaRadius * 0.9f);
                _pellets.Add(new ServerPellet
                {
                    X = Mathf.Cos(angle) * r,
                    Y = Mathf.Sin(angle) * r,
                    Value = GrowthMath.PelletBaseValue
                });
            }
        }

        public int AddPlayer(int playerId, string name, bool isBot)
        {
            var player = new ServerPlayerState
            {
                PlayerId = playerId,
                Name = name,
                Connected = !isBot,
                IsBot = isBot,
            };
            _players.Add(player);

            int teamId = playerId;
            if (_modeConfig.Mode == MatchMode.Duos)
            {
                teamId = (_players.Count % 2 == 0) ? _players.Count / 2 : (_players.Count + 1) / 2;
                if (!_teamScores.ContainsKey(teamId))
                    _teamScores[teamId] = 0;
            }

            var worm = new WormState
            {
                Id = playerId,
                X = (float)(_rng.NextDouble() * _matchArenaRadius - _matchArenaRadius * 0.5f),
                Y = (float)(_rng.NextDouble() * _matchArenaRadius - _matchArenaRadius * 0.5f),
                Heading = (float)(_rng.NextDouble() * Mathf.PI * 2f),
                Mass = 10f,
                IsBoosting = false,
                IsDead = false,
                TeamId = teamId,
                Segments = new List<WormState.Segment>(),
            };
            _allWorms.Add(worm);

            if (isBot)
            {
                BotSkillTier tier = BotAI.GetRandomTier(playerId);
                player.BotTier = tier;
                _botContexts.Add(new BotContext(tier, playerId));
            }

            return playerId;
        }

        public void RemovePlayer(int playerId)
        {
            int idx = _players.FindIndex(p => p.PlayerId == playerId);
            if (idx >= 0)
            {
                bool wasBot = _players[idx].IsBot;
                _players.RemoveAt(idx);
                int wormIdx = _allWorms.FindIndex(w => w.Id == playerId);
                if (wormIdx >= 0) _allWorms.RemoveAt(wormIdx);
                if (wasBot)
                {
                    _botContexts.RemoveAll(b => b.PlayerId == playerId);
                }
            }
        }

        public int GetPlayerIndex(int playerId) => _allWorms.FindIndex(w => w.Id == playerId);

        public bool ApplyInput(int playerId, float heading, bool boostHeld)
        {
            int idx = GetPlayerIndex(playerId);
            if (idx < 0 || _allWorms[idx].IsDead) return false;
            var player = _players.Find(p => p.PlayerId == playerId);
            if (player != null) player.LastInputTime = DateTime.UtcNow.Ticks / 10000000.0;
            var ws = _allWorms[idx];

            float preX = ws.X, preY = ws.Y;

            float angleDiff = heading - ws.Heading;
            while (angleDiff > Mathf.PI) angleDiff -= Mathf.PI * 2f;
            while (angleDiff < -Mathf.PI) angleDiff += Mathf.PI * 2f;

            float turnMultiplier = 1f;
            if (_modeConfig.Mode == MatchMode.Ranked1v1) turnMultiplier = 1.5f;
            float turnRadius = (1f + ws.Mass * 0.0008f) * MovementMath.TurnRadiusMultiplier;
            float maxTurnPerTick = MovementMath.TurnSpeed * TickRate / turnRadius * turnMultiplier;
            if (Mathf.Abs(angleDiff) > maxTurnPerTick * 2f)
                return false;

            float maxSpeed = MovementMath.CalculateSpeed(ws.Mass, boostHeld) * 1.2f;
            float maxDistPerTick = maxSpeed * TickRate;
            float expectedDist = Mathf.Sqrt(
                (ws.X + Mathf.Cos(heading) * maxDistPerTick - ws.X) * (ws.X + Mathf.Cos(heading) * maxDistPerTick - ws.X) +
                (ws.Y + Mathf.Sin(heading) * maxDistPerTick - ws.Y) * (ws.Y + Mathf.Sin(heading) * maxDistPerTick - ws.Y));
            float maxAllowedMove = MovementMath.CalculateSpeed(ws.Mass, true) * TickRate * 2f;
            if (expectedDist > maxAllowedMove)
                return false;

            float currentDist = Mathf.Sqrt(ws.X * ws.X + ws.Y * ws.Y);
            if (currentDist > _matchArenaRadius * 1.1f)
                return false;

            MovementMath.IntegrateMovement(ref ws, heading, boostHeld, TickRate);
            GrowthMath.ApplyBoostDrain(ref ws, TickRate);

            float totalMoved = Mathf.Sqrt((ws.X - preX) * (ws.X - preX) + (ws.Y - preY) * (ws.Y - preY));
            if (totalMoved > maxAllowedMove)
            {
                ws.X = preX; ws.Y = preY;
                _allWorms[idx] = ws;
                return false;
            }

            _allWorms[idx] = ws;
            return true;
        }

        public void Tick(double currentTime = 0)
        {
            _gameTime += TickRate;

            for (int i = 0; i < _allWorms.Count; i++)
            {
                if (_allWorms[i].IsDead) continue;
                var player = _players.Find(p => p.PlayerId == _allWorms[i].Id);
                if (player == null) continue;

                if (player.IsBot)
                {
                    int botIdx = _botContexts.FindIndex(b => b.PlayerId == _allWorms[i].Id);
                    if (botIdx >= 0)
                    {
                        var pellets = GetPelletInfo();
                        BotDecision decision = BotAI.Decide(
                            _allWorms[i], _botContexts[botIdx],
                            _allWorms, pellets, TickRate, _gameTime);
                        var ws = _allWorms[i];
                        MovementMath.IntegrateMovement(ref ws, decision.DesiredHeading, decision.BoostHeld, TickRate);
                        GrowthMath.ApplyBoostDrain(ref ws, TickRate);
                        _allWorms[i] = ws;
                    }
                }
                else if (currentTime > 0 && currentTime - player.LastInputTime > 3.0)
                {
                    var ws = _allWorms[i];
                    MovementMath.IntegrateMovement(ref ws, ws.Heading, false, TickRate);
                    _allWorms[i] = ws;
                }
            }

            if (_modeConfig.Mode == MatchMode.BattleRoyale)
            {
                _brShrinkTimer += TickRate;
                if (_brShrinkTimer >= _modeConfig.ShrinkInterval)
                {
                    _brShrinkTimer = 0f;
                    _brCurrentRadius = Mathf.Max(_brCurrentRadius - 30f, 30f);
                }
                for (int i = 0; i < _allWorms.Count; i++)
                {
                    if (_allWorms[i].IsDead) continue;
                    float dist = Mathf.Sqrt(_allWorms[i].X * _allWorms[i].X + _allWorms[i].Y * _allWorms[i].Y);
                    if (dist > _brCurrentRadius)
                    {
                        var ws = _allWorms[i];
                        ws.Mass -= _modeConfig.ShrinkDamagePerSecond * TickRate;
                        if (ws.Mass <= 1f) ws.IsDead = true;
                        _allWorms[i] = ws;
                        if (ws.IsDead) HandleDeath(i, -1);
                    }
                }
            }

            CheckPelletCollisions();
            CheckWormCollisions();
            MaintainPelletCount();
            _comboSystem.Update(_gameTime);
            CheckEndCondition();
        }

        public bool IsMatchEnded => _matchEnded;
        public event Action OnMatchEnded;

        private void FireMatchEnded()
        {
            _matchEnded = true;
            OnMatchEnded?.Invoke();
        }

        private void CheckEndCondition()
        {
            int aliveCount = 0;
            int lastAliveId = -1;
            HashSet<int> aliveTeams = new HashSet<int>();

            for (int i = 0; i < _allWorms.Count; i++)
            {
                if (_allWorms[i].IsDead) continue;
                aliveCount++;
                lastAliveId = _allWorms[i].Id;
                if (_modeConfig.Mode == MatchMode.Duos)
                    aliveTeams.Add(_allWorms[i].TeamId);
            }

            if (_modeConfig.Mode == MatchMode.Ranked1v1)
            {
                if (_gameTime >= _modeConfig.RoundDurationSeconds || aliveCount <= 1)
                {
                    int winnerId = aliveCount >= 1 ? lastAliveId : GetLeadingPlayer();
                    if (winnerId >= 0) _roundWins[winnerId]++;
                    _currentRound++;
                    if (_currentRound > _modeConfig.RoundsToWin || _roundWins[winnerId] >= _modeConfig.RoundsToWin)
                        FireMatchEnded();
                    else
                        ResetRound();
                }
                return;
            }

            if (_modeConfig.Mode == MatchMode.BattleRoyale)
            {
                if (aliveCount <= 1 && _allWorms.Count >= 2)
                    FireMatchEnded();
                if (_gameTime >= _modeConfig.RoundDurationSeconds)
                    FireMatchEnded();
                return;
            }

            if (_modeConfig.Mode == MatchMode.Duos)
            {
                if (aliveTeams.Count <= 1 && _players.Count >= 2)
                    FireMatchEnded();
                if (_gameTime >= _modeConfig.RoundDurationSeconds)
                    FireMatchEnded();
                return;
            }

            if (_gameTime >= 600f)
                FireMatchEnded();
        }

        private void ResetRound()
        {
            for (int i = 0; i < _allWorms.Count; i++)
            {
                var ws = _allWorms[i];
                ws.X = (float)(_rng.NextDouble() * _matchArenaRadius - _matchArenaRadius * 0.5f);
                ws.Y = (float)(_rng.NextDouble() * _matchArenaRadius - _matchArenaRadius * 0.5f);
                ws.Heading = (float)(_rng.NextDouble() * Mathf.PI * 2f);
                ws.Mass = 10f;
                ws.IsDead = false;
                ws.Segments = new List<WormState.Segment>();
                _allWorms[i] = ws;
            }
            _gameTime = 0f;
            _pellets.Clear();
            SpawnInitialPellets(500);
        }

        private int GetLeadingPlayer()
        {
            int bestId = -1;
            float bestMass = -1f;
            for (int i = 0; i < _allWorms.Count; i++)
            {
                if (_allWorms[i].IsDead) continue;
                if (_allWorms[i].Mass > bestMass) { bestMass = _allWorms[i].Mass; bestId = _allWorms[i].Id; }
            }
            return bestId;
        }

        private void CheckPelletCollisions()
        {
            for (int w = 0; w < _allWorms.Count; w++)
            {
                if (_allWorms[w].IsDead) continue;
                float headR = _allWorms[w].HeadRadius();
                float px = _allWorms[w].X;
                float py = _allWorms[w].Y;

                for (int i = _pellets.Count - 1; i >= 0; i--)
                {
                    var pellet = _pellets[i];
                    if (CollisionMath.HeadVsPellet(px, py, headR, pellet.X, pellet.Y, 3f))
                    {
                        var s = _allWorms[w];
                        GrowthMath.ApplyPelletGain(ref s, pellet.Value);
                        _allWorms[w] = s;
                        _pellets.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private void CheckWormCollisions()
        {
            for (int a = 0; a < _allWorms.Count; a++)
            {
                if (_allWorms[a].IsDead) continue;
                float ax = _allWorms[a].X, ay = _allWorms[a].Y;
                float massA = _allWorms[a].Mass;
                float headRadiusA = _allWorms[a].HeadRadius();

                for (int b = a + 1; b < _allWorms.Count; b++)
                {
                    if (_allWorms[b].IsDead) continue;

                    if (_modeConfig.Mode == MatchMode.Duos && _allWorms[a].TeamId == _allWorms[b].TeamId)
                        continue;

                    var hvh = CollisionMath.HeadVsHead(ax, ay, massA, _allWorms[b].X, _allWorms[b].Y, _allWorms[b].Mass);
                    if (hvh == CollisionMath.HeadOnCollisionResult.AWins) HandleDeath(b, a);
                    else if (hvh == CollisionMath.HeadOnCollisionResult.BWins) HandleDeath(a, b);
                    else if (hvh == CollisionMath.HeadOnCollisionResult.BothDie) { HandleDeath(a, -1); HandleDeath(b, -1); }

                    if (_allWorms[a].IsDead) break;
                }

                if (_allWorms[a].IsDead) continue;

                float bodyRadiusB = _allWorms[a].BodyRadius();
                for (int b = 0; b < _allWorms.Count; b++)
                {
                    if (b == a || _allWorms[b].IsDead) continue;
                    if (_modeConfig.Mode == MatchMode.Duos && _allWorms[a].TeamId == _allWorms[b].TeamId)
                        continue;
                    if (_allWorms[b].Segments == null) continue;
                    for (int s = 0; s < _allWorms[b].Segments.Count; s++)
                    {
                        var seg = _allWorms[b].Segments[s];
                        if (CollisionMath.HeadVsBody(ax, ay, headRadiusA, seg.X, seg.Y, _allWorms[b].BodyRadius()))
                        {
                            HandleDeath(a, b);
                            break;
                        }
                    }
                    if (_allWorms[a].IsDead) break;
                }

                if (_allWorms[a].IsDead) continue;

                float boundaryR = _matchArenaRadius;
                float wormDist = Mathf.Sqrt(ax * ax + ay * ay);
                if (wormDist > boundaryR)
                {
                    var ws = _allWorms[a];
                    ws.IsDead = true;
                    _allWorms[a] = ws;
                    HandleDeath(a, -1);
                }
            }
        }

        private void HandleDeath(int deadIdx, int killerIdx)
        {
            if (deadIdx < 0 || deadIdx >= _allWorms.Count || _allWorms[deadIdx].IsDead) return;
            var ws = _allWorms[deadIdx];
            ws.IsDead = true;
            _allWorms[deadIdx] = ws;

            if (killerIdx >= 0)
            {
                _comboSystem.RegisterKill(_allWorms[killerIdx].Id, _gameTime);
                var killerPlayer = _players.Find(p => p.PlayerId == _allWorms[killerIdx].Id);
                if (killerPlayer != null) killerPlayer.Kills++;
                var kw = _allWorms[killerIdx];
                kw.Mass += ws.Mass * 0.5f;
                _allWorms[killerIdx] = kw;
            }

            float massValue = ws.Mass * 2f;
            int burstCount = (int)(massValue / 10f);
            for (int i = 0; i < burstCount && _pellets.Count < _maxPellets; i++)
            {
                float angle = (float)(_rng.NextDouble() * Mathf.PI * 2f);
                float r = (float)(_rng.NextDouble() * 30f);
                _pellets.Add(new ServerPellet
                {
                    X = ws.X + Mathf.Cos(angle) * r,
                    Y = ws.Y + Mathf.Sin(angle) * r,
                    Value = GrowthMath.PelletBaseValue,
                });
            }

            var evtBytes = new byte[8];
            BitConverter.GetBytes(ws.Id).CopyTo(evtBytes, 0);
            int killerId = killerIdx >= 0 ? _allWorms[killerIdx].Id : -1;
            BitConverter.GetBytes(killerId).CopyTo(evtBytes, 4);
            OnEvent?.Invoke(ServerEventType.WormDied, evtBytes);
        }

        private void MaintainPelletCount()
        {
            if (_pellets.Count < _maxPellets && _rng.NextDouble() < 0.3)
            {
                float angle2 = (float)(_rng.NextDouble() * Mathf.PI * 2f);
                float r2 = (float)(_rng.NextDouble() * 30f);
                _pellets.Add(new ServerPellet
                {
                    X = Mathf.Cos(angle2) * r2 * 10f,
                    Y = Mathf.Sin(angle2) * r2 * 10f,
                    Value = GrowthMath.PelletBaseValue,
                });
            }
        }

        public List<PelletInfo> GetPelletInfo()
        {
            var list = new List<PelletInfo>(_pellets.Count);
            foreach (var p in _pellets)
                list.Add(new PelletInfo { X = p.X, Y = p.Y, Value = p.Value });
            return list;
        }

        public float GetScore(int playerId)
        {
            int idx = GetPlayerIndex(playerId);
            if (idx < 0) return 0;
            return _allWorms[idx].Mass;
        }

        public MatchResultMsg GetMatchResultForPlayer(int playerId)
        {
            var p = _players.Find(x => x.PlayerId == playerId);
            if (p == null) return default;
            var lb = GetLeaderboard();
            int idx = GetPlayerIndex(playerId);
            float score = idx >= 0 ? _allWorms[idx].Mass : 0f;
            int rank = lb.FindIndex(e => e.PlayerId == playerId) + 1;
            if (rank <= 0) rank = _players.Count;
            return new MatchResultMsg
            {
                Kills = p.Kills,
                Score = score * 10f,
                Rank = rank,
                CoinsEarned = Mathf.RoundToInt(score * 2f + p.Kills * 25f + Mathf.Max(0, _players.Count - rank) * 10f),
                BPXPEarned = Mathf.RoundToInt(score * 0.5f + p.Kills * 5f + Mathf.Max(0, _players.Count - rank) * 3f),
            };
        }

        public List<LeaderboardEntry> GetLeaderboard()
        {
            var lb = new List<LeaderboardEntry>();
            for (int i = 0; i < _allWorms.Count; i++)
            {
                if (_allWorms[i].IsDead) continue;
                lb.Add(new LeaderboardEntry { PlayerId = _allWorms[i].Id, Score = _allWorms[i].Mass });
            }
            lb.Sort((a, b) => b.Score.CompareTo(a.Score));
            return lb;
        }
    }
}
