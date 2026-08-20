using System.Collections.Generic;
using System.Linq;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory;
using Verse;

namespace Ustas.RimAI.Communication.Memory.UI
{
    internal sealed class MemoryTabHelpers : MemoryTabCollaborator
    {
        internal MemoryTabHelpers(MainTabWindow_Memory owner) : base(owner) { }

        internal void AggregateMemories(
            List<MemoryEntry> memories,
            MemoryLayer targetLayer,
            List<MemoryEntry> sourceList,
            List<MemoryEntry> targetList,
            string promptTemplate)
        {
            var memoriesToSummarize = memories.Where(m => m.CanBeSummarized).ToList();

            if (memoriesToSummarize.Count == 0)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("[Memory] AggregateMemories: All selected memories are pinned, skipping summarization");
                }
                return;
            }

            var byType = memoriesToSummarize.GroupBy(m => MemoryType.Conversation);

            foreach (var typeGroup in byType)
            {
                var items = typeGroup.ToList();

                int latestTimestamp = items.Max(m => m.GameTick);

                var aggregated = new MemoryEntry(
                    content: targetLayer == MemoryLayer.Archive
                        ? CreateArchiveSummary(items, typeGroup.Key)
                        : CreateSimpleSummary(items, typeGroup.Key),
                    type: typeGroup.Key,
                    layer: targetLayer,
                    importance: items.Average(m => m.Importance) + (targetLayer == MemoryLayer.Archive ? 0.3f : 0.2f)
                );

                aggregated.GameTick = latestTimestamp;

                aggregated.keywords.AddRange(items.SelectMany(m => m.keywords).Distinct());
                aggregated.tags.AddRange(items.SelectMany(m => m.tags).Distinct());
                aggregated.AddTag(targetLayer == MemoryLayer.Archive ? "手动归档" : "选中总结");
                if (targetLayer == MemoryLayer.Archive)
                {
                    aggregated.AddTag($"源自{items.Count}条ELS");
                }

                var settings = RimTalkMemoryPatchMod.Settings;
                if (settings.useAISummarization && AI.IndependentAISummarizer.IsAvailable())
                {
                    string cacheKey = AI.IndependentAISummarizer.ComputeCacheKey(Owner.selectedPawn, items);

                    AI.IndependentAISummarizer.RegisterCallback(cacheKey, (aiSummary) =>
                    {
                        if (!string.IsNullOrEmpty(aiSummary))
                        {
                            aggregated.Content = aiSummary;
                            aggregated.RemoveTag("简单总结");
                            aggregated.RemoveTag("简单归档");
                            aggregated.AddTag(targetLayer == MemoryLayer.Archive ? "AI归档" : "AI总结");
                            aggregated.Notes = $"AI {(targetLayer == MemoryLayer.Archive ? "深度归档" : "总结")}已完成";
                        }
                    });

                    AI.IndependentAISummarizer.SummarizeMemories(Owner.selectedPawn, items, promptTemplate);

                    aggregated.AddTag("简单" + (targetLayer == MemoryLayer.Archive ? "归档" : "总结"));
                    aggregated.AddTag("待AI更新");
                    aggregated.Notes = $"AI {(targetLayer == MemoryLayer.Archive ? "深度归档" : "总结")}正在后台处理中...";
                }

                InsertMemoryByTimestamp(targetList, aggregated);
            }

            foreach (var memory in memoriesToSummarize)
            {
                if (memory != null) memory.IsSummarized = true;
            }

            sourceList.RemoveAll(m => m == null || !m.IsPinned);
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
                return "";

            var sb = new System.Text.StringBuilder();

            if (type == MemoryType.Conversation)
            {
                var byPerson = memories
                    .Where(m => !string.IsNullOrEmpty(m.relatedPawnName))
                    .GroupBy(m => m.relatedPawnName)
                    .OrderByDescending(g => g.Count());

                int shown = 0;
                foreach (var group in byPerson.Take(5))
                {
                    if (shown > 0) sb.Append("；");
                    sb.Append($"与{group.Key}对话×{group.Count()}");
                    shown++;
                }

                if (shown == 0)
                    sb.Append($"对话{memories.Count}次");
            }
            else if (type == MemoryType.Action)
            {
                var grouped = memories
                    .Select(m => m.Content.Length > 15 ? m.Content.Substring(0, 15) : m.Content)
                    .GroupBy(a => a)
                    .OrderByDescending(g => g.Count());

                int shown = 0;
                foreach (var group in grouped.Take(3))
                {
                    if (shown > 0) sb.Append("；");
                    sb.Append(group.Count() > 1 ? $"{group.Key}×{group.Count()}" : group.Key);
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
                    if (shown > 0) sb.Append("；");

                    string content = group.First().Content;
                    if (content.Length > 40)
                        content = content.Substring(0, 40) + "...";

                    sb.Append(group.Count() > 1 ? $"{content}×{group.Count()}" : content);
                    shown++;
                }
            }

            if (sb.Length > 0 && memories.Count > 3)
                sb.Append($"（共{memories.Count}条）");

            return sb.Length > 0 ? sb.ToString() : $"{type}记忆{memories.Count}条";
        }

        internal string CreateArchiveSummary(List<MemoryEntry> memories, MemoryType type)
        {
            if (memories == null || memories.Count == 0)
                return "";

            var sb = new System.Text.StringBuilder();
            sb.Append($"{(type == MemoryType.Conversation ? "对话" : type == MemoryType.Action ? "行动" : type.ToString())}归档（{memories.Count}条）：");

            if (type == MemoryType.Conversation)
            {
                var byPerson = memories
                    .Where(m => !string.IsNullOrEmpty(m.relatedPawnName))
                    .GroupBy(m => m.relatedPawnName)
                    .OrderByDescending(g => g.Count());

                int shown = 0;
                foreach (var group in byPerson.Take(10))
                {
                    if (shown > 0) sb.Append("；");
                    sb.Append($"与{group.Key}对话×{group.Count()}");
                    shown++;
                }
            }
            else if (type == MemoryType.Action)
            {
                var grouped = memories
                    .Select(m => m.Content.Length > 20 ? m.Content.Substring(0, 20) : m.Content)
                    .GroupBy(a => a)
                    .OrderByDescending(g => g.Count());

                int shown = 0;
                foreach (var group in grouped.Take(5))
                {
                    if (shown > 0) sb.Append("；");
                    sb.Append(group.Count() > 1 ? $"{group.Key}×{group.Count()}" : group.Key);
                    shown++;
                }
            }
            else
            {
                var grouped = memories
                    .GroupBy(m => m.Content.Length > 30 ? m.Content.Substring(0, 30) : m.Content)
                    .OrderByDescending(g => g.Count());

                int shown = 0;
                foreach (var group in grouped.Take(8))
                {
                    if (shown > 0) sb.Append("；");

                    string content = group.First().Content;
                    if (content.Length > 60)
                        content = content.Substring(0, 60) + "...";

                    sb.Append(group.Count() > 1 ? $"{content}×{group.Count()}" : content);
                    shown++;
                }
            }

            return sb.ToString();
        }
    }
}
