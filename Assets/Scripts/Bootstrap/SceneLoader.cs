using UnityEngine;
using UnityEngine.SceneManagement;

// Lives in the bootstrap scene, which is never unloaded. Menu and task scenes talk to it
// through SOAP events rather than a reference, because a scene that is not loaded yet
// cannot hold a serialized ref to one that is.
public sealed class SceneLoader : MonoBehaviour
{
    [SerializeField] private GameEventString loadSceneRequested;
    [SerializeField] private BoolVariable    isLoading;
    [SerializeField] private string          firstSceneName;

    private string _current;

    private void Start()
    {
        _ = SwapTo(firstSceneName);
    }

    private void OnEnable()
    {
        loadSceneRequested.EventListeners += OnLoadRequested;
    }

    private void OnDisable()
    {
        loadSceneRequested.EventListeners -= OnLoadRequested;
    }

    private void OnLoadRequested(string sceneName) => _ = SwapTo(sceneName);

    // A second request while one is in flight would unload a scene that is still loading.
    private async Awaitable SwapTo(string sceneName)
    {
        if (isLoading.Value)
        {
            return;
        }

        isLoading.Value = true;

        // A throw between here and the reset would leave the flag set and the guard above
        // would then swallow every later request, so the whole menu would stop responding
        // with nothing in the log. A misnamed scene is enough to get there.
        try
        {
            if (!string.IsNullOrEmpty(_current))
            {
                await Finish(SceneManager.UnloadSceneAsync(_current));
                _current = null;
            }

            if (!string.IsNullOrEmpty(sceneName))
            {
                await Finish(SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive));
                // Without this the bootstrap scene stays active, and anything the new scene
                // instantiates is parented into a scene that never unloads.
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
                _current = sceneName;
            }
        }
        // An Awaitable nobody awaits swallows its exception, so a broken wire on this path
        // would otherwise leave no trace at all.
        catch (System.OperationCanceledException)
        {
            // The bootstrap scene is going away. Nothing to report.
        }
        catch (System.Exception e)
        {
            Debug.LogException(e, this);
        }
        finally
        {
            isLoading.Value = false;
        }
    }

    private async Awaitable Finish(AsyncOperation operation)
    {
        while (!operation.isDone)
        {
            await Awaitable.NextFrameAsync(destroyCancellationToken);
        }
    }
}
