---
paths: ["**/*.cs"]
---

## C# conventions — rationale and carve-outs

The bans themselves are in `.claude/rules/code-conventions.md` (always loaded). This file holds the
*why*, the exact carve-outs, and the worked examples for the rules that need them. It loads when a
`.cs` file is read — if you are editing C# through Unity MCP only, read it explicitly.

### Serialized field encapsulation

Never `public` for inspector-exposed data. Use `[SerializeField] private T name;` plus
`public T Name => name;` when external code needs read access. **New files have no exemption** —
author them correctly from the start.

### `[SerializeField]` inside `#if`

Unity serializes field references by name; stripping a field in one build config leaves the
scene/prefab YAML with a dangling reference and produces "Serialized field not found" warnings. Gate
*behaviour* (method bodies, `AddListener` calls, `SetActive`) behind `#if` — keep the field
declarations unconditional. This bites hardest on `UNITY_WEBGL` / `UNITY_EDITOR` splits, which this
project has by definition.

### Tuned values are serialized; structural constants are not

A number that decides how something **looks or feels** is set by whoever is looking at the screen,
not by whoever recompiles. Durations, rates, colours, intensities, distances, thresholds, counts of
things a designer would add or remove — all `[SerializeField]`, with a `[Tooltip]` when the units or
the meaning are not obvious from the name. Burying one in a `const` costs a compile and a domain
reload per attempt, which in practice means it is tuned once and never revisited.

A number that decides **what is correct** stays a `const`. A stride into an array, a sorting-order
budget, a bit width, an epsilon, a required element count: there is one right answer, and exposing it
invites a value that silently breaks the arithmetic around it. Making it editable is not flexibility,
it is a foot-gun with an inspector row.

The test is what a change to the number does. If it produces a different-looking but still-correct
build, serialize it. If it produces a broken one, leave it `const` and let the name carry the reason.

```csharp
✓  [SerializeField] [Min(0.001f)] private float fadeSeconds = 0.25f;   // feel — the designer's call
✓  private const int ChannelsPerVertex = 4;                            // the format says 4, not taste
✗  private const float FadeSeconds = 0.25f;                            // feel, locked behind a compile
✗  [SerializeField] private int channelsPerVertex = 4;                 // set it to 3 and nothing draws
```

Both live cases are worth reading: `EmberGround` in `Assets/Scripts/Bootstrap/` for the serialized
side, and `CardTableView.OrdersPerCard` in `Assets/Scripts/AceOfShadows/` for the structural one.

Two consequences worth stating. **Serialized beats `const` even for a value nothing has retuned yet**
— "we only ever use 3" is a statement about today, and the whole cost of the rule is one inspector
row. And guard the values where a bad entry breaks the arithmetic rather than merely looking wrong —
a denominator gets `[Min(0.001f)]`, so a zero is unenterable instead of producing `NaN` or a frozen
ease. A fraction that is merely out of taste needs no attribute; say the intended range in the
`[Tooltip]` and trust the person tuning it. `Assets/Scripts/UI/EmberStyle.cs` is the worked example of
both halves.

Where several components share one feel, the tuned values move to a `ScriptableObject` that each
reads — `Assets/Data/EmberStyle.asset` is the worked example. Do not copy a value out of a shared
asset into a `const` somewhere else "to match it": the copy stops matching the moment the asset is
retuned, and the comment claiming they agree is what makes the drift hard to find.

### Always write the access modifier

C# defaults an unmarked member to `private`, so `void OnEnable()` and `private void OnEnable()`
compile identically. Write the second one anyway. The bare form is ambiguous to a reader: it does not
say whether the author *chose* private or simply never thought about the visibility, and that is
exactly the question a reviewer asks about a method that could plausibly be part of a public surface.
Unity message methods are the common offender and get no exemption — `Awake`, `OnEnable`, `Update`
and the rest are called by the engine, not by other types, so they are `private` and should say so.

This covers every member, not just methods: fields, `const`s, and properties too. Order the modifiers
the way the compiler prints them — `private static readonly`, never `static private readonly`.

```csharp
✗  void Update()                         // private by default — but was that deliberate?
✗  static void ResetAllInstances()
✗  async Awaitable SwapTo(string name)
✗  [SerializeField] TMP_Text label;
✗  readonly float[] _frameTimes;
✗  const int AfterEveryOtherStart = 999_999;

✓  private void Update()
✓  private static void ResetAllInstances()
✓  private async Awaitable SwapTo(string name)
✓  [SerializeField] private TMP_Text label;
✓  private readonly float[] _frameTimes;
✓  private const int AfterEveryOtherStart = 999_999;
```

Keeping a field block's columns aligned is worth the reflow — `private ` is the same width on every
line, so an aligned block stays aligned.

**Review-enforced.** Roslyn will not flag it; `.editorconfig` carries no severity for
`dotnet_style_require_accessibility_modifiers` here.

### SOLID, broadly

One reason to change per class. When a class starts mixing concerns, split it. **Heuristic trigger:**
a single `.cs` past ~600 lines, or a `MonoBehaviour` whose serialized fields span 4+ unrelated
`[Header]` groups, is a smell — stop and extract a responsibility (a strategy, a static helper, a
plain collaborator). Line count is a proxy, not a cap: a flat data/SO definition can legitimately be
long; the real test is how many *reasons to change* the type owns.

The specific shape to protect here: **the simulation model and the view are separate types.** A card
stack knows its own order and raises a change event; a renderer draws it. A dialogue script knows its
lines and speakers; a chat view lays them out. Keeping that seam is what makes the logic testable
without a scene, which is the point.

### No singletons, no static runtime registries

Don't introduce `static Instance { get; }` accessors, `GetInstance()` static methods, or
`static List<T> Active` registries for project runtime types. Cross-system runtime discovery goes
through SOAP — pick whichever fits the shape: `GenericVariable<T>` / `BoolVariable` for shared state,
`GameEvent<T>` for fan-out. For runtime-`AddComponent` paths, instantiate the GO inactive, set the
ref via a public setter, then
`SetActive(true)` so `OnEnable` sees the wired ref. The only acceptable static state is **Editor-only**
and pure helpers with no instance state.

### No cross-hierarchy scene references

A serialized scene reference (GameObject, Component, RectTransform — anything that lives in a scene,
not an asset) on component A may only point at GameObject B if B is A's parent, child,
sibling-descendant, or A's own GO. Routing a driver through a scene-root "controller" GO that
references panels under a far-away canvas is **not** allowed — put the driver on the panel's immediate
parent and target the child. Same rule for `[SerializeField] UnityEvent` persistent calls: don't wire a
UnityEvent on A to a method on far-away component B; raise a SOAP `GameEvent*` and let B subscribe via
its own listener.

Reason: cross-hierarchy refs make scenes brittle — moving or renaming the target silently breaks the
link, and the dependency is invisible from the source GO. Parent↔child keeps every dependency visible
right under the GO that owns it; cross-hierarchy comms goes through globally-discoverable SOAP assets
which can't dangle silently. SOAP asset refs are not scene refs and are unaffected by this rule.

This rule does real work in an additive-scene setup: a menu scene must never hold a serialized ref
into a task scene, because the task scene isn't loaded when the menu wires up.

### No defensive null checks — the five carve-outs

Don't guard refs that should be wired (serialized fields, `GetComponent` results, a SOAP asset ref,
state set by lifecycle transitions). Let `NullReferenceException` surface — it points at the broken
wire. If a null check feels needed because the ref might genuinely be null in a legitimate path, the
fix is usually to **set the ref correctly at the transition**, not to
guard every read.

**The test is: would deleting this guard cause anything worse than a clean `NullReferenceException`
that names the broken wire?** If not, the guard is banned — it converts a loud, locatable failure into
a silent no-op. "Might not be wired", "just in case", "defensive", and "it's cheap" are never reasons.
Five cases pass the test; nothing else does:

1. **Lazy-init** — the null check *is* the initialization (`if (_x == null) { _x = Build(); }`).
   Deleting it changes behaviour. A check that merely *skips* work when null is not lazy-init, it's a
   banned guard.
2. **Designed sentinel** — null is a specified value with a defined meaning. Required: the meaning is
   documented at the field declaration, e.g.
   `[SerializeField] private Sprite fallbackAvatar; // optional — null renders initials instead`, so
   "unset on purpose" is distinguishable from "forgot to wire it". Undocumented optionality reads as
   the bug this rule exists to expose.
3. **Optional method parameter** — the parameter is declared with a `= null` default and the null
   branch is part of the signature's contract.
4. **Editor-time tolerance** — `OnValidate` or editor-only code, where a half-authored asset
   legitimately has unset refs and an NRE per keystroke is noise, not signal. Runtime code gets no
   equivalent.
5. **SOAP ref not yet published** — a runtime-published SOAP value (a `ColorVariable` a scene has
   yet to write, say) read from a callback that can fire before its publisher's `OnEnable`:
   `[ExecuteAlways]` paths, anything ticking before `Start`, and — in this project specifically —
   anything in a scene that was just loaded additively, where the other scene's publishers may not
   have run yet. This is "wired but not populated *this frame*", which is a different failure from
   "unwired" — the asset ref itself is still expected non-null and must not be guarded. Required:
   documented at the read site.

Explicitly **not** carve-outs: `OnDestroy`/teardown (a wired ref is still wired at teardown; only a
collaborator that may never have been *created* qualifies, and that's case 1 or 2), and "the ref is set
by a lifecycle transition" (fix the transition). Count/emptiness checks (`list.Count == 0`) aren't null
guards and are unaffected.

**Network and parse results are not covered by this rule at all.** A `UnityWebRequest` body, a
deserialized DTO field, an avatar texture that 404'd — those are *data*, and missing data is a
specified state the assignment explicitly asks to handle. Validate them at the boundary, map them into
a domain model that cannot be half-formed, and let the rest of the code assume the model is whole. The
ban is on guarding *wiring*, not on validating *input*.

**Hook-enforced:** `hooks/pre-commit` blocks a newly-added `== null` / `!= null` / `?.` / `??` on a
`[SerializeField]` field or a `GetComponent` result (idents the file also assigns `= null` are treated
as legitimately nullable and skipped); tag a justified guard with a trailing `SG-ALLOW`. **The
null-conditional counts** — `_bake?.Release()` in an `OnDestroy` is the same banned teardown guard as
`if (_bake != null)`, and being idiomatic C# is exactly why it slips past review.

### Prefer SOAP for cross-system comms

Default to SO events/variables when two systems that don't share a hierarchy need to talk; keep direct
refs only for intra-system internals (a class talking to its own children/components).

### Listen through the listener component, never in code

A type that reacts to a SOAP event does **not** hold the `GameEvent*` asset and does **not** write
`EventListeners += Handler`. It exposes a `public` method, and a `GameEventListener*` component on the
same GameObject holds the event ref and calls that method through its inspector `UnityEvent`.

```csharp
✗  [SerializeField] private GameEventString messageRequested;
✗  private void OnEnable()  { messageRequested.EventListeners += Show; }
✗  private void OnDisable() { messageRequested.EventListeners -= Show; }
✗  private void Show(string message) { ... }

✓  public void Show(string message) { ... }
✓  // GameEventListenerString on this GameObject: _GameEvent = the asset, response -> Show
```

The reason is that a code subscription is invisible from the scene. Selecting the GameObject shows no
event ref, so nobody can tell what it listens to without opening the file, and a channel with no
subscriber and a subscriber with no channel both look identical in the inspector — which is to say,
they look like nothing at all. The listener component puts both halves on the GameObject where a
reviewer, and Unity's own "Find References In Scene", can see them.

Pick the concrete subclass that matches the payload — `GameEventListenerString`,
`GameEventListenerVoid`, and the rest live one folder per type under
`Assets/Scripts/SOAP/Runtime/Events/`. A `GameEventListenerVoid` response takes no argument; the
typed ones pass their value dynamically.

**The listener subscribes in `OnEnable`, so it must sit on a GameObject that is active when the event
can fire.** A banner that switches *itself* off never re-subscribes. Put the listener on a parent that
stays active and have its method toggle a child — `TaskMessageBanner` is the worked example.

Two things this rule does not cover: **raising** is still a plain `evt.Raise(value)` call from code,
and `Button.onClick.AddListener` is Unity UI, not SOAP.

**Review-enforced**, plus a `grep` that finds every violation at once:

```
grep -rn "EventListeners" Assets/Scripts Assets/Tests | grep -v "Assets/Scripts/SOAP/Runtime"
```

### No `DontDestroyOnLoad`

Session-wide services (scene loader, FPS counter, global canvas) live in the persistent bootstrap
scene — build index 0, loaded first, never unloaded — not behind `DontDestroyOnLoad`. Don't propose
DDOL as the persistence mechanism.


### Comments say *why*, never *what*

A comment that narrates the line below it is noise — the reader already has the line, and the comment
now has to be maintained alongside it. The same goes for a comment that traces *how* the code reaches
its result: control flow is the one thing source is genuinely good at expressing.

Write the comment the code cannot write for itself. That is a reason: a constraint you were working
under, an alternative you rejected and why, a platform quirk, a consequence that only shows up
somewhere else. Those do not drift when the implementation is rewritten, because they were never a
description of it.

The trap is the comment that starts as a reason and keeps going. Stop at the reason.

```csharp
// ✗ narrates the line, then explains the mechanism the code already shows
// SetText with a format argument rather than string interpolation: this runs every
// refresh for the whole session, and interpolation would allocate each time.
label.SetText("{0:0} FPS", fps);

// ✓ the reason alone
// Interpolation would allocate on every refresh, for the whole session.
label.SetText("{0:0} FPS", fps);
```

```csharp
// ✗ the first sentence is a reason; the rest re-reads the code
// A browser reports a zero-size canvas while the tab is backgrounded. Dividing by it
// writes NaN anchors, and because _applied is only recorded on success the next frame
// retries instead of latching the broken layout for the session.

// ✓
// A browser reports a zero-size canvas while the tab is backgrounded, and NaN anchors
// do not repair themselves.
```

**Review-enforced.** No hook catches this one — a comment is prose, and no scanner can tell a reason
from a description.
