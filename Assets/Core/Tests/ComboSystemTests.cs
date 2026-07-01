using NUnit.Framework;
using WormCore;

public class ComboSystemTests
{
    [Test]
    public void RegisterKill_FirstKill_NoCallout()
    {
        var combo = new ComboSystem();
        string triggeredCallout = null;
        combo.OnComboEvent += (id, msg, streak) => triggeredCallout = msg;

        combo.RegisterKill(1, 0f);

        Assert.IsNull(triggeredCallout);
        Assert.AreEqual(1, combo.GetCurrentStreak(1));
    }

    [Test]
    public void RegisterKill_TwoKills_TriggersDoubleKill()
    {
        var combo = new ComboSystem();
        string triggeredCallout = null;
        int triggeredStreak = 0;
        combo.OnComboEvent += (id, msg, streak) => { triggeredCallout = msg; triggeredStreak = streak; };

        combo.RegisterKill(1, 0f);
        combo.RegisterKill(1, 1f);

        Assert.AreEqual("Double Kill", triggeredCallout);
        Assert.AreEqual(2, triggeredStreak);
    }

    [Test]
    public void RegisterKill_FiveKills_TriggersRampage()
    {
        var combo = new ComboSystem();
        string triggeredCallout = null;
        combo.OnComboEvent += (id, msg, streak) => triggeredCallout = msg;

        combo.RegisterKill(1, 0f);
        combo.RegisterKill(1, 1f);
        combo.RegisterKill(1, 2f);
        combo.RegisterKill(1, 3f);
        combo.RegisterKill(1, 4f);

        Assert.AreEqual("Rampage", triggeredCallout);
    }

    [Test]
    public void RegisterKill_EightKills_TriggersGodlike()
    {
        var combo = new ComboSystem();
        string triggeredCallout = null;
        combo.OnComboEvent += (id, msg, streak) => triggeredCallout = msg;

        for (int i = 0; i < 8; i++)
            combo.RegisterKill(1, i * 1f);

        Assert.AreEqual("GODLIKE", triggeredCallout);
    }

    [Test]
    public void ComboWindowExpires_ResetsStreak()
    {
        var combo = new ComboSystem { ComboWindowSeconds = 5f };
        int calloutCount = 0;
        combo.OnComboEvent += (id, msg, streak) => calloutCount++;

        combo.RegisterKill(1, 0f);
        combo.RegisterKill(1, 1f);
        Assert.AreEqual(2, combo.GetCurrentStreak(1));

        combo.Update(10f);

        combo.RegisterKill(1, 11f);
        Assert.AreEqual(1, calloutCount);
    }

    [Test]
    public void Reset_ClearsStreak()
    {
        var combo = new ComboSystem();
        combo.RegisterKill(1, 0f);
        combo.RegisterKill(1, 1f);
        combo.Reset(1);

        Assert.AreEqual(0, combo.GetCurrentStreak(1));
    }

    [Test]
    public void MultiplePlayers_TrackedIndependently()
    {
        var combo = new ComboSystem();
        combo.RegisterKill(1, 0f);
        combo.RegisterKill(1, 1f);
        combo.RegisterKill(2, 0f);

        Assert.AreEqual(2, combo.GetCurrentStreak(1));
    }

    [Test]
    public void GetCalloutText_AllStreaks()
    {
        Assert.AreEqual("Double Kill", ComboSystem.GetCalloutText(2));
        Assert.AreEqual("Triple Kill", ComboSystem.GetCalloutText(3));
        Assert.AreEqual("Multi Kill", ComboSystem.GetCalloutText(4));
        Assert.AreEqual("Rampage", ComboSystem.GetCalloutText(5));
        Assert.AreEqual("Dominating", ComboSystem.GetCalloutText(6));
        Assert.AreEqual("Unstoppable", ComboSystem.GetCalloutText(7));
        Assert.AreEqual("GODLIKE", ComboSystem.GetCalloutText(8));
        Assert.AreEqual("GODLIKE", ComboSystem.GetCalloutText(15));
    }
}
