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

    // Formatted with the card count, so the wording cannot drift from the deck size.
    [SerializeField] private string completedMessage = "All {0} cards moved.";

    private readonly List<Flight> _flights = new List<Flight>(4);

    private CardStacks _stacks;
    private float      _sinceLastMove;

    private void Start()
    {
        _stacks = new CardStacks(cardCount);
        table.Build(cardCount);
        Restart();
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

    // Called by the GameEventListenerVoid on this GameObject, and by Start for the first run.
    public void Restart()
    {
        _stacks.Reset();
        _flights.Clear();

        // Primed, so the first card leaves on the first frame instead of after a dead second.
        _sinceLastMove = moveInterval;

        table.SeatAll();
        table.SetCounts(_stacks.SourceCount, _stacks.TargetCount);
        taskMessageRequested.Raise(string.Empty);
    }

    private void BeginMove()
    {
        var cardId = _stacks.BeginMove();

        table.Lift(cardId);
        table.SetCounts(_stacks.SourceCount, _stacks.TargetCount);

        _flights.Add(new Flight
        {
            CardId    = cardId,
            FromIndex = _stacks.SourceCount,
            ToIndex   = _stacks.TargetCount,
            Elapsed   = 0f,
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
            var position = Vector3.Lerp(from, to, t);
            position.y += arcHeight * Mathf.Sin(t * Mathf.PI);

            table.SetFlightPosition(flight.CardId, position);
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
