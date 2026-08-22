using System;
using NUnit.Framework;

public class CardStacksTests
{
    private const int DeckSize = 144;

    [Test]
    public void EveryCardStartsOnTheSourceStack()
    {
        var stacks = new CardStacks(DeckSize);

        Assert.AreEqual(DeckSize, stacks.SourceCount);
        Assert.AreEqual(0, stacks.TargetCount);
        Assert.IsFalse(stacks.IsComplete);
    }

    [Test]
    public void ALiftedCardCountsOnNeitherStack()
    {
        var stacks = new CardStacks(DeckSize);

        stacks.BeginMove();

        Assert.AreEqual(DeckSize - 1, stacks.SourceCount);
        Assert.AreEqual(0, stacks.TargetCount);
        Assert.AreEqual(1, stacks.InFlightCount);
    }

    [Test]
    public void ALandedCardCountsOnTheTargetStack()
    {
        var stacks = new CardStacks(DeckSize);

        stacks.CompleteMove(stacks.BeginMove());

        Assert.AreEqual(DeckSize - 1, stacks.SourceCount);
        Assert.AreEqual(1, stacks.TargetCount);
        Assert.AreEqual(0, stacks.InFlightCount);
    }

    [Test]
    public void TheRunIsNotCompleteWhileTheLastCardIsStillInTheAir()
    {
        var stacks = new CardStacks(DeckSize);
        for (var i = 0; i < DeckSize - 1; i++)
        {
            stacks.CompleteMove(stacks.BeginMove());
        }

        stacks.BeginMove();

        Assert.AreEqual(0, stacks.SourceCount);
        Assert.IsFalse(stacks.CanBegin);
        Assert.IsFalse(stacks.IsComplete);
    }

    [Test]
    public void TheRunCompletesWhenTheLastCardLands()
    {
        var stacks = new CardStacks(DeckSize);
        for (var i = 0; i < DeckSize; i++)
        {
            stacks.CompleteMove(stacks.BeginMove());
        }

        Assert.AreEqual(DeckSize, stacks.TargetCount);
        Assert.IsTrue(stacks.IsComplete);
    }

    [Test]
    public void CardsLeaveTheSourceFromTheTopDown()
    {
        var stacks = new CardStacks(DeckSize);

        Assert.AreEqual(DeckSize - 1, stacks.BeginMove());
        Assert.AreEqual(DeckSize - 2, stacks.BeginMove());
    }

    [Test]
    public void LiftingFromAnEmptySourceThrows()
    {
        var stacks = new CardStacks(1);
        stacks.BeginMove();

        Assert.Throws<InvalidOperationException>(() => stacks.BeginMove());
    }

    [Test]
    public void LandingACardThatIsNotInTheAirThrows()
    {
        var stacks = new CardStacks(DeckSize);

        Assert.Throws<ArgumentException>(() => stacks.CompleteMove(0));
    }

    [Test]
    public void ResetReturnsEveryCardToTheSource()
    {
        var stacks = new CardStacks(DeckSize);
        for (var i = 0; i < DeckSize; i++)
        {
            stacks.CompleteMove(stacks.BeginMove());
        }

        stacks.Reset();

        Assert.AreEqual(DeckSize, stacks.SourceCount);
        Assert.AreEqual(0, stacks.TargetCount);
        Assert.IsFalse(stacks.IsComplete);
    }

    [Test]
    public void RejectsAnEmptyDeck()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CardStacks(0));
    }
}
