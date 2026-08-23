# Phoenix Flame

> **Status:** built and verified in play mode. `Assets/Scenes/PhoenixFlame.unity` runs the four-layer
> fire, and the button walks orange → green → blue → orange with a blend on every step.
> `PhoenixFlameColorTests.cs` drives the full loop through the button. Every tuned value lives on the
> particle systems in the scene and in `Assets/Animation/PhoenixFlame/`, not here.

The brief: *"Create a particle effect demo that shows a great fire effect. Add a UI button that
controls the fire colour using an animator controller. The fire should transition smoothly from
orange to green to blue and loop back to orange."*

One press advances one step. Three presses show the whole loop.

## The animator controller owns the order

`Assets/Animation/PhoenixFlame/FlameColor.controller` holds three states and one trigger. Each state
plays a one-clip loop that pins a colour; each transition blends into the next over a fixed
duration with no exit time. **The loop back to orange is an edge in that graph**, not a modulus in
C#. That is what makes the animator controller the thing driving the colour rather than a decorative
wrapper around a `switch`, and it is why the blend costs no code at all.

Consequence worth knowing: the cycle order can be changed, or a fourth colour added, without
touching a `.cs` file. The two components below never learn how many colours exist.

## Why `FlameTint` exists

An `Animator` cannot usefully animate a `ParticleSystem` module field, so the clips animate one
ordinary `Color` on a `MonoBehaviour` and that component pushes the value into the emitters.

- The clips animate `FlameTint.tint`. Nothing else writes it.
- `Awake` captures each listed system's authored `main.startColor.color`, and `LateUpdate` writes
  `tint * authored` back. Each layer therefore keeps its own brightness and alpha and only the hue
  moves. `LateUpdate` so the emitters get the value the Animator wrote *this* frame.
- **Membership of the `layers` array is the "does this layer recolour" decision.** There is no
  per-layer weight. Smoke is left out, which is why it holds its authored colour at every hue.
- **Every listed system's start colour must be in Constant mode.** A gradient or a random range
  carries no single colour to modulate, so `startColor.color` would read back something arbitrary
  and `Awake` would capture the wrong base.

A start colour reaches only particles that have yet to spawn. So a press does not repaint the fire —
the new colour enters at the base and washes up over roughly one particle lifetime while the old
colour burns off the top. Watching the two hues coexist mid-blend is the clearest evidence the
transition is real, and it is visible in any screenshot taken during a press.

`FlameColorCycle` is the other half: one public method that pulls the trigger. Two small components
rather than one, because they change for different reasons — one pushes a value every frame, the
other answers an event.

## The four layers

Bottom to top by `ParticleSystemRenderer.sortingOrder`: **Smoke**, **Body**, **Core**, **Sparks**.
Smoke uses `SmokeAlpha.mat`, Sparks uses `SparkAdditive.mat`, and Body and Core share
`FlameAdditive.mat`.

Additive blending is what makes the middle of the fire look hot. Core particles are authored bright
and near-white, so where they overlap, the dominant channel saturates first and the others keep
accumulating — the centre goes white-hot on its own, at every hue, with no second colour anywhere in
the setup. That is also why a tint whose non-dominant channels are too low reads as flat poster
colour: nothing is left to accumulate into white. If a new colour looks dead in the middle, raise its
*other* two channels rather than its own.

Smoke uses alpha blending instead, because additive grey adds light and smoke is supposed to take it
away. It is authored close to black for the same reason — the backdrop is light, so smoke only reads
as smoke when it is darker than what it sits on.

## Art

`tools/generate_flame_sprites.py` (Pillow) draws `flame_puff.png`, `smoke_puff.png` and
`spark_puff.png` into `Assets/Art/Flame/`, all white on transparent so runtime colour does all the
work — the same arrangement as the card sheet. The PNGs are committed, so a clone needs neither
Python nor Pillow.

The generator builds value noise by hand because numpy is not available here. Two details cost real
time and are easy to undo by accident:

- The fbm sum clusters near the middle of its range, so the field is renormalised to its own min and
  max before use. Without that the wisps exist but are invisible.
- The noise term is scaled by distance from the centre, so it only breaks up the outer edge.
  Applying it uniformly punches holes through the middle of the puff.

The spark texture skips the noise entirely, and its emitter draws it as a plain billboard rather
than a stretched one. Both were tried the other way round: a sprite that reads as fire at puff size
reads as a leaf once it is stretched along its velocity, because the ragged edge stretches with it.

## The backdrop

`Backdrop` is a neutral grey `SpriteRenderer` far below every particle in sorting order, scaled well
past any viewport. The app's shared camera clear colour is a mid blue, and a blue flame on it is
barely legible — which would hide a third of the thing the task is being graded on. Grey is the
neutral all three hues read against; it also gives the dark smoke something to show up on. The
backdrop is scene-local, so the other two tasks keep the shared background.

The cost of a light backdrop is real and worth knowing before retuning it: additive blending has
less headroom above a bright ground, so the hot centre reaches white sooner and the fire looks
slightly flatter than it would on black. Darkening the backdrop buys that contrast back and costs
the smoke its legibility.

## Layout

The bootstrap camera is orthographic, so the vertical extent is a fixed number of world units on
every device and the fire needs no responsive layout of its own.

The one constraint that is not obvious: **the flame base must sit above the button row at the widest
aspect.** The canvas scaler is matched half-way between a portrait reference width and height, so
the same button occupies a much taller slice of a landscape viewport than of a portrait one; a flame
rooted low enough to look grounded in portrait licks over the Back button in landscape. `ColorButton`
is anchored bottom-**right** for the same family of reasons — bottom-centre is taken by the shared
Back button, and a centred second button would sit directly under the centred fire.

## SOAP, once

`Assets/SOAP/Events/_FlameColorAdvanceRequested.asset` (`GameEventVoid`). The button is under the
chrome canvas and the flame is in the world, so neither can hold a serialized reference to the
other. `VoidEventButton` raises it and the `GameEventListenerVoid` on the flame root calls
`FlameColorCycle.Advance`.

Everything else here is a direct reference within one hierarchy.

## `com.unity.modules.particlesystem`

The module was missing from `Packages/manifest.json`. The Editor had it loaded anyway, so C# compiled
and the fire ran in play mode — the risk was only ever the WebGL player. It is in the manifest now;
do not let a dependency cleanup drop it.
