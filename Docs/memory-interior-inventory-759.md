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

## Semantic decision — OPTION A (closing gate)

### Before (Wave A wart — confirmed defect)

```text
Attach:
  Query-aware GetContext (often initiator-only) → Projection DISCARDED
Scriban {{p.memory}}:
  GetContext({ PawnId }) → final prompt Memory text (query-blind)
Talk path:
  1 + N GetContext
TokenBudget:
  set on Attach request but unread by MemoryContextProvider
```

### After (OPTION A closing gate + Knowledge/budget follow-up)

```text
AttachTypedMemoryContext:
  conversationEntryBudget = DefaultTokenBudget / 80   # 25 entries shared
  perPawnTokenBudget = (budget / N) * 80
  for each talk pawn:
    GetContext({ PawnId, Query, TokenBudget=perPawn, IncludeKnowledge=false })
    → TypedMemoryProjections[PawnId]
  once:
    GetKnowledge({ Query, PawnId=initiator, TokenBudget=Default })
    → TypedKnowledgeProjection
Scriban {{p.memory}}:
  presents TypedMemoryProjections (no GetContext)
Scriban {{knowledge}}:
  presents TypedKnowledgeProjection (no second GetKnowledge on Talk path)
Talk path retrievals:
  N GetContext (IncludeKnowledge=false) + 1 GetKnowledge
  — not N nested knowledge matches, not 1+N memory, not N×full budget
PawnIds:
  not set on Talk Attach requests (resolver uses PawnId only)
TokenBudget:
  shared conversation ceiling; equal split across talk pawns
```

### UnifiedMemoryInjector disposition

**Retained** as compatibility / untyped fallback (`GetFourLayerMemories` when typed Access is null)
and as an independent typed API with `Query=dialogueContext`. **Not** on the normal Talk
Scriban path when Attach has prepared projections.

### Fallback

`MemoryVariableProvider` falls back to `GetContext({ PawnId })` only when
`LastContext` has no precomputed Projection for that pawn (preview / templates without Attach).
Normal Talk path must not hit fallback (covered by Stage759 Case7).

### Prompt output behavior

**Intentionally changed:** final Talk Memory text is now **query-aware** (uses `talkRequest.Prompt`).
Treated as a verified defect fix after Wave A characterization proved Attach discarded the rich Projection.

E2E: `Stage759MemoryPromptProjectionE2ETests` (OPTION A contract) + updated Wave A characterization.
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
| logging TEMPORARY (Memory Verse `Log.*` call sites) | ~324 | **301** (baseline recommitted) |
| catch TEMPORARY (Memory exception-file / by_module) | 35 / 107 | **32** / **103** (baseline recommitted) |
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

**7.5.9 COMPLETE** after OPTION A closing gate (query-aware Projection reaches final Talk prompt;
N retrievals per talk, not 1+N; TokenBudget live via entry-cap convention).
Whimsical not edited.
