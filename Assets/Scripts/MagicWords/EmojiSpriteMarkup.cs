using System.Text;
using TMPro;

// TMP resolves a codepoint through the whole font chain before it ever reaches a sprite asset, so
// the monochrome emoji font in the fallback list shadows every colour sprite that shares a
// codepoint. Explicit <sprite> markup outranks both, which is the only lever TMP offers here.
//
// The sheet stays the single source of truth for which emoji are in colour: the index comes from
// the same asset the markup resolves against, so adding a sprite makes that emoji colour with no
// second list to keep in step.
public static class EmojiSpriteMarkup
{
    public static string Apply(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var sprites = TMP_Settings.defaultSpriteAsset;
        StringBuilder builder = null;
        var index = 0;

        while (index < text.Length)
        {
            var isPair = char.IsHighSurrogate(text[index])
                      && index + 1 < text.Length
                      && char.IsLowSurrogate(text[index + 1]);

            var width     = isPair ? 2 : 1;
            var codepoint = isPair ? (uint)char.ConvertToUtf32(text[index], text[index + 1]) : text[index];
            var sprite    = sprites.GetSpriteIndexFromUnicode(codepoint);

            if (sprite >= 0)
            {
                // Built only once a sprite is actually found, so a line with no colour emoji —
                // most of them — is returned as it came in.
                if (builder == null)
                {
                    builder = new StringBuilder(text.Length + 16);
                    builder.Append(text, 0, index);
                }

                builder.Append("<sprite=").Append(sprite).Append('>');
            }
            else if (builder != null)
            {
                builder.Append(text, index, width);
            }

            index += width;
        }

        return builder == null ? text : builder.ToString();
    }
}
