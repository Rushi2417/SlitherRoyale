using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WormCore
{
    public static class MovementMath
    {
        public const float BaseSpeed = 320f;
        public const float BoostMultiplier = 1.7f;
        public const float SegmentSpacing = 6f;
        public const float TurnSpeed = 4.5f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateSpeed(float mass, bool isBoosting)
        {
            float speedPenalty = 1f - (mass / 5000f > 0.4f ? 0.4f : mass / 5000f);
            float speed = BaseSpeed * speedPenalty;
            return isBoosting ? speed * BoostMultiplier : speed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateTurnRadius(float mass)
        {
            return 1f + mass * 0.0008f;
        }

        public static void IntegrateMovement(
            ref WormState state,
            float desiredHeading,
            bool boostHeld,
            float deltaTime)
        {
            state.IsBoosting = boostHeld;

            float turnRadius = CalculateTurnRadius(state.Mass);
            float angleDiff = desiredHeading - state.Heading;
            while (angleDiff > 3.14159274f) angleDiff -= 6.28318548f;
            while (angleDiff < -3.14159274f) angleDiff += 6.28318548f;

            float maxTurn = TurnSpeed * deltaTime / turnRadius;
            float clamped = angleDiff < -maxTurn ? -maxTurn : (angleDiff > maxTurn ? maxTurn : angleDiff);
            state.Heading += clamped;
            while (state.Heading > 3.14159274f) state.Heading -= 6.28318548f;
            while (state.Heading < -3.14159274f) state.Heading += 6.28318548f;

            float speed = CalculateSpeed(state.Mass, boostHeld);
            float moveX = Cos(state.Heading) * speed * deltaTime;
            float moveY = Sin(state.Heading) * speed * deltaTime;
            state.X += moveX;
            state.Y += moveY;

            UpdateSegments(ref state, moveX, moveY);
        }

        private static void UpdateSegments(ref WormState state, float moveX, float moveY)
        {
            if (state.Mass <= 0f) return;
            int targetCount = (int)(GrowthMath.MassToLength(state.Mass) / SegmentSpacing);
            if (targetCount < 3) targetCount = 3;
            state.TargetSegmentCount = targetCount;

            if (state.Segments == null)
                state.Segments = new List<WormState.Segment>(targetCount);

            if (moveX != 0f || moveY != 0f)
            {
                state.Segments.Insert(0, new WormState.Segment
                {
                    X = state.X,
                    Y = state.Y
                });
            }

            float maxDist = targetCount * SegmentSpacing;
            float accumulated = 0f;
            for (int i = state.Segments.Count - 1; i >= 0; i--)
            {
                if (i == 0) break;
                float dx = state.Segments[i - 1].X - state.Segments[i].X;
                float dy = state.Segments[i - 1].Y - state.Segments[i].Y;
                float dist = Sqrt(dx * dx + dy * dy);
                accumulated += dist;
                if (accumulated > maxDist)
                    state.Segments.RemoveAt(i);
            }

            int maxSegments = targetCount + 5;
            while (state.Segments.Count > maxSegments)
                state.Segments.RemoveAt(state.Segments.Count - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Cos(float v) { return (float)System.Math.Cos(v); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Sin(float v) { return (float)System.Math.Sin(v); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Sqrt(float v) { return (float)System.Math.Sqrt(v); }
    }
}
