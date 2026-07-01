using System.Collections.Generic;
using NUnit.Framework;
using WormCore;

public class GrowthMathTests
{
    [Test]
    public void PelletMassGain_ReturnsPelletValue()
    {
        float gain = GrowthMath.PelletMassGain(2.5f);
        Assert.AreEqual(2.5f, gain, 0.001f);
    }

    [Test]
    public void BoostMassDrainRate_IncreasesWithMass()
    {
        float small = GrowthMath.BoostMassDrainRate(10f);
        float large = GrowthMath.BoostMassDrainRate(1000f);
        Assert.Greater(large, small);
    }

    [Test]
    public void MassToLength_Proportional()
    {
        float len = GrowthMath.MassToLength(50f);
        Assert.AreEqual(50f * GrowthMath.MassPerSegment, len, 0.001f);
    }

    [Test]
    public void ApplyBoostDrain_ReducesMass()
    {
        var state = new WormState
        {
            Mass = 100f, IsBoosting = true,
            Segments = new List<WormState.Segment>()
        };
        float initial = state.Mass;
        GrowthMath.ApplyBoostDrain(ref state, 1f);
        Assert.Less(state.Mass, initial);
    }

    [Test]
    public void ApplyBoostDrain_NoDrainWhenNotBoosting()
    {
        var state = new WormState
        {
            Mass = 100f, IsBoosting = false,
            Segments = new List<WormState.Segment>()
        };
        float initial = state.Mass;
        GrowthMath.ApplyBoostDrain(ref state, 1f);
        Assert.AreEqual(initial, state.Mass, 0.001f);
    }

    [Test]
    public void ApplyBoostDrain_MinimumMass()
    {
        var state = new WormState
        {
            Mass = 3f, IsBoosting = true,
            Segments = new List<WormState.Segment>()
        };
        GrowthMath.ApplyBoostDrain(ref state, 10f);
        Assert.GreaterOrEqual(state.Mass, 3f);
    }

    [Test]
    public void ApplyPelletGain_IncreasesMass()
    {
        var state = new WormState
        {
            Mass = 50f, Segments = new List<WormState.Segment>()
        };
        GrowthMath.ApplyPelletGain(ref state, 5f);
        Assert.AreEqual(55f, state.Mass, 0.001f);
    }

    [Test]
    public void DeathBurstPelletValue_DividesMassEvenly()
    {
        float value = GrowthMath.DeathBurstPelletValue(100f, 20);
        Assert.AreEqual(5f, value, 0.001f);
    }

    [Test]
    public void DeathBurstPelletValue_MinPelletCount()
    {
        float value = GrowthMath.DeathBurstPelletValue(100f, 0);
        Assert.AreEqual(100f, value, 0.001f);
    }
}
