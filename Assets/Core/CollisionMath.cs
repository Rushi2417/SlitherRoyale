namespace WormCore
{
    public static class CollisionMath
    {
        public static bool HeadVsPellet(
            float headX, float headY, float headRadius,
            float pelletX, float pelletY, float pelletRadius)
        {
            return false;
        }

        public static CollisionResult HeadVsBody(
            float headX, float headY, float headRadius,
            float bodySegmentX, float bodySegmentY, float bodyRadius)
        {
            return CollisionResult.None;
        }

        public static HeadOnCollisionResult HeadVsHead(
            float headAX, float headAY, float massA,
            float headBX, float headBY, float massB,
            float equalMassThreshold)
        {
            return HeadOnCollisionResult.None;
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
