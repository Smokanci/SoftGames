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

    [Header("Reveal")]
    [SerializeField] [Min(0.1f)] private float charactersPerSecond = 40f;
    [SerializeField] [Min(0f)]   private float secondsBetweenLines = 0.4f;

    private readonly List<DialogueRowView>                     _rows            = new List<DialogueRowView>();
    private readonly Dictionary<string, List<DialogueRowView>> _rowsByAvatarUrl = new Dictionary<string, List<DialogueRowView>>();
    private readonly Dictionary<string, Sprite>                _spriteByUrl     = new Dictionary<string, Sprite>();

    // Runs for as long as the conversation takes to type itself. The caller owns the failure
    // path, and the scene unloading cancels this partway through.
    public async Awaitable Show(IReadOnlyList<DialogueLine> lines)
    {
        Clear();

        // Every avatar starts downloading before the first line types, not when its row appears,
        // so a portrait has the whole reveal to land in. One request per distinct URL rather than
        // per row: a speaker has as many rows as they have lines, and they all want the same image.
        foreach (var line in lines)
        {
            if (line.HasAvatar && !_rowsByAvatarUrl.ContainsKey(line.AvatarUrl))
            {
                _ = FillAvatars(line.AvatarUrl, RowsFor(line.AvatarUrl));
            }
        }

        foreach (var line in lines)
        {
            var row = Instantiate(rowPrefab, content);
            row.Bind(line);
            _rows.Add(row);

            if (line.HasAvatar)
            {
                ShowAvatarOn(row, line.AvatarUrl);
            }

            ScrollToNewest();

            await row.Reveal(charactersPerSecond, destroyCancellationToken);
            await Awaitable.WaitForSecondsAsync(secondsBetweenLines, destroyCancellationToken);
        }
    }

    public void Clear()
    {
        foreach (var row in _rows)
        {
            Destroy(row.gameObject);
        }

        _rows.Clear();
        _rowsByAvatarUrl.Clear();
        _spriteByUrl.Clear();
    }

    // A row appears partway through the conversation, so its image has either landed already or
    // is still in flight. The two cases are the same picture and different bookkeeping.
    private void ShowAvatarOn(DialogueRowView row, string url)
    {
        if (_spriteByUrl.TryGetValue(url, out var sprite))
        {
            row.SetAvatar(sprite);
            return;
        }

        RowsFor(url).Add(row);
    }

    private void ScrollToNewest()
    {
        // The row was built this frame, so the scroll position would otherwise be measured
        // against content that does not include it yet.
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        // Until the log outgrows the viewport there is nothing hidden to scroll to, and the
        // setter would push the content off its anchor rather than do nothing.
        if (content.rect.height > scroll.viewport.rect.height)
        {
            scroll.verticalNormalizedPosition = 0f;
        }
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

    private async Awaitable FillAvatars(string url, List<DialogueRowView> waiting)
    {
        // Nobody awaits this, and an Awaitable nobody awaits swallows its exception.
        try
        {
            var sprite = await avatars.Load(url);

            // Null means the image is unavailable, which the rows already render as initials. The
            // waiting list empties either way, so a list with rows in it always means a fetch that
            // is still in flight.
            if (sprite != null)
            {
                _spriteByUrl[url] = sprite;

                foreach (var row in waiting)
                {
                    row.SetAvatar(sprite);
                }
            }

            waiting.Clear();
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
