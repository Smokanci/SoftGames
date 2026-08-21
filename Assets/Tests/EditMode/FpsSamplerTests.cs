using System;
using NUnit.Framework;

public class FpsSamplerTests
{
    [Test]
    public void ReportsNothingBeforeTheFirstFrame()
    {
        var sampler = new FpsSampler(60);

        Assert.IsFalse(sampler.HasSample);
        Assert.AreEqual(0f, sampler.FramesPerSecond);
    }

    [Test]
    public void SteadyFramesReportTheirRate()
    {
        var sampler = new FpsSampler(60);
        for (var i = 0; i < 60; i++)
        {
            sampler.AddFrame(1f / 60f);
        }

        Assert.AreEqual(60f, sampler.FramesPerSecond, 0.01f);
    }

    [Test]
    public void OldFramesLeaveTheWindow()
    {
        var sampler = new FpsSampler(4);
        for (var i = 0; i < 4; i++)
        {
            sampler.AddFrame(1f / 30f);
        }
        for (var i = 0; i < 4; i++)
        {
            sampler.AddFrame(1f / 120f);
        }

        Assert.AreEqual(120f, sampler.FramesPerSecond, 0.01f);
    }

    [Test]
    public void OneStalledFrameDragsTheAverageDown()
    {
        var sampler = new FpsSampler(10);
        for (var i = 0; i < 9; i++)
        {
            sampler.AddFrame(1f / 60f);
        }
        sampler.AddFrame(0.25f);

        // 9 frames at 1/60 plus one 250ms stall is 0.4s for 10 frames.
        Assert.AreEqual(25f, sampler.FramesPerSecond, 0.01f);
    }

    [Test]
    public void RejectsAnEmptyWindow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FpsSampler(0));
    }
}
