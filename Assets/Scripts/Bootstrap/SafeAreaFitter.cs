using UnityEngine;

// Outside a mobile player Screen.safeArea is always the full screen rect, so the whole component
// would write the anchors the RectTransform already has. WebGL in particular never receives the
// browser's env(safe-area-inset-*) values — that inset belongs to the WebGL template, not to C#.
// The class itself stays declared on every platform so the scenes and prefabs that carry it do
// not lose their script reference.
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
#if UNITY_IOS || UNITY_ANDROID
    private RectTransform     _rect;
    private Rect              _applied;
    private ScreenOrientation _orientation;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        Apply();
    }

    // Polled rather than event-driven: Unity raises no callback when either the safe area or the
    // orientation changes.
    private void Update()
    {
        if (_applied == Screen.safeArea && _orientation == Screen.orientation)
        {
            return;
        }

        Apply();
    }

    private void Apply()
    {
        // A NaN anchor never repairs itself, so a zero-size frame must not reach the divide.
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        var safe = Screen.safeArea;

        var min = safe.position;
        var max = safe.position + safe.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        _rect.anchorMin = min;
        _rect.anchorMax = max;

        _applied     = safe;
        _orientation = Screen.orientation;
    }
#endif
}
