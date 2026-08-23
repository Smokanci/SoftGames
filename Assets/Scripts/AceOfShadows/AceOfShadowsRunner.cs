using System.Collections.Generic;
using UnityEngine;

public sealed class AceOfShadowsRunner : MonoBehaviour
{
    private struct Flight
    {
        public int   CardId;
        public int   FromIndex;
        public int   ToIndex;
        public float Elapsed;
        public float ArcHeight;
        public int   Turns;
        public float LeanDegrees;
        public float Drift;
    }

    [SerializeField] private CardTableView   table;
    [SerializeField] private GameEventString taskMessageRequested;

    [Header("Run")]
    [SerializeField] private int    cardCount    = 144;
    [SerializeField] private float  moveInterval = 1f;
    // Kept under moveInterval so a card always lands before the next one leaves. Raising it
    // past the interval is survivable — the flight list holds however many are in the air.
    [SerializeField] private float  moveDuration = 0.55f;
    [SerializeField] private float  arcHeight    = 1.4f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Arc, turn, lean and drift are rolled per card, so no two moves look alike without needing
    // more than one flight path. Every term here returns to zero at t = 1, which is what keeps
    // the card landing square on its slot with no separate settling step.
    [Header("Flight variety")]
    [Tooltip("Multiplier range on Arc Height. Both ends above zero.")]
    [SerializeField] private Vector2 arcVariation = new Vector2(0.7f, 1.35f);
    [Tooltip("Odds that a card takes a whole turn instead of only leaning. 0 = never, 1 = always.")]
    [Range(0f, 1f)]
    [SerializeField] private float   spinChance   = 0.65f;
    [Tooltip("Odds a turning card goes anticlockwise. 0.5 = no bias either way.")]
    [Range(0f, 1f)]
    [SerializeField] private float   spinLeftChance = 0.5f;
    [Tooltip("Peak tilt at the top of the arc, in degrees, either way.")]
    [SerializeField] private float   leanDegrees  = 14f;
    [Tooltip("Sideways bulge at the top of the arc, in world units, either way.")]
    [SerializeField] private float   drift        = 0.35f;
    [Tooltip("Extra size at the top of the arc, as a fraction. The camera is orthographic, so this is the only depth cue.")]
    [SerializeField] private float   scaleBump    = 0.18f;

    // Formatted with the card count, so the wording cannot drift from the deck size.
    [SerializeField] private string completedMessage = "All {0} cards moved.";

    private readonly List<Flight> _flights = new List<Flight>(4);

    private CardStacks _stacks;
    private float      _sinceLastMove;

    private void Start()
    {
        _stacks = new CardStacks(cardCount);

        table.Build(cardCount);
        table.SeatAll();
        table.SetCounts(_stacks.SourceCount, _stacks.TargetCount);

        // Primed, so the first card leaves on the first frame instead of after a dead second.
        _sinceLastMove = moveInterval;
    }

    private void Update()
    {
        AdvanceFlights();

        if (!_stacks.CanBegin)
        {
            return;
        }

        _sinceLastMove += Time.deltaTime;
        if (_sinceLastMove < moveInterval)
        {
            return;
        }

        _sinceLastMove -= moveInterval;
        BeginMove();
    }

    private void BeginMove()
    {
        var cardId = _stacks.BeginMove();

        table.Lift(cardId);
        table.SetCounts(_stacks.SourceCount, _stacks.TargetCount);

        _flights.Add(new Flight
        {
            CardId      = cardId,
            FromIndex   = _stacks.SourceCount,
            ToIndex     = _stacks.TargetCount,
            Elapsed     = 0f,
            ArcHeight   = arcHeight * Random.Range(arcVariation.x, arcVariation.y),
            // A whole number of turns, so the same eased t that finishes the travel also
            // finishes the spin flat.
            Turns       = Random.value < spinChance ? (Random.value < spinLeftChance ? 1 : -1) : 0,
            LeanDegrees = Random.Range(-leanDegrees, leanDegrees),
            Drift       = Random.Range(-drift, drift),
        });
    }

    private void AdvanceFlights()
    {
        for (var i = _flights.Count - 1; i >= 0; i--)
        {
            var flight = _flights[i];
            flight.Elapsed += Time.deltaTime;

            if (flight.Elapsed >= moveDuration)
            {
                _flights.RemoveAt(i);
                Land(flight.CardId);
                continue;
            }

            // Read fresh every frame rather than cached at lift-off, so a viewport resize
            // mid-flight moves the card with the stacks instead of flying it to a stale spot.
            var from = table.RestingPosition(false, flight.FromIndex);
            var to   = table.RestingPosition(true, flight.ToIndex);

            var t        = ease.Evaluate(flight.Elapsed / moveDuration);
            var arc      = Mathf.Sin(t * Mathf.PI);
            var position = Vector3.Lerp(from, to, t);
            position.y += flight.ArcHeight * arc;
            position.x += flight.Drift * arc;

            var roll  = flight.Turns * 360f * t + flight.LeanDegrees * arc;
            var scale = 1f + scaleBump * arc;

            table.SetFlightPose(flight.CardId, position, roll, scale);
            _flights[i] = flight;
        }
    }

    private void Land(int cardId)
    {
        _stacks.CompleteMove(cardId);

        table.Seat(cardId, true, _stacks.TargetCount - 1);
        table.SetCounts(_stacks.SourceCount, _stacks.TargetCount);

        if (_stacks.IsComplete)
        {
            taskMessageRequested.Raise(string.Format(completedMessage, _stacks.TotalCards));
        }
    }
}
