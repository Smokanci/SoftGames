# Shared UI

> **Status:** built and in use. Two things live here: the Ember button, and the app's type. Every
> button in the app is an instance of `Assets/Prefabs/EmberButton.prefab` — the three menu entries,
> the pause button and the pause overlay's Resume and Exit on `Assets/Prefabs/TaskChrome.prefab`,
> Phoenix Flame's colour button, and Ace of Shadows' fast-forward button.
> `Assets/Tests/EditMode/EmberHeatTests.cs` drives the press model with no scene. Every tuned
> level, duration and proportion lives on `Assets/Data/EmberStyle.asset`; the only number an
> instance owns is its hue. None of them are quoted here.

A button has to answer three questions without text: *is this the one I am about to press*, *did my
press land*, and *is the app busy*. The Ember treatment answers them with heat — a glow that pools
under the face, a rim that lights, and a burst of light at the point of contact.

## The seam

`EmberStyle` is the authored numbers and nothing else. `EmberHeat` is the press model: no
`MonoBehaviour`, no scene, so an EditMode test drives a press, a release and a full idle cycle
without play mode. It chases seven channels — `Glow`, `Rim`, `Offset`, `Scale`, `Spread`, `White`
and `Caption` — and `EmberButtonView` writes each of them onto a graphic. `EmberButtonGroup` decides
which sibling is hot and whether input is allowed. Four types because they change for four different
reasons — a retune touches only the first.

The one piece of timing the view does own is the beat between the click and the action — see
*The click waits* below. Everything else it draws, it reads.

## One style asset

`EmberStyle` is a `ScriptableObject` holding every level, duration and proportion the look uses.
Both `EmberHeat` and `EmberButtonView` read it; neither keeps a private copy of anything.

The reason it is one asset rather than a constant per file is that **the tuning is deliberately
global.** A button that breathes at a different rate from its neighbour reads as a bug rather than
as a highlight, so there is nothing per-button to author. The hue is the single exception, and it
lives on the instance because the spec asks each task to own its colour.

The rim's wash toward white is its own chased channel rather than something the view derives from
`Rim`. Deriving it meant reading `Rim`'s position between `HoverRim` and `PressRim`, and once hover
was raised close to press that span nearly closed — the derivation would have gone from a smooth
ramp to a jump across a sliver. `White` says what it means and does not care how near the two levels
sit.

Four fields carry meaning a number cannot:

- **`settleFactor` defines what the durations mean.** `pressSeconds`, `hoverSeconds` and
  `releaseSeconds` are "seconds to within about 5% of the target", and that sentence is only true at
  the authored `settleFactor`. Raise it and every duration silently starts meaning a tighter approach
  than it says.
- **`bloomSpreadX` / `bloomSpreadY` are multiples of the face's own width and height**, not pixels.
  That is what lets one asset serve a wide menu button and a square pause button — a single fixed
  spread gives the wide one a circle that overshoots its ends.
- **`hoverGlow` is not an alpha.** It multiplies into `glowIntensity` on the way to the graphic, so
  it is free to go past 1 and does.
- **`idleHigh` is a ceiling the hover has to clear.** The hovered button is also the breathing one,
  so the step a person sees is `hoverGlow` minus wherever the breath happens to be. Raise `idleHigh`
  toward `hoverGlow` and hovering stops registering, which is exactly how the first tuning here went
  wrong. `EmberHeatTests.HoverOutrunsTheIdleBreathOnTheSameButton` holds the margin.

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

## Hover is not just a brighter glow

A glow that brightens is a step a person misses while their eye is on the pointer, so hover moves
every channel at once and each one carries a different part of the message:

- the **glow** brightens and, through `Spread`, widens the pool under the face;
- the **rim** lights and washes part-way to white;
- the **face lifts** by `hoverLift` and grows by `hoverScale`;
- the **caption** comes up from `captionRestAlpha` to full, which is what makes the buttons nobody
  is pointing at recede.

Two of these exist for the press rather than for the hover. The lift means the press dip starts
above the resting line and ends below it, so the same `pressOffset` buys twice the travel; the
scale means the press has something to cancel, which lands harder than a shrink from rest.

**Hover arrives faster than it leaves.** `hoverSeconds` is a fraction of `releaseSeconds` and that
asymmetry is deliberate: a highlight that fades in at the speed it fades out has not finished
arriving by the time the pointer has moved on, while a slow fade out is what keeps a fast sweep
across a menu from strobing. `EmberHeatTests.HoverArrivesFasterThanItLeaves` holds it.

## The click waits

`EmberButtonView` is the only listener on `Button.onClick`. It starts a `commitDelay` countdown and
raises its own `Committed` event when that runs out; `SceneLoadRequest`, `VoidEventButton` and
`PauseMenu` all listen there instead of on the `Button`. One owner of press timing, and nothing
downstream has to know a delay exists.

The delay alone would not be enough for a button that swaps scenes. `SceneLoader` unloads the
current scene first, so the button that started the swap is destroyed a frame or two after the
request — cutting whatever it was doing. What saves it is that **at the instant the request goes
out, every channel except the bloom is holding a constant**, and a constant survives being cut. Only
the bloom is still moving, so the view shortens its span on commit to end no later than the request:
`Min(span, age + commitDelay)`. The clamp is the load-bearing part — a span pushed *out* would drop
the bloom's own progress and make it jump backwards, so a bloom already ending before the commit is
left alone.

That is the invariant to keep if `commitDelay` is retuned: **the bloom ends no later than the action
goes out**, and the action goes out no sooner than the press is legible.

`Button.interactable` is untouched during the wait. Turning it off would take the button out of
keyboard navigation and grey it through the `Button`'s own transition, which the prefab has disabled
anyway. The siblings are locked instead, from the press — see *Busy starts at the press* below.

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

`EmberButtonGroup` learns a scene is loading by reading
`Assets/SOAP/Variables/_IsLoadingScene.asset` every frame. It **polls the variable rather than
subscribing**, because subscribing to a SOAP channel in code is banned project-wide (see
`.claude/rules/csharp-conventions.md`) and a `BoolVariable` is cheap to read. This is that
variable's first reader outside the scene loader itself.

### Busy starts at the press

The loading flag alone would leave the siblings live for one whole `commitDelay`, which is long
enough to press a second one. So the group's idea of busy is **the flag or any child still holding
its click**, and it reads that hold off `EmberButtonView.Committing`. The lock therefore begins on
the press and runs unbroken into the load: the loader sets the flag inside the same frame the
`Committed` event fires, so there is no frame between the two where the group would let go.

A press that raises no scene load at all — Resume, the colour button — ends the window when its hold
ends, which is the whole reason busy is not simply latched.

Two ordering points hold it together:

- **Presses are taken before the busy test, not after.** A press that starts and ends inside one
  frame would otherwise leave `_committed` unset, and the button that just committed would be
  locked and dimmed instead of held at press heat.
- **The committed button is cleared on the falling edge of busy**, not of the flag, so a group whose
  press never became a load still resets.

## The prefab contract

`Assets/Prefabs/EmberButton.prefab`:

```
EmberButton   CanvasGroup + Button (transition None) + EmberButtonView
├─ Glow       resting heat, additive, pooled under the face
├─ Face       the panel that moves and scales on hover and press
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
component of its own beyond the `Image`. The view holds a ref to it and to `Label` so it can fade
both captions together — it scales the alpha each one was authored with rather than replacing the
colour, so a glyph stays pure authoring in every respect but its opacity. **Both refs must be
wired**, on a caption-only button as much as on a glyph-only one; the prefab wires them and an
instance inherits them.

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

## Type

Two font assets in `Assets/Art/Fonts/`, both generated from Google's Archivo family:

- **`ArchivoExpanded SDF`** — display only. Scene titles and Ace of Shadows' stack counters. Its
  width is the whole point; at body size it reads as a mistake, so do not use it for running text.
- **`Archivo SDF`** — everything else. Button captions, dialogue, the FPS readout.

`Archivo SDF` is the `m_defaultFontAsset` on `Assets/TextMesh Pro/Resources/TMP Settings.asset`, so a
new `TMP_Text` gets it without being told.

**The fallback chain is what keeps Magic Words working.** Both assets are
`AtlasPopulationMode.Static` and carry Latin only, but the dialogue text arrives from a remote
endpoint and can contain anything. The global fallback list is, in order:

1. `NotoEmoji SDF` — monochrome emoji outlines, drawn on demand.
2. `LiberationSans SDF` — the last resort, and the only asset here with broad coverage.

Dropping `LiberationSans SDF` off the end would turn any non-Latin character in the feed into a
missing glyph. Reordering it above the others would spend it on characters Archivo already has, in a
face that does not match.

**The colour emoji do not travel this chain.** TMP resolves a codepoint through every font in the
chain before it looks at a sprite asset, so `NotoEmoji SDF` would shadow each colour sprite that
shares a codepoint. Magic Words rewrites those codepoints into explicit `<sprite>` markup instead,
which outranks both — so the fallback font is the monochrome floor under the colour sheet, not a
rival to it. `Assets/Scripts/MagicWords/CLAUDE.md` has the mechanism and what it costs.

**Static atlases are a deliberate trade.** A dynamic atlas would cover every codepoint on demand,
but it rasterises at runtime and grows the texture mid-session — on WebGL that is a visible hitch on
first use. The character set is baked instead: ASCII, Latin-1, and the punctuation the copy actually
uses (dashes, curly quotes, ellipsis, arrows, bullet). Adding a character to the copy that is not in
that set falls through to the fallback chain rather than failing, which is why the chain is not
optional.
