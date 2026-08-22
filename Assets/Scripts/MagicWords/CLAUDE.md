# Magic Words

> **Status:** built and verified in play mode. `Assets/Scenes/MagicWords.unity` fetches the endpoint,
> then types the conversation out one line at a time with colour emoji and circular avatars, and falls
> back to initials for the one speaker the avatar list never names. Every banner state — loading,
> loaded, empty, unreadable, failed — was driven on screen, and the dead-URL fallback was driven from
> a local payload carrying a refused host, a 4xx, and a 200 that is not an image.
> `EmojiVocabularyTests.cs`, `DialogueScriptTests.cs` and `EmojiSpriteMarkupTests.cs` cover the model
> and the sprite rewrite headlessly, and
> `Assets/Tests/PlayMode/MagicWordsExitTests.cs` covers leaving the task both while the fetch is in
> flight and partway through the reveal.
> Tuned values live in the scene, not here: the endpoint, its timeout and the banner wording sit on
> `MagicWordsRunner`, the reveal speed, the gap between lines and the scroll smoothing on
> `DialogueLogView`, the avatar timeout on `AvatarLibrary`.

The brief: *"Create a system that combines text and Unicode emojis to render character dialogue using
data from the endpoint below. Load the data dynamically at runtime and handle cases where avatar URLs
may not load or data is missing."*

## The pipeline, in one line each

`MagicWordsResponse` → `DialogueScript` → `DialogueLine` → `DialogueRowView`.

- **`MagicWordsDto.cs`** — what `JsonUtility` fills. Field names are the endpoint's keys and cannot be
  renamed. Every field here can arrive null or empty; nothing else in the task has to know that.
- **`DialogueScript`** — plain C#, no Unity types beyond the DTO. Turns the raw payload into a list
  that cannot be half-formed. This is the boundary the "validate input, don't guard wiring" rule in
  `.claude/rules/csharp-conventions.md` is talking about.
- **`DialogueLine`** — immutable, already substituted, already sided, already carries its initials.
  A row view reads fields and sets them on components; it makes no decisions.
- **`DialogueRowView` / `DialogueLogView`** — `MonoBehaviour`s that draw. No parsing, no HTTP. The
  log view owns the pace of the conversation; the row view owns the pace of its own letters.

Both requests — the payload and each avatar — go through `WebRequests.SendAsync` in
`Assets/Scripts/Common/`, which frame-polls the operation and takes the caller's
`destroyCancellationToken`, so a scene unloaded mid-request stops there instead of resuming into a
destroyed component. The cancellation throws out through `using var request`, so the request object
is disposed on the way. Whether the browser then aborts the underlying fetch is untested — the
guarantee this task rests on is that nothing resumes into the unloaded scene, not that the socket
closes. `UnityWebRequestTexture` needs `com.unity.modules.unitywebrequesttexture`, which the manifest
pulls in for this task alone.

**A missed cancellation is invisible until it is not.** It produces no wrong value — it resumes a
continuation inside a destroyed component, seconds later, once the request finally answers. That is
why `MagicWordsExitTests` waits well past the scene swap before it passes: the exception it watches
for arrives on network time, not on a frame count, and one that lands after the test returns gets
blamed on whatever ran next.

`DialogueScript.FromResponse` is a static method rather than a constructor because the empty result
is a real answer, and `DialogueScript.Empty` is what both a null response and an unparseable body
become.

## What each missing thing turns into

The live payload only exercises two of these — a speaker with no avatar record, and a name listed
twice. The rest are covered by the fixture in `DialogueScriptTests.cs`, which is shaped like the
endpoint and adds the cases it does not carry.

| Gap in the data | What the player sees |
| --- | --- |
| Entry has no `text` | The entry is dropped. A speaker bubble with nothing in it is noise. |
| Entry has no `name` | The line renders; the speaker label is switched off. |
| Speaker has no avatar record | Initials in a circle, on the left. |
| Avatar `position` missing or unrecognised | Left. `Left` is the default, `"right"` is the opt-in. |
| Avatar URL 404s, refuses, or returns non-image | Initials in a circle. `AvatarLibrary` never tells the row *which* failure it was. |
| `{token}` not in the emoji table | Left in the text with its braces, so the gap is visible. |
| An emoji character the sheet has no sprite for | Monochrome line art, for every emoji the fallback font carries. Newer than the font is still a box. |
| Body is not JSON at all | Its own banner message, carrying the parse error. |
| Body is valid JSON with no lines | A different banner message, so the two are not confusable. |
| Request fails or times out | Banner carries `UnityWebRequest.error`. |

**A repeated avatar name keeps the first record.** The payload names one character twice with two
different URLs, the second of which is broken. Nothing makes a later record more trustworthy than the
one before it, so the second is dropped and never fetched. Reversing that rule is a one-line change in
`DialogueScript.IndexAvatars` and would cost one wasted request per repeated name.

**A consequence worth knowing: the running demo never fires the dead-URL fallback.** Both broken URLs
in the payload sit on records nothing reaches — one belongs to a name that never speaks, the other to
the dropped duplicate. The initials circle on screen comes from the speaker with no avatar record at
all. The dead-URL path is real and covered, but it is not visible from the build.

## Emoji: colour where we have it, line art everywhere else

The model emits **real Unicode characters** — `EmojiVocabulary.Substitute` swaps `{token}` for the
codepoint, and no TMP markup ever enters the model. That keeps the substitution testable with a plain
string compare and keeps the view the only thing that knows TMP exists.

Two things then resolve those codepoints, and which one wins is decided in the view:

- **`Assets/Art/Emoji/EmojiSpriteAsset.asset`**, wired into `TMP Settings → Default Sprite Asset`,
  holds the colour glyphs. `EmojiSpriteMarkup.Apply` rewrites any codepoint it has a sprite for into
  `<sprite=N>`, and `DialogueRowView.Bind` calls that on its way to the label.
- **`Assets/Art/Fonts/NotoEmoji SDF.asset`**, a *dynamic* font asset in `TMP Settings → Fallback Font
  Assets`, catches everything else. It rasterizes any codepoint its source font carries the first time
  a line asks for it, so an emoji nobody planned for still draws — in monochrome line art.

**The dynamic asset is committed with its atlas cleared**, and rebuilds it at runtime the first time
a line asks for a glyph — which is also the state every build ships, because `Clear Dynamic Data On
Build` is on. A committed copy of that asset measured in megabytes rather than kilobytes means
somebody saved an expanded atlas back into it; clear it again rather than leaving it, or a texture
that the build discards anyway rides along in git forever.

**The markup is not decoration, it is the only lever.** TMP walks the whole font chain before it ever
looks at a sprite asset, so once a monochrome emoji font sits in the fallback list it shadows every
colour sprite sharing a codepoint. Explicit `<sprite>` outranks both. Verified in play mode: without
the rewrite the scene reported zero sprite elements and every emoji came from the font.

`EmojiSpriteMarkup` reads the same `TMP_Settings.defaultSpriteAsset` the markup resolves against, so
the sheet is the single source of truth for what is in colour — there is no second list to keep in
step, and adding a sprite is enough to promote that emoji.

Two traps this arrangement leaves standing. TMP resolves a sprite by *one* codepoint, so a skin tone
or a ZWJ family draws as its separate parts rather than one glyph; joining them means splitting on
grapheme clusters and emitting `<sprite>` per cluster. And `TMP_FontAsset.TryAddCharacters` refuses
astral codepoints on the dynamic asset even though `FontEngine.TryGetGlyphWithUnicodeValue` resolves
them and the text path renders them — so do not trust that API to pre-warm the atlas.

The route to the colour sheet was not the obvious one:

- The stock EmojiOne sprite asset TMP ships holds a small fixed set of smileys, none of which the
  payload asks for.
- Noto Color Emoji is a **CBDT/CBLC** font — every glyph is a PNG, not an outline. TMP's font-asset
  pipeline loads glyphs with FreeType's no-bitmap flag, so `TryAddCharacters` reports every codepoint
  missing and bakes an empty atlas, with nothing in the console. Probing `FontEngine` directly
  confirmed it: `TryGetGlyphWithUnicodeValue` succeeds under every load flag *except* `LOAD_NO_BITMAP`.
  Rebuilding the atlas by hand is not possible from a script either — `TryPackGlyphsInAtlas` and
  `RenderGlyphsToTexture` are both internal to Unity.
- So the glyphs are lifted straight out of the font's bitmap table instead.
  `tools/generate_emoji_sheet.py` reads `Assets/Art/Fonts/NotoColorEmoji-Subset.ttf` with fonttools,
  writes `Assets/Art/Emoji/emoji_sheet.png`, and records the rects in `emoji_sheet.json`. The sheet is
  committed, so a clone needs neither Python nor fonttools.

To add an emoji **in colour**: add it to the subset font's codepoint list, add it to `EMOJI` in the
generator, re-run the generator, then extend the sprite asset's character and glyph tables to match
`emoji_sheet.json`. Add the `{token}` for it to `Assets/Data/EmojiTable.asset`.

Skipping those steps no longer costs a missing-glyph box — the fallback font draws the emoji in line
art instead. Only the colour is lost, and it is lost visibly.

`EmojiTable` is a `ScriptableObject` rather than a `Dictionary` in code because the endpoint names an
*emotion* and leaves the choice of glyph to the client, so which face means `intrigued` is a design
decision, not a fact about the data.

## Avatars

`AvatarLibrary` fires **one request per distinct URL**, not one per row — a speaker has as many rows as
they have lines and they all want the same image. A URL that fails is remembered as failed, so a dead
link costs one request for the whole session rather than one per line.

**Every download starts before the first line types**, not when its row appears. `Show` walks the whole
line list for distinct URLs first, so a portrait has the entire reveal to arrive in instead of the
frames after its own row. A row created later either finds its sprite already in `_spriteByUrl` or
adds itself to the list of rows waiting on that URL — the same picture, different bookkeeping.

The portrait `Image` starts disabled with the initials showing, and `SetAvatar` swaps them. The
initials hold the space either way, so a row never resizes when an image lands late.

`request.timeout` is set explicitly. Without it an unreachable host holds the row on its initials until
the platform's own timeout expires, which on WebGL is the browser's and can run into minutes.

Downloaded textures belong to no scene, so `OnDestroy` destroys each sprite and its texture by hand;
unloading the scene does not take them.

**The log renders once per scene load, and that invariant is load-bearing.** `Start` is the only
caller of `MagicWordsRunner.Load`, so `DialogueLogView.Show` runs once and no second render can
outrun the avatar downloads still in flight. The staged reveal widens the window this protects:
`Show` now runs for as long as the conversation takes to type, so a second render would collide with
a first one that is still going. A retry control cannot be added on its own — it needs three things
with it: a re-entry guard on the fetch, a generation counter so a late avatar cannot write into
destroyed rows, and a way to clear `AvatarLibrary`'s failure set so a dead URL is tried again.
Recovering from a failed load today means leaving to the menu and re-entering, which reloads the
scene.

## The reveal

`Show` is a loop: spawn a row, type it, pause, spawn the next. It is an `Awaitable` the runner awaits,
so a cancellation inside it lands in the runner's `OperationCanceledException` catch and any other
failure lands in the one below it, which raises the failed banner. Each of the three async paths here
takes its **own** component's `destroyCancellationToken` — the runner's for the payload,
`AvatarLibrary`'s for the downloads, the log view's for the reveal. They stop together because the
scene unload destroys all three, not because they share a token.

**The letters arrive by `maxVisibleCharacters`, not by growing the string.** `Bind` sets the whole line
before `Reveal` hides it, so TMP lays the bubble out against the finished text on the first frame. The
row never reflows mid-line, the scroll position never chases a growing bubble, and an emoji sprite
counts as one character and so arrives whole. Growing the string would give all three of those away.
`Reveal` hides the text rather than `Bind` doing it, so a row nobody reveals still reads.

The character count is **accumulated against `Time.deltaTime`**, not stepped one per frame. A speed
above the refresh rate then still reads as that speed instead of stalling at the frame rate — which
matters on WebGL, where the frame rate is the browser's to decide.

`ScrollToNewest` rebuilds the layout before it hands off, because the new row was built this frame
and the content size does not know about it yet. It starts a slide only once the log outgrows the
viewport: below that there is nothing hidden, and the setter would push the content off its anchor
rather than do nothing.

**The log slides to the bottom, it does not snap.** `SlideToNewest` eases the distance to the bottom
down to zero over the frames that follow, so the older rows move up instead of jumping. It smooths
that distance **in pixels** and converts back to `verticalNormalizedPosition` each frame: the
normalized value means a different distance every time the content grows, so smoothing it directly
would speed the slide up as the log got longer. Two invariants hold it together — the loop re-reads
the distance to the bottom **every frame**, so a row landing mid-slide extends the run instead of
restarting it, and only one slide runs at a time, which is what the stored `Coroutine` handle
guards. The row lays itself out at full size before it types (above), so nothing but a new row ever
moves the bottom.

It is a **coroutine**, and the only one in this task. Everything else here is an `Awaitable`, because
everything else waits on a download or is awaited by the runner. This waits on the next frame and
nobody awaits it, so a coroutine costs it nothing and saves it two things: `StartCoroutine` hands
back the handle that makes "one at a time" a stored reference rather than a bool, and the scene
unload stops it outright, with no `OperationCanceledException` to catch on a path nobody awaits.

## Layout

One `HorizontalLayoutGroup` per row, avatar and bubble. `DialogueRowView.Bind` flips
`reverseArrangement` and both label alignments from the side; nothing else changes between the two
sides.

`childForceExpandWidth` on the row **must stay off**. It overrides a child's `LayoutElement`
regardless of `flexibleWidth`, which stretched the avatar into an ellipse. The avatar pins its size
with matching min and preferred values; the bubble asks for zero preferred width and takes all the
flexible space, so the split is deterministic at any aspect. Verified at 2:1 landscape and 9:16
portrait.

The circle is `UI/Skin/Knob.psd` under a `Mask`, so the portrait and the initials are both clipped to
it and neither needs a round sprite of its own.

## SOAP, once

`Assets/SOAP/Events/_TaskMessageRequested.asset` (`GameEventString`) is the only channel this task
uses. The banner lives inside `TaskChrome.prefab`; the runner does not. **An empty string hides the
banner**, so one channel carries loading, empty, unreadable, failed, and the all-clear. The failure
messages take the reason as `{0}`, because a player build has no console to read it in — a message
authored without the placeholder still renders, it just says less.

Everything else here is a direct reference: runner → log view → row prefab, and `AvatarLibrary` sits on
the same GameObject as the log view.

## Two things that will bite

- **`Awaitable` that nobody awaits swallows its exception.** Both fire-and-forget paths here —
  `Load` in the runner and `FillAvatars` in the log view — catch and log for that reason alone. Delete
  a `catch` and a failure becomes a loading banner that never goes away.
- **The endpoint is a mock.** Its token set and its avatar hosts can change without notice, which is
  why an unknown `{token}` stays visible instead of being dropped silently.
