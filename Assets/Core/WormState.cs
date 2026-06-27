using System.Collections.Generic;

namespace WormCore
{
    public struct WormState
    {
        public int Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Heading { get; set; }
        public float Mass { get; set; }
        public bool IsBoosting { get; set; }
        public int TeamId { get; set; }
        public int SkinId { get; set; }
        public bool IsDead { get; set; }

        public List<Segment> Segments { get; set; }

        public struct Segment
        {
            public float X { get; set; }
            public float Y { get; set; }
        }
    }
}
