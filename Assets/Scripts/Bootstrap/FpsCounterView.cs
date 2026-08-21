using TMPro;
using UnityEngine;

public sealed class FpsCounterView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private int      windowSize      = 60;
    [SerializeField] private float    refreshInterval = 0.25f;

    private FpsSampler _sampler;
    private float      _sinceRefresh;

    private void Awake()
    {
        _sampler = new FpsSampler(windowSize);
    }

    private void Update()
    {
        _sampler.AddFrame(Time.unscaledDeltaTime);

        _sinceRefresh += Time.unscaledDeltaTime;
        if (_sinceRefresh < refreshInterval)
        {
            return;
        }
        _sinceRefresh = 0f;

        // Interpolation would allocate on every refresh, for the whole session.
        label.SetText(_sampler.HasSample ? "{0:0} FPS" : "-- FPS", _sampler.FramesPerSecond);
    }
}
