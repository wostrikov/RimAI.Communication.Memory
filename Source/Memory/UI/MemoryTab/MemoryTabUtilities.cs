using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.UI
{
    internal sealed class MemoryTabUtilities : MemoryTabCollaborator
    {
        internal MemoryTabUtilities(MainTabWindow_Memory owner) : base(owner) { }

        // ==================== Helper Methods ====================
        
        internal List<MemoryEntry> GetFilteredMemories()
        {
            if (Owner.filtersDirty || Owner.cachedFilteredMemories == null)
            {
                RebuildFilteredMemories();
                Owner.filtersDirty = false;
            }
            
            return Owner.cachedFilteredMemories;
        }
        
        internal void RebuildFilteredMemories()
        {
            if (Owner.currentMemoryComp == null)
            {
                Owner.cachedFilteredMemories = new List<MemoryEntry>();
                return;
            }
            
            var memories = new List<MemoryEntry>();
            
            if (Owner.showABM)
            {
                memories.AddRange(Owner.currentMemoryComp.ActiveMemories.Where(m => Owner.filterType == null || m.Type == Owner.filterType.Value));
            }
            
            if (Owner.showSCM)
            {
                memories.AddRange(Owner.currentMemoryComp.SituationalMemories.Where(m => Owner.filterType == null || m.Type == Owner.filterType.Value));
            }
            
            if (Owner.showELS)
            {
                memories.AddRange(Owner.currentMemoryComp.EventLogMemories.Where(m => Owner.filterType == null || m.Type == Owner.filterType.Value));
            }
            
            if (Owner.showCLPA)
            {
                memories.AddRange(Owner.currentMemoryComp.ArchiveMemories.Where(m => Owner.filterType == null || m.Type == Owner.filterType.Value));
            }
            
            // Sort by timestamp (newest first)
            Owner.cachedFilteredMemories = memories.OrderByDescending(m => m.GameTick).ToList();
        }
        
        internal float GetCardHeight(MemoryLayer layer)
        {
            switch (layer)
            {
                case MemoryLayer.Active:
                    return 80f;
                case MemoryLayer.Situational:
                    return 100f;
                case MemoryLayer.EventLog:
                    return 130f;
                case MemoryLayer.Archive:
                    return 160f;
                default:
                    return 100f;
            }
        }
        
        internal int GetContentMaxLength(MemoryLayer layer)
        {
            switch (layer)
            {
                case MemoryLayer.Active:
                    return 80;
                case MemoryLayer.Situational:
                    return 120;
                case MemoryLayer.EventLog:
                    return 200;
                case MemoryLayer.Archive:
                    return 300;
                default:
                    return 120;
            }
        }
        
        internal Color GetLayerColor(MemoryLayer layer)
        {
            switch (layer)
            {
                case MemoryLayer.Active:
                    return new Color(0.3f, 0.8f, 1f); // Cyan
                case MemoryLayer.Situational:
                    return new Color(0.3f, 1f, 0.5f); // Green
                case MemoryLayer.EventLog:
                    return new Color(1f, 0.8f, 0.3f); // Yellow
                case MemoryLayer.Archive:
                    return new Color(0.8f, 0.4f, 1f); // Purple
                default:
                    return Color.white;
            }
        }
        
        internal string GetLayerLabel(MemoryLayer layer)
        {
            switch (layer)
            {
                case MemoryLayer.Active:
                    return "ABM";
                case MemoryLayer.Situational:
                    return "SCM";
                case MemoryLayer.EventLog:
                    return "ELS";
                case MemoryLayer.Archive:
                    return "CLPA";
                default:
                    return "UNK";
            }
        }
        
        internal void DrawNoPawnSelected(Rect rect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, "RimTalk_MindStream_SelectColonist".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
        
        internal void DrawNoMemoryComponent(Rect rect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "RimTalk_NoMemoryComponent".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        internal void OpenCommonKnowledgeDialog()
        {
            if (Current.Game == null)
            {
                Messages.Message("RimTalk_MindStream_MustEnterGame".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            var memoryManager = Find.World.GetComponent<MemoryManager>();
            if (memoryManager == null)
            {
                Messages.Message("RimTalk_MindStream_CannotFindManager".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            Find.WindowStack.Add(new Dialog_CommonKnowledge(memoryManager.CommonKnowledge));
        }
        
        internal void ShowOperationGuide()
        {
            string guide = "RimTalk_MindStream_GuideContent".Translate();
            
            Find.WindowStack.Add(new Dialog_MessageBox(guide, "RimTalk_Close".Translate(), null, "RimTalk_MindStream_OperationGuide".Translate()));
        }
    }
}