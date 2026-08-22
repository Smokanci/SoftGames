using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Downloads each avatar once and keeps it until the scene unloads. A URL that fails is
// remembered as failed, so a dead link costs one request rather than one per line its
// speaker has.
public sealed class AvatarLibrary : MonoBehaviour
{
    // Without it an unreachable host holds the row on its initials until the platform's own
    // timeout expires, which on WebGL is the browser's and can be minutes.
    [SerializeField] private int requestTimeoutSeconds = 10;

    private readonly Dictionary<string, Sprite> _loaded      = new Dictionary<string, Sprite>();
    private readonly HashSet<string>            _unavailable = new HashSet<string>();

    // Returns null when the image cannot be shown, whatever the reason. The caller draws its
    // own fallback and never learns which failure it was.
    public async Awaitable<Sprite> Load(string url)
    {
        if (_loaded.TryGetValue(url, out var cached))
        {
            return cached;
        }

        if (_unavailable.Contains(url))
        {
            return null;
        }

        using var request = UnityWebRequestTexture.GetTexture(url);
        request.timeout = requestTimeoutSeconds;

        await WebRequests.SendAsync(request, destroyCancellationToken);

        // Covers a refused connection, a 4xx, and a 200 whose body is not an image. The payload
        // carries the first two, on URLs no speaker reaches.
        if (request.result != UnityWebRequest.Result.Success)
        {
            _unavailable.Add(url);
            Debug.LogWarning($"Avatar unavailable ({request.error}): {url}", this);
            return null;
        }

        var texture = DownloadHandlerTexture.GetContent(request);
        var sprite  = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        _loaded.Add(url, sprite);
        return sprite;
    }

    private void OnDestroy()
    {
        // Downloaded textures belong to no scene, so unloading this one does not take them.
        foreach (var sprite in _loaded.Values)
        {
            Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}
