using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.UI
{
    internal sealed class MemoryTabControls : MemoryTabCollaborator
    {
        internal MemoryTabControls(MainTabWindow_Memory owner) : base(owner) { }

        // ==================== Control Panel ====================
        
        internal void DrawControlPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(MainTabWindow_Memory.SPACING);
            float y = innerRect.y;
            
            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(innerRect.x, y, innerRect.width, 30f), "RimTalk_MindStream_MemoryFilters".Translate());
            Text.Font = GameFont.Small;
            y += 35f;
            
            // Layer Filters
            y = DrawLayerFilters(innerRect, y);
            y += 10f;
            
            // Type Filters
            y = DrawTypeFilters(innerRect, y);
            y += 10f;
            
            
            // Separator
            Widgets.DrawLineHorizontal(innerRect.x, y, innerRect.width);
            y += 15f;
            
            // Batch Actions
            y = DrawBatchActions(innerRect, y);
            y += 10f;
            
            // Global Actions
            DrawGlobalActions(innerRect, y);
        }
        
        internal float DrawLayerFilters(Rect parentRect, float startY)
        {
            float y = startY;
            
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Widgets.Label(new Rect(parentRect.x, y, parentRect.width, 20f), "RimTalk_MindStream_Layers".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            y += 22f;
            
            float checkboxHeight = 24f;
            
            bool prevShowABM = Owner.showABM;
            bool prevShowSCM = Owner.showSCM;
            bool prevShowELS = Owner.showELS;
            bool prevShowCLPA = Owner.showCLPA;
            
            // ABM
            Rect abmRect = new Rect(parentRect.x, y, parentRect.width, checkboxHeight);
            Color abmColor = new Color(0.3f, 0.8f, 1f); // Cyan
            DrawColoredCheckbox(abmRect, "RimTalk_MindStream_ABM".Translate(), ref Owner.showABM, abmColor, MemoryLayer.Active);
            y += checkboxHeight + 2f;
            
            Rect scmRect = new Rect(parentRect.x, y, parentRect.width, checkboxHeight);
            Color scmColor = new Color(0.3f, 1f, 0.5f); // Green
            DrawColoredCheckbox(scmRect, "RimTalk_MindStream_SCM".Translate(), ref Owner.showSCM, scmColor, MemoryLayer.Situational);
            y += checkboxHeight + 2f;
            
            Rect elsRect = new Rect(parentRect.x, y, parentRect.width, checkboxHeight);
            Color elsColor = new Color(1f, 0.8f, 0.3f); // Yellow
            DrawColoredCheckbox(elsRect, "RimTalk_MindStream_ELS".Translate(), ref Owner.showELS, elsColor, MemoryLayer.EventLog);
            y += checkboxHeight + 2f;
            
            Rect clpaRect = new Rect(parentRect.x, y, parentRect.width, checkboxHeight);
            Color clpaColor = new Color(0.8f, 0.4f, 1f); // Purple
            DrawColoredCheckbox(clpaRect, "RimTalk_MindStream_CLPA".Translate(), ref Owner.showCLPA, clpaColor, MemoryLayer.Archive);
            y += checkboxHeight;
            
            if (Owner.showABM != prevShowABM || Owner.showSCM != prevShowSCM || Owner.showELS != prevShowELS || Owner.showCLPA != prevShowCLPA)
            {
                Owner.filtersDirty = true;
            }
            
            return y;
        }
        
        internal void DrawColoredCheckbox(Rect rect, string label, ref bool value, Color color, MemoryLayer? rightClickLayer)
        {
            if (rightClickLayer.HasValue)
            {
                if (Event.current.type == EventType.MouseDown && 
                    Event.current.button == 1 && 
                    rect.Contains(Event.current.mousePosition))
                {
                    ShowCreateMemoryMenu(rightClickLayer.Value);
                    Event.current.Use();
                    return;
                }
            }
            
            // Colored indicator
            Rect colorRect = new Rect(rect.x, rect.y + 2f, 3f, rect.height - 4f);
            Widgets.DrawBoxSolid(colorRect, color);
            
            // Checkbox
            Rect checkboxRect = new Rect(rect.x + 8f, rect.y, rect.width - 8f, rect.height);
            Widgets.CheckboxLabeled(checkboxRect, label, ref value);
            
            if (rightClickLayer.HasValue && Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(rect, "RimTalk_MindStream_RightClickToCreate".Translate(Parts.Utilities.GetLayerLabel(rightClickLayer.Value)));
            }
        }
        
        internal float DrawTypeFilters(Rect parentRect, float startY)
        {
            float y = startY;
            
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Widgets.Label(new Rect(parentRect.x, y, parentRect.width, 20f), "RimTalk_MindStream_Type".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            y += 22f;
            
            float buttonHeight = 28f;
            float spacing = 2f;
            
            // All
            bool isAllSelected = Owner.filterType == null;
            if (isAllSelected)
                GUI.color = new Color(0.5f, 0.7f, 1f);
            if (Widgets.ButtonText(new Rect(parentRect.x, y, parentRect.width, buttonHeight), "RimTalk_MindStream_All".Translate()))
            {
                if (Owner.filterType != null)
                {
                    Owner.filterType = null;
                    Owner.selectedMemories.Clear();
                    Owner.filtersDirty = true;
                }
            }
            GUI.color = Color.white;
            y += buttonHeight + spacing;
            
            // Conversation
            bool isConvSelected = Owner.filterType == MemoryType.Conversation;
            if (isConvSelected)
                GUI.color = new Color(0.5f, 0.7f, 1f);
            if (Widgets.ButtonText(new Rect(parentRect.x, y, parentRect.width, buttonHeight), "RimTalk_MindStream_Conversation".Translate()))
            {
                if (Owner.filterType != MemoryType.Conversation)
                {
                    Owner.filterType = MemoryType.Conversation;
                    Owner.selectedMemories.Clear();
                    Owner.filtersDirty = true;
                }
            }
            GUI.color = Color.white;
            y += buttonHeight + spacing;
            
            // Action
            bool isActionSelected = Owner.filterType == MemoryType.Action;
            if (isActionSelected)
                GUI.color = new Color(0.5f, 0.7f, 1f);
            if (Widgets.ButtonText(new Rect(parentRect.x, y, parentRect.width, buttonHeight), "RimTalk_MindStream_Action".Translate()))
            {
                if (Owner.filterType != MemoryType.Action)
                {
                    Owner.filterType = MemoryType.Action;
                    Owner.selectedMemories.Clear();
                    Owner.filtersDirty = true;
                }
            }
            GUI.color = Color.white;
            y += buttonHeight;
            
            return y;
        }
        
        internal float DrawBatchActions(Rect parentRect, float startY)
        {
            float y = startY;
            
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Widgets.Label(new Rect(parentRect.x, y, parentRect.width, 20f), "RimTalk_MindStream_BatchActions".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            y += 22f;
            
            float buttonHeight = 32f;
            float spacing = 5f;
            bool hasSelection = Owner.selectedMemories.Count > 0;
            
            var targetMemories = hasSelection ? Owner.selectedMemories.ToList() : Owner.cachedMemories;
            int targetCount = targetMemories.Count;
            
            int abmCount = targetMemories.Count(m => m.Layer == MemoryLayer.Active);
            int scmCount = targetMemories.Count(m => m.Layer == MemoryLayer.Situational);
            int summarizableCount = abmCount + scmCount;
            
            GUI.enabled = summarizableCount > 0;
            string summarizeLabel;
            if (hasSelection)
            {
                if (abmCount > 0 && scmCount > 0)
                {
                    summarizeLabel = "RimTalk_MindStream_SummarizeSelectedBoth".Translate(abmCount, scmCount);
                }
                else if (abmCount > 0)
                {
                    summarizeLabel = "RimTalk_MindStream_SummarizeSelectedABM".Translate(abmCount);
                }
                else
                {
                    summarizeLabel = "RimTalk_MindStream_SummarizeSelectedSCM".Translate(scmCount);
                }
            }
            else
            {
                if (abmCount > 0 && scmCount > 0)
                {
                    summarizeLabel = "RimTalk_MindStream_SummarizeAllBoth".Translate(abmCount, scmCount);
                }
                else if (abmCount > 0)
                {
                    summarizeLabel = "RimTalk_MindStream_SummarizeAllABM".Translate(abmCount);
                }
                else
                {
                    summarizeLabel = "RimTalk_MindStream_SummarizeAllSCM".Translate(scmCount);
                }
            }
            if (Widgets.ButtonText(new Rect(parentRect.x, y, parentRect.width, buttonHeight), summarizeLabel))
            {
                Parts.Actions.SummarizeMemories(targetMemories);
            }
            GUI.enabled = true;
            y += buttonHeight + spacing;
            
            // Archive Selected/All (ELS -> CLPA)
            int elsCount = targetMemories.Count(m => m.Layer == MemoryLayer.EventLog);
            GUI.enabled = elsCount > 0;
            string archiveLabel;
            if (hasSelection)
            {
                archiveLabel = "RimTalk_MindStream_ArchiveN".Translate(targetCount).ToString();
            }
            else
            {
                archiveLabel = "RimTalk_MindStream_ArchiveAllCount".Translate(elsCount);
            }
            if (Widgets.ButtonText(new Rect(parentRect.x, y, parentRect.width, buttonHeight), archiveLabel))
            {
                Parts.Actions.ArchiveMemories(targetMemories);
            }
            GUI.enabled = true;
            y += buttonHeight + spacing;
            
            // Delete Selected/All
            GUI.enabled = targetCount > 0;
            GUI.color = targetCount > 0 ? new Color(1f, 0.4f, 0.4f) : Color.white;
            string deleteLabel;
            if (hasSelection)
            {
                deleteLabel = "RimTalk_MindStream_DeleteN".Translate(targetCount).ToString();
            }
            else
            {
                deleteLabel = "RimTalk_MindStream_DeleteAllCount".Translate(targetCount);
            }
            if (Widgets.ButtonText(new Rect(parentRect.x, y, parentRect.width, buttonHeight), deleteLabel))
            {
                Parts.Actions.DeleteMemories(targetMemories);
            }
            GUI.color = Color.white;
            GUI.enabled = true;
            y += buttonHeight;
            
            return y;
        }
        
        internal void DrawGlobalActions(Rect parentRect, float startY)
        {
            float y = startY;
            
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Widgets.Label(new Rect(parentRect.x, y, parentRect.width, 20f), "RimTalk_MindStream_GlobalActions".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            y += 22f;
            
            float buttonHeight = 32f;
            float spacing = 5f;
            
            // Summarize All
            if (Widgets.ButtonText(new Rect(parentRect.x, y, parentRect.width, buttonHeight), "RimTalk_MindStream_SummarizeAll".Translate()))
            {
                Parts.Actions.SummarizeAll();
            }
            y += buttonHeight + spacing;

            

            float halfWidth = (parentRect.width - spacing) / 2f;
            
            // Export button (left)
            GUI.color = new Color(0.5f, 0.8f, 1f);
            if (Widgets.ButtonText(new Rect(parentRect.x, y, halfWidth, buttonHeight), "RimTalk_Memory_Export".Translate()))
            {
                Parts.ImportExport.ExportMemories();
            }
            
            // Import button (right)
            GUI.color = new Color(0.8f, 1f, 0.5f);
            if (Widgets.ButtonText(new Rect(parentRect.x + halfWidth + spacing, y, halfWidth, buttonHeight), "RimTalk_Memory_Import".Translate()))
            {
                Parts.ImportExport.ImportMemories();
            }
            GUI.color = Color.white;
        }
        
        internal void ShowCreateMemoryMenu(MemoryLayer layer)
        {
            if (Owner.selectedPawn == null || Owner.currentMemoryComp == null)
            {
                Messages.Message("RimTalk_MindStream_PleaseSelectColonist".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            
            string layerName = Parts.Utilities.GetLayerLabel(layer);
            
            options.Add(new FloatMenuOption("RimTalk_MindStream_AddConversationTo".Translate(layerName), delegate
            {
                Find.WindowStack.Add(new Dialog_CreateMemory(Owner.selectedPawn, Owner.currentMemoryComp, layer, MemoryType.Conversation));
            }));
            
            options.Add(new FloatMenuOption("RimTalk_MindStream_AddActionTo".Translate(layerName), delegate
            {
                Find.WindowStack.Add(new Dialog_CreateMemory(Owner.selectedPawn, Owner.currentMemoryComp, layer, MemoryType.Action));
            }));
            
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}