using System;

// The press look is four numbers chasing targets, not four coroutines. A press that lands while
// the release is still running has to redirect the value that is already moving, and two
// competing coroutines cannot — one of them wins and the button jumps. Kept clear of
// MonoBehaviour so an EditMode test drives it with no scene and no play mode; the style asset it
// reads can be built with ScriptableObject.CreateInstance, which needs neither.
public sealed class EmberHeat
{
    private readonly EmberStyle _style;

    private float _glow;
    private float _rim;
    private float _offset;
    private float _phase;

    private bool _pointerOver;
    private bool _pressed;
    private bool _alive;
    private bool _locked;

    public EmberHeat(EmberStyle style)
    {
        _style = style;
        _glow  = style.NormalGlow;
        _rim   = style.NormalRim;
    }

    public float Glow   => _glow;
    public float Rim    => _rim;
    public float Offset => _offset;

    public void SetState(bool pointerOver, bool pressed, bool alive, bool locked)
    {
        _pointerOver = pointerOver;
        _pressed     = pressed;
        _alive       = alive;
        _locked      = locked;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        if (_alive && !_pressed && !_locked)
        {
            _phase += deltaTime / _style.IdlePeriod;
            _phase -= (float)Math.Floor(_phase);
        }

        var rate = Approach(deltaTime, _pressed ? _style.PressSeconds : _style.ReleaseSeconds);
        _glow   += (TargetGlow() - _glow) * rate;
        _rim    += (TargetRim() - _rim) * rate;
        _offset += (TargetOffset() - _offset) * rate;
    }

    private float TargetGlow()
    {
        if (_locked)
        {
            return _style.LockedGlow;
        }
        if (_pressed)
        {
            return _style.PressGlow;
        }
        if (_pointerOver)
        {
            return _style.HoverGlow;
        }
        if (_alive)
        {
            return _style.IdleLow + (_style.IdleHigh - _style.IdleLow) * Wave();
        }
        return _style.NormalGlow;
    }

    private float TargetRim()
    {
        if (_locked)
        {
            return _style.NormalRim;
        }
        if (_pressed)
        {
            return _style.PressRim;
        }
        return _pointerOver ? _style.HoverRim : _style.NormalRim;
    }

    private float TargetOffset()
    {
        return _pressed && !_locked ? _style.PressOffset : 0f;
    }

    private float Wave()
    {
        return 0.5f - 0.5f * (float)Math.Cos(2.0 * Math.PI * _phase);
    }

    // Frame-rate independent: the same wall-clock time gets the same distance covered whether it
    // arrives as one long frame or ten short ones.
    private float Approach(float deltaTime, float seconds)
    {
        return 1f - (float)Math.Exp(-_style.SettleFactor * deltaTime / seconds);
    }
}
