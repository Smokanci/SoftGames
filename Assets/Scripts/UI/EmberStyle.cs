using UnityEngine;

// The Ember look is one asset rather than a constant per file. A button that breathes differently
// from its neighbours reads as a bug, not as a highlight, so the tuning is deliberately global and
// only the hue is per-instance. Retuning the whole app is an inspector edit here.
[CreateAssetMenu(fileName = "EmberStyle", menuName = "SoftGames/Ember Style")]
public sealed class EmberStyle : ScriptableObject
{
    [Header("Glow")]
    [SerializeField] private float normalGlow    = 0.40f;
    [Tooltip("Free to go past 1: this multiplies into Glow Intensity, it is not an alpha.")]
    [SerializeField] private float hoverGlow     = 1.50f;
    [SerializeField] private float pressGlow     = 1.00f;
    [SerializeField] private float lockedGlow    = 0.34f;
    [SerializeField] private float idleLow       = 0.52f;

    [Tooltip("Ceiling of the idle breath. Keep it well under Hover Glow, or hovering the button " +
             "that is already breathing is a step nobody can see.")]
    [SerializeField] private float idleHigh      = 0.72f;

    [SerializeField] private float glowIntensity = 0.60f;

    [Tooltip("Glow size under a hovered or pressed face, as a multiple of its resting size.")]
    [SerializeField] private float hoverGlowSpread = 1.22f;

    [Header("Rim and caption")]
    [SerializeField] private float normalRim = 0.28f;
    [SerializeField] private float hoverRim  = 0.90f;
    [SerializeField] private float pressRim  = 1.00f;

    [Tooltip("How far the rim washes toward white at full press. 0 keeps it on the button's own hue.")]
    [SerializeField] private float whiteMix = 0.50f;

    [Tooltip("The same wash while hovered.")]
    [SerializeField] private float hoverWhiteMix = 0.45f;

    [Tooltip("Alpha of the caption on a button nobody is pointing at. Hover takes it to 1, which " +
             "is what makes the unhovered ones recede.")]
    [SerializeField] private float captionRestAlpha = 0.75f;

    [Header("Motion")]
    [Tooltip("Pixels the face drops while held. The whole travel, not half of it.")]
    [SerializeField] private float pressOffset = 2f;

    [Tooltip("Pixels the face rises while hovered, so the press dip travels from above the line " +
             "to below it.")]
    [SerializeField] private float hoverLift = 2f;

    [Tooltip("Face scale while hovered. The press cancels it, which is half of the dip's punch.")]
    [SerializeField] private float hoverScale = 1.02f;

    [Tooltip("Seconds to within about 5% of full press. True only at the authored Settle Factor.")]
    [SerializeField] [Min(0.001f)] private float pressSeconds = 0.06f;

    [Tooltip("Seconds to within about 5% of full hover. A highlight has to snap on and drift off, " +
             "so this is much shorter than Release Seconds rather than equal to it.")]
    [SerializeField] [Min(0.001f)] private float hoverSeconds = 0.07f;

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

    public float NormalGlow      => normalGlow;
    public float HoverGlow       => hoverGlow;
    public float PressGlow       => pressGlow;
    public float LockedGlow      => lockedGlow;
    public float IdleLow         => idleLow;
    public float IdleHigh        => idleHigh;
    public float GlowIntensity   => glowIntensity;
    public float HoverGlowSpread => hoverGlowSpread;

    public float NormalRim        => normalRim;
    public float HoverRim         => hoverRim;
    public float PressRim         => pressRim;
    public float WhiteMix         => whiteMix;
    public float HoverWhiteMix    => hoverWhiteMix;
    public float CaptionRestAlpha => captionRestAlpha;

    public float PressOffset    => pressOffset;
    public float HoverLift      => hoverLift;
    public float HoverScale     => hoverScale;
    public float PressSeconds   => pressSeconds;
    public float HoverSeconds   => hoverSeconds;
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
