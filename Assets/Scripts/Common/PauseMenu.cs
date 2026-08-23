using UnityEngine;
using UnityEngine.InputSystem;

// Lives on TaskChrome rather than in the bootstrap scene. That prefab is in all three task scenes
// and in none of the menu, so "the menu cannot be paused" needs no flag to say so.
//
// Sits on a GameObject that stays active and toggles a child, for the same reason
// TaskMessageBanner does: a component that switched itself off would stop reading the keyboard,
// and the overlay could then never be dismissed.
public sealed class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject      panel;
    [SerializeField] private EmberButtonView pauseButton;
    [SerializeField] private EmberButtonView resumeButton;

    // The backdrop's hierarchy order already swallows clicks meant for the task. It does nothing
    // about EventSystem navigation, which would still walk the keyboard focus out of the overlay
    // and into the task's own buttons.
    [SerializeField] private CanvasGroup taskChrome;

    // The views raise this one Commit Delay after the click, not on it, so both buttons finish
    // their press before the screen changes under them.
    private void OnEnable()
    {
        pauseButton.Committed += Pause;
        resumeButton.Committed += Resume;
    }

    private void OnDisable()
    {
        pauseButton.Committed -= Pause;
        resumeButton.Committed -= Resume;

        // Exit is pressed while paused, so the scene unloads with time stopped. A zero left behind
        // would follow the player into the menu and into the next task, which reads as a hang.
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // A touch-only player has no keyboard device at all.
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (panel.activeSelf)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    private void Pause()
    {
        panel.SetActive(true);
        taskChrome.interactable = false;
        Time.timeScale = 0f;
    }

    private void Resume()
    {
        panel.SetActive(false);
        taskChrome.interactable = true;
        Time.timeScale = 1f;
    }
}
