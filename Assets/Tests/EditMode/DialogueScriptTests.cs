using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class DialogueScriptTests
{
    // Shaped like the endpoint, trimmed to the cases that matter: a speaker whose avatar is
    // on the right, a speaker the avatar list never mentions, an avatar nobody speaks for,
    // a name that appears twice, an entry whose words are only spaces, and an entry with no
    // name.
    private const string Payload = @"
    {
        ""dialogue"":
        [
            {""name"": ""Sheldon"", ""text"": ""I admit {satisfied} it is elegant.""},
            {""name"": ""Penny"", ""text"": ""It is called fun, Sheldon.""},
            {""name"": ""Neighbour"", ""text"": ""I fully agree {affirmative}""},
            {""name"": ""Sheldon"", ""text"": ""   ""},
            {""text"": ""Somebody said this.""}
        ],
        ""avatars"":
        [
            {""name"": ""Sheldon"", ""url"": ""https://example.com/sheldon.png"", ""position"": ""left""},
            {""name"": ""Penny"", ""url"": ""https://example.com/penny.png"", ""position"": ""right""},
            {""name"": ""Nobody"", ""url"": ""https://example.com/nobody.png"", ""position"": ""right""},
            {""name"": ""Sheldon"", ""url"": ""https://example.com/broken"", ""position"": ""right""}
        ]
    }";

    private static readonly string Satisfied = char.ConvertFromUtf32(0x1F60C);

    private static EmojiVocabulary Vocabulary()
    {
        return new EmojiVocabulary(new Dictionary<string, string> { { "satisfied", Satisfied } });
    }

    private static DialogueScript Parse()
    {
        return DialogueScript.FromResponse(JsonUtility.FromJson<MagicWordsResponse>(Payload), Vocabulary());
    }

    [Test]
    public void LinesKeepThePayloadOrderAndDropTheWordlessOne()
    {
        var lines = Parse().Lines;

        Assert.AreEqual(4, lines.Count);
        Assert.AreEqual("Sheldon", lines[0].Speaker);
        Assert.AreEqual("Penny", lines[1].Speaker);
        Assert.AreEqual("Neighbour", lines[2].Speaker);
    }

    [Test]
    public void TokensAreSubstitutedOnTheWayIn()
    {
        Assert.AreEqual($"I admit {Satisfied} it is elegant.", Parse().Lines[0].Text);
    }

    [Test]
    public void APositionOfRightPutsTheSpeakerOnTheRight()
    {
        Assert.AreEqual(DialogueSide.Right, Parse().Lines[1].Side);
    }

    [Test]
    public void ASpeakerWithNoAvatarEntryGetsNoUrlAndTheLeftSide()
    {
        var neighbour = Parse().Lines[2];

        Assert.IsFalse(neighbour.HasAvatar);
        Assert.IsNull(neighbour.AvatarUrl);
        Assert.AreEqual(DialogueSide.Left, neighbour.Side);
    }

    [Test]
    public void ARepeatedAvatarNameKeepsTheFirstEntry()
    {
        var sheldon = Parse().Lines[0];

        Assert.AreEqual("https://example.com/sheldon.png", sheldon.AvatarUrl);
        Assert.AreEqual(DialogueSide.Left, sheldon.Side);
    }

    [Test]
    public void ALineWithNoNameStillRenders()
    {
        var anonymous = Parse().Lines[3];

        Assert.AreEqual(string.Empty, anonymous.Speaker);
        Assert.AreEqual("Somebody said this.", anonymous.Text);
        Assert.AreEqual("?", anonymous.SpeakerInitials);
    }

    [Test]
    public void InitialsComeFromTheSpeakerName()
    {
        Assert.AreEqual("S", Parse().Lines[0].SpeakerInitials);
    }

    [Test]
    public void ABodyThatDidNotParseBecomesAnEmptyScript()
    {
        Assert.AreEqual(0, DialogueScript.FromResponse(null, Vocabulary()).Count);
    }

    [Test]
    public void APayloadWithNoDialogueBecomesAnEmptyScript()
    {
        var response = JsonUtility.FromJson<MagicWordsResponse>(@"{""avatars"": []}");

        Assert.AreEqual(0, DialogueScript.FromResponse(response, Vocabulary()).Count);
    }
}
