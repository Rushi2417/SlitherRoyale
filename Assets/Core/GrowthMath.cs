using System.Runtime.CompilerServices;

namespace WormCore
{
    public static class GrowthMath
    {
        public const float PelletBaseValue = 1.0f;     // increased from 0.5 for better feel
        public const float BoostDrainBase = 0.8f;
        public const float MassPerSegment = 3f;
        public const float MinMass = 5f;
        public const float MaxMass = 8000f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float PelletMassGain(float pelletValue)
        {
            return pelletValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float BoostMassDrainRate(float currentMass)
        {
            return BoostDrainBase * (1f + currentMass * 0.0002f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MassToLength(float mass)
        {
            return mass * MassPerSegment;
        }

        /// <summary>Expected segment count for a worm of this mass. Minimum 3.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MassToSegmentCount(float mass)
        {
            int count = (int)(MassToLength(mass) / 8f); // 8f = MovementMath.SegmentSpacing
            return count < 3 ? 3 : count;
        }


        public static void ApplyBoostDrain(ref WormState state, float deltaTime)
        {
            if (!state.IsBoosting) return;
            float drain = BoostMassDrainRate(state.Mass) * deltaTime;
            float newMass = state.Mass - drain;
            state.Mass = newMass < MinMass ? MinMass : newMass;
        }

        public static void ApplyPelletGain(ref WormState state, float pelletValue)
        {
            state.Mass += PelletMassGain(pelletValue);
            if (state.Mass > MaxMass) state.Mass = MaxMass;
        }

        public static float DeathBurstPelletValue(float wormMass, int pelletCount)
        {
            return wormMass / (pelletCount > 1 ? pelletCount : 1);
        }
    }
}
