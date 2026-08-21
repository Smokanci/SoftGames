using UnityEngine;

public abstract class BaseVariable : ScriptableObject
{
    protected abstract void ResetToInitial();
    protected abstract bool HasRuntimeDrift();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetAllInstances()
    {
        var all = Resources.FindObjectsOfTypeAll<BaseVariable>();
        for (var i = 0; i < all.Length; i++)
        {
            all[i].ResetToInitial();
        }
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterPlayModeReset()
    {
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change)
    {
        // EnteredEditMode, not ExitingPlayMode: the drift check compares serialized
        // runtimeValue to initialValue, so it survives the domain reload between the two
        // (a NonSerialized dirty flag would not). Nothing here reads runtime values at
        // quit; if something ever does, it must run before this reset.
        if (change != UnityEditor.PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        var all = Resources.FindObjectsOfTypeAll<BaseVariable>();
        var any = false;
        for (var i = 0; i < all.Length; i++)
        {
            if (!all[i].HasRuntimeDrift())
            {
                continue;
            }

            all[i].ResetToInitial();
            UnityEditor.EditorUtility.SetDirty(all[i]);
            any = true;
        }
        if (any)
        {
            UnityEditor.AssetDatabase.SaveAssets();
        }
    }
#endif
}
