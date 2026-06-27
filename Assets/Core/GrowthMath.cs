using System.Runtime.CompilerServices;

namespace WormCore
{
    public static class GrowthMath
    {
        public const float PelletBaseValue = 0.5f;
        public const float BoostDrainBase = 0.8f;
        public const float MassPerSegment = 3f;

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

        public static void ApplyBoostDrain(ref WormState state, float deltaTime)
        {
            if (!state.IsBoosting) return;
            float drain = BoostMassDrainRate(state.Mass) * deltaTime;
            state.Mass = state.Mass - drain > 3f ? state.Mass - drain : 3f;
        }

        public static void ApplyPelletGain(ref WormState state, float pelletValue)
        {
            state.Mass += PelletMassGain(pelletValue);
        }

        public static float DeathBurstPelletValue(float wormMass, int pelletCount)
        {
            return wormMass / (pelletCount > 1 ? pelletCount : 1);
        }
    }
}
