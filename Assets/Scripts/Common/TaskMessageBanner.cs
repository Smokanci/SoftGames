using TMPro;
using UnityEngine;

// Lives on a GameObject that stays active and toggles a child, rather than being the thing
// toggled: the GameEventListenerString beside it subscribes in OnEnable, so a banner that
// switched itself off would never subscribe again and would stay silent for the session.
public sealed class TaskMessageBanner : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text   label;

    // An empty message hides the banner, so one channel carries both directions and a task
    // does not need a second event to take its message back down.
    public void Show(string message)
    {
        label.SetText(message);
        panel.SetActive(!string.IsNullOrEmpty(message));
    }
}
