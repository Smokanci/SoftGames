using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueLogView : MonoBehaviour
{
    [SerializeField] private ScrollRect      scroll;
    [SerializeField] private RectTransform   content;
    [SerializeField] private DialogueRowView rowPrefab;
    [SerializeField] private AvatarLibrary   avatars;

    private readonly List<DialogueRowView>                     _rows            = new List<DialogueRowView>();
    private readonly Dictionary<string, List<DialogueRowView>> _rowsByAvatarUrl = new Dictionary<string, List<DialogueRowView>>();

    public void Show(IReadOnlyList<DialogueLine> lines)
    {
        Clear();

        foreach (var line in lines)
        {
            var row = Instantiate(rowPrefab, content);
            row.Bind(line);
            _rows.Add(row);

            if (line.HasAvatar)
            {
                RowsFor(line.AvatarUrl).Add(row);
            }
        }

        // One request per distinct URL rather than per row: a speaker has as many rows as
        // they have lines, and they all want the same image.
        foreach (var pair in _rowsByAvatarUrl)
        {
            _ = FillAvatars(pair.Key, pair.Value);
        }

        scroll.verticalNormalizedPosition = 1f;
    }

    public void Clear()
    {
        foreach (var row in _rows)
        {
            Destroy(row.gameObject);
        }

        _rows.Clear();
        _rowsByAvatarUrl.Clear();
    }

    private List<DialogueRowView> RowsFor(string url)
    {
        if (!_rowsByAvatarUrl.TryGetValue(url, out var rows))
        {
            rows = new List<DialogueRowView>();
            _rowsByAvatarUrl.Add(url, rows);
        }

        return rows;
    }

    private async Awaitable FillAvatars(string url, List<DialogueRowView> rows)
    {
        // Nobody awaits this, and an Awaitable nobody awaits swallows its exception.
        try
        {
            var sprite = await avatars.Load(url);

            // Null means the image is unavailable, which the rows already render as initials.
            if (sprite == null)
            {
                return;
            }

            foreach (var row in rows)
            {
                row.SetAvatar(sprite);
            }
        }
        catch (OperationCanceledException)
        {
            // The scene is unloading.
        }
        catch (Exception e)
        {
            Debug.LogException(e, this);
        }
    }
}
