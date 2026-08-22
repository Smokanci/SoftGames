using System;
using UnityEngine;

// JsonUtility fills these, so the field names are the endpoint's keys and cannot be renamed.
// A key the payload omits arrives as null or an empty string. Turning that into a decision is
// DialogueScript's job, which is why nothing downstream of it has to ask whether a field was
// present.
[Serializable]
public sealed class MagicWordsResponse
{
    [SerializeField] private DialogueEntry[] dialogue;
    [SerializeField] private AvatarEntry[]   avatars;

    public DialogueEntry[] Dialogue => dialogue;
    public AvatarEntry[]   Avatars  => avatars;
}

[Serializable]
public sealed class DialogueEntry
{
    [SerializeField] private string name;
    [SerializeField] private string text;

    public string Name => name;
    public string Text => text;
}

[Serializable]
public sealed class AvatarEntry
{
    [SerializeField] private string name;
    [SerializeField] private string url;
    [SerializeField] private string position;

    public string Name     => name;
    public string Url      => url;
    public string Position => position;
}
