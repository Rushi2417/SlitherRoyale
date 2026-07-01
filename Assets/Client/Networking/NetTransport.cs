using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace SlitherRoyale.Client.Networking
{
    public class NetTransport : IDisposable
    {
        private Socket _socket;
        private EndPoint _remoteEndPoint;
        private ConcurrentQueue<ReceivedPacket> _inbox = new ConcurrentQueue<ReceivedPacket>();
        private bool _running;
        private readonly object _lock = new object();

        public struct ReceivedPacket
        {
            public byte[] Data;
            public int Length;
            public IPEndPoint Source;
        }

        public void StartServer(int port)
        {
            Stop();
            lock (_lock)
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.Bind(new IPEndPoint(IPAddress.Any, port));
                _socket.Blocking = false;
                _running = true;
            }
            BeginReceive();
        }

        public void StartClient(string host, int port)
        {
            Stop();
            lock (_lock)
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.Blocking = false;
                _remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
                _running = true;
            }
            BeginReceive();
        }

        public void SetRemoteEndPoint(IPEndPoint ep)
        {
            _remoteEndPoint = ep;
        }

        public void Send(byte[] data, int length)
        {
            if (_socket == null || _remoteEndPoint == null) return;
            lock (_lock)
            {
                try { _socket.SendTo(data, length, SocketFlags.None, _remoteEndPoint); }
                catch (Exception e) { Debug.LogWarning($"[NetTransport] Send error: {e.Message}"); }
            }
        }

        public void SendTo(byte[] data, int length, IPEndPoint target)
        {
            if (_socket == null) return;
            lock (_lock)
            {
                try { _socket.SendTo(data, length, SocketFlags.None, target); }
                catch (Exception e) { Debug.LogWarning($"[NetTransport] SendTo error: {e.Message}"); }
            }
        }

        public bool TryReceive(out ReceivedPacket packet)
        {
            return _inbox.TryDequeue(out packet);
        }

        private void Stop()
        {
            _running = false;
            lock (_lock)
            {
                if (_socket != null)
                {
                    try { _socket.Close(); } catch { }
                    _socket = null;
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void BeginReceive()
        {
            if (!_running) return;
            byte[] buffer = new byte[4096];
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            lock (_lock)
            {
                if (_socket == null || !_running) return;
                try
                {
                    _socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None,
                        ref sender, ar => OnReceive(ar, buffer), null);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[NetTransport] BeginReceive error: {e.Message}");
                }
            }
        }

        private void OnReceive(IAsyncResult ar, byte[] buffer)
        {
            if (!_running) return;
            try
            {
                EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                int len;
                lock (_lock)
                {
                    if (_socket == null) return;
                    len = _socket.EndReceiveFrom(ar, ref sender);
                }
                if (len > 0)
                {
                    byte[] data = new byte[len];
                    Array.Copy(buffer, data, len);
                    _inbox.Enqueue(new ReceivedPacket { Data = data, Length = len, Source = (IPEndPoint)sender });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NetTransport] Receive error: {e.Message}");
            }
            BeginReceive();
        }
    }
}
