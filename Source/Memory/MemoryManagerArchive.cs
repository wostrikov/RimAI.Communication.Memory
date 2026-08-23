using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Patches;
using Ustas.RimAI.Communication.Memory.Policy;

namespace Ustas.RimAI.Communication.Memory
{
    internal sealed class MemoryManagerArchive : MemoryManagerCollaborator
    {
        internal MemoryManagerArchive(MemoryManager owner) : base(owner) { }

internal void CheckArchiveInterval(int currentDay)
        {
            var cadence = MemoryArchiveCyclePolicy.DecideCadence(
                RimTalkMemoryPatchMod.Settings.enableAutoArchive,
                lastArchiveDay,
                currentDay,
                RimTalkMemoryPatchMod.Settings.archiveIntervalDays);
            if (cadence == MemoryArchiveCadenceAction.SkipDisabled ||
                cadence == MemoryArchiveCadenceAction.NotDue)
                return;
            if (cadence == MemoryArchiveCadenceAction.ArmFirstDay)
            {
                lastArchiveDay = currentDay;
                return;
            }
            
            Log.Message($"[RimAI.Memory] 📚 Day {currentDay}: Triggering CLPA archive (every {RimTalkMemoryPatchMod.Settings.archiveIntervalDays} days)");
            
            int totalArchivedPawns = 0;
            int totalArchivedEntries = 0;
            int totalRemovedELS = 0;
            
            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (!pawn.IsColonist && !MemoryManager.IsColonyAnimalWithVocalLink(pawn))
                        continue;
                    
                    var fourLayerComp = pawn.TryGetComp<FourLayerMemoryComp>();
                    if (fourLayerComp == null)
                        continue;
                    
                    if (fourLayerComp.EventLogMemories.Count == 0)
                        continue;
                    
                    var nonPinnedELS = fourLayerComp.EventLogMemories
                        .Where(m => !m.IsPinned)
                        .ToList();
                    
                    if (nonPinnedELS.Count == 0)
                        continue;
                    
                    int archiveCount = MemoryArchiveCyclePolicy.ArchiveCount(nonPinnedELS.Count);
                    
                    var toArchive = nonPinnedELS
                        .OrderBy(m => m.GameTick)
                        .Take(archiveCount)
                        .ToList();
                    
                    if (toArchive.Count == 0)
                        continue;
                    
                    var byType = toArchive.GroupBy(m => m.Type);
                    
                    int archivedCount = 0;
                    foreach (var typeGroup in byType)
                    {
                        var memories = typeGroup.ToList();
                        
                        string archiveSummary = CreateArchiveSummary(memories, typeGroup.Key);
                        
                        int latestTimestamp = memories.Max(m => m.GameTick);
                        
                        var archiveEntry = new MemoryEntry(
                            content: archiveSummary,
                            type: typeGroup.Key,
                            layer: MemoryLayer.Archive,
                            importance: memories.Average(m => m.Importance) + 0.3f
                        );
                        
                        archiveEntry.GameTick = latestTimestamp;
                        
                        archiveEntry.keywords.AddRange(memories.SelectMany(m => m.keywords).Distinct());
                        archiveEntry.tags.AddRange(memories.SelectMany(m => m.tags).Distinct());
                        archiveEntry.AddTag("自动归档");
                        archiveEntry.AddTag($"源自{memories.Count}条ELS");
                        
                        if (MemoryArchiveCyclePolicy.UseOptionalAiSummary(
                            RimTalkMemoryPatchMod.Settings.useAISummarization,
                            AI.IndependentAISummarizer.IsAvailable()))
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
                        
                        fourLayerComp.ArchiveMemories.Insert(0, archiveEntry);
                        archivedCount++;
                    }
                    
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
                    
                    int maxArchive = RimTalkMemoryPatchMod.Settings.maxArchiveMemories;
                    if (fourLayerComp.ArchiveMemories.Count > maxArchive)
                    {
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
            
            lastArchiveDay = currentDay;
            
            if (totalArchivedPawns > 0)
            {
                Log.Message($"[RimAI.Memory] ✅ Автоархівацію CLPA завершено: колоністів {totalArchivedPawns}, створено записів CLPA {totalArchivedEntries}, вилучено ELS {totalRemovedELS} (перші 25%)");
                
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
            
            if (type == MemoryType.Conversation)
            {
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
