using System.Collections.Generic;
using SlitherRoyale.Client.Networking;
using SlitherRoyale.Client.UI;
using SlitherRoyale.Client.VFX;
using SlitherRoyale.Server;
using UnityEngine;
using UnityEngine.UI;
using WormCore;

namespace SlitherRoyale.Client.Gameplay
{
    public class NetworkedGameManager : MonoBehaviour
    {
        public CameraFollow cameraFollow;
        public WormRenderer wormRenderer;
        public BotManager botManager;
        public LeaderboardUI leaderboardUI;
        public ComboCalloutUI comboCalloutUI;
        public DeathBurstVFX deathBurstVFX;
        public Bootstrapper bootstrapper;
        public MapConfig mapConfig;

        private ClientNetworkManager _netManager;
        private ClientPrediction _prediction;
        private EntityInterpolation _interpolation;
        private ServerNetworkManager _serverManager;
        private MapMechanics _mapMechanics;
        private Dictionary<int, string> _wormNames;  // Fixed: dictionary by ID, not list
        private bool _matchEnded;
        private bool _localWormDead;
        private float _matchResultTimer;
        private const float MatchResultTimeout = 5f;

        private void Start()
        {
            _netManager = GetComponent<ClientNetworkManager>();
            if (_netManager == null)
                _netManager = gameObject.AddComponent<ClientNetworkManager>();

            _prediction = GetComponent<ClientPrediction>();
            if (_prediction == null)
                _prediction = gameObject.AddComponent<ClientPrediction>();

            _interpolation = GetComponent<EntityInterpolation>();
            if (_interpolation == null)
                _interpolation = gameObject.AddComponent<EntityInterpolation>();

            _mapMechanics = FindObjectOfType<MapMechanics>();

            _wormNames = new Dictionary<int, string>(32);

            _netManager.OnServerEvent += OnServerEvent;
        }

        private void Update()
        {
            if (_matchEnded) return;

            if (_localWormDead)
            {
                _matchResultTimer += Time.deltaTime;
                if (_netManager.HasMatchResult || _matchResultTimer >= MatchResultTimeout)
                {
                    ShowMatchResult();
                    return;
                }
                return;
            }

            if (_netManager.TryGetSnapshot(out var snap))
            {
                _mapMechanics?.SetShrinkRadius(snap.ShrinkRadius);

                int localIdx = snap.LocalWorm.Id;

                var localWorm = new WormState
                {
                    Id = snap.LocalWorm.Id,
                    X = snap.LocalWorm.X,
                    Y = snap.LocalWorm.Y,
                    Heading = snap.LocalWorm.Heading,
                    Mass = snap.LocalWorm.Mass,
                    IsDead = snap.LocalWorm.IsDead,
                    Segments = GenerateSegments(snap.LocalWorm),
                };

                if (!_prediction.HasPrediction)
                    _prediction.Initialize(localWorm);

                Vector2 dir = InputHandler.GetSteerDirection();
                float desiredHeading = Mathf.Atan2(dir.y, dir.x);
                bool boostHeld = InputHandler.IsBoosting();
                _prediction.ApplyInput(desiredHeading, boostHeld, Time.deltaTime);
                _prediction.Reconcile(snap);

                var predicted = _prediction.PredictedState;
                predicted.Segments = GenerateSegmentsFromPredicted(predicted, snap.LocalWorm.SegmentCount);

                if (!snap.LocalWorm.IsDead)
                {
                    wormRenderer.UpdateFromState(predicted);
                    cameraFollow.SetTarget(predicted.X, predicted.Y, predicted.Mass);
                }
                else
                {
                    _localWormDead = true;
                }

                _interpolation.ApplySnapshot(snap.Entities);

                var entityWorms = new List<WormState>();
                var names = new List<string>();
                int selfId = snap.LocalWorm.Id;

                foreach (var ent in snap.Entities)
                {
                    if (_interpolation.TryGetInterpolated(ent.Id, out float ix, out float iy, out float ih, out float im))
                    {
                        entityWorms.Add(new WormState
                        {
                            Id = ent.Id,
                            X = ix, Y = iy, Heading = ih, Mass = im,
                            IsDead = ent.IsDead,
                            Segments = GenerateSegmentsFromInterpolated(ix, iy, ih, ent.SegmentCount),
                        });
                        names.Add(GetWormName(ent.Id));
                    }
                }

                entityWorms.Insert(0, predicted);
                names.Insert(0, "YOU");

                if (botManager != null)
                    botManager.UpdateRenderers(entityWorms);

                if (leaderboardUI != null)
                    leaderboardUI.UpdateLeaderboard(entityWorms, selfId, names);
            }
        }

        private void ShowMatchResult()
        {
            _matchEnded = true;
            int kills = 0;
            float score = 0;
            int rank = 0;
            string deathReason = "You were eliminated";

            if (_netManager.HasMatchResult)
            {
                var result = _netManager.MatchResult;
                kills = result.Kills;
                score = result.Score;
                rank = result.Rank;
            }

            var data = new ResultsData
            {
                Kills = kills,
                Score = score,
                DeathReason = deathReason,
                Rank = rank,
            };

            if (bootstrapper != null)
                Destroy(gameObject);
            ScreenManager.Instance.NavigateTo<ResultsScreen>(data);
        }

        private string GetWormName(int id)
        {
            if (!_wormNames.TryGetValue(id, out string name))
            {
                name = BotAI.GetBotName(id);
                _wormNames[id] = name;
            }
            return name;
        }

        private void OnServerEvent(ServerEventMsg evt)
        {
            switch (evt.EventType)
            {
                case ServerEventType.WormDied:
                    if (evt.Payload.Length >= 8)
                    {
                        int deadId = System.BitConverter.ToInt32(evt.Payload, 0);
                        int killerId = System.BitConverter.ToInt32(evt.Payload, 4);
                        if (killerId >= 0 && deadId != _netManager.PlayerId && killerId == _netManager.PlayerId)
                        {
                            comboCalloutUI?.ShowCallout("Kill", 1);
                        }
                    }
                    break;
            }
        }

        private List<WormState.Segment> GenerateSegments(EntitySnapshot snap)
        {
            int count = snap.SegmentCount > 0 ? snap.SegmentCount : (int)(GrowthMath.MassToLength(snap.Mass) / MovementMath.SegmentSpacing);
            if (count < 3) count = 3;
            var segs = new List<WormState.Segment>(count);
            float spacing = MovementMath.SegmentSpacing;
            for (int i = 0; i < count; i++)
            {
                float dist = spacing * (i + 1);
                segs.Add(new WormState.Segment
                {
                    X = snap.X - Mathf.Cos(snap.Heading) * dist,
                    Y = snap.Y - Mathf.Sin(snap.Heading) * dist,
                });
            }
            return segs;
        }

        private List<WormState.Segment> GenerateSegmentsFromPredicted(WormState ws, int serverCount)
        {
            int count = serverCount > 0 ? serverCount : (int)(GrowthMath.MassToLength(ws.Mass) / MovementMath.SegmentSpacing);
            if (count < 3) count = 3;
            var segs = new List<WormState.Segment>(count);
            float spacing = MovementMath.SegmentSpacing;
            for (int i = 0; i < count; i++)
            {
                float dist = spacing * (i + 1);
                segs.Add(new WormState.Segment
                {
                    X = ws.X - Mathf.Cos(ws.Heading) * dist,
                    Y = ws.Y - Mathf.Sin(ws.Heading) * dist,
                });
            }
            return segs;
        }

        private List<WormState.Segment> GenerateSegmentsFromInterpolated(float x, float y, float heading, int segCount)
        {
            int count = segCount > 0 ? segCount : 10;
            if (count < 3) count = 3;
            var segs = new List<WormState.Segment>(count);
            float spacing = MovementMath.SegmentSpacing;
            for (int i = 0; i < count; i++)
            {
                float dist = spacing * (i + 1);
                segs.Add(new WormState.Segment
                {
                    X = x - Mathf.Cos(heading) * dist,
                    Y = y - Mathf.Sin(heading) * dist,
                });
            }
            return segs;
        }

        private void OnDestroy()
        {
            if (_netManager != null)
                _netManager.OnServerEvent -= OnServerEvent;
        }
    }
}
