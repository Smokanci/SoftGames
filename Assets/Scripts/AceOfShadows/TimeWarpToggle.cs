using TMPro;
using UnityEngine;

// A full deal is one card a second, so watching the whole run means watching it for over two
// minutes. This is the affordance that lets someone see the end of it.
//
// It writes Time.timeScale rather than a private factor on the runner, so the flights, the cadence
// and the ground all speed up together and there is only one value to put back.
//
// Listens to the view rather than to Button.onClick, for the reason VoidEventButton gives: the
// press has been seen before anything acts on it. The view is this GameObject's own component.
[RequireComponent(typeof(EmberButtonView))]
public sealed class TimeWarpToggle : MonoBehaviour
{
    [SerializeField] private TMP_Text caption;

    [Tooltip("Scale the button warps to. The runner starts at most one card per frame, so a value past frameRate * Move Interval stops buying speed.")]
    [SerializeField] private float  fastScale     = 20f;
    [Tooltip("Formatted with the scale, so the caption cannot drift from what the button does.")]
    [SerializeField] private string captionFormat = "{0:0}x";

    private EmberButtonView _view;
    private bool            _fast;

    private void Awake()
    {
        _view = GetComponent<EmberButtonView>();
    }

    private void OnEnable()
    {
        _view.Committed += Toggle;
        Apply(false);
    }

    private void OnDisable()
    {
        _view.Committed -= Toggle;

        // Time.timeScale outlives this scene, and in the editor it outlives the play session. A
        // warp left behind would follow the player into the next task and the editor into the next
        // run. Unloading the scene and stopping play mode both reach this.
        Time.timeScale = 1f;
    }

    private void Toggle()
    {
        Apply(!_fast);
    }

    private void Apply(bool fast)
    {
        _fast = fast;

        var scale = fast ? fastScale : 1f;
        Time.timeScale = scale;
        caption.text   = string.Format(captionFormat, scale);
    }
}
