using UnityEngine;

// Which colour follows which, and the loop back to orange, live in FlameColor.controller as
// transitions on one trigger. Keeping the order there rather than in a switch here is what makes
// the animator controller the thing driving the colour, and what makes the blend free.
public sealed class FlameColorCycle : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private static readonly int NextColor = Animator.StringToHash("Next");

    // Called by the GameEventListenerVoid on this GameObject, wired to _FlameColorAdvanceRequested.
    public void Advance()
    {
        animator.SetTrigger(NextColor);
    }
}
