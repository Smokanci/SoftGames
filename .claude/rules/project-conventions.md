## Documentation

- **Never mirror a tuned value into a doc.** A `CLAUDE.md` literal that restates an asset/field value (a card count, a duration, a colour, "today: `_FooData`") drifts the moment the asset is retuned, and the doc then lies with authority. Point at the source instead — name the SO, the field, or the folder and let the reader open it.
- **Do document what a value can't tell you:** invariants, formulas, units, ordering constraints, and worked examples. Those don't drift when a number changes.
- Docs describe the *current* state. Historical cautionary tales are fine but must read unambiguously as past ("X reached 900 lines before it was split"), never as present tense.
- **Per-class internals and lookup tables live in a plain-`.md` sibling** of the subsystem's `CLAUDE.md`, not inside it — a `CLAUDE.md` pays its context cost on every read of its subtree. The parent points at the sibling, and the root `CLAUDE.md` indexes them, because they do not auto-load and are otherwise invisible from a cold session.

## Design docs & plans

- **Every doc opens with a status blockquote** — a `>` paragraph directly under the `#` title saying where the work actually stands and pointing at the code that proves it (label it `**Status:**` when the blockquote also carries design prose, so the standing is findable). A doc with no status line is indistinguishable from one nobody started.
- **An implementation plan is named `plan-*.md`** — prefix, not suffix — and is **deleted once its work lands**. A landed plan left reading as pending is worse than no plan: the next reader implements it twice. Before deleting, skim it for sections that are still *live backlog* rather than completed work and relocate those into the owning subsystem's `CLAUDE.md` — a plan is often 90% done with two unstarted follow-ups buried in it, and deleting it wholesale silently drops them.

## Reviewer-facing README

This repo is a submitted assignment, so `README.md` is read by someone with no context and no
intention of running the Editor. It carries, at minimum: the hosted WebGL link, a one-screen
architecture overview, the trade-offs taken per task with their reasons, and the setup line that
installs the git hooks (`git config core.hooksPath hooks`) — until a contributor runs that, none of
the hooks fire.
