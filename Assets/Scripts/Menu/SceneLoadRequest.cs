using UnityEngine;
using UnityEngine.UI;

// Subscribes in code rather than through the Button's inspector UnityEvent: a persistent
// call would have to name a target object, and the conventions ban wiring one across the
// hierarchy. The Button is this GameObject's own, which is allowed.
[RequireComponent(typeof(Button))]
public sealed class SceneLoadRequest : MonoBehaviour
{
    [SerializeField] private GameEventString loadSceneRequested;
    [SerializeField] private string          sceneName;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(Raise);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(Raise);
    }

    private void Raise()
    {
        loadSceneRequested.Raise(sceneName);
    }
}
