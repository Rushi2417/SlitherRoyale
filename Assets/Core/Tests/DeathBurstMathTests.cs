using System.Collections.Generic;
using NUnit.Framework;
using WormCore;

public class DeathBurstMathTests
{
    [Test]
    public void GenerateBurstPellets_ReturnsExpectedCount()
    {
        var worm = new WormState
        {
            Mass = 100f, X = 0f, Y = 0f,
            Segments = new List<WormState.Segment>
            {
                new WormState.Segment { X = 0f, Y = 0f },
                new WormState.Segment { X = 10f, Y = 0f },
                new WormState.Segment { X = 20f, Y = 0f },
            }
        };
        var pellets = DeathBurstMath.GenerateBurstPellets(worm);
        Assert.IsNotNull(pellets);
        Assert.Greater(pellets.Count, 0);
        Assert.LessOrEqual(pellets.Count, DeathBurstMath.MaxBurstPellets);
    }

    [Test]
    public void GenerateBurstPellets_NoSegments_UsesRadialSpread()
    {
        var worm = new WormState
        {
            Mass = 50f, X = 100f, Y = 200f,
            Segments = null
        };
        var pellets = DeathBurstMath.GenerateBurstPellets(worm);
        Assert.IsNotNull(pellets);
        Assert.Greater(pellets.Count, 0);
        Assert.Greater(pellets[0].Value, 0f);
    }

    [Test]
    public void GenerateBurstPellets_MinimumCount()
    {
        var worm = new WormState
        {
            Mass = 1f, X = 0f, Y = 0f,
            Segments = new List<WormState.Segment>
            {
                new WormState.Segment { X = 0f, Y = 0f }
            }
        };
        var pellets = DeathBurstMath.GenerateBurstPellets(worm);
        Assert.GreaterOrEqual(pellets.Count, DeathBurstMath.MinBurstPellets);
    }

    [Test]
    public void GenerateBurstPellets_MaximumCount()
    {
        var worm = new WormState
        {
            Mass = 500f, X = 0f, Y = 0f,
            Segments = new List<WormState.Segment>
            {
                new WormState.Segment { X = 0f, Y = 0f },
                new WormState.Segment { X = 10f, Y = 0f },
            }
        };
        var pellets = DeathBurstMath.GenerateBurstPellets(worm);
        Assert.LessOrEqual(pellets.Count, DeathBurstMath.MaxBurstPellets);
    }

    [Test]
    public void EachPellet_HasPositiveValue()
    {
        var worm = new WormState
        {
            Mass = 100f, X = 0f, Y = 0f,
            Segments = new List<WormState.Segment>
            {
                new WormState.Segment { X = 0f, Y = 0f },
                new WormState.Segment { X = 10f, Y = 10f },
            }
        };
        var pellets = DeathBurstMath.GenerateBurstPellets(worm);
        foreach (var p in pellets)
            Assert.Greater(p.Value, 0f);
    }
}
