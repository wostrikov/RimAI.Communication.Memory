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
            if (request?.LayeredPawnMemories == true)
                return GetLayeredPawnMemories(request);

            var pawn = MemoryPawnResolver.Resolve(request);
            var settings = RimTalkMemoryPatchMod.Settings;
            int maxABMRounds = settings?.maxABMInjectionRounds ?? 3;
            int maxTotal = ResolveMaxMemoryEntries(request, settings?.maxInjectedMemories ?? 10);
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

            var knowledge = request?.IncludeKnowledge == false
                ? new MemoryContextResult { Source = "typed" }
                : GetKnowledge(request);
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

            var pawn = MemoryPawnResolver.Resolve(request);
            var scores = new List<KnowledgeScore>();
            int maxEntries = request.MaxEntries > 0
                ? request.MaxEntries
                : ResolveMaxMemoryEntries(request, settings?.maxInjectedKnowledge ?? 10);
            string text = library.InjectKnowledgeWithDetails(
                query,
                maxEntries,
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

        MemoryContextResult GetLayeredPawnMemories(MemoryContextRequest request)
        {
            var pawn = MemoryPawnResolver.Resolve(request);
            if (pawn == null)
                return new MemoryContextResult { Source = "typed" };
            var comp = pawn.TryGetComp<FourLayerMemoryComp>();
            if (comp == null)
                return new MemoryContextResult { Source = "typed" };

            int sinceTick = request.SinceTick;
            int limit = request.PerLayerLimit > 0 ? request.PerLayerLimit : 5;
            var memories = new List<MemoryContextEntry>();
            AppendLayer(memories, comp.ArchiveMemories, "Archive", limit, sinceTick);
            AppendLayer(memories, comp.EventLogMemories, "EventLog", limit, sinceTick);
            AppendLayer(memories, comp.SituationalMemories, "Situational", limit, sinceTick);
            return new MemoryContextResult
            {
                Memories = memories,
                Source = "typed"
            };
        }

        static void AppendLayer(
            List<MemoryContextEntry> target,
            IEnumerable<MemoryEntry> source,
            string kind,
            int limit,
            int sinceTick)
        {
            if (source == null)
                return;
            int added = 0;
            foreach (var entry in source)
            {
                if (entry == null || added >= limit)
                    break;
                if (sinceTick > 0 && entry.GameTick <= sinceTick)
                    break;
                var text = entry.DisplayContent ?? entry.Content;
                if (string.IsNullOrEmpty(text))
                    continue;
                target.Add(new MemoryContextEntry(
                    entry.Id ?? string.Empty,
                    kind,
                    text,
                    entry.Importance,
                    entry.GameTick,
                    entry.Type.ToString()));
                added++;
            }
        }

        /// <summary>
        /// Applies settings quota capped by TokenBudget using the Relations convention
        /// (<see cref="MemoryContextDefaults.TokensPerMemoryEntry"/> tokens per entry).
        /// Default Talk budget 2000 → 25 entries, so typical maxInjectedMemories=10 is unchanged.
        /// </summary>
        static int ResolveMaxMemoryEntries(MemoryContextRequest request, int settingsMax)
        {
            int maxTotal = settingsMax > 0 ? settingsMax : 10;
            if (request?.TokenBudget > 0)
            {
                int fromBudget = Math.Max(1, request.TokenBudget / MemoryContextDefaults.TokensPerMemoryEntry);
                maxTotal = Math.Min(maxTotal, fromBudget);
            }

            if (request != null && request.MaxEntries > 0)
                maxTotal = Math.Min(maxTotal, request.MaxEntries);
            return maxTotal;
        }
    }
}
