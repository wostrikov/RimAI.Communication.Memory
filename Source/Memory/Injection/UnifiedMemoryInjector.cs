using System.Collections.Generic;
using System.Linq;
using Verse;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Core.Diagnostics;
using Ustas.RimAI.Core.Memory;

namespace Ustas.RimAI.Communication.Memory.Injection
{
    public static class UnifiedMemoryInjector
    {
        public static string Inject(Pawn pawn, string dialogueContext)
        {
            if (pawn == null)
                return string.Empty;

            var typed = MemoryContextAccess.Current;
            if (typed != null)
            {
                return typed.GetContext(new MemoryContextRequest
                {
                    PawnId = pawn.ThingID,
                    Query = dialogueContext
                }).Projection ?? string.Empty;
            }
            
            var settings = RimTalkMemoryPatchMod.Settings;
            int maxABMRounds = settings?.maxABMInjectionRounds ?? 3;
            int maxTotalMemories = settings?.maxInjectedMemories ?? 10;

            var abmList = ABMCollector.Collect(pawn, maxABMRounds);
            
            if (Prefs.DevMode)
            {
                RimAiLog.Debug(RimAiLogCategory.Memory, $"[UnifiedMemoryInjector] ABM collected: {abmList.Count}/{maxABMRounds} for {pawn.LabelShort}");
            }
            
            int remainingQuota = maxTotalMemories - abmList.Count;
            
            var elsList = new List<MemoryEntry>();
            if (remainingQuota > 0)
            {
                elsList = ELSCollector.Collect(pawn, dialogueContext, remainingQuota);
                
                if (Prefs.DevMode)
                {
                    RimAiLog.Debug(RimAiLogCategory.Memory, $"[UnifiedMemoryInjector] ELS/CLPA collected: {elsList.Count}/{remainingQuota} for {pawn.LabelShort}");
                }
            }
            
            var allMemories = new List<MemoryEntry>();
            allMemories.AddRange(abmList);
            allMemories.AddRange(elsList);
            
            if (allMemories.Count == 0)
            {
                return string.Empty;
            }
            
            if (Prefs.DevMode)
            {
                RimAiLog.Debug(RimAiLogCategory.Memory, $"[UnifiedMemoryInjector] Total memories: {allMemories.Count}/{maxTotalMemories} for {pawn.LabelShort}");
            }
            
            return MemoryFormatter.Format(allMemories, startIndex: 1);
        }
        
        public static string InjectABMOnly(Pawn pawn)
        {
            if (pawn == null)
                return string.Empty;
            
            var settings = RimTalkMemoryPatchMod.Settings;
            int maxABMRounds = settings?.maxABMInjectionRounds ?? 3;
            
            var abmList = ABMCollector.Collect(pawn, maxABMRounds);
            
            if (abmList.Count == 0)
            {
                return "(Спогадів ABM немає)";
            }
            
            return MemoryFormatter.Format(abmList, startIndex: 1);
        }
    }
}
