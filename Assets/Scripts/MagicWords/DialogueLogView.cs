using System;
using System.Collections;
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
    [SerializeField] [Min(0.1f)]  private float charactersPerSecond = 40f;
    [SerializeField] [Min(0f)]    private float secondsBetweenLines = 0.4f;

    [Header("Scroll")]
    [SerializeField] [Min(0.01f)] private float slideSmoothTime = 0.18f;

    private readonly List<DialogueRowView>                     _rows            = new List<DialogueRowView>();
    private readonly Dictionary<string, List<DialogueRowView>> _rowsByAvatarUrl = new Dictionary<string, List<DialogueRowView>>();
    private readonly Dictionary<string, Sprite>                _spriteByUrl     = new Dictionary<string, Sprite>();

    private Coroutine _slide;
    private float     _slideVelocity;

    private float HiddenHeight     => content.rect.height - scroll.viewport.rect.height;
    private float DistanceToBottom => scroll.verticalNormalizedPosition * HiddenHeight;

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

        // Until the log outgrows the viewport there is nothing hidden to scroll to. A slide
        // already running needs no second one: it reads the new bottom on its next frame.
        if (HiddenHeight > 0f && _slide == null)
        {
            _slide = StartCoroutine(SlideToNewest());
        }
    }

    // Measures the distance to the bottom every frame instead of tweening to a position captured
    // up front. The next row lands while this is still running and pushes the bottom further down,
    // and the smoothing carries on into the longer distance rather than restarting against it.
    private IEnumerator SlideToNewest()
    {
        while (DistanceToBottom > 1f)
        {
            yield return null;

            var hidden = HiddenHeight;

            // Clearing the log shrinks the content back inside the viewport, which leaves no
            // distance to divide by and nothing left to slide towards.
            if (hidden <= 0f)
            {
                break;
            }

            var distance = Mathf.SmoothDamp(DistanceToBottom, 0f, ref _slideVelocity, slideSmoothTime);

            scroll.verticalNormalizedPosition = distance / hidden;
        }

        if (HiddenHeight > 0f)
        {
            scroll.verticalNormalizedPosition = 0f;
        }

        _slideVelocity = 0f;
        _slide         = null;
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
