# Ember buttons

> **Status:** built and in use. Every button in the app is an instance of
> `Assets/Prefabs/EmberButton.prefab` — the three menu entries, the pause button and the pause
> overlay's Resume and Exit on `Assets/Prefabs/TaskChrome.prefab`, and Phoenix Flame's colour
> button.
> `Assets/Tests/EditMode/EmberHeatTests.cs` drives the press model with no scene. Every tuned
> level, duration and proportion lives on `Assets/Data/EmberStyle.asset`; the only number an
> instance owns is its hue. None of them are quoted here.

A button has to answer three questions without text: *is this the one I am about to press*, *did my
press land*, and *is the app busy*. The Ember treatment answers them with heat — a glow that pools
under the face, a rim that lights, and a burst of light at the point of contact.

## The seam

`EmberStyle` is the authored numbers and nothing else. `EmberHeat` is the press model: no
`MonoBehaviour`, no scene, so an EditMode test drives a press, a release and a full idle cycle
without play mode. `EmberButtonView` owns no timing of its own; it reads `Glow`, `Rim` and `Offset`
and writes them onto three graphics. `EmberButtonGroup` decides which sibling is hot and whether
input is allowed. Four types because they change for four different reasons — a retune touches only
the first.

## One style asset

`EmberStyle` is a `ScriptableObject` holding every level, duration and proportion the look uses.
Both `EmberHeat` and `EmberButtonView` read it; neither keeps a private copy of anything.

The reason it is one asset rather than a constant per file is that **the tuning is deliberately
global.** A button that breathes at a different rate from its neighbour reads as a bug rather than
as a highlight, so there is nothing per-button to author. The hue is the single exception, and it
lives on the instance because the spec asks each task to own its colour.

It also removes a class of drift the two-file version had: the view compares `_heat.Rim` against
`HoverRim` and `PressRim` to decide how far to wash the rim toward white, and those are now the same
fields the model chases rather than a second set that a retune could leave disagreeing.

Two fields carry meaning a number cannot:

- **`settleFactor` defines what the durations mean.** `pressSeconds` and `releaseSeconds` are
  "seconds to within about 5% of the target", and that sentence is only true at the authored
  `settleFactor`. Raise it and every duration silently starts meaning a tighter approach than it
  says.
- **`bloomSpreadX` / `bloomSpreadY` are multiples of the face's own width and height**, not pixels.
  That is what lets one asset serve a wide menu button and a square pause button — a single fixed
  spread gives the wide one a circle that overshoots its ends.

`EmberHeat` takes the asset in its constructor and holds no `MonoBehaviour` dependency, so an
EditMode test builds one with `ScriptableObject.CreateInstance` and drives a full press with no
scene and no play mode. `CreateInstance` runs the field initializers, so the tests assert against
the authored defaults without a fixture asset to keep in sync.

## Chasing targets, not playing clips

Every value moves toward a target by an exponential approach each frame, and the target is a pure
function of the current state. Nothing is a coroutine and nothing is a tween.

The reason is the press that lands while the release is still running. Two competing coroutines
cannot both own one value — one wins and the button visibly jumps. A value that is already moving
just redirects.

The approach rate is `1 - exp(-k·dt/seconds)`, which covers the same fraction of the remaining
distance per unit of wall-clock time however the frames are cut. A press looks identical at 30 and
at 144 fps, and `EmberHeatTests.FrameRateDoesNotChangeWhereItLands` holds that.

`Time.unscaledDeltaTime` drives it: a button that stops answering because something scaled time
reads as broken.

## One breathing button at a time

Three buttons pulsing on their own timers reads as a slot machine. The idle glow is instead a single
mark meaning "this is the one you are about to press" — so it follows the pointer, falls back to
keyboard focus, and defaults to the first live child so the menu never looks asleep.

Something has to own that choice, and it is the parent. A child telling its parent "I am hot now"
would be a scene reference pointing the wrong way, so `EmberButtonGroup` **polls its children**
instead: it reads `PointerOver` and `Selected`, then pushes the answer back down through
`SetIdleOwner`. No child ever names its group.

The same direction explains `ConsumePressed()`. It is a latch, not a callback — a press that starts
and ends inside a single frame is still there to be read when the group next looks.

Set `idleGlow` false on a group whose buttons are chrome rather than a choice. The pause button uses
that: nothing about a task screen is asking to be pressed. The pause overlay is a separate group with
`idleGlow` on, because Resume and Exit *are* a choice — that is why the overlay is a sibling of the
chrome's group rather than a child of it.

## Commit, with nothing filling

While a scene swap runs, the button that started it holds press-level heat and every other one cools
below rest, dims through its `CanvasGroup` and stops taking raycasts. That is the whole "you pressed
this, the app heard you" signal — no bar, no spinner, no progress of any kind, because none of them
would be telling the truth about an additive load that finishes in a frame or two.

`EmberButtonGroup` learns the app is busy by reading `Assets/SOAP/Variables/_IsLoadingScene.asset`
every frame. It **polls the variable rather than subscribing**, because subscribing to a SOAP channel
in code is banned project-wide (see `.claude/rules/csharp-conventions.md`) and a `BoolVariable` is
cheap to read. This is that variable's first reader outside the scene loader itself.

The group clears its committed button on the loading edge back to false, so a task scene that exits
back to the menu finds the buttons already reset.

## The prefab contract

`Assets/Prefabs/EmberButton.prefab`:

```
EmberButton   CanvasGroup + Button (transition None) + EmberButtonView
├─ Glow       resting heat, additive, pooled under the face
├─ Face       the panel that moves on press
│  ├─ Rim     the outline that lights
│  ├─ Label   TMP
│  └─ Glyph   an icon instead of a word — inactive until an instance turns it on
└─ Bloom      the press burst, additive
```

The prefab holds the `style` reference, so every instance inherits it. An instance sets four
things: its caption — either the label text or a sprite on `Glyph` with the label left empty — the
`hue` on `EmberButtonView`, its placement (anchors, size, pivot or `LayoutElement`), and the
component that does the actual work (`SceneLoadRequest`, `VoidEventButton`, whatever the screen
needs). **Overriding anything else on an instance is the bug** — it is how one button ends up
breathing out of step with the rest.

`Glyph` is a child of `Face` rather than of the root so it dips with the press, and it carries no
component of its own beyond the `Image`: the view never touches it, so a glyph is pure authoring.

Three invariants hold the prefab together:

- **The `Button`'s own transition is None.** Colour Tint fades exactly one graphic and cannot move a
  face or scale a bloom, so leaving it on would only fight `EmberButtonView` over the same `Image`.
- **The press dip moves `Face`, never the root.** The root is under a `VerticalLayoutGroup` in the
  menu, and layout rewrites the root's position every time it rebuilds. A child is out of its reach.
- **`Bloom` is last in the hierarchy** so it draws over the face, and `Glow` is first so it draws
  under it. Both have `raycastTarget` off; only `Face` takes input.

## Additive, and what that costs

`Assets/Art/UI/UIAdditive.shader` (`Blend One One`), used through `EmberAdditive.mat` by both the
glow and the bloom. Alpha-blended light over a dark panel has to darken the panel's own colour to
brighten the edge, which reads as grey haze rather than heat.

The catch is that additive blending ignores the alpha channel entirely, so the shader multiplies it
into RGB by hand. Alpha is an intensity here, not coverage — that is what lets the view fade a glow
by writing `color.a`.

It carries no clip-rect variants, so **an Ember button inside a `RectMask2D` will have its glow and
bloom escape the mask.** Nothing in this project does that; a scrolling list of them would need the
mask keywords added.

## Art

`tools/generate_ui_sprites.py` (Pillow) draws `ui_panel.png`, `ui_rim.png`, `ember_radial.png` and
`ui_pause.png` into `Assets/Art/UI/`, white on transparent so the runtime tint decides the hue — the
same arrangement the card sheet and the flame puffs use. The PNGs are committed, so a clone needs
neither Python nor Pillow.

Three things about it are load-bearing:

- The two rectangles are nine-sliced, so their size in a scene is independent of the size drawn here
  and only the corner radius is baked in. **`BORDER` must stay larger than `RADIUS`** or the slice
  cuts through the curve and the corners smear. The script refuses to run otherwise.
- The radial blob uses a squared falloff, not a linear one. A linear ramp reads as a flat disc with a
  hard edge once it is tinted and stretched across a button.
- **The pause glyph is not nine-sliced**, so unlike the two rectangles its bar proportions are the
  ones that reach the screen. They are authored as fractions of the square, which keeps the shape
  intact if the source is ever redrawn larger. It also has to import as **Sprite / Single**, not
  Multiple — the project's texture default is Multiple, left over from the card sheet, and it
  auto-slices the glyph into two bars.

## Verifying it in the Editor

Two traps, both of which cost time here:

- **`manage_camera screenshot` with a named camera excludes ScreenSpace-Overlay canvases**, and every
  canvas in this project is overlay — so a menu capture comes back as an empty background. Switching
  the canvases to `ScreenSpaceCamera` on `Camera.main` at runtime works and is discarded when play
  mode stops.
- **A screenshot cannot catch the bloom in flight.** It is over in well under a second and the
  capture lands a frame or more later, by which time the alpha is zero. Pause the editor first, then
  write the bloom's size and colour directly — with `Update` frozen, the values stay put for the
  capture.
