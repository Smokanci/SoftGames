using System;

// The press look is a set of numbers chasing targets, not a set of coroutines. A press that lands
// while the release is still running has to redirect the value that is already moving, and two
// competing coroutines cannot — one of them wins and the button jumps. Kept clear of
// MonoBehaviour so an EditMode test drives it with no scene and no play mode; the style asset it
// reads can be built with ScriptableObject.CreateInstance, which needs neither.
public sealed class EmberHeat
{
    private readonly EmberStyle _style;

    private float _glow;
    private float _rim;
    private float _offset;
    private float _scale;
    private float _spread;
    private float _white;
    private float _caption;
    private float _phase;

    private bool _pointerOver;
    private bool _pressed;
    private bool _alive;
    private bool _locked;

    public EmberHeat(EmberStyle style)
    {
        _style  = style;
        _glow   = style.NormalGlow;
        _rim    = style.NormalRim;
        _scale  = 1f;
        _spread = 1f;
    }

    public float Glow    => _glow;
    public float Rim     => _rim;
    public float Offset  => _offset;
    public float Scale   => _scale;
    public float Spread  => _spread;
    public float White   => _white;
    public float Caption => _caption;

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

        var rate = Approach(deltaTime, Seconds());
        _glow    += (TargetGlow() - _glow) * rate;
        _rim     += (TargetRim() - _rim) * rate;
        _offset  += (TargetOffset() - _offset) * rate;
        _scale   += (TargetScale() - _scale) * rate;
        _spread  += (TargetSpread() - _spread) * rate;
        _white   += (TargetWhite() - _white) * rate;
        _caption += (TargetCaption() - _caption) * rate;
    }

    // A highlight that arrives at the speed it leaves is one nobody sees: by the time it lands the
    // pointer has moved on. Hover snaps in and drifts out.
    private float Seconds()
    {
        if (_pressed)
        {
            return _style.PressSeconds;
        }
        return _pointerOver && !_locked ? _style.HoverSeconds : _style.ReleaseSeconds;
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
        if (_locked)
        {
            return 0f;
        }
        if (_pressed)
        {
            return _style.PressOffset;
        }
        return _pointerOver ? -_style.HoverLift : 0f;
    }

    private float TargetScale()
    {
        return _pointerOver && !_pressed && !_locked ? _style.HoverScale : 1f;
    }

    private float TargetSpread()
    {
        return (_pointerOver || _pressed) && !_locked ? _style.HoverGlowSpread : 1f;
    }

    private float TargetWhite()
    {
        if (_locked)
        {
            return 0f;
        }
        if (_pressed)
        {
            return _style.WhiteMix;
        }
        return _pointerOver ? _style.HoverWhiteMix : 0f;
    }

    private float TargetCaption()
    {
        return (_pointerOver || _pressed) && !_locked ? 1f : 0f;
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
