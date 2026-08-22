using UnityEngine;

// Carries the colour the Animator animates and pushes it into the emitters. An Animator cannot
// drive a ParticleSystem's start colour on its own, and routing every layer through one animated
// value means the clips hold three colours rather than three colours per layer.
public sealed class FlameTint : MonoBehaviour
{
    // The clips in FlameColor.controller animate this field. Nothing else writes it.
    [SerializeField] private Color tint = Color.white;

    // Every listed system's start colour has to be in Constant mode — a gradient or a random
    // range carries no single colour to modulate. A layer left out of this array keeps the
    // colour it was authored with, which is how the smoke holds its own colour while the fire
    // changes hue.
    [SerializeField] private ParticleSystem[] layers;

    private Color[] _authored;

    private void Awake()
    {
        _authored = new Color[layers.Length];
        for (var i = 0; i < layers.Length; i++)
        {
            _authored[i] = layers[i].main.startColor.color;
        }
    }

    // A start colour reaches only particles that have yet to spawn, so a colour change washes up
    // through the fire over one particle lifetime instead of switching every pixel at once.
    // LateUpdate, so the value the Animator wrote this frame is the one the emitters get.
    private void LateUpdate()
    {
        for (var i = 0; i < layers.Length; i++)
        {
            var main = layers[i].main;
            main.startColor = tint * _authored[i];
        }
    }
}
