using UnityEngine;

// Subscribes in code rather than through an inspector UnityEvent: a persistent call would have to
// name a target object, and the conventions ban wiring one across the hierarchy. The view is this
// GameObject's own, which is allowed.
//
// Listens to the view rather than to Button.onClick because the view holds the click for one
// Commit Delay first. Raising on the click itself would start the swap on top of the press, and
// the unload would take this scene's buttons with the effect still running.
[RequireComponent(typeof(EmberButtonView))]
public sealed class SceneLoadRequest : MonoBehaviour
{
    [SerializeField] private GameEventString loadSceneRequested;
    [SerializeField] private string          sceneName;

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
        loadSceneRequested.Raise(sceneName);
    }
}
