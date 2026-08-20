using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.UI
{
    internal sealed class MemoryTabActions : MemoryTabCollaborator
    {
        internal MemoryTabActions(MainTabWindow_Memory owner) : base(owner) { }

        // ==================== Batch Actions ====================
        
        internal void SummarizeMemories(List<MemoryEntry> targetMemories)
        {
            if (Owner.currentMemoryComp == null || targetMemories == null || targetMemories.Count == 0)
                return;
            
            var abmMemories = targetMemories
                .Where(m => m.Layer == MemoryLayer.Active && m.CanBeSummarized)
                .ToList();
                
            var scmMemories = targetMemories
                .Where(m => m.Layer == MemoryLayer.Situational && m.CanBeSummarized)
                .ToList();
            
            var allMemoriesToSummarize = new List<MemoryEntry>();
            allMemoriesToSummarize.AddRange(abmMemories);
            allMemoriesToSummarize.AddRange(scmMemories);
                
            if (allMemoriesToSummarize.Count == 0)
            {
                Messages.Message("Немає спогадів ABM або SCM, які можна підсумувати", MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            string confirmMessage;
            if (abmMemories.Count > 0 && scmMemories.Count > 0)
            {
                confirmMessage = $"Підсумувати {abmMemories.Count} спогадів ABM і {scmMemories.Count} спогадів SCM?";
            }
            else if (abmMemories.Count > 0)
            {
                confirmMessage = $"Підсумувати {abmMemories.Count} спогадів ABM?";
            }
            else
            {
                confirmMessage = $"Підсумувати {scmMemories.Count} спогадів SCM?";
            }
            
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                confirmMessage,
                delegate
                {
                    Parts.Helpers.AggregateMemories(
                        allMemoriesToSummarize,
                        MemoryLayer.EventLog,
                        Owner.currentMemoryComp.SituationalMemories,
                        Owner.currentMemoryComp.EventLogMemories,
                        "daily_summary"
                    );
                    
                    foreach (var abm in abmMemories)
                    {
                        Owner.currentMemoryComp.ActiveMemories.Remove(abm);
                    }
                    
                    Owner.selectedMemories.Clear();
                    Owner.filtersDirty = true;
                    Messages.Message("RimTalk_MindStream_SummarizedN".Translate(scmMemories.Count), MessageTypeDefOf.PositiveEvent, false);
                }
            ));
        }
        
        internal void ArchiveMemories(List<MemoryEntry> targetMemories)
        {
            if (Owner.currentMemoryComp == null || targetMemories == null || targetMemories.Count == 0)
                return;
            
            var elsMemories = targetMemories
                .Where(m => m.Layer == MemoryLayer.EventLog && m.CanBeSummarized)
                .ToList();
                
            if (elsMemories.Count == 0)
            {
                Messages.Message("RimTalk_MindStream_NoELSSelected".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "RimTalk_MindStream_ArchiveConfirm".Translate(elsMemories.Count),
                delegate
                {
                    Parts.Helpers.AggregateMemories(
                        elsMemories,
                        MemoryLayer.Archive,
                        Owner.currentMemoryComp.EventLogMemories,
                        Owner.currentMemoryComp.ArchiveMemories,
                        "deep_archive"
                    );
                    
                    Owner.selectedMemories.Clear();
                    Owner.filtersDirty = true;
                    Messages.Message("RimTalk_MindStream_ArchivedN".Translate(elsMemories.Count), MessageTypeDefOf.PositiveEvent, false);
                }
            ));
        }
        
        internal void DeleteMemories(List<MemoryEntry> targetMemories)
        {
            if (Owner.currentMemoryComp == null || targetMemories == null || targetMemories.Count == 0)
                return;
            
            int count = targetMemories.Count;
            
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "RimTalk_MindStream_DeleteConfirm".Translate(count),
                delegate
                {
                    foreach (var memory in targetMemories.ToList())
                    {
                        Owner.currentMemoryComp.DeleteMemory(memory.Id);
                    }
                    
                    Owner.selectedMemories.Clear();
                    Owner.filtersDirty = true;
                    Messages.Message("RimTalk_MindStream_DeletedN".Translate(count), MessageTypeDefOf.PositiveEvent, false);
                }
            ));
        }
        
        internal void SummarizeAll()
        {
            List<Pawn> pawnsToSummarize = new List<Pawn>();
            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.FreeColonists)
                {
                    var comp = pawn.TryGetComp<PawnMemoryComp>();
                    if (comp != null && comp.SituationalMemories.Count > 0)
                    {
                        pawnsToSummarize.Add(pawn);
                    }
                }
            }
            
            if (pawnsToSummarize.Count > 0)
            {
                var memoryManager = Find.World.GetComponent<MemoryManager>();
                memoryManager?.QueueManualSummarization(pawnsToSummarize);
                Messages.Message("RimTalk_MindStream_QueuedSummarization".Translate(pawnsToSummarize.Count), MessageTypeDefOf.TaskCompletion, false);
            }
            else
            {
                Messages.Message("RimTalk_MindStream_NoNeedSummarization".Translate(), MessageTypeDefOf.RejectInput, false);
            }
        }
        
        
    }
}
