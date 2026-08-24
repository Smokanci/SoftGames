# SOAP

ScriptableObject Architecture Pattern: events, variables, references, asmdef, gotchas.
Loaded automatically when working under `Assets/Scripts/SOAP/`.

This is a **subset ported from the Kuiper-Prospector project**, then trimmed again to only the types these three demos actually reference. Read `Runtime/` for what that leaves. What is absent is absent on purpose: see *Not ported* below before going looking for it, and re-derive a new type from the base classes rather than assuming it once existed and was lost.

## Inter-system communication

Solo project — the same person writes the code and authors the assets. SOAP is used for **decoupling**, not designer ergonomics: scenes and prefabs reference assets, not each other, so systems can evolve independently. In this project it has one load-bearing job: a task scene loaded additively has no sanctioned way to reach the bootstrap scene's services, because singletons and `DontDestroyOnLoad` are both banned. SOAP is that path.

- **Pick the lightest shape for the job.** For state, a typed `Variable` deriving from `GenericVariable<T>`; for triggers and fan-out, a `GameEvent<T>`. Read the folder for what exists today — a type not there is a type nothing needed yet, and adding one is a two-line subclass plus a `[CreateAssetMenu]`.
- **Don't SOAP-ify intra-system internals.** A card stack talking to its own renderer, or a dialogue view laying out its own rows, stays direct refs — SO indirection there is friction without payoff. Most of what these three demos do lives inside one scene and wants a plain C# event.

## Layout

`SOAP.Runtime.asmdef` sits at `Runtime/`, `autoReferenced: true`, with **no references** — the NaughtyAttributes dependency Kuiper's copy carried was stripped during the port. `Game.Runtime` lists it, as must any asmdef added later.

Asset folders under `Assets/SOAP/` mirror the runtime layout (`Events/`, `Variables/`). Read the `[CreateAssetMenu]` attributes for the current create-menu list rather than trusting a copy here.

## Gotchas

- **Listening is a component, never a code subscription.** `EventListeners += Handler` is banned outside this folder — put a `GameEventListener*` on the GameObject and wire its `UnityEvent` to a `public` method. The rule and its reasoning are in `.claude/rules/csharp-conventions.md`. Raising stays a plain `Raise(value)` call.
- **Logging is off by default.** `GameEvent.Raise` is `[Conditional("SOAP_DEBUG")]` — add `SOAP_DEBUG` to scripting defines to see raises.
- **Variable reset is incomplete by design.** `BaseVariable.ResetAllInstances` runs at `RuntimeInitializeLoadType.BeforeSceneLoad` and resets only SOs already loaded. A variable referenced solely by a scene loaded later will not reset between play sessions. Either fix works here: **Player → Preloaded Assets**, or moving the asset under a `Resources/` folder as Kuiper does. This project carries no `Resources/` ban — TMP's imported assets already sit in one.
- **Inspector edits broadcast in play mode.** `GenericVariable<T>.OnValidate` defers `OnValueChanged.Invoke` via `EditorApplication.delayCall`, because Unity forbids hierarchy mutations inside `OnValidate`. Without it, editing `runtimeValue` directly bypasses the `Value` setter and silently fails to notify subscribers.
- **`GameEventListener` no longer tolerates an unwired event.** Kuiper's copy returned early when `_GameEvent` was null, which made a mis-wired listener silently never fire — the exact loud-failure-into-silent-no-op this project's null-guard ban exists to prevent. The guards were removed during the port: an unassigned event now throws in `OnEnable` and the stack trace names the GameObject.
- **A `[SerializeField] T field = null;` initializer disables the null-guard hook for that field.** `hooks/wired-idents.sh` treats any ident the file assigns `= null` as legitimately nullable, and a declaration initializer counts. Declare a wired serialized field without an initializer, or the guard that protects it stops running. (The type that carried this shape, `GameObjectReferenceSetter`, has since been deleted along with the rest of the unreferenced port.)

## Not ported

Left in Kuiper deliberately. Do not assume a missing piece is an oversight:

- **`Lists`, `ObjectPool`** — nothing here pools; the 144 cards are all alive at once.
- **`CooldownVariable`** — `CooldownValue` is an `{ expirationTime, cooldown }` gameplay pair from Kuiper, not a SOAP primitive. No task here has a cooldown.
- **`UniqueID`, `GameObjectReferencesDatabase`, and the whole `GameObjectReference` family.** The database's only job is `GetById(long)`, resolving a stable 64-bit id derived from an asset GUID so a save file can name an asset it holds no reference to. There is no save format here, so nothing ever resolves an id. The reference types themselves went the same way: no consumer in these three demos needs the GameObject itself rather than its state.
- **Every typed channel nothing references.** Int, Float, String, Sprite, Vector2, Vector3 and GameObject variables and events, the `*Reference` inspector wrappers, `GameEventRaiser`, `GameEventHelper`, `UnityVoidEvent`. All were ported, none was ever wired, and that much unused surface reads as an imported framework rather than a design. Adding one back is a subclass of `GenericVariable<T>` or `GameEvent<T>`.
- **The whole `SOAP/Editor` half** (~5,700 lines: custom inspectors, drawers, the event and variable flow analyzers, the GUI flow viewers). The default inspector assigns SO assets to serialized fields fine. If wiring bugs start costing real time, the flow analyzers are the thing worth fetching — they answer "who listens to this event", which nothing else here does.
