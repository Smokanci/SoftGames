# CHECK recipes for this repo

> **Status:** partial. *Hard bans*, *Doc conventions*, *Code size and review coverage*, both *Submission* recipes, and the **EditMode** suite recipe were run against this repo and their output recorded below. The **PlayMode** recipe now has a suite to discover but has not itself been run headlessly here, and everything under *WebGL build* remains an **unexecuted template** — nothing has been built yet. Re-run and re-record them once there is something to run, and correct any recipe that turns out wrong.

Run everything from the repo root. `CHECK:` lines execute under `/bin/sh`, not zsh. This matters: zsh aborts a command whose glob matches nothing (`no matches found: Docs/*.md`), where `/bin/sh` passes the pattern through and the loop's `[ -e "$f" ]` guard handles it. A recipe that works when you paste it into your terminal can still fail as a gate.

`EXPECT:` is a plain substring, or `/regex/`. Output is trimmed before matching, so a trailing newline never decides a gate — and neither does the leading whitespace macOS `wc -l` prints.

**Never gate a count on a bare substring.** `EXPECT: 0 failed` is contained in `10 failed`, and `EXPECT: 0` is contained in `30` — the gate then reports green on the exact answer you were guarding against. Every count below is anchored instead: `/^0$/` for a lone number, `/\b0 failed\b/` inside a sentence.

Anchors without the `m` flag bind the *whole* output, not one line of it. That is deliberate for the scan gates here: `/^status scan complete$/` matches only when the completion line is the sole output, so any `NO STATUS` line printed above it fails the gate.

## Hard bans

`hooks/pre-commit` enforces the bans in `.claude/rules/code-conventions.md` against staged lines. Run it as a gate before committing, rather than discovering the block at commit time:

```
CHECK: git add -A && sh hooks/pre-commit && echo "hard bans clean"
EXPECT: hard bans clean
```

Verified: prints `hard bans clean`. Drop the `git add -A` if you are staging deliberately. With an empty index it exits 0, so this gate only means something once your changes are staged — an unstaged working tree passes it vacuously.

## Unity test suites

*The EditMode recipe has been run here and its output is recorded below. The PlayMode one has a
suite to discover — `Assets/Tests/PlayMode/BootstrapSmokeTests.cs`, green in the editor's Test
Runner — but the batch invocation below has not been executed here yet.*

`-testPlatform` selects **one** suite. A green EditMode run reports success having never loaded a scene, so a single test gate is a gate that cannot fail for the reason you care about. Write two.

**Never pass `-quit` with `-runTests`.** It exits the editor before the run finishes: the process returns 0, the log shows a clean compile, and no results file is ever written — which reads exactly like success until you look for the XML. This bit here, inherited from a recipe that carried `-quit` and had never been executed.

**A suite gate must require a nonzero total.** `EXPECT: /\b0 failed\b/` also matches `0 total, 0 passed, 0 failed`, so an empty or undiscovered suite reports green. Anchor on the count as well.

Resolve the editor from the version pin rather than hardcoding a path, so the check follows `ProjectSettings/ProjectVersion.txt` when the project upgrades.

**EditMode:**

```
CHECK: U="/Applications/Unity/Hub/Editor/$(awk '/^m_EditorVersion:/{print $2}' ProjectSettings/ProjectVersion.txt)/Unity.app/Contents/MacOS/Unity"; "$U" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/edit-results.xml >/dev/null 2>&1; sed -n 's/.*<test-run [^>]*total="\([0-9]*\)"[^>]*passed="\([0-9]*\)"[^>]*failed="\([0-9]*\)".*/EditMode: \1 total, \2 passed, \3 failed/p' Logs/edit-results.xml | head -1
EXPECT: /[1-9][0-9]* total,.*\b0 failed\b/
```

**PlayMode** — same shape, different platform and a different results path. Reusing `edit-results.xml` silently overwrites the first suite's evidence.

```
CHECK: U="/Applications/Unity/Hub/Editor/$(awk '/^m_EditorVersion:/{print $2}' ProjectSettings/ProjectVersion.txt)/Unity.app/Contents/MacOS/Unity"; "$U" -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults Logs/play-results.xml >/dev/null 2>&1; sed -n 's/.*<test-run [^>]*total="\([0-9]*\)"[^>]*passed="\([0-9]*\)"[^>]*failed="\([0-9]*\)".*/PlayMode: \1 total, \2 passed, \3 failed/p' Logs/play-results.xml | head -1
EXPECT: /[1-9][0-9]* total,.*\b0 failed\b/
```

Verified: the EditMode recipe prints `EditMode: 5 total, 5 passed, 0 failed`. The word boundary in `EXPECT` is load-bearing — a bare `0 failed` substring is also contained in `10 failed`, so a double-digit failure count would read as green. A missing or unwritten XML prints nothing, so the gate fails rather than passing on absence, which is what you want when the editor never started.

The first run after a package change is consumed by the reimport and executes nothing. Run it twice, or treat a missing results file as "not run" rather than as a failure.

Set a generous timeout on these: `node gate-check.mjs --timeout 900`.

## WebGL build

*Unexecuted template — nothing has been built yet.* Adjust the output path to wherever your build actually lands.

**The build exists and has its loader:**

```
CHECK: for f in index.html Build/*.loader.js; do [ -e "Builds/WebGL/$f" ] || echo "MISSING $f"; done; echo "webgl output complete"
EXPECT: /^webgl output complete$/
```

**Compression matches what the host serves.** A `.br` build on a host that sends no `Content-Encoding` fails to load, and it fails only in the browser — never in the Editor. Gate the pair together: what the build produced, and whether the host config that serves it is present.

```
CHECK: ls Builds/WebGL/Build/ | sed -n 's/.*\.\(br\|gz\|unityweb\)$/\1/p' | sort -u | tr '\n' ' '; echo; [ -f Builds/WebGL/_headers ] && echo "headers present" || echo "NO _headers"
EXPECT: headers present
```

If you host somewhere that sets encoding for you, replace this gate with an asserted one naming the host and the loaded URL.

**Build size.** Read the number once, then gate it so a careless import cannot double it unnoticed:

```
CHECK: du -sk Builds/WebGL | awk '{printf "%d MB\n", ($1+1023)/1024}'
EXPECT: /^<the size your first run printed> MB$/
```

*Template, not a measurement* — run it, read the number, write it into `EXPECT`.

## Doc conventions

**Status blockquote on every doc** — `.claude/rules/project-conventions.md` requires one directly under the title:

```
CHECK: for f in Docs/*.md; do [ -e "$f" ] || continue; awk 'NR<=6 && /^>/{ok=1} END{exit !ok}' "$f" || echo "NO STATUS: $f"; done; echo "status scan complete"
EXPECT: /^status scan complete$/
```

Verified: prints `status scan complete`. `Docs/` does not exist yet, so it currently passes vacuously — the `[ -e "$f" ]` guard is what keeps it from erroring instead.

**No landed plan left behind** — a `plan-*.md` is deleted once its work ships:

```
CHECK: ls Docs/plan-*.md 2>/dev/null | wc -l
EXPECT: /^0$/
```

Verified: `0`.

## Code size and review coverage

**Nothing new past the SRP line.** The threshold lives in `.claude/rules/code-conventions.md`; this reports the offender count so the gate is about it not growing:

```
CHECK: git ls-files -- 'Assets/*.cs' | xargs wc -l 2>/dev/null | awk '$2!="total" && $1>600 {print $1, $2}' | wc -l
EXPECT: /^0$/
```

Verified: `0` today, because no C# is tracked yet. Set `EXPECT` to whatever the count is when you open the ledger, so the gate fails when your change adds one rather than when it merely notices an existing file.

**No untracked `.cs` escaping review.** Untracked files produce no diff output and otherwise skip a review entirely:

```
CHECK: git ls-files --others --exclude-standard -- '*.cs' | wc -l
EXPECT: /^0$/
```

Verified: `0`.

## Submission

**The README carries a live link.** The single most expensive thing to forget, because the reviewer hits it first:

```
CHECK: grep -c 'https://' README.md 2>/dev/null || echo 0
EXPECT: /^[1-9]/
```

Verified as a command: prints `0` today, so the gate correctly fails. `README.md` exists but carries no
`https://` link, because the build is not hosted yet — which is the state this gate is built to catch.

**Every task is reachable from the menu.** Gate the build's scene list against the count you expect, rather than trusting that you added the last one:

```
CHECK: grep -c 'path: Assets/' ProjectSettings/EditorBuildSettings.asset
EXPECT: /^5$/
```

Verified: prints `5` — bootstrap, menu and the three task scenes, all registered. Correct the number if the scene layout changes.

## Checks that do not fit a CHECK line

Unity console errors, in-editor wiring, and anything needing a live Editor go through Unity MCP, not `/bin/sh`. Those are asserted gates: tick them with the MCP output quoted in `EVIDENCE:`, and let the summary show them as asserted. Do not wrap an MCP call in a fake shell check to make the ledger look greener than it is.

The same applies to most of what this assignment is actually graded on — whether the card motion reads as smooth, whether the fire looks good, whether the layout holds on a real phone. No command decides those. Assert them, and make the evidence line name the device, the browser, the resolution, and the screenshot path, so the claim can be re-checked by someone who was not there.
