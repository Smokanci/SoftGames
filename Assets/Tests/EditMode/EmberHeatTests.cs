using NUnit.Framework;
using UnityEngine;

public class EmberHeatTests
{
    private const float Frame = 1f / 60f;

    private EmberStyle _style;

    // CreateInstance runs the field initializers, so the asset's authored defaults are what these
    // tests assert against — no scene, no play mode, and no fixture asset to keep in sync.
    [SetUp]
    public void SetUp()
    {
        _style = ScriptableObject.CreateInstance<EmberStyle>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_style);
    }

    private static void Run(EmberHeat heat, float seconds)
    {
        var frames = (int)(seconds / Frame);
        for (var i = 0; i < frames; i++)
        {
            heat.Tick(Frame);
        }
    }

    [Test]
    public void RestsCold()
    {
        var heat = new EmberHeat(_style);
        Run(heat, 1f);

        Assert.AreEqual(_style.NormalGlow, heat.Glow, 0.01f);
        Assert.AreEqual(_style.NormalRim, heat.Rim, 0.01f);
        Assert.AreEqual(0f, heat.Offset, 0.01f);
    }

    [Test]
    public void PressReachesFullHeatWithinItsOwnDuration()
    {
        var heat = new EmberHeat(_style);
        heat.SetState(true, true, false, false);
        Run(heat, 0.1f);

        Assert.AreEqual(_style.PressGlow, heat.Glow, 0.05f);
        Assert.AreEqual(_style.PressRim, heat.Rim, 0.05f);
        Assert.AreEqual(_style.PressOffset, heat.Offset, 0.2f);
    }

    [Test]
    public void ReleaseSettlesToHoverNotToCold()
    {
        var heat = new EmberHeat(_style);
        heat.SetState(true, true, false, false);
        Run(heat, 0.1f);

        heat.SetState(true, false, false, false);
        Run(heat, 0.6f);

        Assert.AreEqual(_style.HoverGlow, heat.Glow, 0.05f);
        Assert.AreEqual(_style.HoverRim, heat.Rim, 0.05f);
        Assert.AreEqual(0f, heat.Offset, 0.05f);
    }

    [Test]
    public void PressBeatsRelease()
    {
        var pressed = new EmberHeat(_style);
        pressed.SetState(true, true, false, false);
        Run(pressed, 0.06f);

        var released = new EmberHeat(_style);
        released.SetState(true, true, false, false);
        Run(released, 1f);
        released.SetState(true, false, false, false);
        Run(released, 0.06f);

        // The same elapsed time covers far more of the press than of the release, or a press that
        // lands during a release looks like a slow fade instead of a hit.
        Assert.Greater(pressed.Glow - _style.NormalGlow, _style.PressGlow - released.Glow);
    }

    [Test]
    public void IdleBreathesInsideItsBand()
    {
        var heat = new EmberHeat(_style);
        heat.SetState(false, false, true, false);

        var low = float.MaxValue;
        var high = float.MinValue;
        for (var i = 0; i < 60 * 10; i++)
        {
            heat.Tick(Frame);
            if (i < 60)
            {
                continue;
            }
            low = heat.Glow < low ? heat.Glow : low;
            high = heat.Glow > high ? heat.Glow : high;
        }

        Assert.GreaterOrEqual(low, _style.IdleLow - 0.05f);
        Assert.LessOrEqual(high, _style.IdleHigh + 0.05f);
        Assert.Greater(high - low, 0.1f);
    }

    [Test]
    public void PressedIdleOwnerHoldsFullHeat()
    {
        var heat = new EmberHeat(_style);
        heat.SetState(false, false, true, false);
        Run(heat, 1.1f);
        heat.SetState(false, true, true, false);
        Run(heat, 0.2f);

        var held = heat.Glow;
        Run(heat, 2.2f);

        Assert.AreEqual(held, heat.Glow, 0.01f);
    }

    [Test]
    public void LockedCoolsBelowRest()
    {
        var heat = new EmberHeat(_style);
        heat.SetState(true, false, true, true);
        Run(heat, 1f);

        Assert.AreEqual(_style.LockedGlow, heat.Glow, 0.02f);
        Assert.AreEqual(_style.NormalRim, heat.Rim, 0.02f);
    }

    [Test]
    public void LockedButtonDoesNotDip()
    {
        var heat = new EmberHeat(_style);
        heat.SetState(true, true, false, true);
        Run(heat, 0.5f);

        Assert.AreEqual(0f, heat.Offset, 0.01f);
    }

    [Test]
    public void ZeroDeltaChangesNothing()
    {
        var heat = new EmberHeat(_style);
        heat.SetState(true, true, true, false);
        heat.Tick(0f);

        Assert.AreEqual(_style.NormalGlow, heat.Glow, 0.0001f);
    }

    [Test]
    public void FrameRateDoesNotChangeWhereItLands()
    {
        var fast = new EmberHeat(_style);
        fast.SetState(true, false, false, false);
        for (var i = 0; i < 120; i++)
        {
            fast.Tick(1f / 120f);
        }

        var slow = new EmberHeat(_style);
        slow.SetState(true, false, false, false);
        for (var i = 0; i < 15; i++)
        {
            slow.Tick(1f / 15f);
        }

        Assert.AreEqual(fast.Glow, slow.Glow, 0.02f);
    }
}
