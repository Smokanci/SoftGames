using System;
using System.Collections.Generic;
using System.Text;

// Replaces a {token} in the dialogue with a Unicode emoji. Plain C# with no Unity types, so
// an EditMode test can assert the substituted string without a scene or a font.
public sealed class EmojiVocabulary
{
    private readonly Dictionary<string, string> _emojiByToken;

    // Copied rather than held, so the case rule belongs to the vocabulary and not to whoever
    // built the table.
    public EmojiVocabulary(IEnumerable<KeyValuePair<string, string>> emojiByToken)
    {
        if (emojiByToken == null)
        {
            throw new ArgumentNullException(nameof(emojiByToken));
        }

        _emojiByToken = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in emojiByToken)
        {
            _emojiByToken[pair.Key] = pair.Value;
        }
    }

    // An unrecognised token is left exactly as it was written. The endpoint is a mock whose
    // token set can change, and a visible {shrug} says the table is short — a silent drop
    // would leave a sentence that reads fine and is missing a word nobody knows about.
    public string Substitute(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (text.IndexOf('{') < 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        var index   = 0;

        while (index < text.Length)
        {
            var open  = text.IndexOf('{', index);
            var close = open < 0 ? -1 : text.IndexOf('}', open + 1);

            if (close < 0)
            {
                builder.Append(text, index, text.Length - index);
                break;
            }

            builder.Append(text, index, open - index);

            var token = text.Substring(open + 1, close - open - 1);
            if (_emojiByToken.TryGetValue(token, out var emoji))
            {
                builder.Append(emoji);
            }
            else
            {
                builder.Append(text, open, close - open + 1);
            }

            index = close + 1;
        }

        return builder.ToString();
    }
}
