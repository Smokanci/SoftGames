using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

public static class WebRequests
{
    // UnityWebRequestAsyncOperation is not awaitable, so the wait is a frame poll — the same
    // shape SceneLoader uses for AsyncOperation. The token is the caller's
    // destroyCancellationToken, so a scene unloaded mid-request stops here instead of
    // resuming into a destroyed component.
    public static async Awaitable SendAsync(UnityWebRequest request, CancellationToken token)
    {
        var operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            await Awaitable.NextFrameAsync(token);
        }
    }
}
