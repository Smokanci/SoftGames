using System;
using System.Diagnostics;
using UnityEngine;

[Serializable]
public abstract class GameEvent<T> : ScriptableObject
{
    public event Action<T> EventListeners = delegate { };

    public virtual void Raise(T value)
    {
        LogRaise(value);
        EventListeners(value);
    }

    public void ClearEventsListeners()
    {
        EventListeners = delegate { };
    }

    [Conditional("SOAP_DEBUG")]
    private void LogRaise(T value)
    {
        UnityEngine.Debug.Log($"[GameEvent<{typeof(T).Name}>] Raising event from asset '{name}' with value: {value}");
    }
}
