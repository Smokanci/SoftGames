using UnityEngine;
using UnityEngine.Events;

// Fires in Start after every other component, so listeners subscribed in Awake/OnEnable
// across the scene have all run before the helper raises its UnityEvent.
[DefaultExecutionOrder(AfterEveryotherStart)]
public class GameEventHelper : MonoBehaviour
{
    private const int AfterEveryotherStart = 999_999;

    [SerializeField] private UnityEvent raiseEvent;

    private void Start()
    {
        raiseEvent.Invoke();
    }
}
