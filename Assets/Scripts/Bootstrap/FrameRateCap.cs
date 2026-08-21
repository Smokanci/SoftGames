using UnityEngine;

// The body compiles out of the WebGL player, where requestAnimationFrame already paces the loop to
// the display and any positive targetFrameRate switches Unity off it — see this folder's CLAUDE.md.
// UNITY_EDITOR has to lead the condition: WebGL is the active build target, so UNITY_WEBGL is
// defined in the editor too and a bare !UNITY_WEBGL would never run in play mode, which is the one
// place the cap is actually wanted.
public sealed class FrameRateCap : MonoBehaviour
{
#if UNITY_EDITOR || !UNITY_WEBGL
    // Once is enough: the bootstrap scene is never unloaded, and targetFrameRate survives additive
    // scene loads and quality-level changes.
    private void OnEnable()
    {
        var hz = Screen.currentResolution.refreshRateRatio.value;

        // A display that reports no rate would cap at zero, which Unity reads as unbounded.
        if (hz <= 0d)
        {
            return;
        }

        // Ceil, not round: a cap a fraction under the display rate drops a frame every few seconds,
        // and one a fraction over it costs nothing.
        Application.targetFrameRate = Mathf.CeilToInt((float)hz);
    }
#endif
}
