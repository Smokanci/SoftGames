using System;
using System.Collections.Generic;

public sealed class CardStacks
{
    private readonly int       _totalCards;
    private readonly List<int> _source;
    private readonly List<int> _inFlight;
    private readonly List<int> _target;

    public CardStacks(int totalCards)
    {
        if (totalCards < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCards), totalCards, "A deck needs at least one card.");
        }

        _totalCards = totalCards;
        _source     = new List<int>(totalCards);
        _inFlight   = new List<int>(4);
        _target     = new List<int>(totalCards);

        Reset();
    }

    public int TotalCards    => _totalCards;
    public int SourceCount   => _source.Count;
    public int InFlightCount => _inFlight.Count;
    public int TargetCount   => _target.Count;

    public bool CanBegin => _source.Count > 0;

    // A card in the air belongs to neither stack, so this cannot read as finished while
    // one is still travelling — the only reason the in-flight place exists at all.
    public bool IsComplete => _target.Count == _totalCards;

    public int BeginMove()
    {
        if (_source.Count == 0)
        {
            throw new InvalidOperationException("The source stack is empty.");
        }

        var cardId = _source[_source.Count - 1];
        _source.RemoveAt(_source.Count - 1);
        _inFlight.Add(cardId);

        return cardId;
    }

    public void CompleteMove(int cardId)
    {
        if (!_inFlight.Remove(cardId))
        {
            throw new ArgumentException($"Card {cardId} is not in the air.", nameof(cardId));
        }

        _target.Add(cardId);
    }

    public void Reset()
    {
        _source.Clear();
        _inFlight.Clear();
        _target.Clear();

        for (var i = 0; i < _totalCards; i++)
        {
            _source.Add(i);
        }
    }
}
