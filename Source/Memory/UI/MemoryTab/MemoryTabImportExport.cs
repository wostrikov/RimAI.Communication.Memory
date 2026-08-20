using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Persistence;
using Ustas.RimAI.Core.Diagnostics;

namespace Ustas.RimAI.Communication.Memory.UI
{
    internal sealed class MemoryTabImportExport : MemoryTabCollaborator
    {
        internal MemoryTabImportExport(MainTabWindow_Memory owner) : base(owner) { }

        // ==================== Import/Export ====================
        
        internal void ExportMemories()
        {
            if (Owner.selectedPawn == null || Owner.currentMemoryComp == null)
            {
                Messages.Message("RimTalk_Memory_ExportNoPawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            try
            {
                string fileName = $"{Owner.selectedPawn.Name.ToStringShort}_Memories_{Find.TickManager.TicksGame}.xml";
                string savePath = MemorySidecarStorage.MemoryExportsDirectory;
                
                if (!MemorySidecarStorage.DirectoryExists(savePath))
                {
                    MemorySidecarStorage.CreateDirectory(savePath);
                }
                
                string fullPath = System.IO.Path.Combine(savePath, fileName);
                
                var allMemories = new List<MemoryEntry>();
                allMemories.AddRange(Owner.currentMemoryComp.ActiveMemories);
                allMemories.AddRange(Owner.currentMemoryComp.SituationalMemories);
                allMemories.AddRange(Owner.currentMemoryComp.EventLogMemories);
                allMemories.AddRange(Owner.currentMemoryComp.ArchiveMemories);
                
                string pawnId = Owner.selectedPawn.ThingID;
                string pawnName = Owner.selectedPawn.Name.ToStringShort;
                
                // Serialization / save-load constraint — keep field identity stable. (Verse XML)
                Scribe.saver.InitSaving(fullPath, "MemoryExport");
                Scribe_Values.Look(ref pawnId, "pawnId");
                Scribe_Values.Look(ref pawnName, "pawnName");
                Scribe_Collections.Look(ref allMemories, "memories", LookMode.Deep);
                Scribe.saver.FinalizeSaving();
                
                Messages.Message("RimTalk_Memory_ExportSuccess".Translate(allMemories.Count, fileName), 
                    MessageTypeDefOf.PositiveEvent, false);
                
                RimAiLog.Info(RimAiLogCategory.Memory, $"[RimAI.Memory] Exported {allMemories.Count} memories to: {fullPath}");
                
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "RimTalk_Memory_OpenExportFolder".Translate(),
                    delegate
                    {
                        System.Diagnostics.Process.Start(savePath);
                    },
                    true,
                    "RimTalk_Memory_ExportSuccessTitle".Translate()
                ));
            }
            catch (System.Exception ex)
            {
                Messages.Message("RimTalk_Memory_ExportFailed".Translate(ex.Message), 
                    MessageTypeDefOf.RejectInput, false);
                RimAiLog.Error(RimAiLogCategory.Memory, "[RimAI.Memory] Memory export failed.", exception: ex);
            }
        }
        
        internal void ImportMemories()
        {
            if (Owner.selectedPawn == null || Owner.currentMemoryComp == null)
            {
                Messages.Message("RimTalk_Memory_ImportNoPawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            string savePath = MemorySidecarStorage.MemoryExportsDirectory;
            
            if (!MemorySidecarStorage.DirectoryExists(savePath))
            {
                Messages.Message("RimTalk_Memory_ImportNoFolder".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            var files = MemorySidecarStorage.GetFiles(savePath, "*.xml");
            
            if (files.Length == 0)
            {
                Messages.Message("RimTalk_Memory_ImportNoFiles".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            
            options.Add(new FloatMenuOption("RimTalk_Memory_OpenFolder".Translate(), delegate
            {
                System.Diagnostics.Process.Start(savePath);
            }));
            
            options.Add(new FloatMenuOption("─────────────────────", null));
            
            foreach (var file in files.OrderByDescending(f => MemorySidecarStorage.GetLastWriteTime(f)))
            {
                string fileName = System.IO.Path.GetFileName(file);
                var fileInfo = new System.IO.FileInfo(file);
                string label = $"{fileName} ({fileInfo.Length / 1024}KB - {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm})";
                
                options.Add(new FloatMenuOption(label, delegate
                {
                    ImportFromFile(file);
                }));
            }
            
            Find.WindowStack.Add(new FloatMenu(options));
        }
        
        internal void ImportFromFile(string filePath)
        {
            try
            {
                List<MemoryEntry> importedMemories = new List<MemoryEntry>();
                string pawnId = "";
                string pawnName = "";
                
                Scribe.loader.InitLoading(filePath);
                Scribe_Values.Look(ref pawnId, "pawnId");
                Scribe_Values.Look(ref pawnName, "pawnName");
                Scribe_Collections.Look(ref importedMemories, "memories", LookMode.Deep);
                Scribe.loader.FinalizeLoading();
                
                if (importedMemories == null || importedMemories.Count == 0)
                {
                    Messages.Message("RimTalk_Memory_ImportEmpty".Translate(), 
                        MessageTypeDefOf.RejectInput, false);
                    return;
                }
                
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "RimTalk_Memory_ImportConfirm".Translate(pawnName, importedMemories.Count, Owner.selectedPawn.Name.ToStringShort),
                    delegate
                    {
                        int imported = 0;
                        
                        foreach (var memory in importedMemories)
                        {
                            switch (memory.Layer)
                            {
                                case MemoryLayer.Active:
                                    Owner.currentMemoryComp.ActiveMemories.Add(memory);
                                    imported++;
                                    break;
                                    
                                case MemoryLayer.Situational:
                                    if (Owner.currentMemoryComp.SituationalMemories.Count < RimTalkMemoryPatchMod.Settings.maxSituationalMemories)
                                    {
                                        Owner.currentMemoryComp.SituationalMemories.Add(memory);
                                        imported++;
                                    }
                                    break;
                                    
                                case MemoryLayer.EventLog:
                                    if (Owner.currentMemoryComp.EventLogMemories.Count < RimTalkMemoryPatchMod.Settings.maxEventLogMemories)
                                    {
                                        Owner.currentMemoryComp.EventLogMemories.Add(memory);
                                        imported++;
                                    }
                                    break;
                                    
                                case MemoryLayer.Archive:
                                    if (Owner.currentMemoryComp.ArchiveMemories.Count < RimTalkMemoryPatchMod.Settings.maxArchiveMemories)
                                    {
                                        Owner.currentMemoryComp.ArchiveMemories.Add(memory);
                                        imported++;
                                    }
                                    break;
                            }
                        }
                        
                        Owner.filtersDirty = true;
                        
                        Messages.Message("RimTalk_Memory_ImportSuccess".Translate(imported, importedMemories.Count), 
                            MessageTypeDefOf.PositiveEvent, false);
                        
                        RimAiLog.Info(RimAiLogCategory.Memory, $"[RimAI.Memory] Imported {imported}/{importedMemories.Count} memories from: {filePath}");
                    }
                ));
            }
            catch (System.Exception ex)
            {
                Messages.Message("RimTalk_Memory_ImportFailed".Translate(ex.Message), 
                    MessageTypeDefOf.RejectInput, false);
                RimAiLog.Error(RimAiLogCategory.Memory, "[RimAI.Memory] Memory import failed.", exception: ex);
            }
        }
    }
}