using System.Collections.Generic;
using System.Linq;
using Verse;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.Injection
{
    public static class ELSCollector
    {
        public static List<MemoryEntry> Collect(Pawn pawn, string context, int maxCount)
        {
            var result = new List<MemoryEntry>();
            
            if (pawn == null || maxCount <= 0)
                return result;
            
            var comp = pawn.TryGetComp<FourLayerMemoryComp>();
            if (comp == null)
                return result;
            
            DynamicMemoryInjection.InjectMemoriesWithDetails(
                comp,
                context,
                maxCount,
                out var scores
            );
            
            if (scores == null || scores.Count == 0)
                return result;
            
            result = scores.Select(s => s.Memory).ToList();
            
            if (Prefs.DevMode && result.Count > 0)
            {
                Log.Message($"[ELSCollector] Collected {result.Count} memories for {pawn.LabelShort}");
            }
            
            return result;
        }
        
        public static List<MemoryEntry> CollectELSOnly(Pawn pawn, string context, int maxCount)
        {
            var all = Collect(pawn, context, maxCount * 2);
            return all
                .Where(m => m.Layer == MemoryLayer.EventLog)
                .Take(maxCount)
                .ToList();
        }
        
        public static List<MemoryEntry> CollectCLPAOnly(Pawn pawn, string context, int maxCount)
        {
            var all = Collect(pawn, context, maxCount * 2);
            return all
                .Where(m => m.Layer == MemoryLayer.Archive)
                .Take(maxCount)
                .ToList();
        }
    }
}