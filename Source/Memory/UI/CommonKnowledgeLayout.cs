using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.UI
{
    internal sealed class CommonKnowledgeLayout : Dialog_CommonKnowledgeCollaborator
    {
        internal CommonKnowledgeLayout(Dialog_CommonKnowledge owner) : base(owner) { }

public void DoWindowContents(Rect inRect)
        {
            // Top Toolbar
            Rect toolbarRect = new Rect(0f, 0f, inRect.width, TOOLBAR_HEIGHT);
            DrawToolbar(toolbarRect);
            
            float contentY = TOOLBAR_HEIGHT + SPACING;
            float contentHeight = inRect.height - contentY;
            
            // Left Sidebar
            Rect sidebarRect = new Rect(0f, contentY, SIDEBAR_WIDTH, contentHeight);
            DrawSidebar(sidebarRect);
            
            // Center List
            float centerX = SIDEBAR_WIDTH + SPACING;
            float centerWidth = showRightPanel ? 
                (inRect.width - SIDEBAR_WIDTH - RIGHT_PANEL_WIDTH - SPACING * 3) : 
                (inRect.width - SIDEBAR_WIDTH - SPACING * 2);
            Rect centerRect = new Rect(centerX, contentY, centerWidth, contentHeight);
            Owner.DrawCenterList(centerRect);
            
            // Right Panel (Detail/Edit)
            if (showRightPanel)
            {
                float rightX = centerX + centerWidth + SPACING;
                Rect rightRect = new Rect(rightX, contentY, RIGHT_PANEL_WIDTH, contentHeight);
                Owner.DrawRightPanel(rightRect);
            }
            
            // Handle input events (must be after all UI drawing)
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                if (isMouseDown && isDragReordering && dragReorderInsertIndex >= 0)
                {
                    // ⭐ 拖拽排序完成：将选中条目移动到目标位置
                    Owner.ExecuteDragReorder();
                }
                else if (isMouseDown && !isDragging)
                {
                    // ⭐ 没有进入拖拽模式 → 当作单击处理
                    // 找到鼠标下方的条目
                    float centerX2 = SIDEBAR_WIDTH + SPACING;
                    float centerWidth2 = showRightPanel ? 
                        (inRect.width - SIDEBAR_WIDTH - RIGHT_PANEL_WIDTH - SPACING * 3) : 
                        (inRect.width - SIDEBAR_WIDTH - SPACING * 2);
                    Rect centerRect2 = new Rect(centerX2, contentY, centerWidth2, contentHeight);
                    Rect innerRect2 = centerRect2.ContractedBy(5f);
                    
                    if (innerRect2.Contains(Event.current.mousePosition))
                    {
                        // 转换为 content 坐标
                        float mouseContentY = Event.current.mousePosition.y - innerRect2.y + listScrollPosition.y;
                        var filteredEntries2 = Owner.GetFilteredEntries();
                        int clickedIndex = (int)(mouseContentY / ENTRY_HEIGHT);
                        
                        if (clickedIndex >= 0 && clickedIndex < filteredEntries2.Count)
                        {
                            Owner.HandleEntryClick(filteredEntries2[clickedIndex]);
                        }
                        else
                        {
                            // 点击空白区域，清除选择
                            selectedEntries.Clear();
                            lastSelectedEntry = null;
                        }
                    }
                }
                
                isMouseDown = false;
                isDragging = false;
                isDragReordering = false;
                dragReorderInsertIndex = -1;
                mouseDownOnSelected = false;
                Event.current.Use();
            }
        }

internal void DrawToolbar(Rect rect)
        {
            GUI.Box(rect, "");
            
            Rect innerRect = rect.ContractedBy(5f);
            float x = innerRect.x;
            
            // Search bar (left side)
            float searchWidth = 300f;
            Rect searchRect = new Rect(x, innerRect.y + 5f, searchWidth, 30f);
            searchFilter = Widgets.TextField(searchRect, searchFilter);
            
            // Buttons (right side, stack from right-to-left)
            float buttonWidth = 100f;
            float spacing = 5f;
            
            // Calculate button positions from right to left
            float rightX = innerRect.xMax;
            
            // Clear All button
            rightX -= buttonWidth;
            if (Widgets.ButtonText(new Rect(rightX, innerRect.y + 5f, buttonWidth, BUTTON_HEIGHT), 
                CommonKnowledgeTranslationKeys.ClearAll.Translate()))
            {
                Owner.ClearAllEntries();
            }
            
            // Delete Selected button
            rightX -= buttonWidth + spacing;
            GUI.enabled = selectedEntries.Count > 0;
            if (Widgets.ButtonText(new Rect(rightX, innerRect.y + 5f, buttonWidth, BUTTON_HEIGHT), 
                CommonKnowledgeTranslationKeys.DeleteCount.Translate(selectedEntries.Count)))
            {
                Owner.DeleteSelectedEntries();
            }
            GUI.enabled = true;
            
            // Export button
            rightX -= buttonWidth + spacing;
            string exportLabel = selectedEntries.Count > 0 
                ? CommonKnowledgeTranslationKeys.ExportCount.Translate(selectedEntries.Count) 
                : CommonKnowledgeTranslationKeys.ExportAll.Translate();
            if (Widgets.ButtonText(new Rect(rightX, innerRect.y + 5f, buttonWidth, BUTTON_HEIGHT), exportLabel))
            {
                Owner.ExportToFile();
            }
            
            // Import button
            rightX -= buttonWidth + spacing;
            if (Widgets.ButtonText(new Rect(rightX, innerRect.y + 5f, buttonWidth, BUTTON_HEIGHT), 
                CommonKnowledgeTranslationKeys.Import.Translate()))
            {
                Owner.ShowImportDialog();
            }
            
            // New button
            rightX -= buttonWidth + spacing;
            if (Widgets.ButtonText(new Rect(rightX, innerRect.y + 5f, buttonWidth, BUTTON_HEIGHT), 
                CommonKnowledgeTranslationKeys.New.Translate()))
            {
                Owner.CreateNewEntry();
            }
            
            // Toggle Right Panel button
            rightX -= 40f + spacing;
            string toggleIcon = showRightPanel ? "◀" : "▶";
            if (Widgets.ButtonText(new Rect(rightX, innerRect.y + 5f, 40f, BUTTON_HEIGHT), toggleIcon))
            {
                showRightPanel = !showRightPanel;
            }
        }

internal void DrawSidebar(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            
            Rect innerRect = rect.ContractedBy(SPACING);
            float y = innerRect.y;
            
            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(innerRect.x, y, innerRect.width, 30f), 
                CommonKnowledgeTranslationKeys.Categories.Translate());
            Text.Font = GameFont.Small;
            y += 35f;
            
            // Category buttons
            float categoryHeight = 35f;
            foreach (KnowledgeCategory category in Enum.GetValues(typeof(KnowledgeCategory)))
            {
                bool isSelected = currentCategory == category;
                
                Rect categoryRect = new Rect(innerRect.x, y, innerRect.width, categoryHeight);
                int categoryCount = Owner.GetCategoryCount(category);
                
                // ⭐ 使用辅助方法绘制分类按钮
                if (CommonKnowledgeUIHelpers.DrawCategoryButton(categoryRect, category, isSelected, categoryCount))
                {
                    currentCategory = category;
                    selectedEntries.Clear();
                }
                
                y += categoryHeight + 5f;
            }
            
            y += 20f;
            
            // Auto-Generate Settings (collapsible)
            Rect autoGenHeaderRect = new Rect(innerRect.x, y, innerRect.width, 30f);
            string autoGenIcon = showAutoGenerateSettings ? "▼" : "▶";
            if (Widgets.ButtonText(autoGenHeaderRect, 
                $"{autoGenIcon} {CommonKnowledgeTranslationKeys.AutoGenerate.Translate()}"))
            {
                showAutoGenerateSettings = !showAutoGenerateSettings;
            }
            y += 35f;
            
            if (showAutoGenerateSettings)
            {
                Rect autoGenContentRect = new Rect(innerRect.x, y, innerRect.width, 120f);
                // ⭐ 使用辅助方法绘制自动生成设置
                CommonKnowledgeUIHelpers.DrawAutoGenerateSettings(
                    autoGenContentRect, 
                    Owner.GeneratePawnStatusKnowledge, 
                    Owner.GenerateEventRecordKnowledge
                );
                y += 125f;
            }
            
            // ⭐ 标签测试工具（按钮，点击弹窗）
            Rect tagTestButtonRect = new Rect(innerRect.x, y, innerRect.width, BUTTON_HEIGHT);
            GUI.color = new Color(0.4f, 0.8f, 0.4f); // 浅绿色
            if (Widgets.ButtonText(tagTestButtonRect, "🔍 " + CommonKnowledgeTranslationKeys.TagTest.Translate()))
            {
                Owner.ShowTagTestDialog();
            }
            GUI.color = Color.white;
            y += BUTTON_HEIGHT + 10f;
            
            // ⭐ 使用说明按钮（在统计信息上方）
            float helpButtonY = innerRect.yMax - 100f; // 为统计信息留出60px + 间距
            Rect helpButtonRect = new Rect(innerRect.x, helpButtonY, innerRect.width, BUTTON_HEIGHT);
            
            GUI.color = new Color(0.4f, 0.7f, 1f); // 浅蓝色
            if (Widgets.ButtonText(helpButtonRect, "📘 " + CommonKnowledgeTranslationKeys.Help.Translate()))
            {
                Owner.ShowHelpDialog();
            }
            GUI.color = Color.white;
            
            // Statistics at bottom
            float statsY = innerRect.yMax - 60f;
            Widgets.DrawLineHorizontal(innerRect.x, statsY - 10f, innerRect.width);
            
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            int totalCount = library.Entries.Count;
            int enabledCount = library.Entries.Count(e => e.isEnabled);
            int selectedCount = selectedEntries.Count;
            
            Widgets.Label(new Rect(innerRect.x, statsY, innerRect.width, 20f), 
                CommonKnowledgeTranslationKeys.Total.Translate(totalCount));
            Widgets.Label(new Rect(innerRect.x, statsY + 20f, innerRect.width, 20f), 
                CommonKnowledgeTranslationKeys.Enabled.Translate(enabledCount));
            Widgets.Label(new Rect(innerRect.x, statsY + 40f, innerRect.width, 20f), 
                CommonKnowledgeTranslationKeys.Selected.Translate(selectedCount));
            
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

internal void DrawAutoGenerateSettings(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.3f));
            Rect innerRect = rect.ContractedBy(5f);
            float y = innerRect.y;
            
            var settings = RimTalkMemoryPatchMod.Settings;
            
            // Pawn Status
            bool enablePawnStatus = settings.enablePawnStatusKnowledge;
            Widgets.CheckboxLabeled(new Rect(innerRect.x, y, innerRect.width, 25f), "RimTalk_Knowledge_PawnStatus".Translate(), ref enablePawnStatus);
            settings.enablePawnStatusKnowledge = enablePawnStatus;
            y += 30f;
            
            if (Widgets.ButtonText(new Rect(innerRect.x, y, innerRect.width, 25f), "RimTalk_Knowledge_GenerateNow".Translate()))
            {
                Owner.GeneratePawnStatusKnowledge();
            }
            y += 30f;
            
            // Event Record
            bool enableEventRecord = settings.enableEventRecordKnowledge;
            Widgets.CheckboxLabeled(new Rect(innerRect.x, y, innerRect.width, 25f), "RimTalk_Knowledge_EventRecord".Translate(), ref enableEventRecord);
            settings.enableEventRecordKnowledge = enableEventRecord;
            y += 30f;
            
            if (Widgets.ButtonText(new Rect(innerRect.x, y, innerRect.width, 25f), "RimTalk_Knowledge_GenerateNow".Translate()))
            {
                Owner.GenerateEventRecordKnowledge();
            }
        }
    }
}
