using NUnit.Framework;

// Asserts against the project's own sprite sheet, because the index the markup emits is only
// correct against the asset TMP will resolve it with. Satisfied is in the sheet; the clef is a
// non-emoji astral codepoint, so nothing will ever add a sprite for it.
public class EmojiSpriteMarkupTests
{
    private static readonly string Satisfied = char.ConvertFromUtf32(0x1F60C);
    private static readonly string Clef      = char.ConvertFromUtf32(0x1D11E);

    [Test]
    public void AnEmojiTheSheetHasBecomesSpriteMarkup()
    {
        var applied = EmojiSpriteMarkup.Apply(Satisfied);

        StringAssert.Contains("<sprite=", applied);
        StringAssert.DoesNotContain(Satisfied, applied);
    }

    [Test]
    public void AnEmojiTheSheetLacksIsLeftAlone()
    {
        Assert.AreEqual($"ok {Clef} end", EmojiSpriteMarkup.Apply($"ok {Clef} end"));
    }

    [Test]
    public void TheTextAroundASpriteSurvives()
    {
        var applied = EmojiSpriteMarkup.Apply($"before {Satisfied} after");

        StringAssert.StartsWith("before ", applied);
        StringAssert.EndsWith(" after", applied);
    }

    [Test]
    public void OneLineTakesBothResolvers()
    {
        var applied = EmojiSpriteMarkup.Apply($"{Satisfied}{Clef}");

        StringAssert.Contains("<sprite=", applied);
        StringAssert.Contains(Clef, applied);
    }

    // The no-emoji line is the common one, and returning the same reference is what says no
    // builder was allocated for it.
    [Test]
    public void ALineWithNoEmojiComesBackAsItWent()
    {
        const string Line = "Nothing to rewrite here.";

        Assert.AreSame(Line, EmojiSpriteMarkup.Apply(Line));
    }
}
