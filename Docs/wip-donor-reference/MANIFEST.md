# WIP / donor reference — Memory ↔ RimChat Relations patches

**Status:** non-production reference (outside compile tree)  
**Relocated:** Phase 9.2 Pre-Wave-C Repository Hygiene (2026-08-20)  
**Representation:** `.cs.txt` so architecture scanners and csproj do not treat these as production C#

## Why outside `Source/`

These files lived under `Source/Patches/Relations/` and were compiled by `RimAI.Communication.Memory.csproj`.  
They failed build (`RimTalk.MemoryPatch` missing) and tripped Phase 7 sibling Harmony/reflection guards (targets into RimChat).  
Normal checkout must pass verify-all.

## Manifest

| File | Classification | Former intended path | Provenance | Reactivation |
|---|---|---|---|---|
| `RpgNpcDialogueArchiveManager_FinalizeSession_Patch.cs.txt` | PURE_DONOR_REFERENCE | `Source/Patches/Relations/RpgNpcDialogueArchiveManager_FinalizeSession_Patch.cs` | Donor-era `RimTalk.Memory.Patches.RimChat` Harmony against RimChat archive manager | Only via typed RimAI contract / approved compatibility design — not sibling reflection |
| `RpgNpcDialogueArchiveManager_RecordDiplomacySummary_Patch.cs.txt` | PURE_DONOR_REFERENCE | `Source/Patches/Relations/RpgNpcDialogueArchiveManager_RecordDiplomacySummary_Patch.cs` | Same donor stack for diplomacy summary capture | Same |

## Wave C

Do **not** reintroduce these into `Source/Patches/` during structural moves.  
Empty `Source/Patches/Relations/` was removed after relocation; recreate only for real production patches.
