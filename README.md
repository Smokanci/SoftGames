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

## Running it

Open the project in the editor version pinned in `ProjectSettings/ProjectVersion.txt`. A mismatched
editor silently upgrades asset formats.

The git hooks are **not** installed automatically. Run this once per clone, or none of them fire and
nothing is enforced:

```bash
git config core.hooksPath hooks
```

## Tests

EditMode tests cover the pure logic. `Assets/Tests/PlayMode/` holds a smoke test that boots the
bootstrap scene and checks the menu loads on top of it rather than replacing it — the additive-load
contract the whole shell rests on. Run them from **Window → General → Test Runner**, or headlessly
with the editor closed:

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
