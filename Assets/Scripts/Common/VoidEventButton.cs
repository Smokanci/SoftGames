using UnityEngine;

// Subscribes in code rather than through an inspector UnityEvent: a persistent call would have to
// name a target object, and the conventions ban wiring one across the hierarchy. The view is this
// GameObject's own, which is allowed.
//
// Listens to the view rather than to Button.onClick so the press has been seen before anything
// acts on it — see EmberButtonView.Commit.
[RequireComponent(typeof(EmberButtonView))]
public sealed class VoidEventButton : MonoBehaviour
{
    [SerializeField] private GameEventVoid raiseOnClick;

    private EmberButtonView _view;

    private void Awake()
    {
        _view = GetComponent<EmberButtonView>();
    }

    private void OnEnable()
    {
        _view.Committed += Raise;
    }

    private void OnDisable()
    {
        _view.Committed -= Raise;
    }

    private void Raise()
    {
        raiseOnClick.Raise();
    }
}
