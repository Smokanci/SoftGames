using System;
using System.Collections.Generic;
using UnityEngine;

// The token-to-emoji mapping is authored, not fetched: the v3 payload names emotions and
// leaves the choice of glyph to the client. Keeping it in an asset means retuning a face
// costs an inspector edit rather than a rebuild.
[CreateAssetMenu(fileName = "EmojiTable", menuName = "SoftGames/Emoji Table")]
public sealed class EmojiTable : ScriptableObject
{
    [Serializable]
    private struct Entry
    {
        [SerializeField] private string token;
        [SerializeField] private string emoji;

        public string Token => token;
        public string Emoji => emoji;
    }

    [SerializeField] private Entry[] entries;

    public EmojiVocabulary CreateVocabulary()
    {
        var emojiByToken = new Dictionary<string, string>(entries.Length);

        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(entry.Token))
            {
                continue;
            }

            emojiByToken[entry.Token] = entry.Emoji;
        }

        return new EmojiVocabulary(emojiByToken);
    }
}
