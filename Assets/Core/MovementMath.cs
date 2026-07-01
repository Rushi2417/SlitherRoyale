using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WormCore
{
    public static class MovementMath
    {
        public const float BaseSpeed = 320f;
        public const float BoostMultiplier = 1.7f;
        public const float SegmentSpacing = 8f;
        public const float TurnSpeed = 4.5f;
        public const float MoveDeadZoneSq = 0.01f; // min movement to record a trail point
        public static float TurnRadiusMultiplier = 1f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateSpeed(float mass, bool isBoosting)
        {
            // Speed penalty caps at 40% reduction
            float massFraction = mass / 5000f;
            if (massFraction > 0.4f) massFraction = 0.4f;
            float speedPenalty = 1f - massFraction;
            float speed = BaseSpeed * speedPenalty;
            return isBoosting ? speed * BoostMultiplier : speed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateTurnRadius(float mass)
        {
            return (1f + mass * 0.0008f) * TurnRadiusMultiplier;
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
            // Normalize angle to [-PI, PI]
            while (angleDiff > 3.14159274f) angleDiff -= 6.28318548f;
            while (angleDiff < -3.14159274f) angleDiff += 6.28318548f;

            float maxTurn = TurnSpeed * deltaTime / turnRadius;
            float clamped = angleDiff < -maxTurn ? -maxTurn : (angleDiff > maxTurn ? maxTurn : angleDiff);
            state.Heading += clamped;
            while (state.Heading > 3.14159274f) state.Heading -= 6.28318548f;
            while (state.Heading < -3.14159274f) state.Heading += 6.28318548f;

            float speed = CalculateSpeed(state.Mass, boostHeld);
            float dx = Cos(state.Heading) * speed * deltaTime;
            float dy = Sin(state.Heading) * speed * deltaTime;
            state.X += dx;
            state.Y += dy;

            UpdateSegments(ref state);
        }

        /// <summary>
        /// Follow-the-leader segment chain: O(n). Each segment chases the one
        /// ahead of it, staying SegmentSpacing apart. No List.Insert(0,...).
        /// </summary>
        private static void UpdateSegments(ref WormState state)
        {
            if (state.Mass <= 0f) return;

            int targetCount = (int)(GrowthMath.MassToLength(state.Mass) / SegmentSpacing);
            if (targetCount < 3) targetCount = 3;
            state.TargetSegmentCount = targetCount;

            if (state.Segments == null)
                state.Segments = new List<WormState.Segment>(targetCount + 8);

            // Grow segment list if needed (add tail segments)
            while (state.Segments.Count < targetCount)
            {
                if (state.Segments.Count == 0)
                    state.Segments.Add(new WormState.Segment { X = state.X, Y = state.Y });
                else
                {
                    var last = state.Segments[state.Segments.Count - 1];
                    state.Segments.Add(new WormState.Segment { X = last.X, Y = last.Y });
                }
            }

            // Trim excess segments
            while (state.Segments.Count > targetCount + 4)
                state.Segments.RemoveAt(state.Segments.Count - 1);

            // Follow-the-leader: segment[0] chases head, segment[i] chases segment[i-1]
            float leaderX = state.X;
            float leaderY = state.Y;

            for (int i = 0; i < state.Segments.Count; i++)
            {
                float sdx = leaderX - state.Segments[i].X;
                float sdy = leaderY - state.Segments[i].Y;
                float distSq = sdx * sdx + sdy * sdy;

                if (distSq > SegmentSpacing * SegmentSpacing)
                {
                    float dist = Sqrt(distSq);
                    float ratio = (dist - SegmentSpacing) / dist;
                    float newX = state.Segments[i].X + sdx * ratio;
                    float newY = state.Segments[i].Y + sdy * ratio;
                    state.Segments[i] = new WormState.Segment { X = newX, Y = newY };
                }

                leaderX = state.Segments[i].X;
                leaderY = state.Segments[i].Y;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Cos(float v) { return (float)System.Math.Cos(v); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Sin(float v) { return (float)System.Math.Sin(v); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Sqrt(float v) { return (float)System.Math.Sqrt(v); }
    }
}
