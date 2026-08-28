﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.UI
{
    public class Dialog_CommonKnowledge : Window
    {
        internal Dialog_CommonKnowledgeParts Parts;

        // ==================== Data & State ====================
        internal CommonKnowledgeLibrary library;
        
        // Multi-select support
        internal HashSet<CommonKnowledgeEntry> selectedEntries = new HashSet<CommonKnowledgeEntry>();
        internal CommonKnowledgeEntry lastSelectedEntry = null;
        
        // Drag selection
        internal bool isDragging = false;
        internal bool isMouseDown = false;          
        internal Vector2 dragStartPos = Vector2.zero;
        internal Vector2 dragCurrentPos = Vector2.zero;
        internal Vector2 mouseDownScreenPos = Vector2.zero;
        internal const float DRAG_THRESHOLD = 5f;   
        
        internal bool isDragReordering = false;     
        internal int dragReorderInsertIndex = -1;   
        internal bool mouseDownOnSelected = false;  
        
        // UI State
        internal Vector2 listScrollPosition = Vector2.zero;
        internal Vector2 detailScrollPosition = Vector2.zero;
        internal string searchFilter = "";
        internal bool editMode = false;
        internal bool showRightPanel = true;
        
        // Category filter
        internal KnowledgeCategory currentCategory = KnowledgeCategory.All;
        
        // Auto-generate settings (collapsed in sidebar)
        internal bool showAutoGenerateSettings = false;
        
        internal VirtualListView<CommonKnowledgeEntry> virtualList;
        
        // Edit fields
        internal string editTag = "";
        internal string editContent = "";
        internal float editImportance = 0.5f;
        internal int editTargetPawnId = -1;
        internal KeywordMatchMode editMatchMode = KeywordMatchMode.Any;
        internal KnowledgeEntryCategory editCategory = KnowledgeEntryCategory.None;
        
        // Layout constants
        internal const float TOOLBAR_HEIGHT = 45f;
        internal const float SIDEBAR_WIDTH = 240f;
        internal const float RIGHT_PANEL_WIDTH = 340f;
        internal const float SPACING = 10f;
        internal const float BUTTON_HEIGHT = 32f;
        internal const float ENTRY_HEIGHT = 70f;

        public override Vector2 InitialSize => new Vector2(1200f, 800f);

        public Dialog_CommonKnowledge(CommonKnowledgeLibrary library)
        {
            Parts = new Dialog_CommonKnowledgeParts(this);
            this.library = library;
            this.doCloseX = true;
            this.doCloseButton = false;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
            this.forcePause = false;
            
            virtualList = new VirtualListView<CommonKnowledgeEntry>(
                getItemHeight: (entry) => ENTRY_HEIGHT,
                drawItem: (rect, entry, index) => DrawEntryRow(rect, entry)
            );
            virtualList.EmptyLabel = CommonKnowledgeTranslationKeys.SelectOrCreate.Translate();
        }

        // ==================== Main Layout ====================
        
        

        // ==================== Toolbar ====================
        
        

        // ==================== Sidebar ====================
        
        
        
        

        // ==================== Center List ====================
        
        
        
        
        
        

        
        
        
        
        
        
        

        // ==================== Right Panel ====================
        
        
        
        
        
        
        
        
        
        
        
        

        // ==================== Helper Methods ====================
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
    
        #region Cluster forwards
        public override void DoWindowContents(Rect inRect) => Parts.Layout.DoWindowContents(inRect);
        internal void DrawToolbar(Rect rect) => Parts.Layout.DrawToolbar(rect);
        internal void DrawSidebar(Rect rect) => Parts.Layout.DrawSidebar(rect);
        internal void DrawAutoGenerateSettings(Rect rect) => Parts.Layout.DrawAutoGenerateSettings(rect);
        internal void DrawCenterList(Rect rect) => Parts.List.DrawCenterList(rect);
        internal void DrawEntryRow(Rect rect, CommonKnowledgeEntry entry) => Parts.List.DrawEntryRow(rect, entry);
        internal void HandleEntryClick(CommonKnowledgeEntry entry) => Parts.List.HandleEntryClick(entry);
        internal void ExecuteDragReorder() => Parts.List.ExecuteDragReorder();
        internal void DrawSelectionBox() => Parts.List.DrawSelectionBox();
        internal Rect GetSelectionBox() => Parts.List.GetSelectionBox();
        internal void HandleDragSelectionEvents(Rect listRect, Rect viewRect) => Parts.List.HandleDragSelectionEvents(listRect, viewRect);
        internal void DrawRightPanel(Rect rect) => Parts.Panels.DrawRightPanel(rect);
        internal void DrawEmptyPanel(Rect rect) => Parts.Panels.DrawEmptyPanel(rect);
        internal void DrawMultiSelectionPanel(Rect rect) => Parts.Panels.DrawMultiSelectionPanel(rect);
        internal void DrawDetailPanel(Rect rect, CommonKnowledgeEntry entry) => Parts.Panels.DrawDetailPanel(rect, entry);
        internal void DrawPropertyRow(Rect rect, string label, bool value) => Parts.Panels.DrawPropertyRow(rect, label, value);
        internal void DrawEditPanel(Rect rect) => Parts.Panels.DrawEditPanel(rect);
        internal List<CommonKnowledgeEntry> GetFilteredEntries() => Parts.Actions.GetFilteredEntries();
        internal KnowledgeCategory GetEntryCategory(CommonKnowledgeEntry entry) => Parts.Actions.GetEntryCategory(entry);
        internal string GetCategoryLabel(KnowledgeCategory category) => Parts.Actions.GetCategoryLabel(category);
        internal int GetCategoryCount(KnowledgeCategory category) => Parts.Actions.GetCategoryCount(category);
        internal Color GetCategoryColor(CommonKnowledgeEntry entry) => Parts.Actions.GetCategoryColor(entry);
        internal void CreateNewEntry() => Parts.Actions.CreateNewEntry();
        internal void StartEdit() => Parts.Actions.StartEdit();
        internal void SaveEntry() => Parts.Actions.SaveEntry();
        internal void DeleteSelectedEntries() => Parts.Actions.DeleteSelectedEntries();
        internal void ExportToFile() => Parts.Actions.ExportToFile();
        internal void ShowImportDialog() => Parts.Actions.ShowImportDialog();
        internal void ClearAllEntries() => Parts.Actions.ClearAllEntries();
        internal void GeneratePawnStatusKnowledge() => Parts.Actions.GeneratePawnStatusKnowledge();
        internal void GenerateEventRecordKnowledge() => Parts.Actions.GenerateEventRecordKnowledge();
        internal void ShowHelpDialog() => Parts.Actions.ShowHelpDialog();
        internal void ShowTagTestDialog() => Parts.Actions.ShowTagTestDialog();
        #endregion
}
    internal sealed class CommonKnowledgeActions : Dialog_CommonKnowledgeCollaborator
    {
        internal CommonKnowledgeActions(Dialog_CommonKnowledge owner) : base(owner) { }

internal List<CommonKnowledgeEntry> GetFilteredEntries()
        {
            var entries = library.Entries.AsEnumerable();
            
            // Category filter
            if (currentCategory != KnowledgeCategory.All)
            {
                entries = entries.Where(e => CommonKnowledgeUIHelpers.GetEntryCategory(e) == currentCategory);
            }
            
            // Search filter
            if (!string.IsNullOrEmpty(searchFilter))
            {
                string lower = searchFilter.ToLower();
                entries = entries.Where(e => 
                    e.tag.ToLower().Contains(lower) || 
                    e.content.ToLower().Contains(lower));
            }
            
            return entries.ToList();
        }

internal KnowledgeCategory GetEntryCategory(CommonKnowledgeEntry entry)
        {
            if (entry.tag.Contains("правила") || entry.tag.Contains("Instructions"))
                return KnowledgeCategory.Instructions;
            if (entry.tag.Contains("світобудова") || entry.tag.Contains("Lore"))
                return KnowledgeCategory.Lore;
            if (entry.tag.Contains("Стан колоніста") || entry.tag.Contains("PawnStatus"))
                return KnowledgeCategory.PawnStatus;
            if (entry.tag.Contains("історія") || entry.tag.Contains("History"))
                return KnowledgeCategory.History;
            
            return KnowledgeCategory.Other;
        }

internal string GetCategoryLabel(KnowledgeCategory category)
        {
            switch (category)
            {
                case KnowledgeCategory.All: return "RimTalk_Knowledge_CategoryAll".Translate();
                case KnowledgeCategory.Instructions: return "RimTalk_Knowledge_CategoryInstructions".Translate();
                case KnowledgeCategory.Lore: return "RimTalk_Knowledge_CategoryLore".Translate();
                case KnowledgeCategory.PawnStatus: return "RimTalk_Knowledge_CategoryPawnStatus".Translate();
                case KnowledgeCategory.History: return "RimTalk_Knowledge_CategoryHistory".Translate();
                case KnowledgeCategory.Other: return "RimTalk_Knowledge_CategoryOther".Translate();
                default: return "RimTalk_Knowledge_CategoryUnknown".Translate();
            }
        }

internal int GetCategoryCount(KnowledgeCategory category)
        {
            if (category == KnowledgeCategory.All)
                return library.Entries.Count;
            
            return library.Entries.Count(e => CommonKnowledgeUIHelpers.GetEntryCategory(e) == category);
        }

internal Color GetCategoryColor(CommonKnowledgeEntry entry)
        {
            var category = CommonKnowledgeUIHelpers.GetEntryCategory(entry);
            switch (category)
            {
                case KnowledgeCategory.Instructions: return new Color(0.3f, 0.8f, 0.3f);
                case KnowledgeCategory.Lore: return new Color(0.8f, 0.6f, 0.3f);
                case KnowledgeCategory.PawnStatus: return new Color(0.3f, 0.6f, 0.9f);
                case KnowledgeCategory.History: return new Color(0.7f, 0.5f, 0.7f);
                default: return Color.white;
            }
        }

internal void CreateNewEntry()
        {
            editTag = "";
            editContent = "";
            editImportance = 0.5f;
            editTargetPawnId = -1;
            editMatchMode = KeywordMatchMode.Any;
            editCategory = KnowledgeEntryCategory.None;
            lastSelectedEntry = null;
            selectedEntries.Clear();
            editMode = true;
        }

internal void StartEdit()
        {
            if (selectedEntries.Count == 1)
            {
                var entry = selectedEntries.First();
                editTag = entry.tag;
                editContent = entry.content;
                editImportance = entry.importance;
                editTargetPawnId = entry.targetPawnId;
                editMatchMode = entry.matchMode;
                editCategory = entry.category;
                lastSelectedEntry = entry;
                editMode = true;
            }
        }

internal void SaveEntry()
        {
            if (string.IsNullOrEmpty(editTag) || string.IsNullOrEmpty(editContent))
            {
                Messages.Message(CommonKnowledgeTranslationKeys.TagContentEmpty.Translate(), 
                    MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            if (lastSelectedEntry == null)
            {
                var newEntry = new CommonKnowledgeEntry(editTag, editContent)
                {
                    importance = editImportance,
                    targetPawnId = editTargetPawnId,
                    isUserEdited = true, 
                    matchMode = editMatchMode,
                    category = editCategory
                };
                library.AddEntry(newEntry);
                selectedEntries.Clear();
                selectedEntries.Add(newEntry);
                lastSelectedEntry = newEntry;
            }
            else
            {
                lastSelectedEntry.tag = editTag;
                lastSelectedEntry.content = editContent;
                lastSelectedEntry.importance = editImportance;
                lastSelectedEntry.targetPawnId = editTargetPawnId;
                lastSelectedEntry.matchMode = editMatchMode;
                lastSelectedEntry.category = editCategory;
                
                lastSelectedEntry.InvalidateCache();
            }
            
            editMode = false;
            Messages.Message(CommonKnowledgeTranslationKeys.EntrySaved.Translate(), 
                MessageTypeDefOf.PositiveEvent, false);
        }

internal void DeleteSelectedEntries()
        {
            if (selectedEntries.Count == 0) return;
            
            int count = selectedEntries.Count;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                CommonKnowledgeTranslationKeys.DeleteConfirm.Translate(count),
                delegate
                {
                    foreach (var entry in selectedEntries.ToList())
                    {
                        library.RemoveEntry(entry);
                    }
                    selectedEntries.Clear();
                    lastSelectedEntry = null;
                    Messages.Message(CommonKnowledgeTranslationKeys.Deleted.Translate(count), 
                        MessageTypeDefOf.PositiveEvent, false);
                }
            ));
        }

internal void ExportToFile()
        {
            string content;
            if (selectedEntries.Count > 0)
            {
                // Export selected
                var sb = new System.Text.StringBuilder();
                foreach (var entry in selectedEntries)
                {
                    sb.AppendLine(entry.FormatForExport());
                }
                content = sb.ToString();
            }
            else
            {
                // Export all
                content = library.ExportToText();
            }
            
            GUIUtility.systemCopyBuffer = content;
            int exportCount = selectedEntries.Count > 0 ? selectedEntries.Count : library.Entries.Count;
            Messages.Message(CommonKnowledgeTranslationKeys.ExportedToClipboard.Translate(exportCount), 
                MessageTypeDefOf.PositiveEvent, false);
        }

internal void ShowImportDialog()
        {
            Find.WindowStack.Add(new Dialog_TextInput(
                CommonKnowledgeTranslationKeys.ImportTitle.Translate(),
                CommonKnowledgeTranslationKeys.ImportDescription.Translate(),
                "",
                delegate(string text)
                {
                    int count = library.ImportFromText(text);
                    Messages.Message(CommonKnowledgeTranslationKeys.Imported.Translate(count), 
                        MessageTypeDefOf.PositiveEvent, false);
                },
                null,
                true
            ));
        }

internal void ClearAllEntries()
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                CommonKnowledgeTranslationKeys.ClearConfirm.Translate(),
                delegate
                {
                    library.Clear();
                    selectedEntries.Clear();
                    lastSelectedEntry = null;
                    Messages.Message(CommonKnowledgeTranslationKeys.AllCleared.Translate(), 
                        MessageTypeDefOf.PositiveEvent, false);
                }
            ));
        }

internal void GeneratePawnStatusKnowledge()
        {
            try
            {
                var colonists = Find.CurrentMap?.mapPawns?.FreeColonists;
                if (colonists == null || colonists.Count() == 0)
                {
                    Messages.Message(CommonKnowledgeTranslationKeys.NoColonists.Translate(), 
                        MessageTypeDefOf.RejectInput, false);
                    return;
                }
                
                int currentTick = Find.TickManager.TicksGame;
                int generated = 0;
                
                foreach (var pawn in colonists)
                {
                    try
                    {
                        PawnStatusKnowledgeGenerator.UpdatePawnStatusKnowledge(pawn, library, currentTick);
                        generated++;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[CommonKnowledge] Failed to generate for {pawn.Name}: {ex.Message}");
                    }
                }
                
                Messages.Message(CommonKnowledgeTranslationKeys.GeneratedPawnStatus.Translate(generated), 
                    MessageTypeDefOf.PositiveEvent, false);
            }
            catch (Exception ex)
            {
                Messages.Message(CommonKnowledgeTranslationKeys.GenerationFailed.Translate(ex.Message), 
                    MessageTypeDefOf.RejectInput, false);
            }
        }

internal void GenerateEventRecordKnowledge()
        {
            try
            {
                EventRecordKnowledgeGenerator.ScanRecentPlayLog();
                Messages.Message(CommonKnowledgeTranslationKeys.EventScanTriggered.Translate(), 
                    MessageTypeDefOf.PositiveEvent, false);
            }
            catch (Exception ex)
            {
                Messages.Message(CommonKnowledgeTranslationKeys.GenerationFailed.Translate(ex.Message), 
                    MessageTypeDefOf.RejectInput, false);
            }
        }

internal void ShowHelpDialog()
        {
            string title = CommonKnowledgeTranslationKeys.HelpTitle.Translate();
            string content = CommonKnowledgeTranslationKeys.HelpContent.Translate();
            
            Find.WindowStack.Add(new Dialog_MessageBox(
                content,
                null,
                null,
                null,
                null,
                title
            ));
        }

internal void ShowTagTestDialog()
        {
            Find.WindowStack.Add(new Dialog_TagTest());
        }
    }

    internal sealed class Dialog_CommonKnowledgeParts
    {
        internal readonly Dialog_CommonKnowledge Owner;
        internal readonly CommonKnowledgeLayout Layout;
        internal readonly CommonKnowledgeList List;
        internal readonly CommonKnowledgePanels Panels;
        internal readonly CommonKnowledgeActions Actions;
        internal Dialog_CommonKnowledgeParts(Dialog_CommonKnowledge owner)
        {
            Owner = owner;
            Layout = new CommonKnowledgeLayout(owner);
            List = new CommonKnowledgeList(owner);
            Panels = new CommonKnowledgePanels(owner);
            Actions = new CommonKnowledgeActions(owner);
        }
    }

    
    // ==================== Enums ====================
    
    public enum KnowledgeCategory
    {
        All,
        Instructions,
        Lore,
        PawnStatus,
        History,
        Other
    }
}
