---
name: gates
description: Write acceptance gates to a file before starting substantial work, then prove completion by running them instead of claiming it. Use before a multi-file change, a sweep that must cover everything, a submission pass that must cover every stated requirement, or any task where "done" has to be shown rather than asserted.
argument-hint: "[what the task is]"
---

Open a ledger before the work, not after. The failure this exists to kill is the report that says done at 80 percent: the silently narrowed sweep, the count stated from memory, the gate that passed at minute 10 and broke at minute 60.

A checklist you wrote at minute 2 is still sharp at minute 90, when the pull toward wrapping up is strongest. Your recollection is not.

## Write the gates first

Before real work starts, write `GATES.md` in the repo root — one checkbox per outcome the task actually requires:

```markdown
# Gates: Ace of Shadows

- [ ] G1: the deck is built at the size the brief names
  CHECK: grep -c "144" Assets/_Project/Scripts/AceOfShadows/DeckConfig.cs
  EXPECT: /^1$/
  EVIDENCE: pending

- [ ] G2: EditMode suite green
  CHECK: sed -n 's/.*<test-run [^>]*total="\([0-9]*\)"[^>]*passed="\([0-9]*\)"[^>]*failed="\([0-9]*\)".*/\1 total, \2 passed, \3 failed/p' Logs/edit-results.xml | head -1
  EXPECT: /[1-9][0-9]* total,.*\b0 failed\b/
  EVIDENCE: pending

- [ ] G3: the completion message appears when the last card lands
  EVIDENCE: pending
```

Five rules for writing them:

1. **One gate per outcome the task promised.** If the brief says 144 cards, one gate counts them — not "cards implemented".
2. **Give every gate a `CHECK:` you can.** A command decides; a feeling does not. `references/checks.md` holds the verified ones for this repo.
3. **A check that cannot fail is worse than no check**, because it looks like proof. `CHECK: true` and `CHECK: echo "all good"` prove the shell runs, nothing more. The checker refuses them — it reports `SUSPECT`, unticks the box, and leaves the gate unmet. Write a command whose output changes when the work is wrong, or drop the `CHECK:` and own the gate as asserted.
4. **A gate with no `CHECK:` is allowed but weaker.** The checker labels it *asserted*, not *verified*, and names it in the summary. That label is the point — see below. Much of this project's acceptance is visual, so expect a real share of asserted gates and do not disguise them.
5. **Write them before the first edit.** Gates written after the work are a description of what you did, which is the thing that cannot catch you.

## Run them

```bash
node .claude/skills/gates/gate-check.mjs
```

It runs each `CHECK:` under `/bin/sh`, compares the output against `EXPECT:` (plain substring, or `/regex/`), ticks the box, and writes the deciding output lines into `EVIDENCE:`. Exit 0 means nothing unmet, 1 means gates remain.

A long title or evidence line may wrap onto the next line; the checker joins it. A `CHECK:` or `EXPECT:` may not — joining one would change the command it runs or the pattern it matches, so the checker reports `MALFORMED`, names the line, and leaves the gate unmet. Keep both on one line however long they get.

**Do not pipe it when the exit code matters.** `gate-check.mjs | tail -40` reports the *pipe's* status, not the checker's — a run with 25 unmet gates comes back 0 and reads as clean. Redirect to a file and read it, or check `${PIPESTATUS[0]}`.

Plain exit 0 counts an asserted or abandoned gate as settled, because a human settled it. **Anything that reads the exit code to decide whether the work is proven — CI, a hook, another script — must pass `--strict`**, which succeeds only when every gate was verified by a check that ran. Without it the exit code cannot tell a result from a claim, which is the hole this whole skill is built to close.

Every check re-runs on every invocation, on purpose. A gate that passed early and broke later is exactly the failure the ledger exists to catch, so a stale tick is never trusted — a check that now fails **loses its box** and reports `REGRESSED`. Pass `--fast` to skip gates already verified when one of them is a slow Unity run and you are iterating on a different gate. `--status` reports without running or writing anything. Neither can be combined with `--strict` — both decide a gate from a recorded pass, so the checker refuses the pair rather than reporting proof for a command that never ran.

## Verified is not the same as asserted

The checker separates them and so should you:

- **verified** — a `CHECK:` ran and its output matched `EXPECT:`. This is a result.
- **asserted** — the box is ticked and the evidence line says something real, but no command backs it. This is your word.

`EVIDENCE:` lines reading `pending`, `done`, `ok`, `verified`, `confirmed`, `works`, `lgtm` and their kin are rejected outright and the gate stays unmet. They are what a report reaches for when it has nothing to show. An asserted gate must name its proof — a measurement, a quoted line of output, a file path with the line number, a screenshot path, the device and browser it was seen on:

```markdown
- [x] G7: layout holds on a phone in portrait
  EVIDENCE: iPhone 13 Safari, 390x844 — menu + all 3 tasks, no clipping, no horizontal scroll; screenshot Captures/portrait-390.png
```

When you report, say the split out loud: `9 gates — 7 verified, 2 asserted`. Never round asserted up to proven.

## When a gate is genuinely impossible

Do not quietly drop it and do not fake evidence. Add an `ABANDON:` line and say so in the report:

```markdown
ABANDON: G4 PlayMode suite needs the Editor closed; ran EditMode only
```

**The reason is mandatory.** A bare `ABANDON: G4` leaves the gate unmet — dropping a gate is allowed, dropping one silently is not, and the reason is the entire cost of abandoning. The checker counts an abandoned gate separately, names it and its reason in the summary, and fails `--strict`. A visible handover beats silent degradation. Abandoning a gate because it is hard rather than impossible is the failure this file exists to prevent, so the reason has to survive being read back.

## Report against the ledger

Re-run `gate-check.mjs` at report time — not the run from twenty minutes ago — and paste the summary line into the report. Re-measure every number you are about to state, or label it unverified. A report is a set of claims backed by a ledger, never a sense that the work feels finished.

If you notice yourself composing a summary while boxes are unchecked, that is the reflex this skill exists to interrupt. Open the gates file and pick the next unchecked box.

## Scope

Gates are for work that has to be *done*, not for every turn. No gates file for a one-line fix, a question, a conversational reply, or a tweak you can see the result of. The overhead earns its place on multi-step changes, sweeps that must be exhaustive, and any task long enough that the end of it will not remember the start.

The load-bearing use here is **the submission pass**. This repo is a graded assignment whose requirements are fixed, enumerated, and checked by someone you cannot iterate with. Open one ledger with a gate per stated requirement — the card count, the per-stack counter, the completion message, the endpoint failure paths, the colour cycle, the menu routes, the FPS readout, the responsive check on a real phone, the WebGL build, the live link in the README — and do not send the link until the ledger says so.

`GATES.md` and `gates/` are ignored by git — a ledger is a working artifact of one task, and a stale committed one reads as pending work that nobody owes, the same trap the `plan-*.md` rule guards against. Delete it when the work lands. For this repo there is a second reason: a reviewer who finds a ledger reading `3 asserted (unproven)` is reading your private working notes as a statement about the submission. The README is what the reviewer gets.

## Checks that already work here

`references/checks.md` — `CHECK:` lines for the hard-ban hook, the Unity suites (including the `-testPlatform` one-suite trap), the WebGL build output, doc conventions, and file-size limits. Read it before inventing a check, and note which ones it marks as unexecuted templates.
