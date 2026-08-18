using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Patches;

namespace Ustas.RimAI.Communication.Memory
{
    internal sealed class MemoryManagerArchive : MemoryManagerCollaborator
    {
        internal MemoryManagerArchive(MemoryManager owner) : base(owner) { }

internal void CheckArchiveInterval(int currentDay)
        {
            // 检查设置是否启用CLPA自动归档
            if (!RimTalkMemoryPatchMod.Settings.enableAutoArchive)
                return;
            
            int intervalDays = RimTalkMemoryPatchMod.Settings.archiveIntervalDays;
            
            // 检查是否到达归档间隔
            if (lastArchiveDay == -1)
            {
                // 首次初始化，记录当前天数
                lastArchiveDay = currentDay;
                return;
            }
            
            int daysSinceLastArchive = currentDay - lastArchiveDay;
            
            // 如果距离上次归档还没到间隔天数，直接返回
            if (daysSinceLastArchive < intervalDays)
                return;
            
            // 到达归档时间
            Log.Message($"[RimAI.Memory] 📚 Day {currentDay}: Triggering CLPA archive (every {intervalDays} days)");
            
            int totalArchivedPawns = 0;
            int totalArchivedEntries = 0;
            int totalRemovedELS = 0;
            
            // 遍历所有殖民者，执行 ELS → CLPA 归档
            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                {
                    // ⭐ v3.5.2: 扩展到殖民者 + 配置了链接催化剂的殖民地动物/机械体
                    if (!pawn.IsColonist && !MemoryManager.IsColonyAnimalWithVocalLink(pawn))
                        continue;
                    
                    var fourLayerComp = pawn.TryGetComp<FourLayerMemoryComp>();
                    if (fourLayerComp == null)
                        continue;
                    
                    // 检查是否有 ELS 记忆需要归档
                    if (fourLayerComp.EventLogMemories.Count == 0)
                        continue;
                    
                    // ⭐ 步骤1：选择最旧的前 25% ELS 记忆进行归档（移除 isUserEdited 检查）
                    var nonPinnedELS = fourLayerComp.EventLogMemories
                        .Where(m => !m.IsPinned)
                        .ToList();
                    
                    if (nonPinnedELS.Count == 0)
                        continue;
                    
                    // 计算归档数量（前25%，至少1条）
                    int archiveCount = Math.Max(1, (int)(nonPinnedELS.Count * 0.25f));
                    
                    // 选择最旧的记忆
                    var toArchive = nonPinnedELS
                        .OrderBy(m => m.GameTick)
                        .Take(archiveCount)
                        .ToList();
                    
                    if (toArchive.Count == 0)
                        continue;
                    
                    // ⭐ 步骤2：将选中的记忆按类型分组并总结归档
                    var byType = toArchive.GroupBy(m => m.Type);
                    
                    int archivedCount = 0;
                    foreach (var typeGroup in byType)
                    {
                        var memories = typeGroup.ToList();
                        
                        // 创建归档摘要（简单版本）
                        string archiveSummary = CreateArchiveSummary(memories, typeGroup.Key);
                        
                        // ⭐ 修复：使用被归档记忆中最晚（最新）的timestamp作为归档entry的时间戳
                        int latestTimestamp = memories.Max(m => m.GameTick);
                        
                        var archiveEntry = new MemoryEntry(
                            content: archiveSummary,
                            type: typeGroup.Key,
                            layer: MemoryLayer.Archive,
                            importance: memories.Average(m => m.Importance) + 0.3f // CLPA 记忆重要性更高
                        );
                        
                        // ⭐ 修复：覆盖默认的timestamp
                        archiveEntry.GameTick = latestTimestamp;
                        
                        // 合并关键词和标签
                        archiveEntry.keywords.AddRange(memories.SelectMany(m => m.keywords).Distinct());
                        archiveEntry.tags.AddRange(memories.SelectMany(m => m.tags).Distinct());
                        archiveEntry.AddTag("自动归档");
                        archiveEntry.AddTag($"源自{memories.Count}条ELS");
                        
                        // ⭐ 如果启用 AI 总结，异步更新归档内容
                        if (RimTalkMemoryPatchMod.Settings.useAISummarization && AI.IndependentAISummarizer.IsAvailable())
                        {
                            string cacheKey = AI.IndependentAISummarizer.ComputeCacheKey(pawn, memories);
                            
                            AI.IndependentAISummarizer.RegisterCallback(cacheKey, (aiSummary) =>
                            {
                                if (!string.IsNullOrEmpty(aiSummary))
                                {
                                    archiveEntry.Content = aiSummary;
                                    archiveEntry.RemoveTag("简单归档");
                                    archiveEntry.AddTag("AI归档");
                                    archiveEntry.Notes = "Глибоке архівування ШІ завершено.";
                                }
                            });
                            
                            AI.IndependentAISummarizer.SummarizeMemories(pawn, memories, "deep_archive");
                            
                            archiveEntry.AddTag("简单归档");
                            archiveEntry.AddTag("待AI更新");
                            archiveEntry.Notes = "ШІ виконує глибоке архівування у фоновому режимі…";
                        }
                        
                        // 添加到 CLPA
                        fourLayerComp.ArchiveMemories.Insert(0, archiveEntry);
                        archivedCount++;
                    }
                    
                    // ⭐ 步骤3：只删除已归档的记忆
                    int removedCount = 0;
                    foreach (var memory in toArchive)
                    {
                        if (fourLayerComp.EventLogMemories.Remove(memory))
                        {
                            removedCount++;
                        }
                    }
                    
                    if (archivedCount > 0)
                    {
                        totalArchivedPawns++;
                        totalArchivedEntries += archivedCount;
                        totalRemovedELS += removedCount;
                        
                        if (Prefs.DevMode)
                        {
                            int remainingELS = fourLayerComp.EventLogMemories.Count;
                            Log.Message($"[RimAI.Memory] Archived {archivedCount} CLPA entries for {pawn.LabelShort}, " +
                                       $"removed {removedCount} ELS (前25%), kept {remainingELS} ELS (including {remainingELS - nonPinnedELS.Count + removedCount} pinned/edited)");
                        }
                    }
                    
                    // ⭐ 步骤4：清理超过上限的旧 CLPA 记忆（移除 isUserEdited 检查）
                    int maxArchive = RimTalkMemoryPatchMod.Settings.maxArchiveMemories;
                    if (fourLayerComp.ArchiveMemories.Count > maxArchive)
                    {
                        // 移除最旧的低重要性记忆（只保护固定记忆）
                        var toRemove = fourLayerComp.ArchiveMemories
                            .Where(m => !m.IsPinned)
                            .OrderBy(m => m.Importance)
                            .ThenBy(m => m.GameTick)
                            .Take(fourLayerComp.ArchiveMemories.Count - maxArchive)
                            .ToList();
                        
                        foreach (var memory in toRemove)
                        {
                            fourLayerComp.ArchiveMemories.Remove(memory);
                        }
                        
                        if (Prefs.DevMode && toRemove.Count > 0)
                        {
                            Log.Message($"[RimAI.Memory] Cleaned {toRemove.Count} old CLPA memories for {pawn.LabelShort}");
                        }
                    }
                }
            }
            
            // 更新最后归档日期
            lastArchiveDay = currentDay;
            
            // 输出总结日志
            if (totalArchivedPawns > 0)
            {
                Log.Message($"[RimAI.Memory] ✅ Автоархівацію CLPA завершено: колоністів {totalArchivedPawns}, створено записів CLPA {totalArchivedEntries}, вилучено ELS {totalRemovedELS} (перші 25%)");
                
                // 可选：给用户一个通知
                Messages.Message(
                    $"Автоархівування CLPA завершено: колоністів — {totalArchivedPawns}, архівних спогадів — {totalArchivedEntries}, архівовано ELS — {totalRemovedELS}",
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
            }
            else
            {
                Log.Message($"[RimAI.Memory] ✅ CLPA auto-archive check complete: no memories to archive");
            }
        }

internal string CreateArchiveSummary(List<MemoryEntry> memories, MemoryType type)
        {
            if (memories == null || memories.Count == 0)
                return null;
            
            var summary = new StringBuilder();
            
            // 归档摘要格式：更详细，因为是长期保存
            if (type == MemoryType.Conversation)
            {
                // 对话归档：按对话对象分组
                var byPerson = memories
                    .Where(m => !string.IsNullOrEmpty(m.relatedPawnName))
                    .GroupBy(m => m.relatedPawnName)
                    .OrderByDescending(g => g.Count());
                
                summary.Append($"对话归档（{memories.Count}条）：");
                int shown = 0;
                foreach (var group in byPerson.Take(10))
                {
                    if (shown > 0) summary.Append("；");
                    summary.Append($"与{group.Key}对话×{group.Count()}");
                    shown++;
                }
            }
            else if (type == MemoryType.Action)
            {
                // 行动归档：按行动类型分组
                summary.Append($"行动归档（{memories.Count}条）：");
                
                var grouped = memories
                    .Select(m => m.Content.Length > 20 ? m.Content.Substring(0, 20) : m.Content)
                    .GroupBy(a => a)
                    .OrderByDescending(g => g.Count());
                
                int shown = 0;
                foreach (var group in grouped.Take(5))
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
                // 其他类型归档：保留更多细节
                summary.Append($"{type}归档（{memories.Count}条）：");
                
                var grouped = memories
                    .GroupBy(m => m.Content.Length > 30 ? m.Content.Substring(0, 30) : m.Content)
                    .OrderByDescending(g => g.Count());
                
                int shown = 0;
                foreach (var group in grouped.Take(8))
                {
                    if (shown > 0) summary.Append("；");
                    
                    string content = group.First().Content;
                    if (content.Length > 60)
                        content = content.Substring(0, 60) + "...";
                    
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
            
            return summary.ToString();
        }
    }
}
