using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WormCore
{
    public static class DeathBurstMath
    {
        public const float BurstPelletRadius = 4f;
        public const int MinBurstPellets = 8;
        public const int MaxBurstPellets = 60;
        public const float PelletsPerMassUnit = 0.5f;

        public static List<BurstPellet> GenerateBurstPellets(WormState deadWorm)
        {
            int count = (int)(deadWorm.Mass * PelletsPerMassUnit);
            if (count < MinBurstPellets) count = MinBurstPellets;
            if (count > MaxBurstPellets) count = MaxBurstPellets;

            float valuePerPellet = GrowthMath.DeathBurstPelletValue(deadWorm.Mass, count);
            var pellets = new List<BurstPellet>(count);

            if (deadWorm.Segments == null || deadWorm.Segments.Count <= 1)
            {
                for (int i = 0; i < count; i++)
                {
                    float angle = (float)System.Math.PI * 2f * i / count;
                    float radius = 10f + deadWorm.Mass * 0.05f;
                    pellets.Add(new BurstPellet
                    {
                        X = deadWorm.X + (float)System.Math.Cos(angle) * radius,
                        Y = deadWorm.Y + (float)System.Math.Sin(angle) * radius,
                        Value = valuePerPellet
                    });
                }
                return pellets;
            }

            int segCount = deadWorm.Segments.Count;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float ft = t * (segCount - 1);
                int idx = (int)ft;
                float segT = ft - idx;
                if (idx >= segCount - 1) { idx = segCount - 2; segT = 1f; }

                float px = deadWorm.Segments[idx].X + (deadWorm.Segments[idx + 1].X - deadWorm.Segments[idx].X) * segT;
                float py = deadWorm.Segments[idx].Y + (deadWorm.Segments[idx + 1].Y - deadWorm.Segments[idx].Y) * segT;

                float jitterAngle = (float)System.Math.PI * 2f * i / count;
                float jitterRadius = 4f + (float)System.Math.Sin(i * 1.7f) * 3f;

                pellets.Add(new BurstPellet
                {
                    X = px + (float)System.Math.Cos(jitterAngle) * jitterRadius,
                    Y = py + (float)System.Math.Sin(jitterAngle) * jitterRadius,
                    Value = valuePerPellet
                });
            }

            return pellets;
        }

    }

    public struct BurstPellet
    {
        public float X;
        public float Y;
        public float Value;
    }
}
