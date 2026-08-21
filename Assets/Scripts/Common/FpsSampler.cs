using System;

public sealed class FpsSampler
{
    private readonly float[] _frameTimes;

    private int   _count;
    private int   _next;
    private float _sum;

    public FpsSampler(int windowSize)
    {
        if (windowSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(windowSize), windowSize, "A sampling window needs at least one frame.");
        }

        _frameTimes = new float[windowSize];
    }

    public bool HasSample => _count > 0;

    // Mean frame time inverted once — not the mean of per-frame fps. The two differ,
    // and only this one moves when a single frame stalls, which is the whole point of
    // showing the number.
    public float FramesPerSecond => _sum > 0f ? _count / _sum : 0f;

    public void AddFrame(float unscaledDeltaTime)
    {
        _sum -= _frameTimes[_next];
        _frameTimes[_next] = unscaledDeltaTime;
        _sum += unscaledDeltaTime;

        _next = (_next + 1) % _frameTimes.Length;

        if (_count < _frameTimes.Length)
        {
            _count++;
        }
    }
}
