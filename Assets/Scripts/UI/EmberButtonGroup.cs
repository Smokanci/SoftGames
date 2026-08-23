using UnityEngine;

// One breathing button at a time. Three buttons all pulsing on their own timers reads as a slot
// machine, so the idle glow is a single mark that says "this is the one you are about to press" —
// and something has to own the choice. The parent owns it: a child telling its parent "I am hot
// now" would be a cross-hierarchy reference in the wrong direction, so the parent asks instead.
public sealed class EmberButtonGroup : MonoBehaviour
{
    [SerializeField] private BoolVariable isLoadingScene;
    [SerializeField] private bool         idleGlow = true;

    private EmberButtonView[] _views;
    private EmberButtonView   _committed;
    private bool              _wasLoading;

    private void Awake()
    {
        _views = GetComponentsInChildren<EmberButtonView>(true);
    }

    private void Update()
    {
        var loading = isLoadingScene.Value;
        if (!loading && _wasLoading)
        {
            _committed = null;
        }
        _wasLoading = loading;

        if (!loading)
        {
            TakePresses();
        }

        var owner = idleGlow ? PickIdleOwner() : null;

        foreach (var view in _views)
        {
            view.SetIdleOwner(view == owner);
            view.SetLoading(loading, view == _committed);
        }
    }

    private void TakePresses()
    {
        foreach (var view in _views)
        {
            if (view.ConsumePressed())
            {
                _committed = view;
            }
        }
    }

    // Pointer beats keyboard focus, and something is always lit so the menu never looks asleep.
    private EmberButtonView PickIdleOwner()
    {
        EmberButtonView focused = null;

        foreach (var view in _views)
        {
            if (view.PointerOver)
            {
                return view;
            }
            if (focused == null && view.Selected)
            {
                focused = view;
            }
        }

        if (focused != null)
        {
            return focused;
        }

        foreach (var view in _views)
        {
            if (view.isActiveAndEnabled)
            {
                return view;
            }
        }

        return null;
    }
}
