using System;

namespace WormCore
{
    public enum MatchMode
    {
        FreeForAll,
        Duos,
        Ranked1v1,
        BattleRoyale
    }

    [Serializable]
    public struct ModeConfig
    {
        public MatchMode Mode;
        public int MinPlayers;
        public int MaxPlayers;
        public int TeamSize;
        public float ArenaRadiusMultiplier;
        public float RoundDurationSeconds;
        public bool HasShrinkZone;
        public float ShrinkInterval;
        public float ShrinkDamagePerSecond;
        public bool IsRanked;
        public int RoundsToWin;

        public static ModeConfig GetDefault(MatchMode mode)
        {
            return mode switch
            {
                MatchMode.FreeForAll => new ModeConfig
                {
                    Mode = mode, MinPlayers = 2, MaxPlayers = 20, TeamSize = 1,
                    ArenaRadiusMultiplier = 1f, RoundDurationSeconds = 0f,
                    HasShrinkZone = false, IsRanked = false,
                },
                MatchMode.Duos => new ModeConfig
                {
                    Mode = mode, MinPlayers = 2, MaxPlayers = 20, TeamSize = 2,
                    ArenaRadiusMultiplier = 1f, RoundDurationSeconds = 300f,
                    HasShrinkZone = false, IsRanked = false,
                },
                MatchMode.Ranked1v1 => new ModeConfig
                {
                    Mode = mode, MinPlayers = 2, MaxPlayers = 2, TeamSize = 1,
                    ArenaRadiusMultiplier = 0.4f, RoundDurationSeconds = 90f,
                    HasShrinkZone = false, IsRanked = true, RoundsToWin = 2,
                },
                MatchMode.BattleRoyale => new ModeConfig
                {
                    Mode = mode, MinPlayers = 2, MaxPlayers = 20, TeamSize = 1,
                    ArenaRadiusMultiplier = 1.2f, RoundDurationSeconds = 600f,
                    HasShrinkZone = true, ShrinkInterval = 45f, ShrinkDamagePerSecond = 30f,
                    IsRanked = false,
                },
                _ => default
            };
        }
    }
}
