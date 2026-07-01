using System;
using System.Collections.Generic;
using System.Text;
using WormCore;

namespace SlitherRoyale.Client.Networking
{
    public enum MessageType : byte
    {
        ConnectionRequest = 0x01,
        ConnectionAccepted = 0x02,
        ClientInput = 0x03,
        ServerSnapshot = 0x04,
        ServerEvent = 0x05,
        ServerEventAck = 0x06,
        MatchResult = 0x07,
        Disconnect = 0x08,
    }

    public enum ServerEventType : byte
    {
        WormSpawned = 0x01,
        WormDied = 0x02,
        PelletEaten = 0x03,
        ComboTriggered = 0x04,
        MatchStarted = 0x05,
        MatchEnded = 0x06,
        ShrinkZone = 0x07,
    }

    public struct ClientInputMsg
    {
        public ushort SeqNum;
        public float Heading;
        public bool BoostHeld;
        public int SessionToken;

        public byte[] Serialize()
        {
            var buf = new byte[12];
            buf[0] = (byte)MessageType.ClientInput;
            BitConverter.GetBytes(SeqNum).CopyTo(buf, 1);
            BitConverter.GetBytes(Heading).CopyTo(buf, 3);
            BitConverter.GetBytes(SessionToken).CopyTo(buf, 7);
            buf[11] = (byte)(BoostHeld ? 1 : 0);
            return buf;
        }

        public static ClientInputMsg Deserialize(byte[] data, int offset)
        {
            return new ClientInputMsg
            {
                SeqNum = BitConverter.ToUInt16(data, offset + 1),
                Heading = BitConverter.ToSingle(data, offset + 3),
                SessionToken = BitConverter.ToInt32(data, offset + 7),
                BoostHeld = data[offset + 11] != 0,
            };
        }
    }

    public struct EntitySnapshot
    {
        public int Id;
        public float X, Y, Heading, Mass;
        public bool IsDead;
        public int SegmentCount;
        public int TeamId;

        public void Serialize(List<byte> buf)
        {
            buf.AddRange(BitConverter.GetBytes(Id));
            buf.AddRange(BitConverter.GetBytes(X));
            buf.AddRange(BitConverter.GetBytes(Y));
            buf.AddRange(BitConverter.GetBytes(Heading));
            buf.AddRange(BitConverter.GetBytes(Mass));
            buf.Add((byte)(IsDead ? 1 : 0));
            buf.AddRange(BitConverter.GetBytes(SegmentCount));
            buf.AddRange(BitConverter.GetBytes(TeamId));
        }

        public static EntitySnapshot Deserialize(byte[] data, ref int offset)
        {
            EntitySnapshot s;
            s.Id = BitConverter.ToInt32(data, offset); offset += 4;
            s.X = BitConverter.ToSingle(data, offset); offset += 4;
            s.Y = BitConverter.ToSingle(data, offset); offset += 4;
            s.Heading = BitConverter.ToSingle(data, offset); offset += 4;
            s.Mass = BitConverter.ToSingle(data, offset); offset += 4;
            s.IsDead = data[offset++] != 0;
            s.SegmentCount = BitConverter.ToInt32(data, offset); offset += 4;
            s.TeamId = BitConverter.ToInt32(data, offset); offset += 4;
            return s;
        }
    }

    public struct ServerSnapshotMsg
    {
        public uint Tick;
        public int PlayerId;
        public EntitySnapshot LocalWorm;
        public List<EntitySnapshot> Entities;
        public List<LeaderboardEntry> Leaderboard;
        public float ShrinkRadius;

        public byte[] Serialize()
        {
            var buf = new List<byte>(1024);
            buf.Add((byte)MessageType.ServerSnapshot);
            buf.AddRange(BitConverter.GetBytes(Tick));
            buf.AddRange(BitConverter.GetBytes(PlayerId));
            LocalWorm.Serialize(buf);
            buf.Add((byte)Math.Min(Entities.Count, 255));
            int count = Math.Min(Entities.Count, 255);
            for (int i = 0; i < count; i++)
                Entities[i].Serialize(buf);
            buf.Add((byte)Math.Min(Leaderboard.Count, 10));
            int lbCount = Math.Min(Leaderboard.Count, 10);
            for (int i = 0; i < lbCount; i++)
            {
                buf.AddRange(BitConverter.GetBytes(Leaderboard[i].PlayerId));
                buf.AddRange(BitConverter.GetBytes(Leaderboard[i].Score));
            }
            buf.AddRange(BitConverter.GetBytes(ShrinkRadius));
            return buf.ToArray();
        }

        public static ServerSnapshotMsg Deserialize(byte[] data)
        {
            int offset = 1;
            ServerSnapshotMsg msg;
            msg.Tick = BitConverter.ToUInt32(data, offset); offset += 4;
            msg.PlayerId = BitConverter.ToInt32(data, offset); offset += 4;
            msg.LocalWorm = EntitySnapshot.Deserialize(data, ref offset);
            msg.Entities = new List<EntitySnapshot>();
            msg.Leaderboard = new List<LeaderboardEntry>();
            int entCount = data[offset++];
            for (int i = 0; i < entCount; i++)
                msg.Entities.Add(EntitySnapshot.Deserialize(data, ref offset));
            int lbCount = data[offset++];
            for (int i = 0; i < lbCount; i++)
            {
                var entry = new LeaderboardEntry();
                entry.PlayerId = BitConverter.ToInt32(data, offset); offset += 4;
                entry.Score = BitConverter.ToSingle(data, offset); offset += 4;
                msg.Leaderboard.Add(entry);
            }
            msg.ShrinkRadius = BitConverter.ToSingle(data, offset); offset += 4;
            return msg;
        }
    }

    public struct ServerEventMsg
    {
        public ushort SeqNum;
        public ServerEventType EventType;
        public byte[] Payload;

        public byte[] Serialize()
        {
            int len = 4 + (Payload != null ? Payload.Length : 0);
            var buf = new byte[len];
            buf[0] = (byte)MessageType.ServerEvent;
            BitConverter.GetBytes(SeqNum).CopyTo(buf, 1);
            buf[3] = (byte)EventType;
            if (Payload != null)
                Payload.CopyTo(buf, 4);
            return buf;
        }

        public static ServerEventMsg Deserialize(byte[] data)
        {
            int offset = 1;
            ServerEventMsg msg;
            msg.SeqNum = BitConverter.ToUInt16(data, offset); offset += 2;
            msg.EventType = (ServerEventType)data[offset++];
            msg.Payload = new byte[data.Length - offset];
            Array.Copy(data, offset, msg.Payload, 0, msg.Payload.Length);
            return msg;
        }
    }

    public struct MatchResultMsg
    {
        public int Kills;
        public float Score;
        public int Rank;
        public int CoinsEarned;
        public int BPXPEarned;

        public byte[] Serialize()
        {
            var buf = new byte[21];
            buf[0] = (byte)MessageType.MatchResult;
            BitConverter.GetBytes(Kills).CopyTo(buf, 1);
            BitConverter.GetBytes(Score).CopyTo(buf, 5);
            BitConverter.GetBytes(Rank).CopyTo(buf, 9);
            BitConverter.GetBytes(CoinsEarned).CopyTo(buf, 13);
            BitConverter.GetBytes(BPXPEarned).CopyTo(buf, 17);
            return buf;
        }

        public static MatchResultMsg Deserialize(byte[] data)
        {
            return new MatchResultMsg
            {
                Kills = BitConverter.ToInt32(data, 1),
                Score = BitConverter.ToSingle(data, 5),
                Rank = BitConverter.ToInt32(data, 9),
                CoinsEarned = BitConverter.ToInt32(data, 13),
                BPXPEarned = BitConverter.ToInt32(data, 17),
            };
        }
    }

    public struct ConnectionRequestMsg
    {
        public string PlayerName;

        public byte[] Serialize()
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(PlayerName ?? "Player");
            var buf = new byte[2 + nameBytes.Length];
            buf[0] = (byte)MessageType.ConnectionRequest;
            buf[1] = (byte)Math.Min(nameBytes.Length, 32);
            Array.Copy(nameBytes, 0, buf, 2, Math.Min(nameBytes.Length, 32));
            return buf;
        }

        public static ConnectionRequestMsg Deserialize(byte[] data)
        {
            int len = data[1];
            string name = Encoding.UTF8.GetString(data, 2, len);
            return new ConnectionRequestMsg { PlayerName = name };
        }
    }

    public struct ConnectionAcceptedMsg
    {
        public int PlayerId;
        public int SessionToken;

        public byte[] Serialize()
        {
            var buf = new byte[9];
            buf[0] = (byte)MessageType.ConnectionAccepted;
            BitConverter.GetBytes(PlayerId).CopyTo(buf, 1);
            BitConverter.GetBytes(SessionToken).CopyTo(buf, 5);
            return buf;
        }

        public static ConnectionAcceptedMsg Deserialize(byte[] data)
        {
            return new ConnectionAcceptedMsg
            {
                PlayerId = BitConverter.ToInt32(data, 1),
                SessionToken = BitConverter.ToInt32(data, 5),
            };
        }
    }

    public struct LeaderboardEntry
    {
        public int PlayerId;
        public float Score;
    }
}
