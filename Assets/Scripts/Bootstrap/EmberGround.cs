using UnityEngine;

// One ground, in the persistent scene, rather than one per task scene. A task scene carrying its own
// would pop it in a frame late on every swap, and the four copies would drift apart the moment one
// of them was retuned. What the scenes own instead is two colours, published through SOAP by
// SceneGroundTint.
public sealed class EmberGround : MonoBehaviour
{
    [SerializeField] private ColorVariable tint;
    [SerializeField] private ColorVariable fill;

    [Header("Layers")]
    [SerializeField] private SpriteRenderer flat;
    [SerializeField] private SpriteRenderer bloom;
    [SerializeField] private ParticleSystem motes;

    [Header("Mix")]
    [Tooltip("Coverage of the bloom over the flat fill. This layer is alpha-blended, not additive — " +
             "raising it hides the fill rather than adding light to it.")]
    [SerializeField] private float bloomIntensity = 0.13f;

    [Tooltip("Ash brightness as a fraction of the tint. Ash that matched the bloom exactly would " +
             "disappear into it wherever the two overlap, which is most of the screen.")]
    [SerializeField] private float moteIntensity = 0.35f;

    [Tooltip("Seconds for a scene swap's colour change to land. How close 'landed' is depends on " +
             "Settle Factor below.")]
    [SerializeField] [Min(0.001f)] private float easeSeconds = 0.65f;

    [Tooltip("Sharpness of the ease, and what Ease Seconds means. The remaining distance after t " +
             "seconds is exp(-factor * t / easeSeconds), so 3 lands within 5% and 4.6 within 1%. " +
             "Raise it for a snappier swap, lower it for a longer tail.")]
    [SerializeField] private float settleFactor = 3f;

    private Color _tint;
    private Color _fill;

    private void OnEnable()
    {
        _tint = tint.Value;
        _fill = fill.Value;
        Apply();
    }

    // Polled rather than listened to. The ease is what makes a scene swap cross-fade instead of cut,
    // so this runs every frame regardless of whether the colours changed; EmberButtonGroup reads
    // _IsLoadingScene the same way and for the same reason.
    private void Update()
    {
        var approach = 1f - Mathf.Exp(-settleFactor * Time.unscaledDeltaTime / easeSeconds);
        _tint = Color.Lerp(_tint, tint.Value, approach);
        _fill = Color.Lerp(_fill, fill.Value, approach);
        Apply();
    }

    private void Apply()
    {
        flat.color = _fill;
        bloom.color = new Color(_tint.r, _tint.g, _tint.b, bloomIntensity);

        var main = motes.main;
        main.startColor = new Color(_tint.r * moteIntensity, _tint.g * moteIntensity, _tint.b * moteIntensity, 1f);
    }
}
