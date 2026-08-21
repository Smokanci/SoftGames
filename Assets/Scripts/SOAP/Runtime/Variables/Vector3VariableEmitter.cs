using UnityEngine;

[DefaultExecutionOrder(-100)]
public sealed class Vector3VariableEmitter : MonoBehaviour
{
    [SerializeField] private Vector3Variable variable;

    private void LateUpdate()
    {
        variable.Value = transform.position;
    }
}
