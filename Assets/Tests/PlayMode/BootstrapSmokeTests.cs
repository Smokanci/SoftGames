using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class BootstrapSmokeTests
{
    private const float LoadTimeoutSeconds = 10f;

    [UnityTest]
    public IEnumerator BootstrapBringsUpTheMenuWithoutUnloadingItself()
    {
        yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);

        var deadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
        while (!SceneManager.GetSceneByName("Menu").isLoaded && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Assert.IsTrue(SceneManager.GetSceneByName("Menu").isLoaded, "SceneLoader never brought up the menu.");
        Assert.IsTrue(SceneManager.GetSceneByName("Bootstrap").isLoaded, "The menu replaced the bootstrap scene instead of loading on top of it.");
    }

    // The runner does not reset scene state between tests, so without this the next PlayMode test
    // would start inside the menu with the bootstrap services still running, and would pass or fail
    // depending on the order the runner happened to pick.
    [UnityTearDown]
    public IEnumerator LeaveAnEmptyScene()
    {
        var blank = SceneManager.CreateScene("BootstrapSmokeTeardown");
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
