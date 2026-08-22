using System;
using UnityEngine;
using UnityEngine.Networking;

public sealed class MagicWordsRunner : MonoBehaviour
{
    [SerializeField] private string          endpointUrl;
    [SerializeField] private int             requestTimeoutSeconds = 15;
    [SerializeField] private EmojiTable      emojiTable;
    [SerializeField] private DialogueLogView log;
    [SerializeField] private GameEventString taskMessageRequested;

    [Header("Messages")]
    [SerializeField] private string loadingMessage;
    [SerializeField] private string emptyMessage;
    // A body that is not JSON is a different answer from a payload with no lines, and only the
    // console could tell them apart while they shared one message.
    [SerializeField] private string unreadableMessage;
    // Formatted with the reason the request gave, so the banner says what went wrong.
    [SerializeField] private string failedMessage;

    private void Start()
    {
        _ = Load();
    }

    private async Awaitable Load()
    {
        try
        {
            log.Clear();
            taskMessageRequested.Raise(loadingMessage);

            using var request = UnityWebRequest.Get(endpointUrl);
            request.timeout = requestTimeoutSeconds;

            await WebRequests.SendAsync(request, destroyCancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
            {
                taskMessageRequested.Raise(string.Format(failedMessage, request.error));
                return;
            }

            if (!TryParse(request.downloadHandler.text, out var script, out var problem))
            {
                taskMessageRequested.Raise(string.Format(unreadableMessage, problem));
                return;
            }

            if (script.Count == 0)
            {
                taskMessageRequested.Raise(emptyMessage);
                return;
            }

            taskMessageRequested.Raise(string.Empty);
            log.Show(script.Lines);
        }
        catch (OperationCanceledException)
        {
            // The scene is unloading.
        }
        // An Awaitable nobody awaits swallows its exception, so a throw on this path would
        // otherwise leave the loading banner up and nothing in the log.
        catch (Exception e)
        {
            Debug.LogException(e, this);
            taskMessageRequested.Raise(string.Format(failedMessage, e.Message));
        }
    }

    // False when the body is not JSON at all, which includes an empty body. A script with no
    // lines is the other answer: the payload read fine and carried nothing to draw. The reason
    // comes back with it because a build has no console to read it in.
    private bool TryParse(string json, out DialogueScript script, out string problem)
    {
        try
        {
            var response = JsonUtility.FromJson<MagicWordsResponse>(json);
            script  = DialogueScript.FromResponse(response, emojiTable.CreateVocabulary());
            problem = string.Empty;
            return true;
        }
        catch (ArgumentException e)
        {
            Debug.LogWarning($"Magic Words payload did not parse: {e.Message}", this);
            script  = DialogueScript.Empty;
            problem = e.Message;
            return false;
        }
    }
}
