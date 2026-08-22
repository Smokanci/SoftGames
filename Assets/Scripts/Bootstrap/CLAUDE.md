# Bootstrap

> **Status:** built and verified in play mode. `Assets/Scenes/Bootstrap.unity` is build index 0 and
> hosts everything in this folder. The menu and all three task scenes load additively on top of it.
> `Assets/Scenes/AceOfShadows.unity` is built against the contract below and is the worked example of
> it. The other two task scenes are still stubs carrying only `Assets/Prefabs/TaskChrome.prefab`.

The bootstrap scene is loaded once and never unloaded. That is what lets session-wide services live
in a normal scene instead of under `DontDestroyOnLoad`, which this project bans outright.

## What a task scene may not bring

The bootstrap scene owns the only camera and the only `EventSystem`. A scene that adds either gets a
duplicate at runtime: two `AudioListener`s log a warning every frame, and two input modules make UI
clicks land unpredictably. Task scenes are authored with neither — check `Menu.unity` for the shape
to copy.

It also owns the global canvas that carries the FPS readout. That canvas sits at a higher sorting
order than a task canvas so the readout stays on top; the value is on the `Canvas` component in
`Bootstrap.unity`, not repeated here.

## The scene-swap contract

`SceneLoader` is the only thing that calls `SceneManager`. Everything else asks over SOAP.

- **`Assets/SOAP/Events/_LoadSceneRequested.asset`** (`GameEventString`) carries a scene name.
  `SceneLoader` is the sole listener, reached through the `GameEventListenerString` beside it on
  `[Services]` — never a code subscription, per `.claude/rules/csharp-conventions.md`.
  `SceneLoadRequest` is the sole raiser — put it on a `Button` and set the target scene on the same
  component. It subscribes to its own `Button` in code, which is Unity UI rather than SOAP and so
  falls outside that rule.
- **`Assets/SOAP/Variables/_IsLoadingScene.asset`** (`BoolVariable`) is set for the duration of a
  swap. A request that arrives while it is set is **dropped, not queued** — that is deliberate, since
  the alternative is unloading a scene that is still loading. A double-click on a menu button
  therefore navigates once. Nothing outside `SceneLoader` reads it yet — it is a SOAP asset rather
  than a private field so a loading overlay or a disabled Back button can subscribe later without a
  cross-scene reference. `SceneLoader.SwapTo` clears the flag in a `finally`: if it ever stopped
  doing that, one throw would latch the flag and silently kill every later request.

Requesting the scene that is already loaded still unloads and reloads it. Nothing guards against it
because nothing asks for it — the menu and the Back button always name a different scene.

`BootstrapSmokeTests` in `Assets/Tests/PlayMode/` guards the *additive* half of that contract: it
loads `Bootstrap` on its own and asserts both scenes end up loaded. A regression that made the first
load `Single` instead of `Additive` would still show a working menu in the editor and would only fail
later, when the first Back button found no bootstrap services to return to — so the assertion that
matters is the one on `Bootstrap`, not the one on `Menu`.

## Ordering that a task scene can rely on

`SceneLoader` unloads the outgoing scene **before** it loads the incoming one, so the two never
coexist and a task scene never sees another task scene's objects.

After the load completes, `SceneLoader` calls `SetActiveScene` on the new scene. That is what makes
runtime `Instantiate` calls land in the task scene and unload with it. Without it they would be
parented into the bootstrap scene and leak for the rest of the session. A task scene that spawns
objects depends on this, so do not remove it.

`SceneLoader.Start` kicks off the first load, so every `Awake` and `OnEnable` in the bootstrap scene
has already run by the time any other scene exists. A task scene may assume the SOAP channels and
the FPS readout are live; it may not assume anything about *another* task scene.

## FPS readout

The model/view seam here is the pattern the task code should follow. `FpsSampler` (in
`Assets/Scripts/Common/`) is a plain C# ring buffer with no Unity dependency, driven headlessly by
`Assets/Tests/EditMode/FpsSamplerTests.cs`. `FpsCounterView` is the `MonoBehaviour` and does nothing
but feed it `Time.unscaledDeltaTime` and draw the result.

`FpsSampler.FramesPerSecond` inverts the mean frame time once. It is **not** the mean of per-frame
rates — those two differ, and only this one drops when a single frame stalls, which is the reason to
show the number at all.

The readout reads zero when the Editor window is unfocused, because the Editor barely advances
frames in the background. `PlayerSettings.runInBackground` is enabled to counter this. See the Unity
MCP working notes in the root `CLAUDE.md` before assuming the counter is broken.

### Frame rate: capped in the editor, never in the WebGL player

`FrameRateCap` sets `Application.targetFrameRate` once, in `OnEnable`, to the display's refresh
rate rounded **up** — a cap a fraction under the real rate (a 144 Hz panel reports 143.91) drops a
frame every few seconds, and one a fraction over costs nothing. Once is enough because this scene is
never unloaded and the value survives additive scene loads and quality-level changes; a display whose
refresh rate changes mid-session needs play mode restarted.

Without the cap the editor's game view runs the loop as fast as the GPU allows, because the quality
level the editor uses has `vSyncCount: 0`. A level with `vSyncCount` non-zero ignores
`targetFrameRate` entirely and paces to the display anyway, so the component is harmless either way.

**On WebGL the cap must not run**, which is why the body sits behind `#if UNITY_EDITOR ||
!UNITY_WEBGL`. The browser already paces the loop to the display: Unity's `MainLoop.js` hands the
requested rate to Emscripten's `emscripten_set_main_loop`, which picks `requestAnimationFrame` when
that rate is not positive and `setTimeout(1000 / rate)` when it is. A cap therefore does not tighten
the browser's vsync — it *leaves* vsync for a drifting timer. `QualitySettings.vSyncCount` means
nothing there for the same reason. A readout showing 120 on a 120 Hz panel is the correct result,
not a runaway loop to clamp.

`UNITY_EDITOR` has to lead that condition. WebGL is the active build target, so `UNITY_WEBGL` is
defined in the editor too, and a bare `!UNITY_WEBGL` would compile the cap out of the one place it
is wanted. **Any `#if` written against the target platform in this project has the same trap.**

The two Emscripten files are in the editor install, under
`PlaybackEngines/WebGLSupport/BuildTools/lib/MainLoop.js` and
`.../Emscripten/emscripten/src/library_browser.js`, if the branch needs re-checking on a future
editor version.

## SafeAreaFitter

**Its body compiles only under `UNITY_IOS || UNITY_ANDROID`.** Every other platform, WebGL included,
reports `Screen.safeArea` as the full screen rect, so the component would spend a frame writing the
anchors the `RectTransform` already has. On WebGL the notch inset never reaches C# at all — the
browser publishes it through the CSS `env(safe-area-inset-*)` variables and the Unity player reads
none of them, so **the hosted build handles its insets in the WebGL template**, not here. The class
declaration stays outside the `#if` so `Bootstrap.unity`, `Menu.unity` and `TaskChrome.prefab` keep
their script reference on every target.

Inside the gate it polls `Screen.safeArea` and `Screen.orientation` in `Update`, because Unity raises
no callback for either. It writes anchors only — offsets are left alone, so put it on a
`RectTransform` whose offsets are already zero. It skips a frame that reports a zero-size screen
rather than dividing by it, since `NaN` anchors do not repair themselves.
