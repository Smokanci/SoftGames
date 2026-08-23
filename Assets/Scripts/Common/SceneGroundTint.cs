using UnityEngine;

// A scene cannot reach the bootstrap scene's EmberGround directly, so it publishes the two colours
// it wants and the ground reads them. Every screen carries one, the menu included — a screen with no
// tint of its own would inherit whichever one ran before it.
public sealed class SceneGroundTint : MonoBehaviour
{
    [SerializeField] private ColorVariable tint;
    [SerializeField] private ColorVariable fill;

    [Tooltip("Hue of the bloom and the ash. Keep it saturated — it is read against a near-black fill.")]
    [SerializeField] private Color sceneTint = Color.white;

    [Tooltip("Flat colour behind everything. Phoenix Flame needs this at mid-value or its blue and " +
             "green flame is invisible; every other screen wants it near black.")]
    [SerializeField] private Color sceneFill = Color.black;

    // OnEnable rather than Start, so the ground starts easing toward this screen's colours in the
    // same frame the scene comes up rather than one frame into it.
    private void OnEnable()
    {
        tint.Value = sceneTint;
        fill.Value = sceneFill;
    }
}
