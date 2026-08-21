using UnityEngine;

public class GameEventRaiser<T> : MonoBehaviour
{
    protected GameEvent<T> EventToSend { get; set; } = null;

    protected virtual T ItemToSend { get; set; }

    public void RaiseEvent()
    {
        EventToSend.Raise(ItemToSend);
    }
}
