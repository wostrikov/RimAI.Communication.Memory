using System.Collections.Generic;
using System.Linq;
using Verse;
using Ustas.RimAI.Communication.Data;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.Injection
{
    public static class ABMCollector
    {

        private static HashSet<RoundMemory> _roundMemoryCache;
        public static HashSet<RoundMemory> RoundMemoryCache
        {
            get
            {
                if (_roundMemoryCache == null)
                {
                    _roundMemoryCache = new();
                }
                return _roundMemoryCache;
            }
            set => _roundMemoryCache = value;
        }

        private const int MaxInjectedLength = 5000;

        public static bool IsRoundMemoryEnabled => RimTalkMemoryPatchMod.Settings?.IsRoundMemoryActive ?? false;
        public static List<MemoryEntry> Collect(Pawn pawn, int maxRounds)
        {
            var result = new List<MemoryEntry>();
            
            if (pawn == null || maxRounds <= 0)
                return result;
            
            var comp = pawn.TryGetComp<FourLayerMemoryComp>();
            if (comp == null || comp.ActiveMemories == null || comp.ActiveMemories.Count == 0)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"[ABMCollector] {pawn?.LabelShort}: No ActiveMemories found (comp={comp != null}, count={comp?.ActiveMemories?.Count ?? 0})");
                }
                return result;
            }


            var sortedList = comp.ActiveMemories
                .OrderBy(m => m.Type == MemoryType.Conversation ? 0 : 1) 
                .ThenByDescending(m => m.GameTick) 
                .ToList();
            
            int stackedLength = 0;
            int stackedCount = 0;
            int skippedDuplicate = 0;
            
            if (Prefs.DevMode)
            {
                Log.Message($"[ABMCollector] {pawn.LabelShort}: Processing {sortedList.Count} memories, cache size={RoundMemoryCache?.Count ?? 0}");
            }
            
            foreach (var entry in sortedList)
            {
                if (stackedCount >= maxRounds)
                    break;
                
                if (stackedLength > MaxInjectedLength)
                    break;
                
                if (entry is RoundMemory roundMemory)
                {
                    if (!IsRoundMemoryEnabled) continue;
                    if (RoundMemoryCache != null && RoundMemoryCache.Contains(roundMemory))
                    {
                        if (Prefs.DevMode)
                        {
                            Log.Message("[ABMCollector] Skipped duplicate RoundMemory");
                        }
                        continue;
                    }

                    RoundMemoryCache?.Add(roundMemory);
                }
                
                int contentLength = entry.Content?.Length ?? 0;
                stackedLength += contentLength;
                stackedCount++;
                
                result.Add(entry);
            }
            
            return result;
        }

        public static void ResetDuplicateCache()
        {
            RoundMemoryCache.Clear();
            if (Prefs.DevMode) Log.Message("[RoundMemory] Кеш перевірки дублікатів скинуто");
        }

        public static int GetCollectedCount(List<MemoryEntry> collected)
        {
            return collected?.Count ?? 0;
        }
    }
}
