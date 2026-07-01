using System.Collections.Generic;
using NUnit.Framework;
using WormCore;

public class MovementMathTests
{
    [Test]
    public void CalculateSpeed_BaseSpeed_ReturnsCorrect()
    {
        float speed = MovementMath.CalculateSpeed(10f, false);
        Assert.AreEqual(MovementMath.BaseSpeed * (1f - 10f / 5000f), speed, 0.001f);
    }

    [Test]
    public void CalculateSpeed_BoostMultiplier_Applied()
    {
        float normal = MovementMath.CalculateSpeed(10f, false);
        float boosted = MovementMath.CalculateSpeed(10f, true);
        Assert.AreEqual(normal * MovementMath.BoostMultiplier, boosted, 0.001f);
    }

    [Test]
    public void CalculateSpeed_MassPenalty_CappedAt40Percent()
    {
        float speed = MovementMath.CalculateSpeed(5000f, false);
        float expected = MovementMath.BaseSpeed * (1f - 0.4f);
        Assert.AreEqual(expected, speed, 0.001f);
    }

    [Test]
    public void CalculateTurnRadius_IncreasesWithMass()
    {
        float small = MovementMath.CalculateTurnRadius(10f);
        float large = MovementMath.CalculateTurnRadius(1000f);
        Assert.Greater(large, small);
    }

    [Test]
    public void TurnRadiusMultiplier_AffectsResult()
    {
        MovementMath.TurnRadiusMultiplier = 2f;
        float radius = MovementMath.CalculateTurnRadius(10f);
        Assert.AreEqual((1f + 10f * 0.0008f) * 2f, radius, 0.001f);
        MovementMath.TurnRadiusMultiplier = 1f;
    }

    [Test]
    public void IntegrateMovement_AdvancesPosition()
    {
        var state = new WormState
        {
            X = 0f, Y = 0f, Heading = 0f, Mass = 10f,
            Segments = new List<WormState.Segment>()
        };
        MovementMath.IntegrateMovement(ref state, 0f, false, 0.1f);
        Assert.Greater(state.X, 0f);
        Assert.AreEqual(0f, state.Y, 0.001f);
    }

    [Test]
    public void IntegrateMovement_TurnsCorrectly()
    {
        var state = new WormState
        {
            X = 0f, Y = 0f, Heading = 0f, Mass = 10f,
            Segments = new List<WormState.Segment>()
        };
        MovementMath.IntegrateMovement(ref state, 1.57f, false, 0.1f);
        Assert.Greater(state.Heading, 0f);
    }

    [Test]
    public void IntegrateMovement_ZeroMass_DoesNotCrash()
    {
        var state = new WormState
        {
            X = 0f, Y = 0f, Heading = 0f, Mass = 0f,
            Segments = new List<WormState.Segment>()
        };
        Assert.DoesNotThrow(() =>
            MovementMath.IntegrateMovement(ref state, 0f, false, 0.1f));
    }

    [Test]
    public void SegmentChain_ExtendsWithMovement()
    {
        var state = new WormState
        {
            X = 0f, Y = 0f, Heading = 0f, Mass = 30f,
            Segments = new List<WormState.Segment>()
        };
        for (int i = 0; i < 10; i++)
            MovementMath.IntegrateMovement(ref state, 0f, false, 0.05f);

        Assert.Greater(state.Segments.Count, 3);
    }
}
