using Ustas.RimAI.Communication.Memory.Capture;
using Ustas.RimAI.Communication.Memory.Policy;
using Ustas.RimAI.Communication.Memory.UI;
using Ustas.RimAI.Communication.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    internal sealed class FourLayerSummarization : FourLayerMemoryCompCollaborator
    {
        internal FourLayerSummarization(FourLayerMemoryComp owner) : base(owner) { }

public void DailySummarization()
        {
            if (activeMemories.Count == 0 && situationalMemories.Count == 0) return;

            var pawn = parent as Pawn;
            if (pawn == null) return;

            var allMemoriesToSummarize = new List<MemoryEntry>();
            allMemoriesToSummarize.AddRange(activeMemories.Where(m => m.CanBeSummarized));
            allMemoriesToSummarize.AddRange(situationalMemories.Where(m => m.CanBeSummarized));

            if (allMemoriesToSummarize.Count == 0)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"[Memory] {pawn?.LabelShort ?? "Unknown"} daily summarization: no non-pinned memories to summarize");
                }
                return;
            }

            var byType = allMemoriesToSummarize.GroupBy(m => MemoryType.Conversation);

            foreach (var typeGroup in byType)
            {
                var memories = typeGroup.ToList();
                string simpleSummary = CreateSimpleSummary(memories, typeGroup.Key);

                int latestTimestamp = memories.Max(m => m.GameTick);

                var summaryEntry = new MemoryEntry(
                    content: simpleSummary,
                    type: typeGroup.Key,
                    layer: MemoryLayer.EventLog,
                    importance: memories.Average(m => m.Importance) + 0.2f
                );

                summaryEntry.GameTick = latestTimestamp;

                summaryEntry.keywords.AddRange(memories.SelectMany(m => m.keywords).Distinct());
                summaryEntry.tags.AddRange(memories.SelectMany(m => m.tags).Distinct());
                summaryEntry.AddTag("Стислий підсумок");

                if (RimTalkMemoryPatchMod.Settings.useAISummarization && AI.IndependentAISummarizer.IsAvailable())
                {
                    string cacheKey = AI.IndependentAISummarizer.ComputeCacheKey(pawn, memories);

                    AI.IndependentAISummarizer.RegisterCallback(cacheKey, (aiSummary) =>
                    {
                        if (!string.IsNullOrEmpty(aiSummary))
                        {
                            summaryEntry.Content = aiSummary;
                            summaryEntry.RemoveTag("Стислий підсумок");
                            summaryEntry.AddTag("Підсумок ШІ");
                            summaryEntry.Notes = "Підсумок від ШІ завершено у фоні й оновлено автоматично.";
                        }
                    });

                    AI.IndependentAISummarizer.SummarizeMemories(pawn, memories, "daily_summary");

                    summaryEntry.AddTag("чекає на оновлення ШІ");
                    summaryEntry.Notes = "Підсумок від ШІ обробляється у фоні...";
                }

                InsertMemoryByTimestamp(eventLogMemories, summaryEntry);
            }

            int removedCount = MemoryConsolidationPolicy.ApplyAfterSummary(
                allMemoriesToSummarize,
                activeMemories,
                situationalMemories,
                memory => { if (memory != null) memory.IsSummarized = true; },
                memory => memory.IsPinned);

            if (Prefs.DevMode && removedCount > 0)
            {
                Log.Message($"[Memory] {pawn?.LabelShort ?? "Unknown"} daily summarization: " +
                           $"cleared ABM, removed {removedCount} SCM, kept {situationalMemories.Count} pinned");
            }

            TrimEventLog();
        }

public void ManualSummarization()
        {
            if (activeMemories.Count == 0 && situationalMemories.Count == 0) return;

            var pawn = parent as Pawn;
            if (pawn == null) return;

            var allMemoriesToSummarize = new List<MemoryEntry>();
            allMemoriesToSummarize.AddRange(activeMemories.Where(m => m.CanBeSummarized));
            allMemoriesToSummarize.AddRange(situationalMemories.Where(m => m.CanBeSummarized));

            if (allMemoriesToSummarize.Count == 0)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"[Memory] {pawn?.LabelShort ?? "Unknown"} manual summarization: no non-pinned memories to summarize");
                }
                return;
            }

            var byType = allMemoriesToSummarize.GroupBy(m => MemoryType.Conversation);

            foreach (var typeGroup in byType)
            {
                var memories = typeGroup.ToList();
                string simpleSummary = CreateSimpleSummary(memories, typeGroup.Key);

                int latestTimestamp = memories.Max(m => m.GameTick);

                var summaryEntry = new MemoryEntry(
                    content: simpleSummary,
                    type: typeGroup.Key,
                    layer: MemoryLayer.EventLog,
                    importance: memories.Average(m => m.Importance) + 0.2f
                );

                summaryEntry.GameTick = latestTimestamp;

                summaryEntry.keywords.AddRange(memories.SelectMany(m => m.keywords).Distinct());
                summaryEntry.tags.AddRange(memories.SelectMany(m => m.tags).Distinct());
                summaryEntry.AddTag("Ручний підсумок");

                if (RimTalkMemoryPatchMod.Settings.useAISummarization && AI.IndependentAISummarizer.IsAvailable())
                {
                    string cacheKey = AI.IndependentAISummarizer.ComputeCacheKey(pawn, memories);

                    AI.IndependentAISummarizer.RegisterCallback(cacheKey, (aiSummary) =>
                    {
                        if (!string.IsNullOrEmpty(aiSummary))
                        {
                            summaryEntry.Content = aiSummary;
                            summaryEntry.RemoveTag("Стислий підсумок");
                            summaryEntry.AddTag("Підсумок ШІ");
                            summaryEntry.Notes = "Підсумок від ШІ завершено у фоні й оновлено автоматично.";
                        }
                    });

                    AI.IndependentAISummarizer.SummarizeMemories(pawn, memories, "daily_summary");

                    summaryEntry.AddTag("чекає на оновлення ШІ");
                    summaryEntry.Notes = "Підсумок від ШІ обробляється у фоні...";
                }

                InsertMemoryByTimestamp(eventLogMemories, summaryEntry);
            }

            int removedCount = MemoryConsolidationPolicy.ApplyAfterSummary(
                allMemoriesToSummarize,
                activeMemories,
                situationalMemories,
                memory => { if (memory != null) memory.IsSummarized = true; },
                memory => memory.IsPinned);

            if (Prefs.DevMode && removedCount > 0)
            {
                Log.Message($"[Memory] {pawn?.LabelShort ?? "Unknown"} manual summarization: " +
                           $"cleared ABM, removed {removedCount} SCM, kept {situationalMemories.Count} pinned");
            }

            TrimEventLog();
        }

internal void InsertMemoryByTimestamp(List<MemoryEntry> list, MemoryEntry entry)
        {
            if (list.Count == 0)
            {
                list.Add(entry);
                return;
            }

            int insertIndex = list.FindIndex(m => m.GameTick < entry.GameTick);

            if (insertIndex == -1)
            {
                list.Add(entry);
            }
            else
            {
                list.Insert(insertIndex, entry);
            }
        }

internal string CreateSimpleSummary(List<MemoryEntry> memories, MemoryType type)
        {
            if (memories == null || memories.Count == 0)
                return null;

            var summary = new StringBuilder();

            if (type == MemoryType.Conversation)
            {
                var byPerson = memories
                    .Where(m => !string.IsNullOrEmpty(m.relatedPawnName))
                    .GroupBy(m => m.relatedPawnName)
                    .OrderByDescending(g => g.Count());

                int shown = 0;
                foreach (var group in byPerson.Take(5))
                {
                    if (shown > 0) summary.Append("；");
                    summary.Append($"розмов з {group.Key} ×{group.Count()}");
                    shown++;
                }

                if (shown == 0)
                {
                    summary.Append($"розмов: {memories.Count}");
                }
            }
            else if (type == MemoryType.Action)
            {
                var actions = new List<string>();
                foreach (var m in memories)
                {
                    string action = m.Content.Length > 15 ? m.Content.Substring(0, 15) : m.Content;
                    actions.Add(action);
                }

                var grouped = actions
                    .GroupBy(a => a)
                    .OrderByDescending(g => g.Count());

                int shown = 0;
                foreach (var group in grouped.Take(3))
                {
                    if (shown > 0) summary.Append("；");
                    if (group.Count() > 1)
                    {
                        summary.Append($"{group.Key}×{group.Count()}");
                    }
                    else
                    {
                        summary.Append(group.Key);
                    }
                    shown++;
                }
            }
            else
            {
                var grouped = memories
                    .GroupBy(m => m.Content.Length > 20 ? m.Content.Substring(0, 20) : m.Content)
                    .OrderByDescending(g => g.Count());

                int shown = 0;
                foreach (var group in grouped.Take(5))
                {
                    if (shown > 0) summary.Append("；");

                    string content = group.First().Content;
                    if (content.Length > 40)
                        content = content.Substring(0, 40) + "...";

                    if (group.Count() > 1)
                    {
                        summary.Append($"{content}×{group.Count()}");
                    }
                    else
                    {
                        summary.Append(content);
                    }
                    shown++;
                }
            }

            if (summary.Length > 0 && memories.Count > 3)
            {
                summary.Append($"(усього {memories.Count})");
            }

            return summary.Length > 0 ? summary.ToString() : $"{type}: спогадів {memories.Count}";
        }

internal void TrimEventLog()
        {
            if (eventLogMemories.Count <= MaxELS)
                return;

            int nonPinnedCount = eventLogMemories.Count(m => !m.IsPinned);

            if (nonPinnedCount <= MaxELS)
                return;

            int toRemoveCount = nonPinnedCount - MaxELS;
            var toRemove = eventLogMemories
                .Where(m => !m.IsPinned)
                .OrderBy(m => m.GameTick)
                .Take(toRemoveCount)
                .ToList();

            foreach (var memory in toRemove)
            {
                eventLogMemories.Remove(memory);
                memory.Layer = MemoryLayer.Archive;
                archiveMemories.Insert(0, memory);
            }
        }

internal void ExtractKeywords(MemoryEntry memory)
        {
            if (string.IsNullOrEmpty(memory.Content))
                return;

            var words = memory.Content
                .Split(new[] { ' ', '，', '。', '、', '；', '：', '-', '×' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 1)
                .Distinct()
                .Take(10);

            foreach (var word in words)
            {
                memory.AddKeyword(word);
            }
        }
    }
}
