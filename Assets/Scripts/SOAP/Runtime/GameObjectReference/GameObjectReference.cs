using UnityEngine;

[CreateAssetMenu(menuName = "SOAP/GameObject Reference", fileName = "_GameObjectReference")]
public class GameObjectReference : ScriptableObject
{
    [SerializeField] private GameObject target;

    public GameObject Target { get => target; set => target = value; }
}
