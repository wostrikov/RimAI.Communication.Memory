﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.UI
{
    internal sealed class CommonKnowledgeList : Dialog_CommonKnowledgeCollaborator
    {
        internal CommonKnowledgeList(Dialog_CommonKnowledge owner) : base(owner) { }

internal void DrawCenterList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            
            Rect innerRect = rect.ContractedBy(5f);
            
            // Filter entries
            var filteredEntries = Owner.GetFilteredEntries();
            
            virtualList.SetItems(filteredEntries);
            virtualList.ScrollPosition = listScrollPosition;
            
            
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, filteredEntries.Count * ENTRY_HEIGHT);
            
            HandleDragSelectionEvents(innerRect, viewRect);
            
            Widgets.BeginScrollView(innerRect, ref Owner.listScrollPosition, viewRect, true);
            
            float y = 0f;
            foreach (var entry in filteredEntries)
            {
                Rect entryRect = new Rect(0f, y, viewRect.width, ENTRY_HEIGHT);
                DrawEntryRow(entryRect, entry);
                y += ENTRY_HEIGHT;
            }
            
            if (isDragging)
            {
                DrawSelectionBox();
            }
            else if (isDragReordering && dragReorderInsertIndex >= 0)
            {
                float insertY = dragReorderInsertIndex * ENTRY_HEIGHT;
                Rect lineRect = new Rect(0f, insertY - 2f, viewRect.width, 4f);
                Widgets.DrawBoxSolid(lineRect, new Color(0.3f, 0.8f, 1f, 0.8f));
                
                Rect arrowRect = new Rect(-2f, insertY - 6f, 12f, 12f);
                Widgets.DrawBoxSolid(arrowRect, new Color(0.3f, 0.8f, 1f, 0.9f));
            }
            
            Widgets.EndScrollView();
            
            virtualList.ScrollPosition = listScrollPosition;
            
            // Show filter status
            if (!string.IsNullOrEmpty(searchFilter) || currentCategory != KnowledgeCategory.All)
            {
                Rect statusRect = new Rect(innerRect.x, innerRect.yMax - 25f, innerRect.width, 20f);
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                
                Widgets.Label(statusRect, 
                    CommonKnowledgeTranslationKeys.Showing.Translate(filteredEntries.Count, library.Entries.Count));
                
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
        }

internal void DrawEntryRow(Rect rect, CommonKnowledgeEntry entry)
        {
            bool isSelected = selectedEntries.Contains(entry);
            
            // Background
            if (isSelected)
            {
                Widgets.DrawHighlight(rect);
            }
            else if (Mouse.IsOver(rect))
            {
                Widgets.DrawLightHighlight(rect);
            }
            
            
            Rect innerRect = rect.ContractedBy(5f);
            float x = innerRect.x;
            
            // Checkbox
            Rect checkboxRect = new Rect(x, innerRect.y + 5f, 24f, 24f);
            bool wasEnabled = entry.isEnabled;
            Widgets.Checkbox(checkboxRect.position, ref entry.isEnabled);
            x += 30f;
            
            // Tag
            Rect tagRect = new Rect(x, innerRect.y, 150f, 20f);
            Text.Font = GameFont.Small;
            GUI.color = CommonKnowledgeUIHelpers.GetCategoryColor(entry);
            Widgets.Label(tagRect, $"[{entry.tag}]");
            GUI.color = Color.white;
            x += 155f;
            
            // Importance
            Rect importanceRect = new Rect(x, innerRect.y, 50f, 20f);
            Widgets.Label(importanceRect, entry.importance.ToString("F1"));
            x += 55f;
            
            Rect contentRect = new Rect(innerRect.x + 30f, innerRect.y + 22f, innerRect.width - 95f, 40f);
            Text.Font = GameFont.Tiny;
            string preview = entry.content.Length > 120 ? entry.content.Substring(0, 120) + "..." : entry.content;
            Widgets.Label(contentRect, preview);
            Text.Font = GameFont.Small;
            
            // Extended properties checkboxes (right side)
            const float BUTTON_SIZE = 20f;
            const float BUTTON_SPACING = 5f;
            float rightX = rect.xMax - BUTTON_SIZE * 2 - BUTTON_SPACING - 10f;
            float centerY = rect.y + (rect.height - BUTTON_SIZE) / 2f;

            // Checkbox 1: Can Be Extracted
            bool canBeExtracted = ExtendedKnowledgeEntry.CanBeExtracted(entry);
            Rect extractCheckboxRect = new Rect(rightX, centerY, BUTTON_SIZE, BUTTON_SIZE);
            
            if (Mouse.IsOver(extractCheckboxRect))
            {
                string tooltip = canBeExtracted 
                    ? "RimTalk_CanBeExtractedEnabled".Translate() 
                    : "RimTalk_CanBeExtractedDisabled".Translate();
                TooltipHandler.TipRegion(extractCheckboxRect, tooltip);
            }
            
            bool newExtractValue = canBeExtracted;
            Widgets.Checkbox(extractCheckboxRect.position, ref newExtractValue, BUTTON_SIZE);
            if (newExtractValue != canBeExtracted)
            {
                ExtendedKnowledgeEntry.SetCanBeExtracted(entry, newExtractValue);
            }

            // Checkbox 2: Can Be Matched
            rightX += BUTTON_SIZE + BUTTON_SPACING;
            bool canBeMatched = ExtendedKnowledgeEntry.CanBeMatched(entry);
            Rect matchCheckboxRect = new Rect(rightX, centerY, BUTTON_SIZE, BUTTON_SIZE);
            
            if (Mouse.IsOver(matchCheckboxRect))
            {
                string tooltip = canBeMatched 
                    ? "RimTalk_CanBeMatchedEnabled".Translate() 
                    : "RimTalk_CanBeMatchedDisabled".Translate();
                TooltipHandler.TipRegion(matchCheckboxRect, tooltip);
            }
            
            bool newMatchValue = canBeMatched;
            Widgets.Checkbox(matchCheckboxRect.position, ref newMatchValue, BUTTON_SIZE);
            if (newMatchValue != canBeMatched)
            {
                ExtendedKnowledgeEntry.SetCanBeMatched(entry, newMatchValue);
            }
            
            // Selection indicator
            if (isSelected)
            {
                Rect indicatorRect = new Rect(rect.x, rect.y, 3f, rect.height);
                Widgets.DrawBoxSolid(indicatorRect, new Color(0.3f, 0.6f, 1f));
            }
        }

internal void HandleEntryClick(CommonKnowledgeEntry entry)
        {
            bool ctrl = Event.current.control;
            bool shift = Event.current.shift;
            
            if (ctrl)
            {
                // Ctrl+Click: Toggle selection
                if (selectedEntries.Contains(entry))
                    selectedEntries.Remove(entry);
                else
                    selectedEntries.Add(entry);
                    
                lastSelectedEntry = entry;
            }
            else if (shift && lastSelectedEntry != null)
            {
                // Shift+Click: Range selection
                var filteredEntries = Owner.GetFilteredEntries();
                int startIndex = filteredEntries.IndexOf(lastSelectedEntry);
                int endIndex = filteredEntries.IndexOf(entry);
                
                if (startIndex >= 0 && endIndex >= 0)
                {
                    int min = Math.Min(startIndex, endIndex);
                    int max = Math.Max(startIndex, endIndex);
                    
                    for (int i = min; i <= max; i++)
                    {
                        selectedEntries.Add(filteredEntries[i]);
                    }
                }
                
                lastSelectedEntry = entry;
            }
            else
            {
                // Normal click: Single selection
                selectedEntries.Clear();
                selectedEntries.Add(entry);
                lastSelectedEntry = entry;
                editMode = false;
            }
        }

internal void ExecuteDragReorder()
        {
            var filteredEntries = Owner.GetFilteredEntries();
            if (filteredEntries.Count == 0 || selectedEntries.Count == 0) return;
            
            int targetMainIndex;
            if (dragReorderInsertIndex >= filteredEntries.Count)
            {
                var lastFiltered = filteredEntries[filteredEntries.Count - 1];
                targetMainIndex = library.Entries.IndexOf(lastFiltered) + 1;
            }
            else if (dragReorderInsertIndex <= 0)
            {
                var firstFiltered = filteredEntries[0];
                targetMainIndex = library.Entries.IndexOf(firstFiltered);
            }
            else
            {
                var targetEntry = filteredEntries[dragReorderInsertIndex];
                targetMainIndex = library.Entries.IndexOf(targetEntry);
            }
            
            var entriesToMove = new List<CommonKnowledgeEntry>();
            foreach (var entry in library.Entries)
            {
                if (selectedEntries.Contains(entry))
                {
                    entriesToMove.Add(entry);
                }
            }
            
            if (entriesToMove.Count == 0) return;
            
            foreach (var entry in entriesToMove)
            {
                library.Entries.Remove(entry);
            }
            
            if (dragReorderInsertIndex < filteredEntries.Count)
            {
                var targetEntry = filteredEntries[dragReorderInsertIndex];
                int newIdx = library.Entries.IndexOf(targetEntry);
                if (newIdx >= 0)
                {
                    targetMainIndex = newIdx;
                }
                else
                {
                    targetMainIndex = library.Entries.Count;
                }
            }
            else
            {
                targetMainIndex = library.Entries.Count;
            }
            
            targetMainIndex = Mathf.Clamp(targetMainIndex, 0, library.Entries.Count);
            library.Entries.InsertRange(targetMainIndex, entriesToMove);
        }

internal void DrawSelectionBox()
        {
            Rect selectionBox = GetSelectionBox();
            Widgets.DrawBox(selectionBox);
            Widgets.DrawBoxSolid(selectionBox, new Color(0.3f, 0.6f, 1f, 0.2f));
        }

internal Rect GetSelectionBox()
        {
            float minX = Mathf.Min(dragStartPos.x, dragCurrentPos.x);
            float minY = Mathf.Min(dragStartPos.y, dragCurrentPos.y);
            float maxX = Mathf.Max(dragStartPos.x, dragCurrentPos.x);
            float maxY = Mathf.Max(dragStartPos.y, dragCurrentPos.y);
            
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

internal void HandleDragSelectionEvents(Rect listRect, Rect viewRect)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && listRect.Contains(e.mousePosition))
            {
                isMouseDown = true;
                mouseDownScreenPos = e.mousePosition;
                dragStartPos = e.mousePosition - listRect.position;
                dragCurrentPos = dragStartPos;

                Vector2 localPos = e.mousePosition - listRect.position;
                float mouseContentY = localPos.y + listScrollPosition.y;
                var filteredEntries = Owner.GetFilteredEntries();
                int clickedIdx = (int)(mouseContentY / ENTRY_HEIGHT);

                mouseDownOnSelected = false;
                if (clickedIdx >= 0 && clickedIdx < filteredEntries.Count)
                {
                    mouseDownOnSelected = selectedEntries.Contains(filteredEntries[clickedIdx]);
                }
            }

            if (isMouseDown && e.type == EventType.MouseDrag && e.button == 0)
            {
                float distance = Vector2.Distance(mouseDownScreenPos, e.mousePosition);

                if (!isDragging && !isDragReordering && distance >= DRAG_THRESHOLD)
                {
                    if (mouseDownOnSelected && selectedEntries.Count > 0)
                    {
                        isDragReordering = true;
                    }
                    else
                    {
                        isDragging = true;
                    }
                }

                if (isDragReordering)
                {
                    Vector2 localMousePos = e.mousePosition - listRect.position;
                    float contentMouseY = localMousePos.y + listScrollPosition.y;
                    var filteredEntries = Owner.GetFilteredEntries();

                    dragReorderInsertIndex = Mathf.Clamp(
                        Mathf.RoundToInt(contentMouseY / ENTRY_HEIGHT),
                        0, filteredEntries.Count);

                    e.Use();
                }
                else if (isDragging)
                {
                    dragCurrentPos = e.mousePosition - listRect.position;

                    Rect selectionBoxViewport = GetSelectionBox();
                    Rect selectionBoxContent = new Rect(
                        selectionBoxViewport.x,
                        selectionBoxViewport.y + listScrollPosition.y,
                        selectionBoxViewport.width,
                        selectionBoxViewport.height
                    );

                    var filteredEntries = Owner.GetFilteredEntries();

                    bool ctrl = Event.current.control;
                    if (!ctrl)
                    {
                        selectedEntries.Clear();
                    }

                    float y = 0f;
                    foreach (var entry in filteredEntries)
                    {
                        Rect entryRect = new Rect(0f, y, viewRect.width, ENTRY_HEIGHT);

                        if (selectionBoxContent.Overlaps(entryRect))
                        {
                            selectedEntries.Add(entry);
                        }

                        y += ENTRY_HEIGHT;
                    }

                    e.Use();
                }
            }
        }
    }
}
