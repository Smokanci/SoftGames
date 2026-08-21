# SOAP

ScriptableObject Architecture Pattern: events, variables, references, asmdef, gotchas.
Loaded automatically when working under `Assets/Scripts/SOAP/`.

This is a **subset ported from the Kuiper-Prospector project**, trimmed to what these three demos can use. What is absent is absent on purpose — see *Not ported* below before going looking for it.

## Inter-system communication

Solo project — the same person writes the code and authors the assets. SOAP is used for **decoupling**, not designer ergonomics: scenes and prefabs reference assets, not each other, so systems can evolve independently. In this project it has one load-bearing job: a task scene loaded additively has no sanctioned way to reach the bootstrap scene's services, because singletons and `DontDestroyOnLoad` are both banned. SOAP is that path.

- **Pick the lightest shape for the job.**
  - For state, a typed `Variable` (`BoolVariable`, `IntVariable`, `FloatVariable`, `ColorVariable`, `GenericVariable<T>`).
  - For triggers and fan-out, a `GameEvent` / `GameEvent<T>`.
  - `GameObjectReference` only when a consumer genuinely needs the GameObject itself — a `Transform` to parent to, or a `GetComponent<T>` that isn't expressible as state plus events.
- **If all a consumer does with a `GameObjectReference` is `GetComponent<X>()` to read fields or call methods, that's a smell.** `X` should publish state via variables and accept commands via events, and the reference goes away.
- **When you do need a `GameObjectReference`:** drop a `GameObjectReferenceSetter` (`[DefaultExecutionOrder(-1000)]`) on the providing GameObject; consumers cache `reference.Target` in `Start`.
- **Don't SOAP-ify intra-system internals.** A card stack talking to its own renderer, or a dialogue view laying out its own rows, stays direct refs — SO indirection there is friction without payoff. Most of what these three demos do lives inside one scene and wants a plain C# event.

## Layout

`SOAP.Runtime.asmdef` sits at `Runtime/`, `autoReferenced: true`, with **no references** — the NaughtyAttributes dependency Kuiper's copy carried was stripped during the port. `Game.Runtime` lists it, as must any asmdef added later.

Asset folders under `Assets/SOAP/` should mirror the runtime layout (`Events/`, `Variables/`, `GameObjectReferences/`). Create menu roots are `SOAP/Variable/*`, `SOAP/Game Events/*`, and `SOAP/GameObject Reference` — read the `[CreateAssetMenu]` attributes for the current list rather than trusting a copy here.

## Gotchas

- **Logging is off by default.** `GameEvent.Raise` is `[Conditional("SOAP_DEBUG")]` — add `SOAP_DEBUG` to scripting defines to see raises.
- **Variable reset is incomplete by design.** `BaseVariable.ResetAllInstances` runs at `RuntimeInitializeLoadType.BeforeSceneLoad` and resets only SOs already loaded. A variable referenced solely by a scene loaded later will not reset between play sessions. Either fix works here: **Player → Preloaded Assets**, or moving the asset under a `Resources/` folder as Kuiper does. This project carries no `Resources/` ban — TMP's imported assets already sit in one.
- **Inspector edits broadcast in play mode.** `GenericVariable<T>.OnValidate` defers `OnValueChanged.Invoke` via `EditorApplication.delayCall`, because Unity forbids hierarchy mutations inside `OnValidate`. Without it, editing `runtimeValue` directly bypasses the `Value` setter and silently fails to notify subscribers.
- **`GameEventListener` no longer tolerates an unwired event.** Kuiper's copy returned early when `_GameEvent` was null, which made a mis-wired listener silently never fire — the exact loud-failure-into-silent-no-op this project's null-guard ban exists to prevent. The guards were removed during the port: an unassigned event now throws in `OnEnable` and the stack trace names the GameObject.
- **A `[SerializeField] T field = null;` initializer disables the null-guard hook for that field.** `hooks/wired-idents.sh` treats any ident the file assigns `= null` as legitimately nullable, and a declaration initializer counts. `GameObjectReferenceSetter` carried exactly that shape and its teardown guard slipped past the hook unnoticed; both were removed during the port. Declare a wired serialized field without an initializer, or the guard that protects it stops running.

## Not ported

Left in Kuiper deliberately. Do not assume a missing piece is an oversight:

- **`Lists`, `ObjectPool`** — nothing here pools; the 144 cards are all alive at once.
- **`CooldownVariable`** — `CooldownValue` is an `{ expirationTime, cooldown }` gameplay pair from Kuiper, not a SOAP primitive. No task here has a cooldown.
- **`UniqueID`, and `GameObjectReferencesDatabase` with it.** `UniqueIDScriptableObject` derives a stable 64-bit id from an asset's GUID so a save file can name an asset it does not hold a reference to. There is no save format here, so nothing ever resolves an id — and the database's whole job is `GetById(long)`. `GameObjectReference` is a plain `ScriptableObject` in this project.
- **`Events/AnimatorType`, `Events/LongType`, `Events/PlayerPositionType`** — unused, and the last is Kuiper-specific.
- **The whole `SOAP/Editor` half** (~5,700 lines: custom inspectors, drawers, the event and variable flow analyzers, the GUI flow viewers). The default inspector assigns SO assets to serialized fields fine. If wiring bugs start costing real time, the flow analyzers are the thing worth fetching — they answer "who listens to this event", which nothing else here does.
