# Ace of Shadows

> **Status:** built and verified in play mode. `Assets/Scenes/AceOfShadows.unity` runs a full
> 144-card deal, the counters track the deal, and the completion banner appears as the last card
> settles. `CardStacksTests.cs` covers the model headlessly. Tuned values live on the components in
> the scene, not here.

The brief: *"Create 144 sprites stacked like cards in a deck, with each top card partially covering
the one below. Every 1 second the top card should move smoothly to another stack. Display a counter
above each stack and show a message when all animations are finished."*

144 real card `GameObject`s exist, none of them carries a script, and none is pooled or culled. The
count is an object count.

## The three-place model

`CardStacks` is plain C# with no `UnityEngine` reference, which is why an EditMode test drives all
144 transitions with no scene. It holds three places, not two: **source**, **in flight**, **target**.

A card in the air belongs to neither stack. That is the whole point of the third place. `IsComplete`
asks `TargetCount == TotalCards`, so it cannot read as finished while a card is still travelling —
the wrong answer is unrepresentable rather than guarded against. With a move shorter than the
cadence those two moments differ by one move duration, and the early answer would put the message on
screen with a card mid-flight.

Invariant, true on every frame: `SourceCount + InFlightCount + TargetCount == TotalCards`. Watch the
two counters during a run — they sum to one less than the deck while a card is up.

`BeginMove` on an empty source throws. It is a caller bug, not a state the game reaches;
`AceOfShadowsRunner` checks `CanBegin` first.

## Who owns what

- `CardStacks` — the counts and the transitions. No Unity types, no timing.
- `CardTableView` — builds the deck, knows where a card at stack index N belongs, and draws the two
  counters. **No timing, no animation.**
- `AceOfShadowsRunner` — the clock, the flight pose, and the completion message. Holds the model.

## The flight, and why it lands flat

One flight path, with its values rolled per card — not a bank of animations chosen at random. At
lift-off `AceOfShadowsRunner.BeginMove` draws an arc-height multiplier, a turn count, a lean and a
sideways drift, and stores them on the `Flight` struct. The ranges are serialized under the *Flight
variety* header on the runner.

**Every varied term has to reach zero at `t == 1`.** Arc, drift, lean and the scale bump are all
`sin(t * PI)`, which is zero at both ends; the spin is a *whole* number of turns times `t`, so it
finishes on a multiple of 360. That is what lets the card land square on its slot with no separate
settling step. Add a term that does not have this property and cards start landing crooked.

The same eased `t` drives travel and spin, so the spin decelerates as the card arrives rather than
stopping dead.

The scale bump is the only depth cue available. The camera is orthographic, so the arc alone reads
as a flat detour rather than a card coming toward the viewer.

`CardTableView.Seat` resets rotation and scale, not just position — a card arrives mid-pose, and a
seat that wrote position alone would leave it tilted and oversized in the stack for the rest of the
run.

The runner keeps flights in a list rather than one field. It costs about five lines and it survives
someone raising `moveDuration` past `moveInterval` in the inspector; otherwise that invariant would
live only in a comment.

## Layout is derived from the camera

The bootstrap camera is orthographic, so **the vertical extent is a fixed number of world units on
every device** and the stack geometry never reacts to aspect. Only the horizontal separation varies:

```
halfWidth  = orthographicSize * aspect
stackX     = clamp(halfWidth * separationFraction, minSeparation, maxSeparation)
```

Source sits at `-stackX`, target at `+stackX`. The clamp is what keeps a portrait phone from
overlapping the two stacks and a wide desktop window from pushing them off the sides. Verified at
three aspects: the min clamp fires below roughly 0.54, the formula governs a 9:16 phone, and the max
clamp fires on anything ultrawide.

`CardTableView.Update` polls `Camera.aspect` and re-seats every card when it changes, the same
approach `SafeAreaFitter` takes for `Screen.safeArea`, because Unity raises no callback for either.
`RestingPosition` is read fresh on every flight frame rather than cached at lift-off, so a resize
mid-flight moves the card with the stacks instead of flying it to a stale spot.

The camera comes from `Camera.main`. It lives in the bootstrap scene, so a serialized reference is
both impossible when this scene wires up and banned as a cross-hierarchy reference.

**The counters are world-space `TextMeshPro`, not canvas UI.** They are positioned in world units
beside the stacks, so they follow the layout for free. The chrome above them is canvas-space and
scales on a different rule — `CanvasScaler` match 0.5 — so canvas pixels and world units drift
apart as the aspect changes. Anything placed near the title has to be checked at both extremes.

## Card identity and sorting

Card `i` takes colour `i / glyphs.Length` and glyph `i % glyphs.Length`. Colour therefore runs in
blocks and the glyph cycles inside each block, so the buried stack reads as a gradient and no two
cards repeat a pair. The invariant is `palette.Length * glyphs.Length >= cardCount`; both arrays are
serialized on `CardTableView` and the generator's `SHAPES` order is what the glyph array must match.

Each card owns two sorting orders: body at `2 * indexInStack`, glyph at `2 * indexInStack + 1`. A
lifted card jumps to `FlightOrder`, above the whole range, so it draws over both stacks.

## The art

One texture, `Assets/Art/Cards/cards_sheet.png`, sliced into 13 named sub-sprites: one card body and
twelve glyphs. Everything is drawn white on transparent; all 144 colours come from
`SpriteRenderer.color` at runtime.

`tools/generate_card_sprites.py` draws the sheet and writes the rect table to
`tools/cards_sheet.json`. To change a shape, edit its function and re-run the generator, then
re-slice in the Sprite Editor if the layout moved. The sheet is committed, so a clone needs neither
Python nor Pillow.

A Sprite Atlas asset was tried first and abandoned: `.spriteatlas` files imported through the V1
`NativeFormatImporter` rather than `SpriteAtlasImporter` in that editor session, so the atlas never
produced a packed texture. Laying the sheet out in the generator makes the packed result a committed
file instead of something the editor's sprite-packer mode decides.

**`UnityStats.batches` does not measure this.** It reports one batch per `SpriteRenderer` under URP
regardless — confirmed by flattening all 288 renderers to one sprite, one colour and one sorting
order and watching the number not move. `setPassCalls` is the figure that responds, and it stays in
single digits for the whole frame. Do not treat a high batch count here as evidence of a problem.

## SOAP, once

`Assets/SOAP/Events/_TaskMessageRequested.asset` (`GameEventString`). The banner is inside
`TaskChrome.prefab`; the runner is not, and `.claude/rules/code-conventions.md` forbids crossing that
hierarchy boundary with a serialized reference. **An empty string hides the banner**, so one channel
carries both directions. Everything else in this task — model to view, view to counters — is a direct
reference.

The completion wording is serialized on the runner and formatted with the deck size, so the message
cannot drift from the card count and the label stays dumb about where its text comes from.

`TaskMessageBanner` sits in the shared chrome rather than in this task, because Magic Words needs the
same error state. It lives on a GameObject that stays active and toggles a *child*: the
`GameEventListenerString` beside it subscribes in `OnEnable`, so a banner that switched itself off
would never subscribe again and would stay silent for the whole session.

## Watching a whole run

`Start` primes `_sinceLastMove` to the full interval, so the first card leaves on the first frame
instead of after a dead second.

A full run is 144 seconds. To watch one quickly, set `moveInterval` and `moveDuration` down in the
inspector before entering play mode. Keep the duration below the interval or more than one card is in
the air at a time — which works, but stops matching the brief.
