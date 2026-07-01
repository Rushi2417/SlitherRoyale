using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using SlitherRoyale.Client.Networking;
using UnityEngine;
using WormCore;

namespace SlitherRoyale.Server
{
    public class ServerNetworkManager : MonoBehaviour
    {
        private NetTransport _transport;
        private Thread _serverThread;
        private ServerGameLoop _gameLoop;
        private ServerMatchmaker _matchmaker;
        private ConcurrentQueue<ClientAction> _actions = new ConcurrentQueue<ClientAction>();
        private volatile bool _running;
        private readonly object _gameLock = new object();
        private readonly System.Random _rng = new System.Random();
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private double _lastTickTime;
        private double _lastSnapTime;
        private double _lastEventResendTime;

        private ConcurrentDictionary<IPEndPoint, int> _clientMap = new ConcurrentDictionary<IPEndPoint, int>();
        private ConcurrentDictionary<int, IPEndPoint> _reverseClientMap = new ConcurrentDictionary<int, IPEndPoint>();
        private ConcurrentDictionary<int, double> _lastInputTime = new ConcurrentDictionary<int, double>();
        private ConcurrentDictionary<IPEndPoint, int> _sessionTokens = new ConcurrentDictionary<IPEndPoint, int>();
        private HashSet<int> _disconnectedPlayers = new HashSet<int>();

        private const int ServerPort = 12345;
        private const double TickRate = 1.0 / 25.0;
        private const double SnapshotInterval = 1.0 / 25.0;
        private const double EventResendInterval = 0.5;
        private const int MaxClients = 20;
        private int _nextPlayerId = 1;

        private List<int> _botIds = new List<int>();
        private int _humanCount;
        private ModeConfig _modeConfig = ModeConfig.GetDefault(MatchMode.FreeForAll);

        private struct ClientAction
        {
            public enum ActionType { Connect, Input, Disconnect, Ack }
            public ActionType Type;
            public IPEndPoint EndPoint;
            public int PlayerId;
            public float Heading;
            public bool BoostHeld;
            public ushort AckSeqNum;
        }

        private struct ReliableEvent
        {
            public ushort SeqNum;
            public byte[] Data;
            public HashSet<int> PendingAcks;
            public float Timer;
        }

        private List<ReliableEvent> _pendingEvents = new List<ReliableEvent>();
        private ushort _eventSeqNum;
        private Dictionary<IPEndPoint, HashSet<ushort>> _ackedSeqs = new Dictionary<IPEndPoint, HashSet<ushort>>();

        public void SetModeConfig(ModeConfig cfg)
        {
            _modeConfig = cfg;
        }

        public void StartServer(int port = ServerPort)
        {
            _gameLoop = new ServerGameLoop();
            _gameLoop.Initialize(_modeConfig);
            _gameLoop.OnEvent += OnServerEvent;
            _gameLoop.OnMatchEnded += OnMatchEnded;

            _transport = new NetTransport();
            _transport.StartServer(port);

            _running = true;
            _lastTickTime = _stopwatch.Elapsed.TotalSeconds;
            _lastSnapTime = _lastTickTime;
            _lastEventResendTime = _lastTickTime;
            _serverThread = new Thread(ServerLoop) { IsBackground = true };
            _serverThread.Start();

            _matchmaker = gameObject.AddComponent<ServerMatchmaker>();
            _matchmaker.Initialize(0);
            _matchmaker.OnBotSpawn += OnMatchmakerBotSpawn;
            _matchmaker.OnBotRemoved += OnMatchmakerBotRemoved;

            UnityEngine.Debug.Log("[Server] Started on port " + port);
            SpawnInitialBots();
        }

        private void Start()
        {
            StartServer(ServerPort);
        }

        private void SpawnInitialBots()
        {
            int targetBots = _modeConfig.Mode == MatchMode.Ranked1v1 ? 1 :
                             _modeConfig.Mode == MatchMode.Duos ? _modeConfig.MaxPlayers :
                             15;
            for (int i = 0; i < targetBots; i++)
            {
                int botId = _nextPlayerId++;
                string botName = BotAI.GetBotName(botId);
                lock (_gameLock) { _gameLoop.AddPlayer(botId, botName, true); }
                _botIds.Add(botId);
            }
            UnityEngine.Debug.Log($"[Server] Spawned {targetBots} bots for mode {_modeConfig.Mode}");
        }

        private void ServerLoop()
        {
            while (_running)
            {
                Thread.Sleep(1);
                ProcessIncoming();
                double now = _stopwatch.Elapsed.TotalSeconds;

                if (now - _lastTickTime >= TickRate)
                {
                    _lastTickTime = now;
                    lock (_gameLock) { _gameLoop.Tick(now); }
                }

                if (now - _lastSnapTime >= SnapshotInterval)
                {
                    _lastSnapTime = now;
                    lock (_gameLock) { BroadcastSnapshot(); }
                }

                if (now - _lastEventResendTime >= EventResendInterval)
                {
                    _lastEventResendTime = now;
                    ResendUnackedEvents();
                }
            }
        }

        private void ProcessIncoming()
        {
            while (_transport.TryReceive(out var packet))
            {
                if (packet.Length < 1) continue;
                MessageType type = (MessageType)packet.Data[0];
                var ep = packet.Source;

                switch (type)
                {
                    case MessageType.ConnectionRequest:
                        HandleConnection(ep, packet.Data);
                        break;
                    case MessageType.ClientInput:
                        HandleInput(ep, packet.Data);
                        break;
                    case MessageType.ServerEventAck:
                        HandleAck(ep, packet.Data);
                        break;
                    case MessageType.Disconnect:
                        HandleDisconnect(ep);
                        break;
                }
            }
        }

        private void HandleConnection(IPEndPoint ep, byte[] data)
        {
            if (_clientMap.ContainsKey(ep))
            {
                int existingId = _clientMap[ep];
                lock (_disconnectedPlayers)
                {
                    if (_disconnectedPlayers.Contains(existingId))
                    {
                        _disconnectedPlayers.Remove(existingId);
                        _lastInputTime[existingId] = _stopwatch.Elapsed.TotalSeconds;
                        _clientMap[ep] = existingId;
                        _reverseClientMap[existingId] = ep;
                        int reconnectToken = _sessionTokens.TryGetValue(ep, out int existingToken) ? existingToken : _rng.Next(100000, 999999);
                        _sessionTokens[ep] = reconnectToken;
                        var reaccept = new ConnectionAcceptedMsg { PlayerId = existingId, SessionToken = reconnectToken };
                        var reab = reaccept.Serialize();
                        _transport.SendTo(reab, reab.Length, ep);
                        UnityEngine.Debug.Log($"[Server] Player {existingId} reconnected from {ep}");
                        return;
                    }
                }
                return;
            }
            if (_clientMap.Count >= MaxClients) return;

            int playerId = _nextPlayerId++;
            int token = _rng.Next(100000, 999999);
            lock (_disconnectedPlayers) { _disconnectedPlayers.Remove(playerId); }
            var connMsg = ConnectionRequestMsg.Deserialize(data);
            _clientMap[ep] = playerId;
            _sessionTokens[ep] = token;
            _reverseClientMap[playerId] = ep;
            _lastInputTime[playerId] = _stopwatch.Elapsed.TotalSeconds;

            lock (_gameLock) { _gameLoop.AddPlayer(playerId, connMsg.PlayerName, false); }
            Interlocked.Increment(ref _humanCount);
            if (_matchmaker != null) _matchmaker.OnPlayerConnected();
            _transport.SetRemoteEndPoint(ep);

            var accept = new ConnectionAcceptedMsg { PlayerId = playerId, SessionToken = token };
            var acceptBytes = accept.Serialize();
            _transport.SendTo(acceptBytes, acceptBytes.Length, ep);

            WormState worm;
            lock (_gameLock) { worm = _gameLoop.AllWorms.Find(w => w.Id == playerId); }
            if (worm.Id != 0)
            {
                var evtBytes = new byte[4];
                BitConverter.GetBytes(playerId).CopyTo(evtBytes, 0);
                BroadcastEvent(ServerEventType.WormSpawned, evtBytes);
            }

            UnityEngine.Debug.Log($"[Server] Player {playerId} connected from {ep} (token {token})");
        }

        private void HandleInput(IPEndPoint ep, byte[] data)
        {
            if (!_clientMap.TryGetValue(ep, out int playerId)) return;
            var input = ClientInputMsg.Deserialize(data, 0);
            if (!_sessionTokens.TryGetValue(ep, out int expectedToken) || input.SessionToken != expectedToken)
            {
                UnityEngine.Debug.LogWarning($"[Server] Token mismatch from player {playerId} at {ep}");
                return;
            }
            _lastInputTime[playerId] = _stopwatch.Elapsed.TotalSeconds;
            lock (_gameLock) { _gameLoop.ApplyInput(playerId, input.Heading, input.BoostHeld); }
        }

        private void HandleAck(IPEndPoint ep, byte[] data)
        {
            ushort seqNum = BitConverter.ToUInt16(data, 1);
            if (!_ackedSeqs.TryGetValue(ep, out var set))
            {
                set = new HashSet<ushort>();
                _ackedSeqs[ep] = set;
            }
            set.Add(seqNum);
        }

        private void HandleDisconnect(IPEndPoint ep)
        {
            if (!_clientMap.TryGetValue(ep, out int playerId)) return;

            _clientMap.TryRemove(ep, out _);
            _reverseClientMap.TryRemove(playerId, out _);
            _sessionTokens.TryRemove(ep, out _);

            lock (_disconnectedPlayers)
            {
                _disconnectedPlayers.Add(playerId);
            }

            Interlocked.Decrement(ref _humanCount);
            if (_matchmaker != null) _matchmaker.OnPlayerDisconnected();
            UnityEngine.Debug.Log($"[Server] Player {playerId} disconnected (reconnect window: 10s)");
        }

        private void OnServerEvent(ServerEventType type, byte[] payload)
        {
            var evt = new ServerEventMsg
            {
                SeqNum = _eventSeqNum++,
                EventType = type,
                Payload = payload,
            };
            var data = evt.Serialize();
            var pending = new HashSet<int>(_reverseClientMap.Keys);
            _pendingEvents.Add(new ReliableEvent
            {
                SeqNum = evt.SeqNum,
                Data = data,
                PendingAcks = pending,
                Timer = 0f,
            });
        }

        private void BroadcastEvent(ServerEventType type, byte[] payload)
        {
            OnServerEvent(type, payload);
        }

        private void OnMatchmakerBotSpawn(int botId)
        {
            string botName = BotAI.GetBotName(botId);
            lock (_gameLock) { _gameLoop.AddPlayer(botId, botName, true); }
            _botIds.Add(botId);

            var evtBytes = new byte[4];
            BitConverter.GetBytes(botId).CopyTo(evtBytes, 0);
            BroadcastEvent(ServerEventType.WormSpawned, evtBytes);
        }

        private void OnMatchmakerBotRemoved(int botId)
        {
            _botIds.Remove(botId);
            lock (_gameLock) { _gameLoop.RemovePlayer(botId); }
        }

        public void Poll()
        {
            if (!_running) return;

            if (_matchmaker != null)
                _matchmaker.Tick(Time.deltaTime);

            CheckTimeouts();
        }

        private void Update()
        {
            Poll();
        }

        private void BroadcastSnapshot()
        {
            var lb = _gameLoop.GetLeaderboard();
            var localWorms = _gameLoop.AllWorms;

            foreach (var kv in _clientMap)
            {
                var ep = kv.Key;
                int playerId = kv.Value;
                int wormIdx = _gameLoop.GetPlayerIndex(playerId);
                if (wormIdx < 0) continue;

                var worm = localWorms[wormIdx];
                var localSnap = new EntitySnapshot
                {
                    Id = worm.Id,
                    X = worm.X,
                    Y = worm.Y,
                    Heading = worm.Heading,
                    Mass = worm.Mass,
                    IsDead = worm.IsDead,
                    SegmentCount = worm.Segments?.Count ?? 0,
                    TeamId = worm.TeamId,
                };

                var remoteEntities = new List<EntitySnapshot>();
                foreach (var other in localWorms)
                {
                    if (other.Id == playerId) continue;
                    float dx = other.X - worm.X;
                    float dy = other.Y - worm.Y;
                    if (dx * dx + dy * dy < 1200f * 1200f && !other.IsDead)
                    {
                        remoteEntities.Add(new EntitySnapshot
                        {
                            Id = other.Id,
                            X = other.X,
                            Y = other.Y,
                            Heading = other.Heading,
                            Mass = other.Mass,
                            IsDead = other.IsDead,
                            SegmentCount = other.Segments?.Count ?? 0,
                            TeamId = other.TeamId,
                        });
                    }
                }

                var snap = new ServerSnapshotMsg
                {
                    Tick = (uint)(_gameLoop.GameTime * 25f),
                    PlayerId = playerId,
                    LocalWorm = localSnap,
                    Entities = remoteEntities,
                    Leaderboard = lb,
                    ShrinkRadius = _gameLoop.CurrentShrinkRadius,
                };

                var data = snap.Serialize();
                _transport.SendTo(data, data.Length, ep);
            }
        }

        private void ResendUnackedEvents()
        {
            for (int i = _pendingEvents.Count - 1; i >= 0; i--)
            {
                var evt = _pendingEvents[i];
                var toRemove = new List<int>();
                foreach (int pid in evt.PendingAcks)
                {
                    if (_reverseClientMap.TryGetValue(pid, out var ep))
                    {
                        if (_ackedSeqs.TryGetValue(ep, out var acked) && acked.Contains(evt.SeqNum))
                            toRemove.Add(pid);
                        else
                            _transport.SendTo(evt.Data, evt.Data.Length, ep);
                    }
                    else
                    {
                        toRemove.Add(pid);
                    }
                }
                foreach (var pid in toRemove)
                    evt.PendingAcks.Remove(pid);
                if (evt.PendingAcks.Count == 0)
                    _pendingEvents.RemoveAt(i);
                else
                    _pendingEvents[i] = evt;
            }
        }

        private void CheckTimeouts()
        {
            double now = _stopwatch.Elapsed.TotalSeconds;

            var timedOut = new List<int>();
            lock (_disconnectedPlayers)
            {
                foreach (int pid in _disconnectedPlayers)
                {
                    if (_lastInputTime.TryGetValue(pid, out double lastInput))
                    {
                        if (now - lastInput > 10.0)
                            timedOut.Add(pid);
                    }
                }
            }

            lock (_gameLock)
            {
                foreach (int pid in timedOut)
                {
                    _gameLoop.RemovePlayer(pid);
                    lock (_disconnectedPlayers) { _disconnectedPlayers.Remove(pid); }
                    _lastInputTime.TryRemove(pid, out _);
                    UnityEngine.Debug.Log($"[Server] Player {pid} timed out, removed from game");
                }
            }
        }

        private void OnMatchEnded()
        {
            foreach (var kv in _clientMap)
            {
                MatchResultMsg result;
                lock (_gameLock) { result = _gameLoop.GetMatchResultForPlayer(kv.Value); }
                var data = result.Serialize();
                _transport.SendTo(data, data.Length, kv.Key);
            }
        }

        private void OnDestroy()
        {
            _running = false;
            if (_transport != null)
            {
                _transport.Dispose();
                _transport = null;
            }
        }
    }
}
