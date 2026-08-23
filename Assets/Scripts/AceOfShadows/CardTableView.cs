using System;
using TMPro;
using UnityEngine;

// Draws the two stacks. Knows where a card at stack index N belongs and how to put it there;
// knows nothing about when anything moves.
public sealed class CardTableView : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform  cardsParent;
    [SerializeField] private TMP_Text   sourceCounter;
    [SerializeField] private TMP_Text   targetCounter;

    [Header("Deck")]
    [SerializeField] private Color[]  palette;
    [SerializeField] private Sprite[] glyphs;

    [Header("Layout")]
    [SerializeField] private float cardOffset         = 0.03f;
    [SerializeField] private float stackBaseY         = -3.4f;
    [SerializeField] private float separationFraction = 0.5f;
    [SerializeField] private float minSeparation      = 1.35f;
    [SerializeField] private float maxSeparation      = 3f;
    [SerializeField] private float counterY           = 3.3f;

    // Every card owns two sorting orders so its glyph can sit directly on top of its body.
    private const int OrdersPerCard = 2;
    private const int FlightOrder   = 4096;

    private Camera           _camera;
    private Transform[]      _cards = Array.Empty<Transform>();
    private SpriteRenderer[] _bodies;
    private SpriteRenderer[] _glyphs;
    private bool[]           _onTarget;
    private bool[]           _inFlight;
    private int[]            _stackIndex;
    private float            _appliedAspect;

    private void Awake()
    {
        _camera = Camera.main;
    }

    // Unity raises no callback for a viewport resize, the same reason SafeAreaFitter polls.
    private void Update()
    {
        if (Mathf.Approximately(_camera.aspect, _appliedAspect))
        {
            return;
        }

        ApplyLayout();
    }

    public void Build(int cardCount)
    {
        _cards      = new Transform[cardCount];
        _bodies     = new SpriteRenderer[cardCount];
        _glyphs     = new SpriteRenderer[cardCount];
        _onTarget   = new bool[cardCount];
        _inFlight   = new bool[cardCount];
        _stackIndex = new int[cardCount];

        for (var i = 0; i < cardCount; i++)
        {
            var card = Instantiate(cardPrefab, cardsParent);
            card.name = $"Card {i:000}";

            _cards[i]  = card.transform;
            _bodies[i] = card.GetComponent<SpriteRenderer>();
            _glyphs[i] = card.transform.GetChild(0).GetComponent<SpriteRenderer>();

            // Colour runs in blocks so a buried stack reads as a gradient; the glyph cycles
            // inside each block, so no two of the cardCount cards repeat a pair.
            _bodies[i].color  = palette[i / glyphs.Length];
            _glyphs[i].sprite = glyphs[i % glyphs.Length];
        }
    }

    public void SeatAll()
    {
        for (var i = 0; i < _cards.Length; i++)
        {
            Seat(i, false, i);
        }
    }

    public Vector3 RestingPosition(bool onTarget, int indexInStack)
    {
        var x = Mathf.Clamp(_camera.orthographicSize * _camera.aspect * separationFraction,
                            minSeparation, maxSeparation);

        return new Vector3(onTarget ? x : -x, stackBaseY + indexInStack * cardOffset, 0f);
    }

    public void Seat(int cardId, bool onTarget, int indexInStack)
    {
        _onTarget[cardId]   = onTarget;
        _inFlight[cardId]   = false;
        _stackIndex[cardId] = indexInStack;

        // A card arrives mid-spin and mid-bump, so a seat that only wrote position would leave
        // it crooked and oversized in the stack for the rest of the run.
        var card = _cards[cardId];
        card.position      = RestingPosition(onTarget, indexInStack);
        card.localRotation = Quaternion.identity;
        card.localScale    = Vector3.one;

        SetSortingBase(cardId, indexInStack * OrdersPerCard);
    }

    public void Lift(int cardId)
    {
        _inFlight[cardId] = true;
        SetSortingBase(cardId, FlightOrder);
    }

    public void SetFlightPose(int cardId, Vector3 position, float rollDegrees, float scale)
    {
        var card = _cards[cardId];
        card.position      = position;
        card.localRotation = Quaternion.Euler(0f, 0f, rollDegrees);
        card.localScale    = new Vector3(scale, scale, 1f);
    }

    public void SetCounts(int sourceCount, int targetCount)
    {
        sourceCounter.SetText("{0:0}", sourceCount);
        targetCounter.SetText("{0:0}", targetCount);
    }

    private void SetSortingBase(int cardId, int order)
    {
        _bodies[cardId].sortingOrder = order;
        _glyphs[cardId].sortingOrder = order + 1;
    }

    private void ApplyLayout()
    {
        _appliedAspect = _camera.aspect;

        var counterX = RestingPosition(true, 0).x;
        sourceCounter.transform.position = new Vector3(-counterX, counterY, 0f);
        targetCounter.transform.position = new Vector3(counterX, counterY, 0f);

        for (var i = 0; i < _cards.Length; i++)
        {
            // A card in the air is placed by whoever is flying it; its stored slot is stale.
            if (_inFlight[i])
            {
                continue;
            }

            _cards[i].position = RestingPosition(_onTarget[i], _stackIndex[i]);
        }
    }
}
