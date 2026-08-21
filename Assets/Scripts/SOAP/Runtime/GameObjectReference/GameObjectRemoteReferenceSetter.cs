using UnityEngine;

[DefaultExecutionOrder(-1000)]

public class GameObjectRemoteReferenceSetter : GameObjectReferenceSetter
{
    [SerializeField] private GameObject targetGameObject;

    protected override void Awake()
    {
        gameObjectReference.Target = targetGameObject;
    }
}
