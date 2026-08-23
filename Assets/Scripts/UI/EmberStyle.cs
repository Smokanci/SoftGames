using UnityEngine;

// The Ember look is one asset rather than a constant per file. A button that breathes differently
// from its neighbours reads as a bug, not as a highlight, so the tuning is deliberately global and
// only the hue is per-instance. Retuning the whole app is an inspector edit here.
[CreateAssetMenu(fileName = "EmberStyle", menuName = "SoftGames/Ember Style")]
public sealed class EmberStyle : ScriptableObject
{
    [Header("Glow")]
    [SerializeField] private float normalGlow    = 0.40f;
    [SerializeField] private float hoverGlow     = 0.95f;
    [SerializeField] private float pressGlow     = 1.00f;
    [SerializeField] private float lockedGlow    = 0.34f;
    [SerializeField] private float idleLow       = 0.60f;
    [SerializeField] private float idleHigh      = 0.85f;
    [SerializeField] private float glowIntensity = 0.60f;

    [Header("Rim")]
    [SerializeField] private float normalRim = 0.28f;
    [SerializeField] private float hoverRim  = 0.55f;
    [SerializeField] private float pressRim  = 1.00f;

    [Tooltip("How far the rim washes toward white at full press. 0 keeps it on the button's own hue.")]
    [SerializeField] private float whiteMix = 0.50f;

    [Header("Motion")]
    [Tooltip("Pixels the face drops while held. The whole travel, not half of it.")]
    [SerializeField] private float pressOffset = 2f;

    [Tooltip("Seconds to within about 5% of full press. True only at the authored Settle Factor.")]
    [SerializeField] [Min(0.001f)] private float pressSeconds = 0.06f;

    [Tooltip("Seconds to within about 5% of rest. True only at the authored Settle Factor.")]
    [SerializeField] [Min(0.001f)] private float releaseSeconds = 0.32f;

    [Tooltip("Seconds for one full breath of the idle glow, low to high and back.")]
    [SerializeField] [Min(0.001f)] private float idlePeriod = 4.5f;

    [Tooltip("Defines what the two durations above mean. Raising it makes every one of them a " +
             "tighter approach than its number says.")]
    [SerializeField] private float settleFactor = 3f;

    [Header("Bloom")]
    [SerializeField] [Min(0.001f)] private float bloomSeconds = 0.52f;

    [SerializeField] private float bloomStartSize = 24f;

    [Tooltip("Final bloom width as multiples of the face's own width, not pixels — so a wide " +
             "button gets a wide burst instead of a circle that overshoots its ends.")]
    [SerializeField] private float bloomSpreadX = 1.2f;

    [Tooltip("Final bloom height as multiples of the face's own height, not pixels.")]
    [SerializeField] private float bloomSpreadY = 1.6f;

    [SerializeField] private float bloomIntensity = 0.60f;

    [Header("Commit")]
    [Tooltip("Keyboard and gamepad submit carry no press and release, so the press is held this long.")]
    [SerializeField] private float submitHold = 0.12f;

    [Tooltip("CanvasGroup alpha for a button locked out while another one's scene loads.")]
    [SerializeField] private float dimmedAlpha = 0.34f;

    public float NormalGlow    => normalGlow;
    public float HoverGlow     => hoverGlow;
    public float PressGlow     => pressGlow;
    public float LockedGlow    => lockedGlow;
    public float IdleLow       => idleLow;
    public float IdleHigh      => idleHigh;
    public float GlowIntensity => glowIntensity;

    public float NormalRim => normalRim;
    public float HoverRim  => hoverRim;
    public float PressRim  => pressRim;
    public float WhiteMix  => whiteMix;

    public float PressOffset    => pressOffset;
    public float PressSeconds   => pressSeconds;
    public float ReleaseSeconds => releaseSeconds;
    public float IdlePeriod     => idlePeriod;
    public float SettleFactor   => settleFactor;

    public float BloomSeconds   => bloomSeconds;
    public float BloomStartSize => bloomStartSize;
    public float BloomSpreadX   => bloomSpreadX;
    public float BloomSpreadY   => bloomSpreadY;
    public float BloomIntensity => bloomIntensity;

    public float SubmitHold  => submitHold;
    public float DimmedAlpha => dimmedAlpha;
}
