using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Injection;
using Ustas.RimAI.Communication.Prompt;
using Ustas.RimAI.Core.Diagnostics;
using Ustas.RimAI.Core.Memory;

namespace Ustas.RimAI.Communication.Memory.API
{
    /// <summary>
    /// Provides {{pawn.memory}} for Scriban. On the normal Talk path, returns the Projection
    /// already prepared by PromptManager.AttachTypedMemoryContext (OPTION A).
    /// Fallback retrieval runs only when no precomputed Projection is present.
    /// </summary>
    public static class MemoryVariableProvider
    {
        /// <summary>
        /// Gets Memory text for a pawn. Prefer precomputed TypedMemoryProjections from LastContext.
        /// </summary>
        public static string GetPawnMemory(Pawn pawn)
        {
            if (pawn == null)
            {
                return "";
            }

            // OPTION A — normal Talk path: present Attach's query-aware Projection (no second GetContext).
            var promptContext = PromptManager.LastContext;
            if (promptContext != null
                && promptContext.UsedTypedMemoryContext
                && promptContext.TryGetTypedMemoryProjection(pawn.ThingID, out var precomputed))
            {
                return precomputed ?? string.Empty;
            }

            var typed = MemoryContextAccess.Current;
            if (typed != null)
            {
                // Fallback: template/preview without Attach. Explicit PawnId-only request.
                var result = typed.GetContext(new MemoryContextRequest { PawnId = pawn.ThingID });
                return result?.Projection ?? string.Empty;
            }

            try
            {
                var settings = RimTalkMemoryPatchMod.Settings;

                var fourLayerComp = pawn.TryGetComp<FourLayerMemoryComp>();
                if (fourLayerComp != null)
                {
                    return GetFourLayerMemories(pawn, fourLayerComp, settings);
                }

                var memoryComp = pawn.TryGetComp<PawnMemoryComp>();
                if (memoryComp != null)
                {
                    return PromptNormalizer.Normalize(GetLegacyMemories(memoryComp, settings));
                }

                return "(Компонент пам'яті відсутній)";
            }
            catch (Exception ex)
            {
                // RimAI.exception: TEMPORARY_EXPLICIT_EXCEPTION — Scriban fire-and-forget must not fail talk render.
                RimAiLog.Warning(RimAiLogCategory.Memory, $"[MemoryPatch] Error getting pawn memory for {pawn?.LabelShort}.", exception: ex);
                return "";
            }
        }

        [ThreadStatic]
        private static Dictionary<string, string> _pawnMemoryCache;

        [ThreadStatic]
        private static int _memoryCacheTick;

        private const int MEMORY_CACHE_EXPIRE_TICKS = 120;

        private static string GetFourLayerMemories(Pawn pawn, FourLayerMemoryComp comp, RimTalkMemoryPatchSettings settings)
        {
            string pawnId = pawn.ThingID;
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            if (_pawnMemoryCache == null ||
                currentTick < _memoryCacheTick || 
                currentTick - _memoryCacheTick > MEMORY_CACHE_EXPIRE_TICKS)
            {
                _pawnMemoryCache = new Dictionary<string, string>();
                _memoryCacheTick = currentTick;
            }

            if (_pawnMemoryCache.TryGetValue(pawnId, out string cachedResult))
            {
                if (Prefs.DevMode)
                {
                    RimAiLog.Debug(RimAiLogCategory.Memory, $"[Memory] Using cached result for {pawn.LabelShort}");
                }
                return cachedResult;
            }

            string dialogueContext = GetCurrentDialogueContext();
            string result = UnifiedMemoryInjector.Inject(pawn, dialogueContext);

            if (string.IsNullOrEmpty(result))
            {
                result = GetRecentMemories(comp, settings.maxInjectedMemories);
            }

            result = PromptNormalizer.Normalize(result);

            _pawnMemoryCache[pawnId] = result;

            return result;
        }

        private static string GetRecentMemories(FourLayerMemoryComp comp, int maxCount)
        {
            var recentMemories = new List<MemoryEntry>();

            recentMemories.AddRange(comp.SituationalMemories.Take(maxCount / 2));
            recentMemories.AddRange(comp.EventLogMemories.Take(maxCount / 2));

            if (recentMemories.Count == 0)
            {
                return "(Спогадів поки немає)";
            }

            var sortedMemories = recentMemories
                .OrderByDescending(m => m.GameTick)
                .Take(maxCount)
                .ToList();

            return MemoryFormatter.Format(sortedMemories);
        }

        private static string GetLegacyMemories(PawnMemoryComp comp, RimTalkMemoryPatchSettings settings)
        {
            var memories = comp.GetRelevantMemories(settings.maxInjectedMemories);

            if (memories == null || memories.Count == 0)
            {
                return "(Спогадів поки немає)";
            }

            var sb = new StringBuilder();
            int index = 1;

            foreach (var memory in memories)
            {
                sb.AppendLine($"{index}. {memory.Content} ({memory.AgeString})");
                index++;
            }

            return sb.ToString().TrimEnd();
        }


        private static string GetCurrentDialogueContext()
        {
            // From RimTalkMemoryAPI cache when the untyped fallback path runs.
            var context = Patches.RimTalkMemoryAPI.GetLastRimTalkContext(out _, out int tick);

            // Cache expires after 60 ticks.
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (currentTick - tick > 60)
            {
                return "";
            }

            return context ?? "";
        }

        // Non-obvious edge case — read carefully before changing. (isPinned DynamicMemoryInjection isPinned memory)

        public static string GetPawnABM(Pawn pawn)
        {
            if (pawn == null) return "";

            try
            {
                return PromptNormalizer.Normalize(UnifiedMemoryInjector.InjectABMOnly(pawn));
            }
            catch (Exception ex)
            {
                RimAiLog.Warning(RimAiLogCategory.Memory, $"[MemoryPatch] Error getting ABM for {pawn?.LabelShort}.", exception: ex);
                return "";
            }
        }

        public static string GetPawnELS(Pawn pawn)
        {
            if (pawn == null) return "";

            try
            {
                var comp = pawn.TryGetComp<FourLayerMemoryComp>();
                if (comp == null || comp.EventLogMemories == null || comp.EventLogMemories.Count == 0)
                {
                    return "(Спогадів ELS немає)";
                }

                return PromptNormalizer.Normalize(FormatMemoryList(comp.EventLogMemories, MemoryLayer.EventLog));
            }
            catch (Exception ex)
            {
                RimAiLog.Warning(RimAiLogCategory.Memory, $"[MemoryPatch] Error getting ELS for {pawn?.LabelShort}.", exception: ex);
                return "";
            }
        }

        public static string GetPawnCLPA(Pawn pawn)
        {
            if (pawn == null) return "";

            try
            {
                var comp = pawn.TryGetComp<FourLayerMemoryComp>();
                if (comp == null || comp.ArchiveMemories == null || comp.ArchiveMemories.Count == 0)
                {
                    return "(Спогадів CLPA немає)";
                }

                return PromptNormalizer.Normalize(FormatMemoryList(comp.ArchiveMemories, MemoryLayer.Archive));
            }
            catch (Exception ex)
            {
                RimAiLog.Warning(RimAiLogCategory.Memory, $"[MemoryPatch] Error getting CLPA for {pawn?.LabelShort}.", exception: ex);
                return "";
            }
        }

        private static string FormatMemoryList(List<MemoryEntry> memories, MemoryLayer layer)
        {
            if (memories == null || memories.Count == 0)
            {
                return "";
            }

            var settings = RimTalkMemoryPatchMod.Settings;
            int maxCount = settings?.maxInjectedMemories ?? 10;

            var sortedMemories = memories
                .OrderByDescending(m => m.GameTick)
                .Take(maxCount)
                .ToList();

            return MemoryFormatter.Format(sortedMemories);
        }

        public static string GetPawnMatchELS(Pawn pawn)
        {
            if (pawn == null) return "";

            try
            {
                var comp = pawn.TryGetComp<FourLayerMemoryComp>();
                if (comp == null || comp.EventLogMemories == null || comp.EventLogMemories.Count == 0)
                {
                    return "(Спогадів ELS немає)";
                }

                var settings = RimTalkMemoryPatchMod.Settings;
                string dialogueContext = GetCurrentDialogueContext();

                string result = GetMatchedMemoriesForLayer(
                    pawn,
                    comp,
                    comp.EventLogMemories,
                    MemoryLayer.EventLog,
                    dialogueContext,
                    settings?.maxInjectedMemories ?? 10
                );

                if (string.IsNullOrEmpty(result))
                {
                    return "(Відповідних спогадів ELS немає)";
                }

                return PromptNormalizer.Normalize(result);
            }
            catch (Exception ex)
            {
                RimAiLog.Warning(RimAiLogCategory.Memory, $"[MemoryPatch] Error getting matchELS for {pawn?.LabelShort}.", exception: ex);
                return "";
            }
        }

        public static string GetPawnMatchCLPA(Pawn pawn)
        {
            if (pawn == null) return "";

            try
            {
                var comp = pawn.TryGetComp<FourLayerMemoryComp>();
                if (comp == null || comp.ArchiveMemories == null || comp.ArchiveMemories.Count == 0)
                {
                    return "(Спогадів CLPA немає)";
                }

                var settings = RimTalkMemoryPatchMod.Settings;
                string dialogueContext = GetCurrentDialogueContext();

                string result = GetMatchedMemoriesForLayer(
                    pawn,
                    comp,
                    comp.ArchiveMemories,
                    MemoryLayer.Archive,
                    dialogueContext,
                    settings?.maxInjectedMemories ?? 10
                );

                if (string.IsNullOrEmpty(result))
                {
                    return "(Відповідних спогадів CLPA немає)";
                }

                return PromptNormalizer.Normalize(result);
            }
            catch (Exception ex)
            {
                RimAiLog.Warning(RimAiLogCategory.Memory, $"[MemoryPatch] Error getting matchCLPA for {pawn?.LabelShort}.", exception: ex);
                return "";
            }
        }

        private static string GetMatchedMemoriesForLayer(
            Pawn pawn,
            FourLayerMemoryComp comp,
            List<MemoryEntry> memories,
            MemoryLayer layer,
            string context,
            int maxCount)
        {
            if (memories == null || memories.Count == 0)
            {
                return null;
            }

            DynamicMemoryInjection.InjectMemoriesWithDetails(
                comp,
                context,
                maxCount,
                out var scores,
                layer
            );

            if (scores == null || scores.Count == 0)
            {
                return null;
            }

            var layerMemories = scores
                .OrderByDescending(s => s.TotalScore)
                .Select(s => s.Memory)
                .ToList();

            if (layerMemories.Count == 0)
            {
                return null;
            }

            return MemoryFormatter.Format(layerMemories);
        }
    }
}

