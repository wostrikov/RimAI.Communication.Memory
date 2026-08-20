using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Ustas.RimAI.Communication.Memory;
using System;

namespace Ustas.RimAI.Communication.Memory.UI
{
    internal sealed class MemoryTabTimeline : MemoryTabCollaborator
    {
        internal MemoryTabTimeline(MainTabWindow_Memory owner) : base(owner) { }

        // ==================== Timeline ====================
        
        internal void DrawTimeline(Rect rect)
        {
            if (Owner.currentMemoryComp == null)
                return;
            
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(5f);
            
            var memories = Owner.cachedMemories;
            float totalHeight = Owner.cachedTotalHeight;
            
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, totalHeight);
            
            // Handle drag selection
            HandleDragSelection(innerRect, viewRect);
            
            // Draw timeline
            Widgets.BeginScrollView(innerRect, ref Owner.timelineScrollPosition, viewRect, true);
            
            float minVisibleY = Owner.timelineScrollPosition.y - 200f;
            float maxVisibleY = Owner.timelineScrollPosition.y + innerRect.height + 200f;
            
            int startIndex = 0;
            if (Owner.cachedCardYPositions.Count > 0)
            {
                int binaryResult = Owner.cachedCardYPositions.BinarySearch(minVisibleY);
                if (binaryResult >= 0)
                {
                    startIndex = binaryResult;
                }
                else
                {
                    startIndex = Mathf.Max(0, (~binaryResult) - 1);
                }
            }
            
            for (int i = startIndex; i < memories.Count; i++)
            {
                float y = Owner.cachedCardYPositions[i];
                
                if (y > maxVisibleY)
                {
                    break;
                }
                
                var memory = memories[i];
                float height = Owner.cachedCardHeights[i];
                
                Rect cardRect = new Rect(0f, y, viewRect.width, height);
                DrawMemoryCard(cardRect, memory);
            }
            
            if (Owner.isDragging)
            {
                DrawSelectionBox();
            }
            
            Widgets.EndScrollView();
            
            // Show filter status
            if (Owner.filterType != null || !Owner.showABM || !Owner.showSCM || !Owner.showELS || !Owner.showCLPA)
            {
                Rect statusRect = new Rect(innerRect.x, innerRect.yMax - 25f, innerRect.width, 20f);
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(statusRect, "RimTalk_MindStream_ShowingN".Translate(memories.Count));
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
        }
        
        internal void CheckAndRefreshCache()
        {
            if (Owner.currentMemoryComp == null) return;
            
            int currentCount = Owner.currentMemoryComp.ActiveMemories.Count + 
                             Owner.currentMemoryComp.SituationalMemories.Count + 
                             Owner.currentMemoryComp.EventLogMemories.Count + 
                             Owner.currentMemoryComp.ArchiveMemories.Count;
            
            int currentTick = Find.TickManager.TicksGame;
            
            bool needRefresh = false;
            
            if (Owner.selectedPawn != Owner.lastSelectedPawn) needRefresh = true;
            else if (currentCount != Owner.lastMemoryCount) needRefresh = true;
            else if (Owner.showABM != Owner.lastShowABM) needRefresh = true;
            else if (Owner.showSCM != Owner.lastShowSCM) needRefresh = true;
            else if (Owner.showELS != Owner.lastShowELS) needRefresh = true;
            else if (Owner.showCLPA != Owner.lastShowCLPA) needRefresh = true;
            else if (Owner.filterType != Owner.lastFilterType) needRefresh = true;
            else if (currentTick - Owner.lastRefreshTick > 60) needRefresh = true;
            
            if (needRefresh)
            {
                RefreshCache(currentCount, currentTick);
            }
        }
        
        internal void RefreshCache(int currentCount, int currentTick)
        {
            Owner.lastSelectedPawn = Owner.selectedPawn;
            Owner.lastMemoryCount = currentCount;
            Owner.lastShowABM = Owner.showABM;
            Owner.lastShowSCM = Owner.showSCM;
            Owner.lastShowELS = Owner.showELS;
            Owner.lastShowCLPA = Owner.showCLPA;
            Owner.lastFilterType = Owner.filterType;
            Owner.lastRefreshTick = currentTick;
            
            Owner.cachedMemories = Parts.Utilities.GetFilteredMemories();
            
            Owner.cachedCardHeights.Clear();
            Owner.cachedCardYPositions.Clear();
            Owner.cachedTotalHeight = 0f;
            
            foreach (var memory in Owner.cachedMemories)
            {
                Owner.cachedCardYPositions.Add(Owner.cachedTotalHeight);
                float height = Parts.Utilities.GetCardHeight(memory.Layer);
                Owner.cachedCardHeights.Add(height);
                Owner.cachedTotalHeight += height + MainTabWindow_Memory.CARD_SPACING;
            }
        }
        
        internal void DrawMemoryCard(Rect rect, MemoryEntry memory)
        {
            bool isSelected = Owner.selectedMemories.Contains(memory);
            Color borderColor = Parts.Utilities.GetLayerColor(memory.Layer);
            
            // Background
            if (memory.IsPinned)
            {
                Widgets.DrawBoxSolid(rect, new Color(0.25f, 0.2f, 0.1f, 0.5f));
            }
            else
            {
                Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f, 0.9f));
            }
            
            // Border
            if (isSelected)
            {
                Widgets.DrawBox(rect, 2);
                Rect borderRect = rect.ContractedBy(1f);
                GUI.color = new Color(1f, 0.8f, 0.3f);
                Widgets.DrawBox(borderRect, 2);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = borderColor;
                Widgets.DrawBox(rect, 1);
                GUI.color = Color.white;
            }
            
            // Hover highlight
            if (Mouse.IsOver(rect) && !Owner.isDragging)
            {
                Widgets.DrawLightHighlight(rect);
            }
            
            Rect innerRect = rect.ContractedBy(8f);
            
            float buttonSize = 24f;
            float buttonSpacing = 4f;
            
            // Top-right action buttons
            float buttonX = innerRect.xMax - buttonSize;
            float buttonY = innerRect.y;
            
            // Pin button
            Rect pinButtonRect = new Rect(buttonX, buttonY, buttonSize, buttonSize);
            if (Mouse.IsOver(pinButtonRect))
            {
                Widgets.DrawHighlight(pinButtonRect);
            }
            if (Widgets.ButtonImage(pinButtonRect, memory.IsPinned ? TexButton.ReorderUp : TexButton.ReorderDown))
            {
                memory.IsPinned = !memory.IsPinned;
                if (Owner.currentMemoryComp != null)
                {
                    Owner.currentMemoryComp.PinMemory(memory.Id, memory.IsPinned);
                }
                Event.current.Use();
            }
            TooltipHandler.TipRegion(pinButtonRect, memory.IsPinned ? "RimTalk_MindStream_Unpin".Translate() : "RimTalk_MindStream_Pin".Translate());
            buttonX -= buttonSize + buttonSpacing;
            
            // Edit button
            Rect editButtonRect = new Rect(buttonX, buttonY, buttonSize, buttonSize);
            if (Mouse.IsOver(editButtonRect))
            {
                Widgets.DrawHighlight(editButtonRect);
            }
            if (Widgets.ButtonImage(editButtonRect, TexButton.Rename))
            {
                if (Owner.currentMemoryComp != null)
                {
                    Find.WindowStack.Add(new Dialog_EditMemory(memory, Owner.currentMemoryComp));
                    Owner.filtersDirty = true;
                }
                Event.current.Use();
            }
            TooltipHandler.TipRegion(editButtonRect, "RimTalk_MindStream_Edit".Translate());
            
            
            // Content area (avoid button overlap)
            Rect contentRect = new Rect(innerRect.x, innerRect.y, innerRect.width - (buttonSize * 2 + buttonSpacing + 8f), innerRect.height);
            
            // Header
            Text.Font = GameFont.Tiny;
            string layerLabel = Parts.Utilities.GetLayerLabel(memory.Layer);
            string typeLabel = memory.Type.ToString();
            string timeLabel = memory.AgeString;
            
            string header = $"[{layerLabel}] {typeLabel} ? {timeLabel}";
            if (!string.IsNullOrEmpty(memory.relatedPawnName))
            {
                header += $" ? {"RimTalk_MindStream_With".Translate()} {memory.relatedPawnName}";
            }
            
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Widgets.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 18f), header);
            GUI.color = Color.white;
            
            // Content
            Text.Font = GameFont.Small;
            float contentY = contentRect.y + 20f;
            float contentHeight = contentRect.height - 40f;
            Rect textRect = new Rect(contentRect.x, contentY, contentRect.width, contentHeight);
            
            string displayText = memory.DisplayContent;
            int maxLength = Parts.Utilities.GetContentMaxLength(memory.Layer);
            if (displayText.Length > maxLength)
            {
                displayText = displayText.Substring(0, maxLength) + "...";
            }
            
            Widgets.Label(textRect, displayText);
            
            // Tooltip for full content
            if (memory.Content.Length > maxLength && Mouse.IsOver(textRect))
            {
                TooltipHandler.TipRegion(textRect, memory.DisplayContent);
            }
            
            // Footer (importance/activity bars)
            float barY = contentRect.yMax - 12f;
            float barWidth = (contentRect.width - 4f) / 2f;
            
            Rect importanceBarRect = new Rect(contentRect.x, barY, barWidth, 8f);
            Widgets.FillableBar(importanceBarRect, Mathf.Clamp01(memory.Importance), Texture2D.whiteTexture, BaseContent.ClearTex, false);
            TooltipHandler.TipRegion(importanceBarRect, "RimTalk_MindStream_ImportanceLabel".Translate(memory.Importance.ToString("F2")));
            
            Rect activityBarRect = new Rect(contentRect.x + barWidth + 4f, barY, barWidth, 8f);
            Widgets.FillableBar(activityBarRect, Mathf.Clamp01(memory.Activity), Texture2D.whiteTexture, BaseContent.ClearTex, false);
            TooltipHandler.TipRegion(activityBarRect, "RimTalk_MindStream_ActivityLabel".Translate(memory.Activity.ToString("F2")));
            
            Text.Font = GameFont.Small;
        }
        
        internal void HandleMemoryClick(MemoryEntry memory)
        {
            bool ctrl = Event.current.control;
            bool shift = Event.current.shift;
            
            if (ctrl)
            {
                // Toggle selection
                if (Owner.selectedMemories.Contains(memory))
                    Owner.selectedMemories.Remove(memory);
                else
                    Owner.selectedMemories.Add(memory);
                    
                Owner.lastSelectedMemory = memory;
            }
            else if (shift && Owner.lastSelectedMemory != null)
            {
                // Range selection
                var filteredMemories = Parts.Utilities.GetFilteredMemories();
                int startIndex = filteredMemories.IndexOf(Owner.lastSelectedMemory);
                int endIndex = filteredMemories.IndexOf(memory);
                
                if (startIndex >= 0 && endIndex >= 0)
                {
                    int min = Math.Min(startIndex, endIndex);
                    int max = Math.Max(startIndex, endIndex);
                    
                    for (int i = min; i <= max; i++)
                    {
                        Owner.selectedMemories.Add(filteredMemories[i]);
                    }
                }
                
                Owner.lastSelectedMemory = memory;
            }
            else
            {
                // Single selection
                Owner.selectedMemories.Clear();
                Owner.selectedMemories.Add(memory);
                Owner.lastSelectedMemory = memory;
            }
        }
        
        internal void HandleDragSelection(Rect listRect, Rect viewRect)
        {
            Event e = Event.current;
            
            if (e.type == EventType.MouseDown && e.button == 0 && listRect.Contains(e.mousePosition))
            {
                Owner.isMouseDown = true;
                Owner.mouseDownScreenPos = e.mousePosition;
                Owner.dragStartPos = e.mousePosition - listRect.position;
                Owner.dragCurrentPos = Owner.dragStartPos;
            }
            
            if (Owner.isMouseDown && e.type == EventType.MouseDrag && e.button == 0)
            {
                float distance = Vector2.Distance(Owner.mouseDownScreenPos, e.mousePosition);
                
                if (!Owner.isDragging && distance >= MainTabWindow_Memory.DRAG_THRESHOLD)
                {
                    Owner.isDragging = true;
                }
                
                if (Owner.isDragging)
                {
                    Owner.dragCurrentPos = e.mousePosition - listRect.position;
                    
                    Rect selectionBoxViewport = GetSelectionBox();
                    Rect selectionBoxContent = new Rect(
                        selectionBoxViewport.x, 
                        selectionBoxViewport.y + Owner.timelineScrollPosition.y,
                        selectionBoxViewport.width,
                        selectionBoxViewport.height
                    );
                    
                    var filteredMemories = Parts.Utilities.GetFilteredMemories();
                    
                    bool ctrl = Event.current.control;
                    if (!ctrl)
                    {
                        Owner.selectedMemories.Clear();
                    }
                    
                    float y = 0f;
                    foreach (var memory in filteredMemories)
                    {
                        float height = Parts.Utilities.GetCardHeight(memory.Layer);
                        Rect cardRect = new Rect(0f, y, viewRect.width, height);
                        
                        if (selectionBoxContent.Overlaps(cardRect))
                        {
                            Owner.selectedMemories.Add(memory);
                        }
                        
                        y += height + MainTabWindow_Memory.CARD_SPACING;
                    }
                    
                    e.Use();
                }
            }
        }
        
        internal void DrawSelectionBox()
        {
            Rect selectionBox = GetSelectionBox();
            Widgets.DrawBox(selectionBox);
            Widgets.DrawBoxSolid(selectionBox, new Color(1f, 0.8f, 0.3f, 0.2f));
        }
        
        internal Rect GetSelectionBox()
        {
            float minX = Mathf.Min(Owner.dragStartPos.x, Owner.dragCurrentPos.x);
            float minY = Mathf.Min(Owner.dragStartPos.y, Owner.dragCurrentPos.y);
            float maxX = Mathf.Max(Owner.dragStartPos.x, Owner.dragCurrentPos.x);
            float maxY = Mathf.Max(Owner.dragStartPos.y, Owner.dragCurrentPos.y);
            
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
