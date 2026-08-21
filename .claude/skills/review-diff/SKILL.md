---
name: review-diff
description: Review pending changes (working tree + staged) against a correctness/cleanup/project-specific checklist, then walk the findings one at a time. Use after completing a change, before committing.
allowed-tools: Read, Grep, Glob, Edit, Write, AskUserQuestion, Bash(git status *), Bash(git diff *), Bash(git log *), Bash(git ls-files *), Bash(git show *), Bash(git rev-parse *), Bash(grep *), Bash(wc *), Bash(xargs *), Bash(sort *), Bash(sed *), Bash(find *)
---

<!-- Do NOT add `context: fork` or an `agent:` pin to this skill. Check 3 ("Intent") reads the
     conversation above; a forked subagent has no conversation history and that check would
     silently become a no-op. -->

Review the pending changes. This is the focused working-tree pass; for branch- or PR-scoped review reach for `/code-review` instead.

Gather everything not yet committed:
- `git diff HEAD` — all tracked changes, staged and unstaged.
- `{ git diff HEAD --name-only --diff-filter=ACMR -- '*.cs'; git ls-files --others --exclude-standard -- '*.cs'; } | xargs wc -l | sort -rn` — whole-file line counts for every touched `.cs`, untracked included. This is the evidence for check 11; run it mechanically here so file sizes are on the record before the checklist pass — don't leave it to be remembered mid-review.
- `git status --short` — to find untracked `??` files (they produce no diff output and otherwise escape review entirely). In a project this young most of the diff *is* untracked files, so this is the main input, not an afterthought. Handle by type:
  - **`.cs` and trivial `.asset`** — read directly and apply the full review criteria below.
  - **`.unity` / `.prefab`** — do **not** read the YAML and review it as logic; that's against project convention (serialized component graphs aren't source, and they're edited via the Editor). Only confirm the `.meta` travels with the new asset and note that it was added — wiring correctness is verified in the Editor, not by reading the graph.

If both come back empty, report "Nothing to review" and stop — do not manufacture a review of an empty diff.

## General (always evaluate)

1. **Correctness** — bugs, off-by-one errors, incorrect assumptions. Do not flag *missing* null checks on serialized fields, `GetComponent` results, or SOAP refs — these are expected to be wired and a `NullReferenceException` is the correct failure signal. Do flag ✗ any null guard the diff *adds* on those same refs — `== null`, `!= null`, `?.`, `??` — and recommend stripping it, unless it lands in a documented carve-out from `.claude/rules/csharp-conventions.md`, which you should name. The reverse also holds: a network body, a deserialized DTO field, or a downloaded texture is *data*, not wiring, and code that fails to validate it is a ✗ finding, not a convention win.
2. **Completeness** — does the implementation cover what was requested?
3. **Intent** — based on the conversation above, does the change do what was asked?
4. **Dead code** — unused variables, methods, classes, interfaces, `using`s left behind.
5. **Cleanup** — leftover scaffolding, now-redundant abstractions, obsolete comments.
6. **Refactoring** — anything that warrants restructuring now, judged from the diff. Duplication *against code the diff doesn't touch* won't be visible from these hunks and is out of scope here.
7. **Security** — exposed secrets, unvalidated external input, insecure defaults.
8. **Unintended breakage** — *silent* breakage of existing assets: serialized field rename/reorder, dangling reference. Deliberate format breaks are fine, nothing here ships to users.
9. **Side effects** — behaviour affected outside the change's stated scope.
10. **Conventions** — does the change follow `.claude/rules/code-conventions.md`?
11. **File size / SRP smell** — read the `wc -l` output gathered above: a file past ~600 lines, 4+ unrelated `[Header]` groups, or several unrelated concerns in one type is a god-object smell. The line count is a trigger for scrutiny, not a verdict — a flat data/SO definition that owns one reason to change passes regardless of length. When the smell is real, flag it ⚠ and name the extraction seam you'd cut along (strategy, static helper, plain collaborator) — **even when this diff's own hunk is tiny**, since the file grows one reasonable diff at a time and that's exactly how it escapes notice.
12. **Model/view seam** — the project's testability claim rests on task logic being drivable without a scene. Flag ✗ a `MonoBehaviour` that has absorbed rules a plain C# type should own (deck order, dialogue assembly, colour sequencing), and name the type to extract. This is the check that decides whether the tests can exist at all, so it outranks style.

## Project-specific (only report when the diff touches relevant files; otherwise omit)

13. **Meta file integrity** (assets added/moved/deleted) — any asset without its `.meta`. Focus on moves and deletions; `hooks/pre-commit` already blocks the added-file case. Before filing a missing-`.meta` finding, confirm the repo tracks `.meta` for that file type at all — a repo-wide absence means the project doesn't commit them, and the finding is a false positive.
14. **SOAP wiring** (diff adds a `Raise`, a listener, or a SOAP consumer) — is the channel actually wired? A raised event with no listener, a new listener whose event ref is unset, or a SO ref that NREs on first use is a silent failure. Verify from the C# and the asset wiring you can see; when Unity is connected, confirm in the Editor. Do not block the review on a missing Unity session — say the wiring is unverified instead.
15. **Additive-scene safety** (diff touches scene loading, bootstrap services, or anything that runs at scene start) — a serialized ref reaching from one scene into another, a lookup that assumes another scene's `Awake` already ran, or a service the task scene expects the bootstrap scene to have published. These fail only in the built player and only sometimes, which is the worst way to find them.
16. **WebGL viability** — anything that compiles in the Editor and dies in a browser build: blocking waits on `System.Threading`, `File` IO outside `Application.persistentDataPath`, compute shaders, VFX Graph, synchronous web requests, `Application.Quit`, or reflection that IL2CPP stripping will remove. This project's only deliverable is a WebGL build, so an Editor-only success is not a success.

## Doc-accuracy pass

Run this **before** reporting, not after — its findings enter the same numbered index as everything else, so one walkthrough disposes of the whole review. Scope it to what this diff touches, not a repo-wide sweep:

1. **Drift from the change.** If the diff renames a public symbol, changes an `enum`'s members, or alters a count/list/path that docs quote, search `CLAUDE.md` files for the old name (Grep tool) and flag every stale hit. This is the dominant drift mode — a doc quoting a symbol that just got renamed.
2. **Touched-subtree verification.** For each subsystem the diff edits, spot-check the nested `CLAUDE.md` for that subtree (if one exists) against the now-changed code — symbol names, execution orders, list contents, file paths — and flag any that the change made stale.
3. **New/changed documented surface.** If the change introduces, removes, or significantly alters a system, package, architectural decision, or convention, flag the nested `CLAUDE.md` for the touched subtree as needing an update if one exists; otherwise the root `CLAUDE.md`, but only for project-wide concerns.
4. **The status paragraph.** The root `CLAUDE.md` opens with a status paragraph, and so does every nested one. Re-read the ones this diff touches against what just landed and flag any the change made stale — a system it still calls a plan, a stub it still calls unbuilt, a count it still quotes. It is the first thing a cold reader trusts, so it is the most expensive line in the file to leave rotting.

A stale doc is a finding like any other and goes through the same walkthrough — do not edit a `CLAUDE.md` here.

## Reporting

Evaluate each category internally as ✓ (no issues), ⚠ (minor concern), or ✗ (needs fixing) — but do not emit a line per category. The report is **plain text in the response**. Do **not** call `ReportFindings`.

Deliver it in three phases, in this order.

### Phase 1 — the summary

**Verdict:** one headline line — the ✗/⚠ split and nothing else. `2 blockers, 1 minor` · `No blockers, 2 minor` · `No issues found`.

**The index** — a numbered list, one line per issue, **most-severe first** (every ✗ above every ⚠):

```
1. ✗ [CardStack.cs:212](Assets/_Project/Scripts/AceOfShadows/CardStack.cs:212) — no clamp, indexes off the end
2. ⚠ [FpsCounter.cs:88](Assets/_Project/Scripts/Common/FpsCounter.cs:88) — unused local
```

Line rules:

- **Number** — 1-based. This is the handle the user refers to for the rest of the session, so it never renumbers: a skipped issue keeps its number.
- **Sev** — `✗` or `⚠`, nothing else. This is the severity channel; never restate severity in prose.
- **Link** — link *text* is the basename plus `:line`; the href is the working-directory-relative path, which is what both `git diff` and `git status` print here since the repo root is the Unity project root.
- **Label** — six words at most, naming the defect. Not the fix, not the consequence — both come in phase 2.

Deduplicate: one entry per issue even when it trips several categories, at its true severity.

**Coverage:** a single line listing each category checked with its verdict — e.g. `✓ correctness · ⚠ dead code · ✓ model/view · …`. Omit project-specific categories with nothing in the diff to judge rather than emitting a vacuous ✓: Coverage asserts what was examined, not what was absent. The doc-accuracy pass is not conditional and always appears, as `doc accuracy` — it ran even when the touched subtrees have no `CLAUDE.md`, because establishing that is the check.

Omit the index entirely when there are no findings — a clean diff ends at the Verdict and Coverage lines, and manufacturing a minor concern to fill the list is a worse outcome than an empty one. Stop there; there is nothing to walk.

### Phase 2 — the walkthrough

Take the issues **one at a time, in index order**. For each, print the card and *then* call `AskUserQuestion`. Never batch two issues into one call, and never print the next card before the current one is answered — one issue on screen at a time is the whole point.

```
### 1 of 3 · ✗ [CardStack.cs:212](Assets/_Project/Scripts/AceOfShadows/CardStack.cs:212) — `CardStack.Pop`

**Issue** — the defect *and* its concrete consequence, a couple of sentences. For a bug: the inputs or state that produce the wrong output or crash. For a finding with no runtime failure mode (dead code, cleanup, an SRP smell, a convention breach, a stale doc): who hits it and what goes wrong — "a reader follows the doc, calls `Swap`, and gets a compile error". Never invent a crash that can't happen.

**Recommended fix** — what to change, not a restatement of the problem. Name the carve-out, rule, or file when the fix cites one. Include a minimal code sketch whenever prose alone leaves the edit ambiguous: this card is the only place the fix is spelled out, so don't defer it to a later section.
```

The heading carries the full location — the link, `:line` when the finding anchors to one, then ` — ` and the in-file address that actually locates it: the method, the serialized field, the GameObject path in a scene, the doc heading. A bare file path for a 900-line file is not a location.

The `AskUserQuestion` call offers exactly three options, in this order:

1. **Fix it** — apply the recommended fix. Mark it `(Recommended)` when the fix is unambiguous and self-contained; leave it unmarked when it turns on a judgment call that is the user's to make.
2. **Skip it** — leave the code as it is.
3. **Tell me more** — answer what's unclear, then re-ask the **same** issue. Never advance the walkthrough on this answer.

The free-form "Other" is the escape hatch and must be honored as written: "fix all the rest", "stop here", "fix it but keep the comment", "show me that file first". Anything that isn't a disposition is a request to answer and then re-ask the same issue.

**Write nothing during phase 2.** Record each decision and move on — collecting first is what lets the user see every disposition before any file changes.

### Phase 3 — apply

Once every issue has a disposition — or the user stops the walk early — apply the fixes approved so far in one pass, then close with two lines:

**Applied:** `1, 3` — the files touched.
**Skipped:** `2`.

An early stop applies what was already approved and leaves the rest untouched; add a third line — **Not walked:** `4, 5` — for the issues that never got a card, so every index entry is accounted for and an unseen issue never masquerades as a declined one. A decision the user has already made is not discarded by stopping.

If nothing was approved, say that in one line rather than emitting empty headings. Apply only what was approved — a fix that turns out to need a change the user didn't agree to stops and asks rather than widening on its own.
