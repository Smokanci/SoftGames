using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Draws what EmberHeat computes, and nothing else. The Button's own transition is set to None on
// the prefab: Color Tint fades one graphic and cannot move a face or scale a bloom, so leaving it
// on would only fight this component over the same Image.
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

    // The one thing a button owns rather than shares: its task's colour.
    [SerializeField] private Color hue = new Color(1f, 0.478f, 0.239f, 1f);

    private EmberHeat   _heat;
    private CanvasGroup _canvasGroup;
    private Image       _bloomImage;
    private Vector2     _faceHome;
    private float       _bloomAge;
    private float       _submitHold;
    private bool        _pointerOver;
    private bool        _pressed;
    private bool        _selected;
    private bool        _alive;
    private bool        _held;
    private bool        _locked;
    private bool        _pressLatch;

    public bool PointerOver => _pointerOver;
    public bool Selected    => _selected;

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

        _canvasGroup.alpha          = _locked ? style.DimmedAlpha : 1f;
        _canvasGroup.blocksRaycasts = !loading;
    }

    private void Awake()
    {
        _heat        = new EmberHeat(style);
        _canvasGroup = GetComponent<CanvasGroup>();
        _bloomImage  = bloom.GetComponent<Image>();
        _faceHome    = face.anchoredPosition;
        _bloomAge    = style.BloomSeconds;
    }

    private void Update()
    {
        // Unscaled: a button that stops answering because something scaled time reads as broken.
        var deltaTime = Time.unscaledDeltaTime;

        if (_submitHold > 0f)
        {
            _submitHold -= deltaTime;
            if (_submitHold <= 0f)
            {
                _pressed = false;
            }
        }

        _heat.SetState(_pointerOver, _pressed || _held, _alive, _locked);
        _heat.Tick(deltaTime);

        glow.color = new Color(hue.r, hue.g, hue.b, _heat.Glow * style.GlowIntensity);

        var hot = Mathf.InverseLerp(style.HoverRim, style.PressRim, _heat.Rim) * style.WhiteMix;
        var rimColor = Color.Lerp(hue, Color.white, hot);
        rimColor.a = _heat.Rim;
        rim.color = rimColor;

        face.anchoredPosition = _faceHome + new Vector2(0f, -_heat.Offset);

        TickBloom(deltaTime);
    }

    private void TickBloom(float deltaTime)
    {
        if (_bloomAge >= style.BloomSeconds)
        {
            return;
        }

        _bloomAge += deltaTime;
        var life = Mathf.Clamp01(_bloomAge / style.BloomSeconds);

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
        _bloomAge = 0f;
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
        _pressed     = true;
        _pressLatch  = true;
        _submitHold  = style.SubmitHold;
        bloom.anchoredPosition = Vector2.zero;
        _bloomAge = 0f;
    }
}
