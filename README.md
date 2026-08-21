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

_To be written once the tasks land. It should answer, on one screen: how the bootstrap scene and
additive loading fit together, where the model/view seam sits in each task, and how a reader finds
the code for a given feature._

## Trade-offs

_One short section per task: the decision, the alternative, and why this side won. Written as each
task lands, not reconstructed at the end._

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
