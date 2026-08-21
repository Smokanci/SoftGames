using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class GenericVariable<T> : BaseVariable
{
    [SerializeField] protected T initialValue;
    [SerializeField] protected T runtimeValue;

    private static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;

    public event UnityAction<T> OnValueChanged = delegate { };

    public T InitialValue { get => initialValue; set => initialValue = value; }

    public virtual T Value
    {
        get => runtimeValue;
        set
        {
            if (Comparer.Equals(runtimeValue, value))
            {
                return;
            }

            runtimeValue = value;
            OnValueChanged.Invoke(value);
        }
    }

    protected override void ResetToInitial() => runtimeValue = initialValue;

    protected override bool HasRuntimeDrift() => !Comparer.Equals(runtimeValue, initialValue);

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (UnityEngine.Application.isPlaying)
        {
            var snapshot = runtimeValue;
            UnityEditor.EditorApplication.delayCall += () => OnValueChanged.Invoke(snapshot);
        }
    }
#endif
}
