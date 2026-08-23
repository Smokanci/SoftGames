# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. The project's standing rules live in `.claude/rules/` and load on their own — don't `@`-import them here, that just duplicates them in context.

## Project

**SoftGames Unity Developer Assignment** — a WebGL demo app with three self-contained showcases behind an in-game menu.

1. **Ace of Shadows** — a deck of stacked card sprites; the top card moves to another stack on a timer, with a live count over each stack and an end-of-run message.
2. **Magic Words** — character dialogue built from text plus emoji, fetched from a remote endpoint at runtime, degrading cleanly when an avatar image or a field is missing.
3. **Phoenix Flame** — a layered particle fire whose colour is driven through an Animator controller, cycling orange → green → blue → orange from a UI button.

Cross-cutting requirements that apply to all three: an in-game menu is the only entry point to each task, layout is responsive on mobile and desktop, an FPS readout sits in the top-left corner, and the whole thing ships as a hosted WebGL build linked from `README.md`.

**Status: all three tasks are built and verified in play mode.** Created from the **Universal 2D** template — `Assets/Settings/Renderer2D.asset` is the renderer. The *Intended shape* section below describes code that exists: the bootstrap scene, additive load/unload, the SOAP channels it talks over, and the three assemblies are all in place, and the menu reaches all three task scenes. `Assets/Scripts/SOAP/` holds a runtime subset ported from Kuiper-Prospector.

On scene templates: `Lit2DSceneTemplate.scenetemplate` is the right base for a scene authored in the Editor UI, but `manage_scene create` over MCP cannot use it — see the MCP notes under *Working with this codebase*. The scenes here were made from its fixed template list, which is adequate because only `Bootstrap.unity` holds a camera; a task scene that needs 2D lights has to gain them by hand.

## Intended shape

- A persistent **bootstrap scene** at build index 0 hosts the session-wide services (scene loader, FPS readout, global canvas) and is never unloaded. Menu and task scenes load **additively** on top of it and unload on exit. This is why `DontDestroyOnLoad` is banned outright — see `.claude/rules/code-conventions.md`.
- **SOAP** (ScriptableObject variables, events, and object references) carries cross-system communication. Direct references stay inside a single system.
- Each task keeps its **model separate from its view**: the logic that a test can drive headlessly on one side, the `MonoBehaviour`s that draw it on the other. That seam is what makes the assignment's testability criterion answerable.
- **Three assemblies.** `Game.Runtime` (`Assets/Scripts/`, references `SOAP.Runtime` and `Unity.TextMeshPro`) holds all task code — put scripts there, not in the predefined assembly, because a test asmdef cannot reference `Assembly-CSharp`. `Game.Tests.EditMode` and `Game.Tests.PlayMode` live under `Assets/Tests/` and both reference `Game.Runtime`. They are **not** interchangeable: the PlayMode asmdef must have an empty `includePlatforms` and must not reference `UnityEditor.TestRunner`, or its tests are Editor-only and can never run in a player build.

## Tech stack pins

Versions are pinned in `Packages/manifest.json` and `ProjectSettings/ProjectVersion.txt` — read them there rather than restating them here. Unity 6 and a WebGL target are fixed by the assignment.

## Subsystem docs (nested CLAUDE.md files)

Deep system-specific documentation is colocated with each subsystem and **loads automatically** when files in that subtree are read or edited. One per task subtree, plus one for shared infrastructure, added as each lands. Index them here as they appear.

- `Assets/Scripts/SOAP/CLAUDE.md` — events, variables, references, the asmdef, the reset and inspector-broadcast gotchas, and what was deliberately left out of the port.
- `Assets/Scripts/Bootstrap/CLAUDE.md` — the persistent scene's contract: what a task scene may not bring, the SOAP scene-swap channels, the shared Ember Dust ground and the two SOAP colour channels each scene tints it with, the pause overlay and why it lives on `TaskChrome` instead of here, the ordering a task scene can rely on, and the FPS model/view seam.
- `Assets/Scripts/AceOfShadows/CLAUDE.md` — the three-place card model and why it exists, the camera-derived layout formula, card identity and sorting, the sprite sheet and the atlas that was abandoned, and the one SOAP channel this task uses.
- `Assets/Scripts/MagicWords/CLAUDE.md` — the DTO-to-view pipeline, what every kind of missing data turns into, why the emoji arrive as a TMP sprite asset with a dynamic font asset behind it, the avatar cache, the staged reveal and the render-once invariant it rests on, and the one SOAP channel this task uses.
- `Assets/Scripts/PhoenixFlame/CLAUDE.md` — why the cycle order lives in the animator graph, the animated-colour bridge into the emitters and its Constant-mode constraint, the four layers and their blend modes, the noise generator's two traps, why this scene alone asks the shared ground to go light, and the one SOAP channel this task uses.
- `Assets/Scripts/UI/CLAUDE.md` — the shared Ember button: the heat model and its test seam, the one style asset every button reads and the two fields in it that carry meaning a number cannot, why the parent picks the one idle glow, the commit feedback and its `_IsLoadingScene` read, the prefab contract and its label-or-glyph caption slot, the additive shader and its mask limitation, the two Editor-capture traps, and the app's two font assets with the fallback order that keeps Magic Words legible.

Per-class internals and lookup tables go in a plain-`.md` sibling of the subsystem's `CLAUDE.md`, not inside it, and get indexed here too — siblings do not auto-load and are otherwise invisible from a cold session. The rule and its reasoning are in `.claude/rules/project-conventions.md`.

## Working with this codebase

- **Meta files are source.** Every asset has a `.meta` with its GUID; an orphaned `.meta` breaks references silently. Don't read `.meta` files for context — they only carry GUIDs and importer settings. `hooks/pre-commit` blocks an added asset whose `.meta` isn't staged with it, and the reverse.
- **YAML scenes/prefabs** (`*.unity`, `*.prefab`, `*.asset`) are text but should be edited via the Editor. **Don't read them for context** — they're serialized component graphs, not source of logic. Read the C# instead. Direct YAML edits are fine for trivial in-place tweaks when they're cheaper than going through Unity MCP; reach for MCP when the change involves wiring refs, adding components, or anything that touches GUIDs/fileIDs.
- **Never hand-edit a `.unity` scene file directly** — scene changes go through Unity MCP. If the scene is open in the Editor, a direct file edit triggers a "reload scene?" modal that **stalls the MCP bridge** until dismissed. `.prefab` and `.asset` trivial direct edits are fine; only loaded `.unity` files have this hazard.
- **Don't touch `Library/`, `Temp/`, `Logs/`, `UserSettings/`** — Unity-generated, gitignored.
- **Git hooks live in `hooks/`** (repo root) and are **not** auto-installed. Run `git config core.hooksPath hooks` once — until you do, **none** of the hooks run and nothing is enforced.
  - `pre-commit` enforces the hard bans in `.claude/rules/code-conventions.md`: `DontDestroyOnLoad` and null guards on wired refs. It also blocks a staged asset whose `.meta` isn't staged with it. It scans only newly-added staged lines, so legacy code never blocks; exempt one justified line with a trailing `SG-ALLOW` comment, or bypass the whole hook with `git commit --no-verify`.
  - `commit-msg` strips `Co-Authored-By` trailers.
  - `wired-idents.sh` is not a git hook and runs regardless of `core.hooksPath` — it is the shared "which idents are expected to be wired" scanner, so the two null-guard enforcement points can't drift on what counts as wired.
  - `claude-cs-guard.sh` is not a git hook either: it is a Claude Code `PostToolUse` guard wired from `.claude/settings.json`, running the same bans at edit time so a violation surfaces next to the edit instead of at commit.
- **Claude Code enforcement lives in `.claude/settings.json`**, not in prose: `permissions.deny` blocks `Edit`/`Write` on `**/*.unity`, reads of `**/*.meta`, and access under `Library/`, `Temp/`, `UserSettings/`. The Unity MCP tools aren't `Edit`/`Write`, so the sanctioned scene-editing path stays open, and `Bash` isn't covered either — the deny list is a guardrail against casual access, not a sandbox. `hooks.PostToolUse` runs `hooks/claude-cs-guard.sh` after every `Edit`/`Write` and pulls Unity console errors back into context after any C# change. The guard's matcher is `Edit|Write` only, so C# that reaches disk any other way — the MCP script tools, or a `Bash` heredoc, `sed` or script — skips the edit-time check entirely and is caught at commit time by `hooks/pre-commit` instead.
- **Unity MCP** is checked in at `.mcp.json` (server name `UnityMCP`) so a fresh clone gets it. A per-user entry of the **same name** in `~/.claude.json` takes precedence. Precedence is by name and the name is case-sensitive — a local entry spelled differently runs *alongside* this one: two servers on one Editor, two copies of every tool schema in context, and a `mcp__UnityMCP__*` hook matcher that silently misses the other spelling. Keep exactly one, spelled `UnityMCP`.

### Unity MCP working notes

Each of these cost real time to find. They are quirks of the bridge, not of this project.

- **Issue MCP calls one at a time.** Two `mcp__UnityMCP__*` calls in the same block collide on the bridge and come back "Could not connect to Unity". The general "batch independent tool calls" advice does not apply here.
- **`manage_gameobject` with `save_as_prefab` writes nothing.** It reports "No modifications applied" and still returns success. Use `manage_prefabs action=create_from_gameobject` instead.
- **Component properties passed inside a `create` call do not reliably stick.** Create the component, then set each load-bearing property with its own `manage_components action=set_property`, and read it back when the wiring matters.
- **`manage_scene create` accepts only `empty`, `default`, `3d_basic`, `2d_basic`.** It cannot take a `.scenetemplate` asset.
- **The Editor barely advances frames while its window is unfocused**, so anything time-driven — the FPS readout, a timed animation — reads as frozen or zero when you drive play mode over MCP from the background. `PlayerSettings.runInBackground` is enabled here for exactly that reason; if a fresh checkout shows 0 FPS, check it first before suspecting the code.
- **When the Editor will not compile, run the headless suite instead of poking the GUI.** A real compiler error and a modal-blocked Editor look identical from outside, and only the batch run prints the error list. The invocation is in `README.md`. Never add `-quit` to a `-runTests` run — it exits before the tests execute and returns 0 with no results file.

## Skills

Three live in `.claude/skills/`, invoked as slash commands:

- **`/gates`** — write an acceptance ledger *before* substantial work, then prove completion by running it. The load-bearing use here is the submission pass: one gate per stated requirement, run before the link is sent. `gate-check.mjs` separates *verified* (a command ran and matched) from *asserted* (your word), and refuses evidence lines that say "done". `references/checks.md` holds the recipes, marked for which have actually been run here.
- **`/review-diff`** — the working-tree quality pass, run after a change and before committing. Checklist plus a one-issue-at-a-time walkthrough.
- **`/commit`** — commit staged work, split by concern, with the `.meta` pairing and hook-rejection rules this repo needs.

`GATES.md` and `gates/` are gitignored on purpose — a ledger is a working artifact of one task, and a committed stale one reads as pending work nobody owes.

## README is a deliverable

`README.md` is graded. It is read by someone with no context who will not open the Editor: hosted link first, then a one-screen architecture overview, then the trade-off taken per task and why. `.claude/rules/project-conventions.md` holds the full rule.
