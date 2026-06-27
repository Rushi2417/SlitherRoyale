using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WormCore
{
    public struct WormState
    {
        public int Id;
        public float X;
        public float Y;
        public float Heading;
        public float Mass;
        public bool IsBoosting;
        public int TeamId;
        public int SkinId;
        public bool IsDead;

        public List<Segment> Segments;

        public int TargetSegmentCount;

        public struct Segment
        {
            public float X;
            public float Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float HeadRadius()
        {
            return 6f + Mass * 0.02f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float BodyRadius()
        {
            return 4f + Mass * 0.015f;
        }
    }
}
