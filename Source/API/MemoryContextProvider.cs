using System;
using System.Collections.Generic;
using System.Linq;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Injection;
using Ustas.RimAI.Core.Memory;
using Verse;

namespace Ustas.RimAI.Communication.Memory.API
{
    /// <summary>Canonical typed Memory/Knowledge context. Scriban providers are compatibility adapters.</summary>
    public sealed class MemoryContextProvider : IMemoryContextProvider, IKnowledgeContextProvider
    {
        public MemoryContextResult GetContext(MemoryContextRequest request)
        {
            var pawn = ResolvePawn(request);
            var settings = RimTalkMemoryPatchMod.Settings;
            int maxABMRounds = settings?.maxABMInjectionRounds ?? 3;
            int maxTotal = settings?.maxInjectedMemories ?? 10;
            var memories = new List<MemoryContextEntry>();
            var allEntries = new List<MemoryEntry>();

            if (pawn != null)
            {
                var abm = ABMCollector.Collect(pawn, maxABMRounds) ?? new List<MemoryEntry>();
                allEntries.AddRange(abm);
                int remaining = maxTotal - abm.Count;
                if (remaining > 0)
                {
                    var els = ELSCollector.Collect(pawn, request?.Query, remaining) ?? new List<MemoryEntry>();
                    allEntries.AddRange(els);
                }
            }

            foreach (var entry in allEntries)
            {
                if (entry == null)
                    continue;
                memories.Add(new MemoryContextEntry(
                    entry.Id ?? string.Empty,
                    entry.Layer.ToString(),
                    entry.DisplayContent ?? entry.Content ?? string.Empty,
                    entry.Importance));
            }

            var knowledge = GetKnowledge(request);
            var projection = allEntries.Count == 0
                ? string.Empty
                : MemoryFormatter.Format(allEntries, startIndex: 1);
            return new MemoryContextResult
            {
                Memories = memories,
                Knowledge = knowledge.Knowledge,
                Projection = projection,
                Source = "typed"
            };
        }

        public MemoryContextResult GetKnowledge(MemoryContextRequest request)
        {
            var query = request?.Query ?? string.Empty;
            var settings = RimTalkMemoryPatchMod.Settings;
            var library = MemoryManager.GetCommonKnowledge();
            if (library == null)
            {
                return new MemoryContextResult { Source = "typed", Projection = string.Empty };
            }

            var pawn = ResolvePawn(request);
            var scores = new List<KnowledgeScore>();
            string text = library.InjectKnowledgeWithDetails(
                query,
                settings?.maxInjectedKnowledge ?? 10,
                out scores,
                pawn,
                null);
            var entries = new List<MemoryContextEntry>();
            if (scores != null)
            {
                foreach (var score in scores)
                {
                    if (score?.Entry == null)
                        continue;
                    entries.Add(new MemoryContextEntry(
                        score.Entry.id ?? string.Empty,
                        "knowledge",
                        score.Entry.content ?? string.Empty,
                        score.Score));
                }
            }

            return new MemoryContextResult
            {
                Knowledge = entries,
                Projection = text ?? string.Empty,
                Source = "typed"
            };
        }

        static Pawn ResolvePawn(MemoryContextRequest request)
        {
            var id = request?.PawnId;
            if (string.IsNullOrEmpty(id))
                id = request?.PawnIds?.FirstOrDefault();
            if (string.IsNullOrEmpty(id))
                return null;
            var maps = Find.Maps;
            if (maps == null)
                return null;
            foreach (var map in maps)
            {
                var pawn = map?.mapPawns?.AllPawns?.FirstOrDefault(p => p != null && p.ThingID == id);
                if (pawn != null)
                    return pawn;
            }

            return null;
        }
    }
}
