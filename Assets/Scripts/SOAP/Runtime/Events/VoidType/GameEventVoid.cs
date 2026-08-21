using UnityEngine;

[CreateAssetMenu(menuName = "SOAP/Game Events/Void Event", fileName = "_GameEventVoid")]
public class GameEventVoid : GameEvent<Empty>
{
    public void Raise() => Raise(new Empty());
}