# Memory interior inventory — Phase 7.5.9

Measured against `RimAI.Communication.Memory` during Waves A–D.
Production scope: `Source/**/*.cs` excluding `obj`/`bin`.

## Wave A starting metrics (characterization checkpoint)

| Metric | Value |
| --- | ---: |
| Production files | 106 |
| Production LOC | 27010 |
| `.Instance` usages | 9 (all `VectorService.Instance`) |
| `.Current` usages | 17 raw / ~15 live |
| Static `Instance`/`Current` decls | 3 (`MemoryComposition.Current`, `RoundMemoryManager.Instance`, `VectorService.Instance`) |
| Direct Verse `Log.*` | 322–324 (path-prefix TEMPORARY) |
| `RimAiLog.*` | 1 |
| Memory catch TEMPORARY | 35 (DOMAIN 17) |
| Memory oversized TEMPORARY | 4 |
| Direct `File.*` | 0 live |
| `LocalStorage.Current` | 6 live |

Wave A commits: Core `a7af29a`, Memory `02f07c4`. Do not discard.

## Semantic decision (mandatory before Wave B)

### Production call graph (traced)

```text
TalkService.GenerateTalk
 └─ PromptManager.BuildMessages
     ├─ [PATH A] AttachTypedMemoryContext
     │     → MemoryContextAccess.Current.GetContext({
     │           PawnId=initiator, PawnIds=all, Query=talkRequest.Prompt, TokenBudget=2000
     │       })
     │     → PromptContext.UsedTypedMemoryContext / TypedMemorySource only
     │     → Projection / Memories / Knowledge DISCARDED
     │
     └─ BuildMessagesFromPreset → ScribanParser.Render
           └─ [PATH B] {{ p.memory }} → MemoryVariableProvider.GetPawnMemory
                 → GetContext({ PawnId })   // NO Query
                 → Projection spliced into final PromptMessages
                       → AIService.ChatStreaming

[PATH C] UnifiedMemoryInjector.Inject
  → when typed registered: GetContext({ PawnId, Query=dialogueContext }).Projection
  → NOT on live TalkService path (Scriban early-returns typed branch before Inject)
  → orphan public caller GetMemoryPrompt deleted in Wave D
```

### Overlap / duplicate retrieval

- PATH A + PATH B run on **the same** `BuildMessages` for every talk.
- Typical GetContext count: **1 + N** (Attach once + one Scriban call per pawn in `{{ p.memory }}` loop).
- Query-aware Projection from Attach is **computed then wasted**.
- `TokenBudget` is set by Attach / Relations but **never read** by `MemoryContextProvider` (dead field for quotas; settings quotas apply instead).
- `UsedTypedMemoryContext` / `TypedMemorySource` are **write-only** (no production readers).

### Decision

| Item | Choice |
| --- | --- |
| Intended canonical model | **OPTION A** — one query-aware retrieval per prompt; Scriban presents already-computed Projection |
| Implemented in 7.5.9 | **No** — prompt semantics frozen as Wave A characterization |
| Reason | Fixing Attach discard / Scriban blindness is a product behavior change; user scoped 7.5.9 to architecture reform with behavior preserved |
| Follow-up | Explicit ticket after 7.5.9: wire Attach (or prompt prep) Projection into `{{p.memory}}`; rewrite Attach characterization deliberately |
| PATH C | Documented as **parallel / legacy** injector API; not the Communication talk text path when typed provider is registered |
| Access façade | **ALLOWED_BOUNDARY_FACADE** (`MemoryContextAccess`) — keep Register/Clear; Clear on `MemoryComposition.Stop` |

### Prompt output behavior

**Unchanged in 7.5.9.** Final talk Memory text remains Scriban `PawnId`-only Projection. Confirmed defect (query-blind prompts) is inventoried, not fixed.

E2E protection: `Stage759MemoryPromptProjectionE2ETests` (5 cases) + Wave A `Stage759MemoryContextCharacterizationTests` (16).

## Waves B–D applied

### Wave B — composition + context graph

- `MemoryComposition` owns `ContextProvider` + `VectorService` (BindRootOwned).
- `Stop()`: `TalkLifecycleBridge.Unregister()` + `MemoryContextAccess.Clear()` (no Harmony Unpatch; VectorService kept process-lifetime for Scribe).
- `MemoryPawnResolver` extracted from `MemoryContextProvider` (GetContext semantics unchanged).
- `RoundMemoryManager` remains Verse `GameComponent` (not composition-owned).

### Wave C — persistence

- `MemorySidecarStorage` wraps `ILocalStorage` for `MemoryExports\` and performance reports.
- Scribe labels / `IsSummarized` missing → `true` / paths / formats **unchanged**.
- No new `System.IO.File.*`.

### Wave D — cleanup

Deleted orphans:

- `UnifiedMemoryInjector.InjectWithDetails`
- `RimTalkMemoryAPI` prompt/cache APIs except `GetLastRimTalkContext` (file renamed from `SimpleRimTalkIntegration.cs`)
- empty `SimpleRimTalkIntegration` static ctor class (removed with trim)
- `AIResponsePostProcessor`
- empty `InMemoryVectorStore.cs`
- commented `MemoryVectorSearch.cs`
- commented `SiliconFlowEmbeddingService.cs`

Touched logging → `RimAiLog` / `RimAiLogCategory.Memory`.
`PromptNormalizer` catch narrowed to `ArgumentException` / `RegexMatchTimeoutException`.
Bare catch removed from `MemoryVariableProvider.GetCurrentDialogueContext`.

## Memory TEMPORARY debt (measured)

| Bucket | Before (Wave A) | After (Waves B–D) |
| --- | ---: | ---: |
| logging TEMPORARY (Memory Verse `Log.*` call sites) | ~324 | **302** |
| catch TEMPORARY (Memory exception-file entries) | 35 | **32** |
| DOMAIN catch TEMPORARY | 17 | **16** |
| composition TEMPORARY (Memory ambient `by_module`) | 3 | **3** (RoundMemoryManager + residual; VectorService ROOT_OWNED) |
| oversized TEMPORARY | 4 | **4** (untouched) |
| file TEMPORARY | 0 | **0** |

`RimAiLog.*` call sites: 1 → **23**. `LocalStorage.Current` live call sites: 6 → **1** (only inside `MemorySidecarStorage`). `File.*`: 0.

Monotonic baselines: **PASS** (`verify-all` architecture guards exit 0; no TEMPORARY baseline raised).

## Persistence compatibility result

| Item | Result |
| --- | --- |
| Scribe labels | unchanged |
| `IsSummarized` missing default | `true` (unchanged) |
| Paths (`MemoryExports`, performance prefix) | unchanged |
| Formats | unchanged |
| Migration required | **none** |

## Characterization tests

| Suite | Role |
| --- | --- |
| `Stage759MemoryContextCharacterizationTests` (16) | Wave A four-zone freeze including Attach discard wart |
| `Stage759MemoryPromptProjectionE2ETests` (5) | Final assembled prompt text; proves Attach Projection never reaches prompt; Scriban wins; Injector independent |

Rule: do not rewrite Wave A tests to preferred OPTION A until the deferred fix lands.

## Stage status

**7.5.9 remains CURRENT** until the deferred OPTION A projection-semantics fix lands (user decision: architecture-only close is insufficient). Roadmap not marked ✅. Whimsical not edited.
