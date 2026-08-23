# Bootstrap

> **Status:** built and verified in play mode. `Assets/Scenes/Bootstrap.unity` is build index 0 and
> hosts everything in this folder. The menu and all three task scenes load additively on top of it.
> `Assets/Scenes/AceOfShadows.unity` is built against the contract below and is the worked example of
> it; the other two task scenes follow it.

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

## The ground

`EmberGround` draws the app's only backdrop, and it lives here rather than one copy per task scene.
A task scene carrying its own would pop it in a frame late on every swap, and four copies would
drift apart the moment one of them was retuned. Three layers, bottom to top: a flat fill sprite, a
soft radial bloom low on the screen, and a slow drift of ash particles. Every tuned number is on the
components in `Bootstrap.unity`.

**The motes prewarm, and `EmberGround.OnEnable` clears and restarts them because of it.** The
prewarmed batch spawns before that first `Apply`, so it would carry the start colour serialized on
the system instead of the scene's tint, and those motes then live out a full lifetime in the wrong
colour. Nothing recolours a particle after it spawns — `startColor` is read once, at spawn — so the
only fix is to re-run the prewarm after the colour is set.

**The layers are world-space sprites, not UI.** A `ParticleSystem` cannot render into a
ScreenSpace-Overlay canvas, so the ash forces the whole ground into the world and out of
`[GlobalCanvas]`. They sit at large negative sorting orders to stay under any task scene's sprites.

**Both sprite renderers use `Assets/Art/UI/GroundUnlit.mat`.** The material Unity assigns by default
here is `Sprite-Lit-Default`, and there are no 2D lights in any scene in this project — a lit sprite
renders black. If the ground ever goes black, check the material before anything else.

What a scene owns is two colours, not a ground. `SceneGroundTint` (in `Assets/Scripts/Common/`)
publishes them in `OnEnable`, so the ease starts in the same frame the scene comes up:

- **`Assets/SOAP/Variables/_GroundTint.asset`** (`ColorVariable`) — the hue of the bloom and the ash.
- **`Assets/SOAP/Variables/_GroundFill.asset`** (`ColorVariable`) — the flat colour behind everything.

`EmberGround` **polls** both every frame and eases toward them rather than listening for a change.
That is what turns a scene swap into a cross-fade instead of a cut, and it uses the same
`1 - exp(-k·dt/seconds)` ease as `EmberHeat`, so tuning a swap and tuning a button press mean the
same thing. Both halves of that ease are serialized here, on `EmberGround`; `EmberHeat` reads its
own from `EmberStyle.asset`, and the two are set independently.
`EmberButtonGroup` reads `_IsLoadingScene` the same way and for the same reason.

**Every scene carries a `SceneGroundTint`, the menu included.** A screen with none would silently
inherit whichever screen ran before it. Phoenix Flame is the one that sets `_GroundFill` to a
mid-value grey — its blue and green flame is illegible on near-black — and it is why that task no
longer needs a backdrop of its own.

`Menu.unity` has no background image. The shared ground is the menu's backdrop; anything opaque and
full-rect on a task canvas hides it completely.

Art: `tools/generate_ground_sprites.py` draws the flat fill, and the bloom reuses `ember_radial.png`
from the Ember button set rather than owning a second blob. Both are white on transparent so the
runtime tint decides the hue, the same arrangement as every other generated texture here.

## The scene-swap contract

`SceneLoader` is the only thing that calls `SceneManager`. Everything else asks over SOAP.

- **`Assets/SOAP/Events/_LoadSceneRequested.asset`** (`GameEventString`) carries a scene name.
  `SceneLoader` is the sole listener, reached through the `GameEventListenerString` beside it on
  `[Services]` — never a code subscription, per `.claude/rules/csharp-conventions.md`.
  `SceneLoadRequest` is the sole raiser — put it on an `EmberButtonView` and set the target scene on
  the same component. It listens to that view's `Committed` event, which fires one commit delay
  after the click rather than on it, so the press is legible before the swap starts. That is a plain
  C# event on this GameObject's own component, not a SOAP channel, so it falls outside that rule.
- **`Assets/SOAP/Variables/_IsLoadingScene.asset`** (`BoolVariable`) is set for the duration of a
  swap. A request that arrives while it is set is **dropped, not queued** — that is deliberate, since
  the alternative is unloading a scene that is still loading. A double-click on a menu button
  therefore navigates once. It is a SOAP asset rather than a private field so anything outside the
  loader can read it without a cross-scene reference; `EmberButtonGroup` does, to dim every button
  that did not start the swap. `SceneLoader.SwapTo` clears the flag in a `finally`: if it ever
  stopped doing that, one throw would latch the flag and silently kill every later request.

Requesting the scene that is already loaded still unloads and reloads it. Nothing guards against it
because nothing asks for it — the menu and the pause overlay's Exit always name a different scene.

`BootstrapSmokeTests` in `Assets/Tests/PlayMode/` guards the *additive* half of that contract: it
loads `Bootstrap` on its own and asserts both scenes end up loaded. A regression that made the first
load `Single` instead of `Additive` would still show a working menu in the editor and would only fail
later, when the first Exit found no bootstrap services to return to — so the assertion that matters
is the one on `Bootstrap`, not the one on `Menu`.

## The pause overlay

`PauseMenu` (in `Assets/Scripts/Common/`) lives on `Assets/Prefabs/TaskChrome.prefab`, **not in this
scene**, and that placement is the whole design. The prefab is in all three task scenes and in none
of the menu, so "the menu cannot be paused" needs no flag, no poller and no extra SOAP channel to
say so.

Pausing is `Time.timeScale = 0`. Everything the three tasks animate runs on scaled time, so one
assignment freezes all of them; the Ember buttons and the FPS readout run on
`Time.unscaledDeltaTime` and keep answering. Exit raises `_LoadSceneRequested` through the ordinary
`SceneLoadRequest` component, which means **the scene unloads with time still stopped** — a zero left
behind would follow the player into the menu and read as a hang, so `PauseMenu.OnDisable` restores
the scale. Unloading the task scene is what disables it, so that runs on every exit path.

**Resume goes back to the scale the pause found, not to a flat 1.** `PauseMenu` is not the only
writer of `Time.timeScale` — Ace of Shadows' `TimeWarpToggle` warps it for the fast-forward button —
and a resume that assumed 1 would quietly cancel that warp on every pause.

The component sits on a GameObject that stays active and toggles a *child*, for the same reason
`TaskMessageBanner` does: a component that switched itself off would stop reading the keyboard, and
the overlay could then never be dismissed. Escape toggles it, through
`UnityEngine.InputSystem.Keyboard` — this project has no legacy input, so `Input.GetKeyDown` does not
compile.

Two separate things keep the task from being driven while the overlay is up, and both are needed.
The backdrop is a full-screen raycast target *later in the hierarchy than the chrome*, so it swallows
every click meant for the task underneath. That covers the pointer and nothing else: `EventSystem`
navigation ignores raycasts, and the Ember button prefab navigates `Automatic`, so arrow keys would
otherwise walk the focus straight out of the overlay and onto Phoenix Flame's colour button. So
`PauseMenu` also clears `interactable` on the chrome's `CanvasGroup` — that is the half that stops
the keyboard.

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
