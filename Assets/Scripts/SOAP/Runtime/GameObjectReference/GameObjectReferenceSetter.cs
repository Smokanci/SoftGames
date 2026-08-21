using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class GameObjectReferenceSetter : MonoBehaviour
{
    [SerializeField] protected GameObjectReference gameObjectReference;
    [SerializeField] private bool deactivate = false;

    protected virtual void Awake()
    {
        gameObjectReference.Target = gameObject;
    }

    private void Start()
    {
        if (deactivate)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (gameObjectReference.Target == gameObject)
        {
            gameObjectReference.Target = null;
        }
    }
}
