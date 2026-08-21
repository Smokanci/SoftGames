using System;
using UnityEngine;

[Serializable]
public abstract class VariableReference<T, TU> where T : GenericVariable<TU>
{
    [SerializeField] private bool useConstant = false;
    [SerializeField] private TU   constantValue = default;
    // null is legitimate only while useConstant is true; reaching the else branch
    // with no variable assigned is a wiring bug, so let it surface.
    [SerializeField] private T    variable;

    public TU Value => useConstant ? constantValue : variable.Value;

    public T Variable
    {
        get => variable;
        set => variable = value;
    }

    public bool UseConstant
    {
        get => useConstant;
        set => useConstant = value;
    }

    public TU ConstantValue
    {
        get => constantValue;
        set => constantValue = value;
    }
}
