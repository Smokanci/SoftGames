# SoftGames Unity Developer Assignment

> **Play it here:** _link pending — the WebGL build is not hosted yet._

Three self-contained showcases behind an in-game menu, built in Unity 6 and shipped as a WebGL
build. Written by Cristian Smochina.

| Task | What it does |
|---|---|
| **Ace of Shadows** | 144 stacked card sprites; the top card flies to another stack on a timer, with a live count over each stack and a message when the run finishes. |
| **Magic Words** | Character dialogue assembled from text and emoji, fetched from a remote endpoint at runtime, degrading cleanly when an avatar image or a field is missing. |
| **Phoenix Flame** | A layered particle fire whose colour is driven through an Animator controller, cycling orange → green → blue → orange from a UI button. |

Every task is reached from the menu, the layout responds to both phone and desktop, and an FPS
readout sits in the top-left corner throughout.

## Architecture

**One persistent scene, three interchangeable ones.** `Assets/Scenes/Bootstrap.unity` is build index
0 and is never unloaded. It owns the only camera, the only `EventSystem`, the FPS readout, and the
scene loader. The menu and the three task scenes load **additively** on top of it and unload on
exit, so the session-wide services live in a normal scene instead of behind `DontDestroyOnLoad` —
which the project bans outright.

**Cross-system messages go over ScriptableObject events**, not references. `SceneLoader` is the only
class that touches `SceneManager`; a menu button raises a `GameEventString` naming a scene and does
not know who answers. The same rule applies inside a task: a serialized reference is allowed only
between objects in one hierarchy, so anything crossing from a canvas to the world, or from a task to
the shared chrome, becomes an event asset under `Assets/SOAP/Events/`.

**Every task splits its logic from its drawing.** The logic side is plain C# with no `UnityEngine`
types, so an EditMode test drives it with no scene and no play mode; the `MonoBehaviour` side only
draws. `CardStacks` / `CardTableView` in Ace of Shadows is the worked example, and `FpsSampler` /
`FpsCounterView` in the shell is the same shape.

**One background for the whole app, tinted per screen.** The bootstrap scene draws the only
backdrop — a flat fill, a soft ember bloom, and a slow drift of ash. A task scene never carries its
own; it publishes two colours through SOAP and the shared ground eases toward them, so a scene swap
cross-fades the background instead of cutting it. That is also why Phoenix Flame can ask for a light
ground while the other three stay near-black, with no second background object anywhere.

**Where to find things.** Runtime code is under `Assets/Scripts/`, one folder per subsystem, most
with a `CLAUDE.md` next to the code explaining its contracts and its traps. Start at the root
`CLAUDE.md` for the index. Three assemblies: `Game.Runtime` holds all task code, with
`Game.Tests.EditMode` and `Game.Tests.PlayMode` under `Assets/Tests/`.

## Trade-offs

### Ace of Shadows — 144 real objects, no impostors

**Decision: instantiate all 144 cards and leave them alive for the whole scene.** No pooling, no
culling, no impostor strip standing in for the buried part of a deck. The alternative — draw three
sprites and fake the rest — is a real technique and it would cut the object count by two orders of
magnitude, but the brief says *create 144 sprites*, and a reviewer who opens the hierarchy and finds
three has grounds to call the task undone. The cost was measured rather than assumed: the frame rate
holds at the display rate through a full run.

**A card in flight belongs to neither stack.** The model has three places, not two, so "all
animations are finished" can only mean *the last card landed* — never *the last card left*. With a
move shorter than the one-second cadence those two moments differ, and the two-place version would
show the completion message with a card still in the air. Making the wrong answer unrepresentable
beat guarding against it.

**144 distinct cards from 13 images.** Twelve geometric glyphs times twelve colours. All art is drawn
white on transparent and tinted at runtime through `SpriteRenderer.color`, so the download carries
one small texture instead of 144. `tools/generate_card_sprites.py` draws that sheet with Pillow and
is committed alongside it, so a shape can be changed and the sheet regenerated without hand-editing
pixels.

**Layout is derived from the camera, not authored per device.** The camera is orthographic, so the
vertical extent is a fixed number of world units everywhere and only the horizontal gap between the
stacks has to react to the viewport. One clamped formula covers a portrait phone and an ultrawide
desktop window; there is no second code path and no breakpoint list.

### Magic Words — every gap in the data has one defined answer

**Decision: the parse decides, the view only draws.** `DialogueScript.FromResponse` turns the raw
payload into lines that cannot be half-formed — token already substituted, side already resolved,
initials already computed — so a row view sets fields on components and makes no decisions of its
own. Each kind of missing data gets one answer, written once: an entry with no words is dropped, an
entry with no name renders without a speaker label, an unknown speaker sits on the left, and a name
the payload lists twice keeps its first record and never fetches the second. That is what makes the
degrading behaviour testable in EditMode with no scene and no network.

**Emoji are real Unicode characters, and one the project has no glyph for still renders.** The model
emits the codepoint and no TMP markup, so the substitution is a plain string comparison in a test. The
emoji this payload names are drawn in colour from a committed sprite sheet, because Noto Color Emoji
stores its glyphs as PNGs that TextMeshPro's font pipeline reads as empty —
`tools/generate_emoji_sheet.py` lifts the bitmaps out of the font instead. Anything the sheet does not
cover falls through to a monochrome Noto Emoji that rasterizes on demand, so a mock endpoint that
changes its token set degrades to line art rather than to empty boxes. The cost is a mixed look on a
line that needs both, and a font that ships whole; `Assets/Scripts/MagicWords/CLAUDE.md` carries the
resolution order that lets the two coexist. Both Noto fonts are under the SIL Open Font License, and
each licence travels with its font in `Assets/Art/Fonts/`.

**A missing avatar becomes the speaker's initials in a circle**, not a placeholder image and not an
empty gap. The initials render first and the portrait replaces them when it arrives, so a row never
resizes when an image lands late and a dead URL is simply a row that never changes. Each distinct URL
is fetched once and a failure is remembered, so a broken link costs one request for the session
rather than one per line that speaker has, and every request carries an explicit timeout — WebGL
otherwise inherits the browser's, which can run to minutes.

**The conversation appears at once, in a scroll view.** A typewriter reveal would look livelier, but
the brief asks for nothing timed here, and a timed reveal would put the failure banner behind a wait.
There is no retry control either: the fetch runs once when the scene opens, so recovering from a
failed load means going back to the menu and re-entering. That keeps the task on one code path
instead of the re-entry guard, generation counter, and cache reset a retry button needs to be
correct.

### Phoenix Flame — the colour order lives in the graph, not in a switch

**Decision: the animator controller owns the cycle.** Three states, one trigger, and a transition
from each colour to the next — including the edge back to orange. The button raises an event, one
method pulls the trigger, and nothing in C# knows how many colours there are or which follows which.
Adding a fourth colour is an asset edit. The alternative — a `switch` that picks a colour and lerps
it — would have made the animator controller decoration around code that was already doing the work,
and the brief names the controller as the mechanism.

**An `Animator` cannot drive a particle system's start colour, so one component bridges the two.**
The clips animate an ordinary `Color` field; `FlameTint` multiplies it into each emitter's start
colour every frame. The constraint this creates turned into the best part of the effect: a start
colour only reaches particles that have yet to spawn, so a press does not repaint the fire. The new
colour enters at the base and washes up over about one particle lifetime while the old one burns off
the top, and the two hues are visibly stacked mid-blend. Which layers recolour is decided by which
ones are listed on that component — the smoke is left off the list, so it holds its own colour at
every hue.

**Four hand-tuned layers on the built-in particle system, not VFX Graph.** VFX Graph runs its
simulation on compute shaders, which WebGL does not give you; the built-in system is the one that
survives the target. The puff textures are drawn by a committed Python script the same way the
card sheet is, white on transparent, so every colour on screen comes from runtime tint. Additive
blending on the three fire layers is what makes the core go white-hot without a second colour
anywhere in the setup.

**This scene asks the shared ground to go light.** Every screen publishes two colours that the app's
one background reads (see *Architecture* above); the other three ask for near-black, and this one
asks for a mid grey. A blue flame on near-black is barely legible — which would hide a third of what
this task is graded on. Grey is the neutral all three hues read against, and it is what makes the
smoke visible, since smoke is drawn dark over a light ground rather than bright over a dark one.

**No EditMode test here, on purpose.** There is no plain-C# model to drive: the cycle *is* the
controller graph. The PlayMode test presses the real button three times and asserts the animator
settles on green, then blue, then orange again, and that each state's colour actually reached the
emitters — which is the brief's requirement stated as an assertion.

## Running it

Open the project in the editor version pinned in `ProjectSettings/ProjectVersion.txt`. A mismatched
editor silently upgrades asset formats.

The git hooks are **not** installed automatically. Run this once per clone, or none of them fire and
nothing is enforced:

```bash
git config core.hooksPath hooks
```

## Tests

EditMode tests cover the pure logic. `Assets/Tests/PlayMode/` holds the three tests that need a
running player: one boots the bootstrap scene and checks the menu loads on top of it rather than
replacing it; one enters Magic Words, leaves again while the fetch is still in flight, and fails if a
cancelled request resumes into the unloaded scene; and one walks the Phoenix Flame colour cycle
through the real button and asserts it loops back to orange. Between them they cover the
additive-load contract the whole shell rests on, the teardown path every task inherits from it, and
the one task whose behaviour lives in an asset rather than in code. Run them from
**Window → General → Test Runner**, or headlessly with the editor closed:

```bash
U="/Applications/Unity/Hub/Editor/$(awk '/^m_EditorVersion:/{print $2}' ProjectSettings/ProjectVersion.txt)/Unity.app/Contents/MacOS/Unity"; "$U" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/edit-results.xml
```

`-testPlatform` selects **one** suite. Run it a second time with `-testPlatform PlayMode` and a
different `-testResults` path, or the PlayMode suite goes unrun and reports nothing.

## How this repo is organised

`CLAUDE.md` at the root is the entry point for anyone — human or agent — picking the project up
cold: the intended architecture, the assembly layout, and the conventions that apply. Subsystem
docs sit beside the code they describe.

The project's coding rules live in `.claude/rules/` and are enforced rather than merely stated:
`hooks/claude-cs-guard.sh` checks them at edit time and `hooks/pre-commit` checks them again at
commit time. Both scan only newly-added lines, so the guard never blocks unrelated work.
