using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PhoenixFlameColorTests
{
    private const float LoadTimeoutSeconds  = 10f;
    private const float BlendTimeoutSeconds = 5f;

    // The loop back to orange is the half of the brief that a three-state machine gets wrong most
    // easily, so the third press is the assertion that matters. Driving it through the button
    // rather than through Animator.SetTrigger keeps the SOAP hop and the listener wiring inside
    // the test — a broken persistent call would otherwise pass.
    [UnityTest]
    public IEnumerator PressingTheButtonWalksOrangeGreenBlueAndBackToOrange()
    {
        yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
        yield return WaitForActiveScene("Menu");

        Click("PhoenixFlameButton");
        yield return WaitForActiveScene("PhoenixFlame");

        var animator = GameObject.Find("Flame").GetComponent<Animator>();
        var body     = GameObject.Find("Flame").transform.Find("Body").GetComponent<ParticleSystem>();

        yield return WaitForState(animator, "Orange");
        var orange = body.main.startColor.color;

        Click("ColorButton");
        yield return WaitForState(animator, "Green");
        var green = body.main.startColor.color;

        Click("ColorButton");
        yield return WaitForState(animator, "Blue");
        var blue = body.main.startColor.color;

        Click("ColorButton");
        yield return WaitForState(animator, "Orange");

        // The states could cycle correctly with the clips unbound from FlameTint, or with FlameTint
        // never reaching the emitters. Each state's own channel leading is what proves the colour
        // travelled all the way to the fire.
        Assert.Greater(orange.r, orange.g, "The orange state did not reach the emitters.");
        Assert.Greater(green.g,  green.r,  "The green state did not reach the emitters.");
        Assert.Greater(blue.b,   blue.g,   "The blue state did not reach the emitters.");
    }

    // A trigger is consumed on the next Animator evaluation and the blend runs for a fixed time
    // after that, so the settled state is what to wait for: current is still the outgoing state
    // for the whole transition.
    private static IEnumerator WaitForState(Animator animator, string stateName)
    {
        var deadline = Time.realtimeSinceStartup + BlendTimeoutSeconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (!animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            {
                break;
            }

            yield return null;
        }

        Assert.IsFalse(animator.IsInTransition(0), $"The blend into {stateName} never finished.");
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName(stateName), $"The animator settled somewhere other than {stateName}.");

        // FlameTint pushes the animated value in LateUpdate, so the emitters carry the settled
        // colour one frame after the animator does.
        yield return null;
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
    // would start inside the task scene with the bootstrap services still running, and would pass
    // or fail depending on the order the runner happened to pick.
    [UnityTearDown]
    public IEnumerator LeaveAnEmptyScene()
    {
        var blank = SceneManager.CreateScene("PhoenixFlameColorTeardown");
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
