using System.Collections.Generic;
using NUnit.Framework;

public class EmojiVocabularyTests
{
    private static readonly string Laughing = char.ConvertFromUtf32(0x1F602);
    private static readonly string Neutral  = char.ConvertFromUtf32(0x1F610);

    private static EmojiVocabulary Vocabulary()
    {
        return new EmojiVocabulary(new Dictionary<string, string>
        {
            { "laughing", Laughing },
            { "neutral", Neutral },
        });
    }

    [Test]
    public void AKnownTokenBecomesItsEmoji()
    {
        Assert.AreEqual($"Fine. {Laughing} Happy?", Vocabulary().Substitute("Fine. {laughing} Happy?"));
    }

    [Test]
    public void EveryTokenOnTheLineIsSubstituted()
    {
        Assert.AreEqual($"{Neutral} then {Laughing}", Vocabulary().Substitute("{neutral} then {laughing}"));
    }

    [Test]
    public void AnUnknownTokenKeepsItsBraces()
    {
        Assert.AreEqual($"{Laughing} and {{shrug}}", Vocabulary().Substitute("{laughing} and {shrug}"));
    }

    [Test]
    public void TokenLookupIgnoresCase()
    {
        Assert.AreEqual(Laughing, Vocabulary().Substitute("{LAUGHING}"));
    }

    [Test]
    public void AnUnclosedBraceIsLeftAlone()
    {
        Assert.AreEqual("half a {token", Vocabulary().Substitute("half a {token"));
    }

    [Test]
    public void MissingTextBecomesTheEmptyString()
    {
        Assert.AreEqual(string.Empty, Vocabulary().Substitute(null));
    }
}
