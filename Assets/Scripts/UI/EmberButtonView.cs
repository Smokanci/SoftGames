using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Draws what EmberHeat computes, and owns the beat between the click and the action. The Button's
// own transition is set to None on the prefab: Color Tint fades one graphic and cannot move a face
// or scale a bloom, so leaving it on would only fight this component over the same Image.
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public sealed class EmberButtonView : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [SerializeField] private EmberStyle    style;
    [SerializeField] private RectTransform face;
    [SerializeField] private Image         glow;
    [SerializeField] private Image         rim;
    [SerializeField] private RectTransform bloom;
    [SerializeField] private Graphic       label;
    [SerializeField] private Graphic       glyph;

    // The one thing a button owns rather than shares: its task's colour.
    [SerializeField] private Color hue = new Color(1f, 0.478f, 0.239f, 1f);

    private EmberHeat   _heat;
    private Button      _button;
    private CanvasGroup _canvasGroup;
    private Image       _bloomImage;
    private Vector2     _faceHome;
    private Vector3     _faceScaleHome;
    private Vector3     _glowHome;
    private Color       _labelHome;
    private Color       _glyphHome;
    private float       _bloomAge;
    private float       _bloomSpan;
    private float       _commitCountdown;
    private float       _submitHold;
    private bool        _pointerOver;
    private bool        _pressed;
    private bool        _selected;
    private bool        _alive;
    private bool        _held;
    private bool        _locked;
    private bool        _committing;
    private bool        _pressLatch;
    private Color       _lastGlow;
    private Color       _lastRim;
    private Color       _lastLabel;
    private Color       _lastGlyph;
    private Vector3     _lastGlowScale;
    private Vector2     _lastFacePos;
    private Vector3     _lastFaceScale;

    // Raised one Commit Delay after the click, not on the click. Whoever does the actual work
    // listens here rather than on the Button, so there is one owner of press timing.
    public event Action Committed;

    public bool PointerOver => _pointerOver;
    public bool Selected    => _selected;

    // True through the hold between the click and Committed. The group reads it to lock the other
    // buttons from the press rather than from the scene load, which starts a hold later.
    public bool Committing => _committing;

    // True once per press, for whoever is counting them. The group polls this rather than taking
    // a callback, so a press that starts and ends inside one frame still registers.
    public bool ConsumePressed()
    {
        var pressed = _pressLatch;
        _pressLatch = false;
        return pressed;
    }

    public void SetIdleOwner(bool value)
    {
        _alive = value;
    }

    // A scene swap is running: the button that started it holds its heat, every other one cools
    // and stops taking input.
    public void SetLoading(bool loading, bool committed)
    {
        _held   = loading && committed;
        _locked = loading && !committed;

        // The group calls this every frame for every button. A CanvasGroup alpha write dirties the
        // group's whole subtree, so it is only worth making when the value actually moves.
        var alpha = _locked ? style.DimmedAlpha : 1f;
        if (!Mathf.Approximately(alpha, _canvasGroup.alpha))
        {
            _canvasGroup.alpha = alpha;
        }

        if (_canvasGroup.blocksRaycasts == loading)
        {
            _canvasGroup.blocksRaycasts = !loading;
        }
    }

    private void Awake()
    {
        _heat        = new EmberHeat(style);
        _button      = GetComponent<Button>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _bloomImage  = bloom.GetComponent<Image>();
        _faceHome      = face.anchoredPosition;
        _faceScaleHome = face.localScale;
        _glowHome      = glow.rectTransform.localScale;
        _labelHome   = label.color;
        _glyphHome   = glyph.color;

        // Seeded from the graphics, not left at default, so the change guards in Update are exact
        // from the first frame instead of writing once to establish a baseline.
        _lastGlow      = glow.color;
        _lastRim       = rim.color;
        _lastLabel     = _labelHome;
        _lastGlyph     = _glyphHome;
        _lastGlowScale = _glowHome;
        _lastFacePos   = _faceHome;
        _lastFaceScale = _faceScaleHome;

        _bloomSpan   = style.BloomSeconds;
        _bloomAge    = _bloomSpan;
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(Commit);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(Commit);
        _committing = false;
    }

    private void Update()
    {
        // Unscaled: a button that stops answering because something scaled time reads as broken.
        // The pause overlay's own buttons are pressed at timeScale zero.
        var deltaTime = Time.unscaledDeltaTime;

        if (_submitHold > 0f)
        {
            _submitHold -= deltaTime;
            if (_submitHold <= 0f)
            {
                _pressed = false;
            }
        }

        TickCommit(deltaTime);

        _heat.SetState(_pointerOver, _pressed || _held || _committing, _alive, _locked);
        _heat.Tick(deltaTime);

        // Each Graphic.color setter marks the whole canvas dirty, and the canvas belongs to
        // TaskChrome, which the Magic Words dialogue log is parented into. EmberHeat holds a
        // constant once the button settles, so without these guards two idle buttons would rebuild
        // every dialogue row for as long as the scene is open. Colour and vector == compare with an
        // epsilon, which is the tolerance wanted here anyway.
        SetGraphicColor(glow, new Color(hue.r, hue.g, hue.b, _heat.Glow * style.GlowIntensity), ref _lastGlow);

        var glowScale = _glowHome * _heat.Spread;
        if (glowScale != _lastGlowScale)
        {
            _lastGlowScale                = glowScale;
            glow.rectTransform.localScale = glowScale;
        }

        var rimColor = Color.Lerp(hue, Color.white, _heat.White);
        rimColor.a = _heat.Rim;
        SetGraphicColor(rim, rimColor, ref _lastRim);

        var caption = Mathf.Lerp(style.CaptionRestAlpha, 1f, _heat.Caption);
        SetGraphicColor(label, new Color(_labelHome.r, _labelHome.g, _labelHome.b, _labelHome.a * caption), ref _lastLabel);
        SetGraphicColor(glyph, new Color(_glyphHome.r, _glyphHome.g, _glyphHome.b, _glyphHome.a * caption), ref _lastGlyph);

        var facePos = _faceHome + new Vector2(0f, -_heat.Offset);
        if (facePos != _lastFacePos)
        {
            _lastFacePos          = facePos;
            face.anchoredPosition = facePos;
        }

        var faceScale = _faceScaleHome * _heat.Scale;
        if (faceScale != _lastFaceScale)
        {
            _lastFaceScale  = faceScale;
            face.localScale = faceScale;
        }

        TickBloom(deltaTime);
    }

    private void SetGraphicColor(Graphic target, Color value, ref Color last)
    {
        if (value == last)
        {
            return;
        }

        last         = value;
        target.color = value;
    }

    // The click is held for one Commit Delay so the press has been seen before anything acts on
    // it. A scene swap destroys this button a frame or two later, so the bloom is retimed to land
    // on the same instant — after that every remaining value is holding a constant, and a constant
    // survives being cut.
    private void Commit()
    {
        if (_committing)
        {
            return;
        }

        _committing      = true;
        _commitCountdown = style.CommitDelay;

        // Only ever shortens. Pushing the span out would drop the bloom's own progress and make it
        // jump backwards, and a bloom that already ends before the commit needs no help.
        _bloomSpan = Mathf.Min(_bloomSpan, _bloomAge + style.CommitDelay);
    }

    private void TickCommit(float deltaTime)
    {
        if (!_committing)
        {
            return;
        }

        _commitCountdown -= deltaTime;
        if (_commitCountdown > 0f)
        {
            return;
        }

        _committing = false;
        Committed?.Invoke();
    }

    private void TickBloom(float deltaTime)
    {
        if (_bloomAge >= _bloomSpan)
        {
            return;
        }

        _bloomAge += deltaTime;
        var life = Mathf.Clamp01(_bloomAge / _bloomSpan);

        // Cubic ease-out: the bloom has to be most of its size almost immediately or the press
        // and the light look like two separate events.
        var spread = 1f - (1f - life) * (1f - life) * (1f - life);
        var width  = Mathf.Lerp(style.BloomStartSize, face.rect.width  * style.BloomSpreadX, spread);
        var height = Mathf.Lerp(style.BloomStartSize, face.rect.height * style.BloomSpreadY, spread);

        bloom.sizeDelta   = new Vector2(width, height);
        _bloomImage.color = new Color(hue.r, hue.g, hue.b, (1f - life) * style.BloomIntensity);
    }

    private void StartBloom(Vector2 screenPoint, Camera eventCamera)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(face, screenPoint, eventCamera, out var local);
        bloom.anchoredPosition = local;
        _bloomSpan = style.BloomSeconds;
        _bloomAge  = 0f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerOver = false;
        _pressed     = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pressed    = true;
        _pressLatch = true;
        StartBloom(eventData.position, eventData.pressEventCamera);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pressed = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        _selected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _selected = false;
    }

    // Keyboard and gamepad submit carry no pointer position, so the bloom comes from the middle.
    public void OnSubmit(BaseEventData eventData)
    {
        _pressed    = true;
        _pressLatch = true;
        _submitHold = style.SubmitHold;
        bloom.anchoredPosition = Vector2.zero;
        _bloomSpan = style.BloomSeconds;
        _bloomAge  = 0f;
    }
}
