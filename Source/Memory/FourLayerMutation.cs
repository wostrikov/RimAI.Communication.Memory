using Ustas.RimAI.Communication.Memory.Capture;
using Ustas.RimAI.Communication.Memory.UI;
using Ustas.RimAI.Communication.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Ustas.RimAI.Communication.Memory.Diagnostics;

namespace Ustas.RimAI.Communication.Memory
{
    internal sealed class FourLayerMutation : FourLayerMemoryCompCollaborator
    {
        internal FourLayerMutation(FourLayerMemoryComp owner) : base(owner) { }

public void EditMemory(string memoryId, string newContent, string notes = null)
        {
            var memory = Owner.FindMemoryById(memoryId);
            if (memory != null)
            {
                memory.Content = newContent;
                if (!memory.IsUserEdited)
                {
                    memory.IsUserEdited = true;
                }
                if (!string.IsNullOrEmpty(notes))
                    memory.Notes = notes;
            }
        }

public void PinMemory(string memoryId, bool pinned)
        {
            var memory = Owner.FindMemoryById(memoryId);
            if (memory is RoundMemory roundMemory)
            {
                PinRoundMemory(roundMemory, memoryId);
                return;
            }
            if (memory != null)
            {
                memory.IsPinned = pinned;
            }
            if (memory?.Layer == MemoryLayer.Active && memory.IsPinned == true)
            {
                memory.Layer = MemoryLayer.Situational;
                SituationalMemories?.Add(memory);
                ActiveMemories?.Remove(memory);
            }
        }

public void PinRoundMemory(RoundMemory roundMemory, string memoryId)
        {
            ModuleLog.Message("[RoundMemory] FourLayerMemoryComp.PinMemory: Pinning RoundMemory");

            var newMemory = new MemoryEntry(
            content: string.Empty,
            type: MemoryType.Conversation,
            layer: MemoryLayer.Situational,
            importance: 0.5f
            )
            {
                Content = roundMemory.Content,
                GameTick = roundMemory.GameTick,
                relatedPawnId = roundMemory.relatedPawnId,
                relatedPawnName = roundMemory.relatedPawnName,
                location = roundMemory.location,
                tags = new(roundMemory.tags ?? Enumerable.Empty<string>()),
                keywords = new(roundMemory.keywords ?? Enumerable.Empty<string>()),
                IsUserEdited = true,
                IsPinned = true,
                Notes = roundMemory.Notes,
            };
            SituationalMemories?.Add(newMemory);
            DeleteMemory(memoryId);
            ModuleLog.Message("[RoundMemory] FourLayerMemoryComp.PinMemory: Pinned RoundMemory as MemoryEntry");

            roundMemory.IsPinned = false;

            FourLayerMemoryComp.GetMemoryWindowInstance()?.InvalidateCache();
            ModuleLog.Message("[RoundMemory] FourLayerMemoryComp.PinMemory: Refreshed Memory Window UI");
        }

public void DeleteMemory(string memoryId)
        {
            activeMemories.RemoveAll(m => m.Id == memoryId);
            situationalMemories.RemoveAll(m => m.Id == memoryId);
            eventLogMemories.RemoveAll(m => m.Id == memoryId);
            archiveMemories.RemoveAll(m => m.Id == memoryId);
        }

public void ManualArchive()
        {
            if (eventLogMemories.Count == 0) return;

            var pawn = parent as Pawn;
            if (pawn == null) return;

            var byType = eventLogMemories.GroupBy(m => m.Type);

            int archivedCount = 0;
            foreach (var typeGroup in byType)
            {
                var memories = typeGroup.ToList();
                string archiveSummary = AI.IndependentAISummarizer.SummarizeMemories(pawn, memories, "deep_archive");

                if (!string.IsNullOrEmpty(archiveSummary))
                {
                    var archiveEntry = new MemoryEntry(
                        content: archiveSummary,
                        type: typeGroup.Key,
                        layer: MemoryLayer.Archive,
                        importance: memories.Average(m => m.Importance) + 0.3f
                    );

                    archiveEntry.AddTag("Ручне архівування");
                    archiveEntry.AddTag($"з {memories.Count} записів ELS");
                    archiveMemories.Insert(0, archiveEntry);
                    archivedCount++;
                }
            }

            if (archivedCount > 0)
            {
                eventLogMemories.Clear();
                ModuleLog.Message($"[Memory] {parent.LabelShort} manual archive: {archivedCount} entries");
            }
        }

public void AddActiveMemory(string content, MemoryType type, float importance = 1f, string relatedPawn = null)
        {
            bool flag = IsDuplicateMemory(content, relatedPawn, type);
            if (flag)
            {
                bool devMode = Prefs.DevMode;
                if (devMode)
                {
                    Pawn pawn = parent as Pawn;
                    string pawnLabel = ((pawn != null) ? pawn.LabelShort : null) ?? "Unknown";
                    ModuleLog.Message(string.Concat(
                    [
                        "[Memory] Skipped duplicate memory for ",
                        pawnLabel,
                        ": ",
                        content.Substring(0, Math.Min(50, content.Length)),
                        "..."
                    ]));
                }
            }
            else
            {
                MemoryEntry memory = new MemoryEntry(content, type, MemoryLayer.Active, importance, relatedPawn);
                Owner.ExtractKeywords(memory);
                activeMemories.Insert(0, memory);

                if (IsRoundMemoryEnabled) return;

                int nonPinnedCount = activeMemories.Count((MemoryEntry m) => !m.IsPinned);
                bool flag2 = nonPinnedCount > MaxABM;
                if (flag2)
                {
                    MemoryEntry oldest = (from m in activeMemories
                                          where !m.IsPinned
                                          orderby m.GameTick
                                          select m).FirstOrDefault();
                    bool flag3 = oldest != null;
                    if (flag3)
                    {
                        activeMemories.Remove(oldest);
                        Owner.PromoteToSituational(oldest);
                    }
                }
            }
        }

internal bool IsDuplicateMemory(string content, string relatedPawn, MemoryType type)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            foreach (var memory in activeMemories)
            {
                if (memory.Type == type && memory.Content == content && memory.relatedPawnName == relatedPawn)
                    return true;
            }

            int checkCount = Math.Min(5, situationalMemories.Count);
            for (int i = 0; i < checkCount; i++)
            {
                var memory = situationalMemories[i];
                if (memory.Type == type && memory.Content == content && memory.relatedPawnName == relatedPawn)
                    return true;
            }

            return false;
        }
    }
}
