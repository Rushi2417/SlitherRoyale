using System.Runtime.CompilerServices;

namespace WormCore
{
    public static class CollisionMath
    {
        public const float EqualMassThreshold = 0.15f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HeadVsPellet(
            float headX, float headY, float headRadius,
            float pelletX, float pelletY, float pelletRadius)
        {
            float dx = headX - pelletX;
            float dy = headY - pelletY;
            float distSq = dx * dx + dy * dy;
            float radiusSum = headRadius + pelletRadius;
            return distSq <= radiusSum * radiusSum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CollisionResult HeadVsBody(
            float headX, float headY, float headRadius,
            float bodySegmentX, float bodySegmentY, float bodyRadius)
        {
            float dx = headX - bodySegmentX;
            float dy = headY - bodySegmentY;
            float distSq = dx * dx + dy * dy;
            float radiusSum = headRadius + bodyRadius;
            return distSq <= radiusSum * radiusSum ? CollisionResult.HeadHitBody : CollisionResult.None;
        }

        public static HeadOnCollisionResult HeadVsHead(
            float headAX, float headAY, float massA,
            float headBX, float headBY, float massB,
            float equalMassThreshold = EqualMassThreshold)
        {
            float dx = headAX - headBX;
            float dy = headAY - headBY;
            float distSq = dx * dx + dy * dy;
            float radiusSum = 12f + (massA + massB) * 0.02f;
            if (distSq > radiusSum * radiusSum)
                return HeadOnCollisionResult.None;

            float ratio = massA / (massB > 0.001f ? massB : 0.001f);
            if (ratio >= 1f + equalMassThreshold)
                return HeadOnCollisionResult.AWins;
            if (ratio <= 1f / (1f + equalMassThreshold))
                return HeadOnCollisionResult.BWins;
            return HeadOnCollisionResult.BothDie;
        }

        public enum CollisionResult
        {
            None,
            HeadHitBody
        }

        public enum HeadOnCollisionResult
        {
            None,
            AWins,
            BWins,
            BothDie
        }
    }
}
