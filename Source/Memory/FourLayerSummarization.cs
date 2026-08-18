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
    internal sealed class FourLayerSummarization : FourLayerMemoryCompCollaborator
    {
        internal FourLayerSummarization(FourLayerMemoryComp owner) : base(owner) { }

public void DailySummarization()
        {
            // ⭐ 修复：同时检查ABM和SCM是否有内容
            if (activeMemories.Count == 0 && situationalMemories.Count == 0) return;

            var pawn = parent as Pawn;
            if (pawn == null) return;

            // ⭐ 修复：合并ABM和SCM作为总结池，排除总结过的记忆（即旧的固定记忆）
            var allMemoriesToSummarize = new List<MemoryEntry>();
            allMemoriesToSummarize.AddRange(activeMemories.Where(m => m.CanBeSummarized));
            allMemoriesToSummarize.AddRange(situationalMemories.Where(m => m.CanBeSummarized));

            // 如果没有未总结过的记忆，不需要总结
            if (allMemoriesToSummarize.Count == 0)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"[Memory] {pawn?.LabelShort ?? "Unknown"} daily summarization: no non-pinned memories to summarize");
                }
                return;
            }

            // MemoryType.Conversation即总结得到的ELS的记忆类型，可以根据需要调整为其他类型，建议改为总结独有类型
            var byType = allMemoriesToSummarize.GroupBy(m => MemoryType.Conversation);

            foreach (var typeGroup in byType)
            {
                var memories = typeGroup.ToList();
                string simpleSummary = CreateSimpleSummary(memories, typeGroup.Key);

                // ⭐ 修复：使用被总结记忆中最晚（最新）的timestamp作为总结的时间戳
                int latestTimestamp = memories.Max(m => m.GameTick);

                var summaryEntry = new MemoryEntry(
                    content: simpleSummary,
                    type: typeGroup.Key,
                    layer: MemoryLayer.EventLog,
                    importance: memories.Average(m => m.Importance) + 0.2f
                );

                // ⭐ 修复：覆盖默认的timestamp（MemoryEntry构造函数会自动设置为当前时间）
                summaryEntry.GameTick = latestTimestamp;

                summaryEntry.keywords.AddRange(memories.SelectMany(m => m.keywords).Distinct());
                summaryEntry.tags.AddRange(memories.SelectMany(m => m.tags).Distinct());
                summaryEntry.AddTag("简单总结");

                if (RimTalkMemoryPatchMod.Settings.useAISummarization && AI.IndependentAISummarizer.IsAvailable())
                {
                    string cacheKey = AI.IndependentAISummarizer.ComputeCacheKey(pawn, memories);

                    AI.IndependentAISummarizer.RegisterCallback(cacheKey, (aiSummary) =>
                    {
                        if (!string.IsNullOrEmpty(aiSummary))
                        {
                            summaryEntry.Content = aiSummary;
                            summaryEntry.RemoveTag("简单总结");
                            summaryEntry.AddTag("AI总结");
                            summaryEntry.Notes = "AI 总结已于后台完成并自动更新。";
                        }
                    });

                    AI.IndependentAISummarizer.SummarizeMemories(pawn, memories, "daily_summary");

                    summaryEntry.AddTag("待AI更新");
                    summaryEntry.Notes = "AI 总结正在后台处理中...";
                }

                // ⭐ 修复：根据时间戳插入到正确位置，而不是总是插入到开头
                InsertMemoryByTimestamp(eventLogMemories, summaryEntry);
            }

            foreach (var memory in allMemoriesToSummarize)
            {
                if (memory != null) memory.IsSummarized = true; // 标记为已总结
            }

            // ⭐ 修复：清空ABM（总结后不再需要保留）
            activeMemories.Clear();

            // ⭐ 修复：清空SCM（移除 isUserEdited 检查，只保留固定记忆）
            int beforeCount = situationalMemories.Count;
            situationalMemories.RemoveAll(m => !m.IsPinned);
            int removedCount = beforeCount - situationalMemories.Count;

            if (Prefs.DevMode && removedCount > 0)
            {
                Log.Message($"[Memory] {pawn?.LabelShort ?? "Unknown"} daily summarization: " +
                           $"cleared ABM, removed {removedCount} SCM, kept {situationalMemories.Count} pinned");
            }

            TrimEventLog();
        }

public void ManualSummarization()
        {
            // ⭐ 修复：同时检查ABM和SCM是否有内容
            if (activeMemories.Count == 0 && situationalMemories.Count == 0) return;

            var pawn = parent as Pawn;
            if (pawn == null) return;

            // ⭐ 修复：合并ABM和SCM作为总结池，排除总结过的记忆（即旧的固定记忆）
            var allMemoriesToSummarize = new List<MemoryEntry>();
            allMemoriesToSummarize.AddRange(activeMemories.Where(m => m.CanBeSummarized));
            allMemoriesToSummarize.AddRange(situationalMemories.Where(m => m.CanBeSummarized));

            // 如果没有非固定记忆，不需要总结
            if (allMemoriesToSummarize.Count == 0)
            {
                if (Prefs.DevMode)
                {
                    Log.Message($"[Memory] {pawn?.LabelShort ?? "Unknown"} manual summarization: no non-pinned memories to summarize");
                }
                return;
            }

            // MemoryType.Conversation即总结得到的ELS的记忆类型，可以根据需要调整为其他类型，建议改为总结独有类型
            var byType = allMemoriesToSummarize.GroupBy(m => MemoryType.Conversation);

            foreach (var typeGroup in byType)
            {
                var memories = typeGroup.ToList();
                string simpleSummary = CreateSimpleSummary(memories, typeGroup.Key);

                // ⭐ 修复：使用被总结记忆中最晚（最新）的timestamp作为总结的时间戳
                int latestTimestamp = memories.Max(m => m.GameTick);

                var summaryEntry = new MemoryEntry(
                    content: simpleSummary,
                    type: typeGroup.Key,
                    layer: MemoryLayer.EventLog,
                    importance: memories.Average(m => m.Importance) + 0.2f
                );

                // ⭐ 修复：覆盖默认的timestamp
                summaryEntry.GameTick = latestTimestamp;

                summaryEntry.keywords.AddRange(memories.SelectMany(m => m.keywords).Distinct());
                summaryEntry.tags.AddRange(memories.SelectMany(m => m.tags).Distinct());
                summaryEntry.AddTag("手动总结");

                // ⭐ 修改：手动总结也使用AI（如果启用）
                if (RimTalkMemoryPatchMod.Settings.useAISummarization && AI.IndependentAISummarizer.IsAvailable())
                {
                    string cacheKey = AI.IndependentAISummarizer.ComputeCacheKey(pawn, memories);

                    AI.IndependentAISummarizer.RegisterCallback(cacheKey, (aiSummary) =>
                    {
                        if (!string.IsNullOrEmpty(aiSummary))
                        {
                            summaryEntry.Content = aiSummary;
                            summaryEntry.RemoveTag("简单总结");
                            summaryEntry.AddTag("AI总结");
                            summaryEntry.Notes = "AI 总结已于后台完成并自动更新。";
                        }
                    });

                    AI.IndependentAISummarizer.SummarizeMemories(pawn, memories, "daily_summary");

                    summaryEntry.AddTag("待AI更新");
                    summaryEntry.Notes = "AI 总结正在后台处理中...";
                }

                // ⭐ 修复：根据时间戳插入到正确位置，而不是总是插入到开头
                InsertMemoryByTimestamp(eventLogMemories, summaryEntry);
            }

            foreach (var memory in allMemoriesToSummarize)
            {
                if (memory != null) memory.IsSummarized = true; // 标记为已总结
            }

            // ⭐ 修复：清空ABM（总结后不再需要保留）
            activeMemories.Clear();

            // ⭐ 修复：清空SCM（移除 isUserEdited 检查，只保留固定记忆）
            int beforeCount = situationalMemories.Count;
            situationalMemories.RemoveAll(m => !m.IsPinned);
            int removedCount = beforeCount - situationalMemories.Count;

            if (Prefs.DevMode && removedCount > 0)
            {
                Log.Message($"[Memory] {pawn?.LabelShort ?? "Unknown"} manual summarization: " +
                           $"cleared ABM, removed {removedCount} SCM, kept {situationalMemories.Count} pinned");
            }

            TrimEventLog();
        }

internal void InsertMemoryByTimestamp(List<MemoryEntry> list, MemoryEntry entry)
        {
            // 如果列表为空，直接添加
            if (list.Count == 0)
            {
                list.Add(entry);
                return;
            }

            // 使用二分查找找到插入位置（降序排列，新的在前）
            int insertIndex = list.FindIndex(m => m.GameTick < entry.GameTick);

            // 如果没找到（所有记忆都比新记忆新），添加到末尾
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
                    summary.Append($"与{group.Key}对话×{group.Count()}");
                    shown++;
                }

                if (shown == 0)
                {
                    summary.Append($"对话{memories.Count}次");
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
                summary.Append($"（共{memories.Count}条）");
            }

            return summary.Length > 0 ? summary.ToString() : $"{type}记忆{memories.Count}条";
        }

internal void TrimEventLog()
        {
            if (eventLogMemories.Count <= MaxELS)
                return;

            // ⭐ 修复：只计算非固定的记忆数量（移除 isUserEdited 检查）
            int nonPinnedCount = eventLogMemories.Count(m => !m.IsPinned);

            // 如果非固定记忆没超过上限，则不需要trim
            if (nonPinnedCount <= MaxELS)
                return;

            // ⭐ 修复：按时间戳排序，只移除非固定的最旧记忆（移除 isUserEdited 检查）
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
