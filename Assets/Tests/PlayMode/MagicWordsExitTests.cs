using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MagicWordsExitTests
{
    private const float LoadTimeoutSeconds = 10f;
    private const float LeakWatchSeconds   = 3f;

    // Leaving mid-fetch cancels the payload request and every avatar download through
    // destroyCancellationToken. A missed cancellation produces no wrong value — it resumes a
    // continuation inside a destroyed component, which surfaces only as a logged exception. The
    // test framework already fails a test on a logged error or exception, so that half of the
    // assertion is implicit — LogAssert.NoUnexpectedReceived would not tighten it, it would
    // instead fail on every Debug.Log the engine writes during a scene load.
    [UnityTest]
    public IEnumerator LeavingMagicWordsMidFetchCancelsItQuietly()
    {
        yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
        yield return WaitForActiveScene("Menu");

        Click("MagicWordsButton");
        yield return WaitForActiveScene("MagicWords");

        // One frame is all Start needs to issue the request, and far less than a remote HTTPS GET
        // needs to answer, so the back press below lands while the request is still in flight.
        yield return null;

        Click("BackButton");
        yield return WaitForActiveScene("Menu");

        // A continuation that outlived its component resumes when its request answers, which is a
        // network delay and not a frame count — so the watch is a realtime wait. Overshooting only
        // costs seconds; undershooting lets the exception land after the test has already passed,
        // where the runner blames whatever ran next.
        var watchUntil = Time.realtimeSinceStartup + LeakWatchSeconds;
        while (Time.realtimeSinceStartup < watchUntil)
        {
            yield return null;
        }

        Assert.IsFalse(SceneManager.GetSceneByName("MagicWords").isLoaded, "The task scene never unloaded.");
        Assert.IsTrue(SceneManager.GetSceneByName("Bootstrap").isLoaded, "The bootstrap scene went away with the task scene.");
    }

    // SceneLoader sets the active scene as the last step of a swap, so this waits for the whole
    // swap and not just the load. Waiting on isLoaded instead would let the next click arrive
    // while the loader still holds its in-flight guard, which drops that click silently.
    private static IEnumerator WaitForActiveScene(string sceneName)
    {
        var deadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
        while (SceneManager.GetActiveScene().name != sceneName && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name, "The scene swap never finished.");
    }

    private static void Click(string buttonName)
    {
        var button = GameObject.Find(buttonName);
        Assert.IsNotNull(button, $"No active GameObject named {buttonName} in the loaded scenes.");
        button.GetComponent<Button>().onClick.Invoke();
    }

    // The runner does not reset scene state between tests, so without this the next PlayMode test
    // would start inside the menu with the bootstrap services still running, and would pass or
    // fail depending on the order the runner happened to pick.
    [UnityTearDown]
    public IEnumerator LeaveAnEmptyScene()
    {
        var blank = SceneManager.CreateScene("MagicWordsExitTeardown");
        SceneManager.SetActiveScene(blank);

        for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene != blank && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
