using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using SlitherRoyale.Client.Gameplay;
using UnityEngine;
using WormCore;

namespace SlitherRoyale.Client.Networking
{
    public class ClientNetworkManager : MonoBehaviour
    {
        private NetTransport _transport;
        private Thread _receiveThread;
        private volatile bool _connected;
        private int _playerId;
        private int _sessionToken;
        private ushort _inputSeqNum;
        private float _inputTimer;
        private const float InputInterval = 1f / 25f;
        private const int ServerPort = 12345;

        private ConcurrentQueue<ServerSnapshotMsg> _snapshotQueue = new ConcurrentQueue<ServerSnapshotMsg>();
        private ConcurrentQueue<ServerEventMsg> _eventQueue = new ConcurrentQueue<ServerEventMsg>();
        private MatchResultMsg? _matchResult;
        private ServerSnapshotMsg _latestSnapshot;
        private bool _hasSnapshot;

        public int PlayerId => _playerId;
        public bool IsConnected => _connected;
        public bool HasMatchResult => _matchResult.HasValue;
        public MatchResultMsg MatchResult => _matchResult.Value;

        public delegate void OnEventDelegate(ServerEventMsg evt);
        public event OnEventDelegate OnServerEvent;

        public void ConnectTo(string host, int port)
        {
            _transport = new NetTransport();
            _transport.StartClient(host, port);
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();
            SendConnectionRequest();
        }

        private void Start()
        {
            ConnectTo("127.0.0.1", ServerPort);
        }

        private void SendConnectionRequest()
        {
            var msg = new ConnectionRequestMsg { PlayerName = "Player" };
            _transport.Send(msg.Serialize(), msg.Serialize().Length);
        }

        private void ReceiveLoop()
        {
            while (true)
            {
                if (_transport.TryReceive(out var packet))
                {
                    if (packet.Length == 0) continue;
                    MessageType type = (MessageType)packet.Data[0];
                    switch (type)
                    {
                        case MessageType.ConnectionAccepted:
                            var accepted = ConnectionAcceptedMsg.Deserialize(packet.Data);
                            _playerId = accepted.PlayerId;
                            _sessionToken = accepted.SessionToken;
                            _connected = true;
                            Debug.Log($"[ClientNet] Connected as player {_playerId} with token {_sessionToken}");
                            break;
                        case MessageType.ServerSnapshot:
                            var snap = ServerSnapshotMsg.Deserialize(packet.Data);
                            _snapshotQueue.Enqueue(snap);
                            break;
                        case MessageType.ServerEvent:
                            var evt = ServerEventMsg.Deserialize(packet.Data);
                            _eventQueue.Enqueue(evt);
                            SendAck(evt.SeqNum);
                            break;
                        case MessageType.MatchResult:
                            _matchResult = MatchResultMsg.Deserialize(packet.Data);
                            break;
                    }
                }
                Thread.Sleep(1);
            }
        }

        private void SendAck(ushort seqNum)
        {
            var buf = new byte[3];
            buf[0] = (byte)MessageType.ServerEventAck;
            BitConverter.GetBytes(seqNum).CopyTo(buf, 1);
            _transport.Send(buf, buf.Length);
        }

        private float _reconnectTimer;
        private const float ReconnectInterval = 2f;
        private const float ReconnectTimeout = 15f;
        private float _reconnectElapsed;

        private void Update()
        {
            if (!_connected)
            {
                _reconnectElapsed += Time.deltaTime;
                if (_reconnectElapsed < ReconnectTimeout)
                {
                    _reconnectTimer -= Time.deltaTime;
                    if (_reconnectTimer <= 0f)
                    {
                        _reconnectTimer = ReconnectInterval;
                        Debug.Log("[ClientNet] Attempting reconnect...");
                        SendConnectionRequest();
                    }
                }
                return;
            }

            _inputTimer -= Time.deltaTime;
            if (_inputTimer <= 0f && _connected)
            {
                _inputTimer += InputInterval;
                Vector2 dir = InputHandler.GetSteerDirection();
                float heading = Mathf.Atan2(dir.y, dir.x);
                var inputMsg = new ClientInputMsg
                {
                    SeqNum = _inputSeqNum++,
                    Heading = heading,
                    BoostHeld = InputHandler.IsBoosting(),
                    SessionToken = _sessionToken,
                };
                byte[] inputBytes = inputMsg.Serialize();
                _transport.Send(inputBytes, inputBytes.Length);
            }

            while (_snapshotQueue.TryDequeue(out var snap))
            {
                _latestSnapshot = snap;
                _hasSnapshot = true;
            }

            while (_eventQueue.TryDequeue(out var evt))
            {
                OnServerEvent?.Invoke(evt);
            }
        }

        public ServerSnapshotMsg GetLatestSnapshot()
        {
            return _latestSnapshot;
        }

        public bool TryGetSnapshot(out ServerSnapshotMsg snap)
        {
            snap = _latestSnapshot;
            return _hasSnapshot;
        }

        private void OnDestroy()
        {
            _connected = false;
            if (_transport != null)
            {
                _transport.Dispose();
                _transport = null;
            }
        }
    }
}
