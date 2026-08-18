# Memory interior inventory — Phase 7.5.9 Wave A

Measured against `RimAI.Communication.Memory` HEAD at inventory time.
Production scope: `Source/**/*.cs` excluding `obj`/`bin`. **No Waves B–D applied yet.**

## Starting metrics

| Metric | Value |
| --- | ---: |
| Production files | 106 |
| Production LOC | 27010 |
| `.Instance` usages | 9 (all `VectorService.Instance`) |
| `.Current` usages | 17 raw / ~15 live |
| Static `Instance`/`Current` decls | 3 (`MemoryComposition.Current`, `RoundMemoryManager.Instance`, `VectorService.Instance`) |
| `Lazy<T>` | 0 |
| `??= new` | 10 |
| Direct Verse `Log.*` | 322 |
| `RimAiLog.*` | 1 |
| `catch (Exception)` | 102 raw (~93 live) |
| Bare `catch` | ~14 |
| Direct `File.*` | 0 live |
| `LocalStorage.Current` | 6 live (+2 commented) |
| `AtomicFileWriter` | 0 |
| Long-lived construction outside root | `VectorService.Instance` (self), `RoundMemoryManager` GameComponent, WorldComponents, many static helpers |

## Responsibility map

```text
RimTalkMemoryPatchMod (handshake)
    ↓
MemoryComposition.Start
    ↓ Harmony PatchAll + TalkLifecycleBridge + MemoryContextAccess.Register
    ↓
MemoryContextProvider (typed Core contract)
    ↓ ABMCollector / ELSCollector / MemoryFormatter / MemoryManager.GetCommonKnowledge
    ↓
FourLayerMemoryComp / RoundMemoryManager / CommonKnowledgeLibrary / VectorService
    ↓
Scribe save-game + LocalStorage sidecars (export/performance)
```

### Composition / lifecycle

- `MemoryComposition` — Start only; Stop clears `IsStarted` only (no Unpatch / unsubscribe / `MemoryContextAccess.Clear`).
- `RimTalkMemoryPatchMod` — settings + TryActivate.
- StaticConstructor leftovers: `RimTalkAPIIntegration`, empty `SimpleRimTalkIntegration` / `RimTalkMemoryAPI`, disabled `AIResponsePostProcessor`, `BackCompatibilityFix`.

### Application / use-cases

- Capture: Job patches, Messages, incidents, TalkLifecycle round memory.
- Mutation: `FourLayer*` collaborators, UI dialogs.
- Summarization: `IndependentAISummarizer` (1089 LOC file).

### Context / retrieval

- **Canonical typed path:** `API.MemoryContextProvider` → Core `MemoryContextAccess`.
- Collectors: `ABMCollector`, `ELSCollector`, `UnifiedMemoryInjector`.
- Scoring/recall stack: `AdvancedScoringSystem`, `SemanticScoringSystem`, `DynamicMemoryInjection`, `ProactiveMemoryRecall`, …
- Compat Scriban path: `RimTalkAPIIntegration` / `MemoryVariableProvider` / `KnowledgeVariableProvider`.

### Persistence

- **Primary:** Verse Scribe on World/Game/Pawn comps (`MemoryManager`, `RoundMemoryManager`, `FourLayerMemoryComp`, knowledge library vectors).
- **Sidecar:** `LocalStorage` under SaveDataFolder (`MemoryExports\*.xml`, performance `.txt`).
- **Formats:** do not change filenames, scribe labels, or JSON embedding bodies casually.
- No live `System.IO.File.*`.

### Host integration

- Harmony patches listed in inventory exploration.
- `TalkLifecycleBridge` → Communication `TalkRequest` + Core lifecycle events.
- UI MainTab / ITab / Dialogs.

### External callers (through Core contracts)

| Module | Path |
| --- | --- |
| Communication | `PromptManager.AttachTypedMemoryContext` → `MemoryContextAccess.Current` |
| Relations | `RelationsContextAssemblerExpandMemory` → Current + Knowledge |
| Personas | `DirectorMemoryContext` → layered + knowledge |
| Voices / Actions / Events / Quests | none |

Compile: Memory → Communication + Core. No Memory ProjectReference from siblings.

## MemoryContextProvider disposition (Wave A)

Mixed hub today:

1. Pawn resolve via `Find.Maps` / `ThingID`
2. Settings quotas (`maxABMInjectionRounds`, `maxInjectedMemories`, `maxInjectedKnowledge`)
3. ABM then ELS collection
4. Knowledge injection via `MemoryManager.GetCommonKnowledge()`
5. Projection via `MemoryFormatter.Format`
6. Layered path for Personas (`Archive` / `EventLog` / `Situational` only; not ABM)

### Documented context semantics (must preserve)

**Normal `GetContext`:**

- Defaults: ABM rounds = 3, max total memories = 10 when settings null.
- If pawn resolved: collect ABM, then ELS for remaining slots with `request.Query`.
- Map entries → `MemoryContextEntry(id, layer.ToString(), DisplayContent ?? Content, Importance)`.
- Always call `GetKnowledge`; merge into result.
- `Projection` = empty if no memory entries, else `MemoryFormatter.Format(allEntries, startIndex: 1)`.
- `Source` = `"typed"`.

**Layered `GetContext` (`LayeredPawnMemories=true`):**

- Layers in order: Archive, EventLog, Situational.
- Per layer: walk source list; skip null/empty text; stop at `PerLayerLimit` (default 5).
- If `SinceTick > 0` and `entry.GameTick <= sinceTick`, **break** (assumes newest-first order).
- Entry fields: Id, kind label, DisplayContent ?? Content, Importance, GameTick, Type.ToString().
- No knowledge / no Projection fill on this path.

**`GetKnowledge`:**

- Null library → empty Projection, Source typed.
- `MaxEntries` if > 0 else settings `maxInjectedKnowledge` default 10.
- Scores → kind `"knowledge"`.

**`MemoryFormatter.Format`:**

- Empty list → `""`.
- Lines: `{index}. [{TypeTag}] {Content} ({AgeString})`, trim end.
- Type tags: Conversation/Action/Observation/Event/Emotion/Relationship/Memory.

**`MemoryEntry.DisplayContent`:**

- Legacy body prefix `[对话参与者:` rewritten with translated participants label; semantic body after prefix unchanged.

## Target architecture (planned for Waves B–D — not implemented)

```text
MemoryComposition
  → Memory application (record / recall / context use-cases)
  → retrieval/context (pure assembly + collectors)
  → persistence (Scribe adapters + LocalStorage sidecars)
  → ILocalStorage / host
```

Public contract remains Core `IMemoryContextProvider` / `IKnowledgeContextProvider` via `MemoryContextAccess`.

## Wave B–D backlog (blocked pending review)

1. Own `VectorService` / providers under `MemoryComposition`; clear Access on Stop where safe.
2. Split `MemoryContextProvider` into resolve / retrieve / project; host-neutral tests.
3. Persistence façade over Scribe + sidecars; keep formats.
4. Migrate touched Verse.Log → `RimAiLog`; narrow catch sites when touched.
5. Delete dead adapters (`SimpleRimTalkIntegration` empty, commented SiliconFlow) only when unused.

## Characterization tests (Wave A)

Core: `Stage759MemoryContextCharacterizationTests` — Access lifecycle + pure goldens for AppendLayer / formatter type tags / legacy DisplayContent prefix / GetContext defaults.
