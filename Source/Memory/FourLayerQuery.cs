using Ustas.RimAI.Communication.Memory.Capture;
using Ustas.RimAI.Communication.Memory.UI;
using Ustas.RimAI.Communication.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    internal sealed class FourLayerQuery : FourLayerMemoryCompCollaborator
    {
        internal FourLayerQuery(FourLayerMemoryComp owner) : base(owner) { }

public List<MemoryEntry> RetrieveMemories(MemoryQuery query)
        {
            var results = new List<MemoryEntry>();

            var abmCandidates = activeMemories
                .Where(m => MatchesQuery(m, query))
                .OrderByDescending(m => m.GameTick);
            results.AddRange(abmCandidates);

            if (situationalMemories.Count > 0)
            {
                var scmCandidates = situationalMemories
                    .Where(m => MatchesQuery(m, query))
                    .OrderByDescending(m => m.CalculateRetrievalScore(null, query.keywords))
                    .ThenBy(m => m.Id, StringComparer.Ordinal)
                    .Take(5);
                results.AddRange(scmCandidates);
            }

            if (query.includeContext && results.Count < query.maxCount)
            {
                var elsCandidates = eventLogMemories
                    .Where(m => MatchesQuery(m, query))
                    .OrderByDescending(m => m.CalculateRetrievalScore(null, query.keywords))
                    .ThenBy(m => m.Id, StringComparer.Ordinal)
                    .Take(query.maxCount - results.Count);
                results.AddRange(elsCandidates);
            }

            if (query.layer == MemoryLayer.Archive)
            {
                var clpaCandidates = archiveMemories
                    .Where(m => MatchesQuery(m, query))
                    .OrderByDescending(m => m.Importance)
                    .ThenBy(m => m.Id, StringComparer.Ordinal)
                    .Take(3);
                results.AddRange(clpaCandidates);
            }

            return results.Take(query.maxCount).ToList();
        }

internal bool MatchesQuery(MemoryEntry memory, MemoryQuery query)
        {
            if (query.type.HasValue && memory.Type != query.type.Value)
                return false;

            if (query.layer.HasValue && memory.Layer != query.layer.Value)
                return false;

            if (!string.IsNullOrEmpty(query.relatedPawn) && memory.relatedPawnName != query.relatedPawn)
                return false;

            if (query.tags.Any() && !query.tags.Any(t => memory.tags.Contains(t)))
                return false;

            return true;
        }

public List<MemoryEntry> GetAllMemories()
        {
            var all = new List<MemoryEntry>();
            all.AddRange(activeMemories);
            all.AddRange(situationalMemories);
            all.AddRange(eventLogMemories);
            all.AddRange(archiveMemories);
            return all;
        }

public string GetMemoryContext(int count = 5)
        {
            var query = new MemoryQuery
            {
                maxCount = count,
                includeContext = true
            };

            var memories = RetrieveMemories(query);
            var context = new StringBuilder();

            foreach (var memory in memories)
            {
                context.AppendLine($"- [{memory.TypeName}] {memory.Content} ({memory.AgeString})");
            }

            return context.ToString();
        }

public List<MemoryEntry> GetRelevantMemories(int count = 5)
        {
            var query = new MemoryQuery
            {
                maxCount = count,
                includeContext = true
            };

            return RetrieveMemories(query);
        }

internal MemoryEntry FindMemoryById(string id)
        {
            return activeMemories.FirstOrDefault(m => m.Id == id)
                ?? situationalMemories.FirstOrDefault(m => m.Id == id)
                ?? eventLogMemories.FirstOrDefault(m => m.Id == id)
                ?? archiveMemories.FirstOrDefault(m => m.Id == id);
        }
    }
}
