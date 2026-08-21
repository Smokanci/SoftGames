using UnityEngine;
using UnityEngine.Events;

public abstract class GameEventListener<T, GE> : MonoBehaviour
       where GE : GameEvent<T>
{
    [SerializeField]
    protected GE _GameEvent;

    [SerializeField]
    protected UnityEvent<T> _UnityEventResponse;

    protected virtual void OnEnable()
    {
        _GameEvent.EventListeners += TriggerResponses;
    }

    protected virtual void OnDisable()
    {
        _GameEvent.EventListeners -= TriggerResponses;
    }

    [ContextMenu("Trigger Responses")]
    public void TriggerResponses(T val)
    {
        _UnityEventResponse.Invoke(val);
    }
}
