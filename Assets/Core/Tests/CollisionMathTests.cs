using NUnit.Framework;
using WormCore;

public class CollisionMathTests
{
    [Test]
    public void HeadVsPellet_Overlap_ReturnsTrue()
    {
        bool hit = CollisionMath.HeadVsPellet(0f, 0f, 5f, 3f, 0f, 3f);
        Assert.IsTrue(hit);
    }

    [Test]
    public void HeadVsPellet_NoOverlap_ReturnsFalse()
    {
        bool hit = CollisionMath.HeadVsPellet(0f, 0f, 5f, 20f, 0f, 3f);
        Assert.IsFalse(hit);
    }

    [Test]
    public void HeadVsPellet_TouchingEdges_ReturnsTrue()
    {
        bool hit = CollisionMath.HeadVsPellet(0f, 0f, 5f, 8f, 0f, 3f);
        Assert.IsTrue(hit);
    }

    [Test]
    public void HeadVsBody_Hit_ReturnsTrue()
    {
        // HeadVsBody now returns bool (simplified from enum)
        bool hit = CollisionMath.HeadVsBody(0f, 0f, 5f, 6f, 0f, 4f);
        Assert.IsTrue(hit);
    }

    [Test]
    public void HeadVsBody_NoHit_ReturnsFalse()
    {
        bool hit = CollisionMath.HeadVsBody(0f, 0f, 5f, 50f, 0f, 4f);
        Assert.IsFalse(hit);
    }

    [Test]
    public void HeadVsHead_SmallerWormDies()
    {
        var result = CollisionMath.HeadVsHead(0f, 0f, 50f, 5f, 0f, 200f);
        Assert.AreEqual(CollisionMath.HeadOnCollisionResult.BWins, result);
    }

    [Test]
    public void HeadVsHead_LargerWormWins()
    {
        var result = CollisionMath.HeadVsHead(0f, 0f, 200f, 5f, 0f, 50f);
        Assert.AreEqual(CollisionMath.HeadOnCollisionResult.AWins, result);
    }

    [Test]
    public void HeadVsHead_EqualMass_BothDie()
    {
        var result = CollisionMath.HeadVsHead(0f, 0f, 100f, 5f, 0f, 100f);
        Assert.AreEqual(CollisionMath.HeadOnCollisionResult.BothDie, result);
    }

    [Test]
    public void HeadVsHead_NearEqual_BothDie()
    {
        var result = CollisionMath.HeadVsHead(0f, 0f, 100f, 5f, 0f, 108f);
        Assert.AreEqual(CollisionMath.HeadOnCollisionResult.BothDie, result);
    }

    [Test]
    public void HeadVsHead_NoContact_ReturnsNone()
    {
        var result = CollisionMath.HeadVsHead(0f, 0f, 10f, 100f, 0f, 10f);
        Assert.AreEqual(CollisionMath.HeadOnCollisionResult.None, result);
    }

    [Test]
    public void HeadVsHead_ZeroMassHandling()
    {
        var result = CollisionMath.HeadVsHead(0f, 0f, 0f, 0f, 0f, 10f);
        Assert.AreEqual(CollisionMath.HeadOnCollisionResult.AWins, result);
    }
}
