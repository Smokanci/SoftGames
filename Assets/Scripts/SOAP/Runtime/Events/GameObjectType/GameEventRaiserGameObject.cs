using UnityEngine;

public class GameEventRaiserGameObject : GameEventRaiser<GameObject>
{
    [SerializeField] private GameEventGameObject eventToSend;
    
    private void OnEnable()
    {
        ItemToSend = gameObject;
        EventToSend = eventToSend;
        
        RaiseEvent();
    }
}
