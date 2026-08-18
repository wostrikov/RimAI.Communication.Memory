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
            
            // ⭐ 更新虚拟列表数据
            virtualList.SetItems(filteredEntries);
            virtualList.ScrollPosition = listScrollPosition;
            
            // ⭐ 修复：不在这里处理拖拽，改为手动处理并在ScrollView内绘制选择框
            
            // ⭐ 使用虚拟化渲染
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, filteredEntries.Count * ENTRY_HEIGHT);
            
            // 处理拖拽事件（在BeginScrollView之前）
            HandleDragSelectionEvents(innerRect, viewRect);
            
            Widgets.BeginScrollView(innerRect, ref Owner.listScrollPosition, viewRect, true);
            
            // 绘制条目
            float y = 0f;
            foreach (var entry in filteredEntries)
            {
                Rect entryRect = new Rect(0f, y, viewRect.width, ENTRY_HEIGHT);
                DrawEntryRow(entryRect, entry);
                y += ENTRY_HEIGHT;
            }
            
            // ⭐ 修复：在EndScrollView之前绘制选择框或拖拽排序指示线
            if (isDragging)
            {
                DrawSelectionBox();
            }
            else if (isDragReordering && dragReorderInsertIndex >= 0)
            {
                // ⭐ 绘制拖拽排序的插入指示线
                float insertY = dragReorderInsertIndex * ENTRY_HEIGHT;
                Rect lineRect = new Rect(0f, insertY - 2f, viewRect.width, 4f);
                Widgets.DrawBoxSolid(lineRect, new Color(0.3f, 0.8f, 1f, 0.8f));
                
                // 绘制小三角指示
                Rect arrowRect = new Rect(-2f, insertY - 6f, 12f, 12f);
                Widgets.DrawBoxSolid(arrowRect, new Color(0.3f, 0.8f, 1f, 0.9f));
            }
            
            Widgets.EndScrollView();
            
            // 同步滚动位置
            virtualList.ScrollPosition = listScrollPosition;
            
            // Show filter status
            if (!string.IsNullOrEmpty(searchFilter) || currentCategory != KnowledgeCategory.All)
            {
                Rect statusRect = new Rect(innerRect.x, innerRect.yMax - 25f, innerRect.width, 20f);
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                
                // 显示过滤状态
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
            
            // ⭐ 点击选择现在由 DoWindowContents 的 MouseUp 统一处理
            // 不再使用 ButtonInvisible（它会和拖拽框选冲突）
            
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
            // ⭐ 使用辅助方法获取分类颜色
            GUI.color = CommonKnowledgeUIHelpers.GetCategoryColor(entry);
            Widgets.Label(tagRect, $"[{entry.tag}]");
            GUI.color = Color.white;
            x += 155f;
            
            // Importance
            Rect importanceRect = new Rect(x, innerRect.y, 50f, 20f);
            Widgets.Label(importanceRect, entry.importance.ToString("F1"));
            x += 55f;
            
            // Content preview (multi-line) - 为右侧复选框留出空间
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
            
            // 1. 确定插入目标在主列表中的位置
            int targetMainIndex;
            if (dragReorderInsertIndex >= filteredEntries.Count)
            {
                // 插入到末尾：找到最后一个过滤条目在主列表中的位置之后
                var lastFiltered = filteredEntries[filteredEntries.Count - 1];
                targetMainIndex = library.Entries.IndexOf(lastFiltered) + 1;
            }
            else if (dragReorderInsertIndex <= 0)
            {
                // 插入到开头：找到第一个过滤条目在主列表中的位置
                var firstFiltered = filteredEntries[0];
                targetMainIndex = library.Entries.IndexOf(firstFiltered);
            }
            else
            {
                // 插入到中间：找到目标位置条目在主列表中的位置
                var targetEntry = filteredEntries[dragReorderInsertIndex];
                targetMainIndex = library.Entries.IndexOf(targetEntry);
            }
            
            // 2. 收集选中的条目（保持原有顺序）
            var entriesToMove = new List<CommonKnowledgeEntry>();
            foreach (var entry in library.Entries)
            {
                if (selectedEntries.Contains(entry))
                {
                    entriesToMove.Add(entry);
                }
            }
            
            if (entriesToMove.Count == 0) return;
            
            // 3. 从主列表中移除选中的条目
            foreach (var entry in entriesToMove)
            {
                library.Entries.Remove(entry);
            }
            
            // 4. 重新计算插入位置（因为移除了条目，索引可能变化）
            // 找到目标位置：如果目标条目还在列表中，插入到它前面
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
            
            // 5. 在目标位置插入
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

            // 左键按下：记录起始位置，判断是否在已选中条目上
            if (e.type == EventType.MouseDown && e.button == 0 && listRect.Contains(e.mousePosition))
            {
                isMouseDown = true;
                mouseDownScreenPos = e.mousePosition;
                dragStartPos = e.mousePosition - listRect.position;
                dragCurrentPos = dragStartPos;

                // ⭐ 判断按下位置是否在已选中的条目上
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

            // 鼠标移动中：判断是否超过拖拽阈值
            if (isMouseDown && e.type == EventType.MouseDrag && e.button == 0)
            {
                float distance = Vector2.Distance(mouseDownScreenPos, e.mousePosition);

                if (!isDragging && !isDragReordering && distance >= DRAG_THRESHOLD)
                {
                    if (mouseDownOnSelected && selectedEntries.Count > 0)
                    {
                        // ⭐ 在已选中条目上拖拽 → 拖拽排序模式
                        isDragReordering = true;
                    }
                    else
                    {
                        // ⭐ 在未选中区域拖拽 → 框选模式
                        isDragging = true;
                    }
                }

                if (isDragReordering)
                {
                    // ⭐ 拖拽排序：计算插入位置
                    Vector2 localMousePos = e.mousePosition - listRect.position;
                    float contentMouseY = localMousePos.y + listScrollPosition.y;
                    var filteredEntries = Owner.GetFilteredEntries();

                    // 计算最近的插入线位置（在条目之间）
                    dragReorderInsertIndex = Mathf.Clamp(
                        Mathf.RoundToInt(contentMouseY / ENTRY_HEIGHT),
                        0, filteredEntries.Count);

                    e.Use();
                }
                else if (isDragging)
                {
                    dragCurrentPos = e.mousePosition - listRect.position;

                    // 转换为content坐标
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

                    // 检查哪些条目在选择框内
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
