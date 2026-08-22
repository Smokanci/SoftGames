using System;

public enum DialogueSide
{
    Left,
    Right,
}

// One rendered line. Every field is settled by the time the object exists, so the view never
// has to ask what the payload did or did not carry.
public sealed class DialogueLine
{
    public DialogueLine(string speaker, string text, string avatarUrl, DialogueSide side)
    {
        Speaker         = speaker;
        Text            = text;
        AvatarUrl       = avatarUrl;
        Side            = side;
        SpeakerInitials = InitialsOf(speaker);
    }

    public string Speaker { get; }

    // Emoji already substituted.
    public string Text { get; }

    // Null when the payload names no avatar for this speaker, or names one with no URL.
    public string AvatarUrl { get; }

    public DialogueSide Side { get; }

    // Drawn in place of the portrait, both before it arrives and when it never does.
    public string SpeakerInitials { get; }

    public bool HasAvatar => !string.IsNullOrEmpty(AvatarUrl);

    private static string InitialsOf(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return "?";
        }

        var words    = speaker.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initials = words[0].Substring(0, 1);

        if (words.Length > 1)
        {
            initials += words[1].Substring(0, 1);
        }

        return initials.ToUpperInvariant();
    }
}
