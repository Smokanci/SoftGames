using System;
using System.Collections.Generic;

// The boundary between "whatever the endpoint returned" and a model the view can trust.
// Everything the payload can omit is decided here, once, and the result is either a whole
// line or no line at all.
public sealed class DialogueScript
{
    public static readonly DialogueScript Empty = new DialogueScript(Array.Empty<DialogueLine>());

    private readonly IReadOnlyList<DialogueLine> _lines;

    private DialogueScript(IReadOnlyList<DialogueLine> lines)
    {
        _lines = lines;
    }

    public IReadOnlyList<DialogueLine> Lines => _lines;

    public int Count => _lines.Count;

    // A null response is what a body that did not parse looks like from here.
    public static DialogueScript FromResponse(MagicWordsResponse response, EmojiVocabulary vocabulary)
    {
        if (response == null || response.Dialogue == null)
        {
            return Empty;
        }

        var avatarsByName = IndexAvatars(response.Avatars);
        var lines         = new List<DialogueLine>(response.Dialogue.Length);

        foreach (var entry in response.Dialogue)
        {
            // A line with no words is nothing to draw. A line with no speaker still is.
            if (entry == null || string.IsNullOrWhiteSpace(entry.Text))
            {
                continue;
            }

            var speaker = string.IsNullOrEmpty(entry.Name) ? string.Empty : entry.Name;
            avatarsByName.TryGetValue(speaker, out var avatar);

            lines.Add(new DialogueLine(
                speaker,
                vocabulary.Substitute(entry.Text),
                UrlOf(avatar),
                SideOf(avatar)));
        }

        return lines.Count == 0 ? Empty : new DialogueScript(lines);
    }

    private static Dictionary<string, AvatarEntry> IndexAvatars(AvatarEntry[] avatars)
    {
        var byName = new Dictionary<string, AvatarEntry>(StringComparer.OrdinalIgnoreCase);

        if (avatars == null)
        {
            return byName;
        }

        foreach (var avatar in avatars)
        {
            if (avatar == null || string.IsNullOrEmpty(avatar.Name))
            {
                continue;
            }

            // First entry wins. A repeated name is a conflict in the data, and nothing makes
            // a later record more trustworthy than the one before it.
            if (!byName.ContainsKey(avatar.Name))
            {
                byName.Add(avatar.Name, avatar);
            }
        }

        return byName;
    }

    private static string UrlOf(AvatarEntry avatar)
    {
        if (avatar == null || string.IsNullOrEmpty(avatar.Url))
        {
            return null;
        }

        return avatar.Url;
    }

    private static DialogueSide SideOf(AvatarEntry avatar)
    {
        // A speaker the avatar list never mentions still has to be placed somewhere, and the
        // left is where a chat layout puts the party that is not you.
        if (avatar == null)
        {
            return DialogueSide.Left;
        }

        return string.Equals(avatar.Position, "right", StringComparison.OrdinalIgnoreCase)
            ? DialogueSide.Right
            : DialogueSide.Left;
    }
}
