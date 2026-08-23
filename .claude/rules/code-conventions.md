## General code conventions

The hard rules, one line each. Rationale, carve-outs, and worked examples live in
`.claude/rules/csharp-conventions.md`. It carries a `paths: ["**/*.cs"]` frontmatter scope, but don't
count on that pulling it in — **open it directly** before any non-trivial C# work, and especially
when editing C# through Unity MCP (`create_script` / `script_apply_edits` never touch a file path at
all). A one-liner here is the ban, not the reasoning behind it.

- **Always use braces.** Every `if`, `else`, `for`, `foreach`, `while`, `using` body gets `{ }`, single-line included.
- **Never `public` for inspector-exposed data.** `[SerializeField] private T name;` + `public T Name => name;`. New files have no exemption.
- **Never put `[SerializeField]` fields inside `#if` blocks.** Gate behaviour, never declarations.
- **A tuned value is a `[SerializeField]`, never a `const`.** If someone would change it to make the game look or feel different, it belongs in the inspector. Structural constants stay `const`.
- **Always write the access modifier.** No implicit default on any member — method, field, `const`, property. `private void OnEnable()`, not `void OnEnable()`. Unity message methods included.
- **No singletons. No static runtime registries.** Cross-system runtime discovery goes through SOAP. Editor-only static state is the sole exception.
- **No cross-hierarchy scene references.** A serialized *scene* ref on A may point only at A's own GO, parent, child, or sibling-descendant. Cross-hierarchy comms goes through SOAP.
- **No defensive null checks** on refs that should be wired. Let `NullReferenceException` name the broken wire. Exactly five carve-outs exist — enumerated canonically in `csharp-conventions.md`; nothing else qualifies, and `?.` / `??` count as null checks.
- **No `DontDestroyOnLoad`.** Session-wide services live in the persistent bootstrap scene.
- **Prefer SOAP for cross-system comms.** Direct refs only for intra-system internals (a class talking to its own children/components).
- **Never subscribe to a SOAP event in code.** Listening goes through a `GameEventListener*` component on the GameObject, wired in the inspector to a public method. No `EventListeners +=` outside `Assets/Scripts/SOAP/Runtime/`.
- **One reason to change per class.** A single `.cs` past ~600 lines, or a `MonoBehaviour` whose serialized fields span 4+ unrelated `[Header]` groups, is a smell — extract a responsibility instead of adding the Nth parallel block.
- **Comments say *why*, never *what*.** Drop a comment that narrates the line below it or traces how the code reaches its result. Keep the ones carrying a reason the code cannot state: a constraint, a rejected alternative, a platform quirk.
- **`var` preference is enforced by `.editorconfig`.** Roslyn flags violations; not restated here.

Two of these are hook-enforced (`DontDestroyOnLoad`, null guards on wired refs) at edit time by `hooks/claude-cs-guard.sh` and again at commit time by `hooks/pre-commit`.
Exempt one justified line with a trailing `SG-ALLOW` comment. The rest are review-enforced.
