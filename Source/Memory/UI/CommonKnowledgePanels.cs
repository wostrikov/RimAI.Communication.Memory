﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.UI
{
    internal sealed class CommonKnowledgePanels : Dialog_CommonKnowledgeCollaborator
    {
        internal CommonKnowledgePanels(Dialog_CommonKnowledge owner) : base(owner) { }

internal void DrawRightPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            
            Rect innerRect = rect.ContractedBy(SPACING);
            
            if (editMode)
            {
                DrawEditPanel(innerRect);
            }
            else if (selectedEntries.Count == 0)
            {
                DrawEmptyPanel(innerRect);
            }
            else if (selectedEntries.Count == 1)
            {
                DrawDetailPanel(innerRect, selectedEntries.First());
            }
            else
            {
                DrawMultiSelectionPanel(innerRect);
            }
        }

internal void DrawEmptyPanel(Rect rect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            Widgets.Label(rect, CommonKnowledgeTranslationKeys.SelectOrCreate.Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

internal void DrawMultiSelectionPanel(Rect rect)
        {
            float y = rect.y;
            
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, y, rect.width, 30f), 
                CommonKnowledgeTranslationKeys.ItemsSelected.Translate(selectedEntries.Count));
            Text.Font = GameFont.Small;
            y += 40f;
            
            // Statistics
            int enabledCount = selectedEntries.Count(e => e.isEnabled);
            int disabledCount = selectedEntries.Count - enabledCount;
            float avgImportance = selectedEntries.Average(e => e.importance);
            
            Widgets.Label(new Rect(rect.x, y, rect.width, 25f), 
                CommonKnowledgeTranslationKeys.EnabledCount.Translate(enabledCount));
            y += 25f;
            Widgets.Label(new Rect(rect.x, y, rect.width, 25f), 
                CommonKnowledgeTranslationKeys.DisabledCount.Translate(disabledCount));
            y += 25f;
            Widgets.Label(new Rect(rect.x, y, rect.width, 25f), 
                CommonKnowledgeTranslationKeys.AvgImportance.Translate(avgImportance.ToString("F2")));
            y += 40f;
            
            // Batch actions
            if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, BUTTON_HEIGHT), 
                CommonKnowledgeTranslationKeys.EnableAll.Translate()))
            {
                foreach (var entry in selectedEntries)
                    entry.isEnabled = true;
            }
            y += BUTTON_HEIGHT + 5f;
            
            if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, BUTTON_HEIGHT), 
                CommonKnowledgeTranslationKeys.DisableAll.Translate()))
            {
                foreach (var entry in selectedEntries)
                    entry.isEnabled = false;
            }
            y += BUTTON_HEIGHT + 5f;
            
            if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, BUTTON_HEIGHT), 
                CommonKnowledgeTranslationKeys.ExportItems.Translate(selectedEntries.Count)))
            {
                Owner.ExportToFile();
            }
            y += BUTTON_HEIGHT + 5f;
            
            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, BUTTON_HEIGHT), 
                CommonKnowledgeTranslationKeys.DeleteItems.Translate(selectedEntries.Count)))
            {
                Owner.DeleteSelectedEntries();
            }
            GUI.color = Color.white;
            y += BUTTON_HEIGHT + 10f;
            
            // Extended properties batch operations
            Widgets.DrawLineHorizontal(rect.x, y, rect.width);
            y += 10f;
            
            if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, BUTTON_HEIGHT), "RimTalk_EnableAllExtract".Translate()))
            {
                foreach (var entry in selectedEntries)
                {
                    ExtendedKnowledgeEntry.SetCanBeExtracted(entry, true);
                }
            }
            y += BUTTON_HEIGHT + 5f;

            if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, BUTTON_HEIGHT), "RimTalk_DisableAllExtract".Translate()))
            {
                foreach (var entry in selectedEntries)
                {
                    ExtendedKnowledgeEntry.SetCanBeExtracted(entry, false);
                }
            }
            y += BUTTON_HEIGHT + 5f;

            if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, BUTTON_HEIGHT), "RimTalk_EnableAllMatch".Translate()))
            {
                foreach (var entry in selectedEntries)
                {
                    ExtendedKnowledgeEntry.SetCanBeMatched(entry, true);
                }
            }
            y += BUTTON_HEIGHT + 5f;

            if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, BUTTON_HEIGHT), "RimTalk_DisableAllMatch".Translate()))
            {
                foreach (var entry in selectedEntries)
                {
                    ExtendedKnowledgeEntry.SetCanBeMatched(entry, false);
                }
            }
        }

internal void DrawDetailPanel(Rect rect, CommonKnowledgeEntry entry)
        {
            float y = rect.y;
            
            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, y, rect.width - 100f, 30f), 
                CommonKnowledgeTranslationKeys.Details.Translate());
            Text.Font = GameFont.Small;
            
            // Edit button
            if (Widgets.ButtonText(new Rect(rect.xMax - 90f, y, 90f, 28f), 
                CommonKnowledgeTranslationKeys.Edit.Translate()))
            {
                Owner.StartEdit();
            }
            y += 40f;
            
            // Scrollable content
            Rect scrollOuterRect = new Rect(rect.x, y, rect.width, rect.height - y - 50f);
            float scrollHeight = 500f;
            Rect scrollViewRect = new Rect(0f, 0f, rect.width - 16f, scrollHeight);
            
            Widgets.BeginScrollView(scrollOuterRect, ref Owner.detailScrollPosition, scrollViewRect);
        
            float scrollY = 0f;
            
            // Tag
            CommonKnowledgeUIHelpers.DrawDetailField(
                new Rect(0f, scrollY, scrollViewRect.width, 50f), 
                CommonKnowledgeTranslationKeys.Tag.Translate(), 
                entry.tag
            );
            scrollY += 55f;
            
            // ⭐ Category
            var entryCat = CommonKnowledgeUIHelpers.GetEntryCategory(entry);
            string catDisplay = CommonKnowledgeUIHelpers.GetCategoryLabel(entryCat);
            if (entry.category == KnowledgeEntryCategory.None)
                catDisplay += " (авто)";
            CommonKnowledgeUIHelpers.DrawDetailField(
                new Rect(0f, scrollY, scrollViewRect.width, 25f),
                "Категорія",
                catDisplay
            );
            scrollY += 30f;
            
            // Importance
            CommonKnowledgeUIHelpers.DrawDetailField(
                new Rect(0f, scrollY, scrollViewRect.width, 25f), 
                CommonKnowledgeTranslationKeys.Importance.Translate(), 
                entry.importance.ToString("F1")
            );
            scrollY += 30f;
            
            // Status
            string status = entry.isEnabled 
                ? CommonKnowledgeTranslationKeys.StatusEnabled.Translate() 
                : CommonKnowledgeTranslationKeys.StatusDisabled.Translate();
            CommonKnowledgeUIHelpers.DrawDetailField(
                new Rect(0f, scrollY, scrollViewRect.width, 25f), 
                CommonKnowledgeTranslationKeys.Status.Translate(), 
                status
            );
            scrollY += 30f;
            
            // Visibility
            string visibility = CommonKnowledgeUIHelpers.GetVisibilityText(entry);
            CommonKnowledgeUIHelpers.DrawDetailField(
                new Rect(0f, scrollY, scrollViewRect.width, 50f), 
                CommonKnowledgeTranslationKeys.Visibility.Translate(), 
                visibility
            );
            scrollY += 55f;
            
            // Content
            Widgets.Label(new Rect(0f, scrollY, scrollViewRect.width, 20f), 
                CommonKnowledgeTranslationKeys.Content.Translate() + ":");
            scrollY += 22f;
            
            Rect contentRect = new Rect(0f, scrollY, scrollViewRect.width, 200f);
            Widgets.DrawBoxSolid(contentRect, new Color(0.1f, 0.1f, 0.1f, 0.3f));
            Rect contentInnerRect = contentRect.ContractedBy(5f);
            Widgets.Label(contentInnerRect, entry.content);
            scrollY += 205f;
            
            // Extended properties display
            Widgets.DrawLineHorizontal(0f, scrollY, scrollViewRect.width);
            scrollY += 10f;

            Text.Font = GameFont.Small;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Widgets.Label(new Rect(0f, scrollY, scrollViewRect.width, 25f), "RimTalk_ExtendedProperties".Translate());
            GUI.color = Color.white;
            scrollY += 25f;

            bool canBeExtracted = ExtendedKnowledgeEntry.CanBeExtracted(entry);
            DrawPropertyRow(new Rect(0f, scrollY, scrollViewRect.width, 25f), "RimTalk_CanBeExtracted".Translate(), canBeExtracted);
            scrollY += 25f;

            bool canBeMatched = ExtendedKnowledgeEntry.CanBeMatched(entry);
            DrawPropertyRow(new Rect(0f, scrollY, scrollViewRect.width, 25f), "RimTalk_CanBeMatched".Translate(), canBeMatched);
            scrollY += 25f;
            
            Widgets.EndScrollView();
            
            // Delete button at bottom
            Rect deleteRect = new Rect(rect.x, rect.yMax - 40f, rect.width, BUTTON_HEIGHT);
            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (Widgets.ButtonText(deleteRect, CommonKnowledgeTranslationKeys.Delete.Translate()))
            {
                Owner.DeleteSelectedEntries();
            }
            GUI.color = Color.white;
        }

internal void DrawPropertyRow(Rect rect, string label, bool value)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(new Rect(rect.x, rect.y, 120f, rect.height), label);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            string valueText = value ? "RimTalk_Yes".Translate() : "RimTalk_No".Translate();
            Color valueColor = value ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.3f, 0.3f);
            
            GUI.color = valueColor;
            Widgets.Label(new Rect(rect.x + 120f, rect.y, rect.width - 120f, rect.height), valueText);
            GUI.color = Color.white;
        }

internal void DrawEditPanel(Rect rect)
        {
            float y = rect.y;
            
            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, y, rect.width, 30f), 
                lastSelectedEntry == null 
                    ? CommonKnowledgeTranslationKeys.NewEntry.Translate() 
                    : CommonKnowledgeTranslationKeys.EditEntry.Translate());
            Text.Font = GameFont.Small;
            y += 40f;
            
            // Tag
            Widgets.Label(new Rect(rect.x, y, 100f, 25f), 
                CommonKnowledgeUIHelpers.EnsureColon(CommonKnowledgeTranslationKeys.Tag.Translate()));
            editTag = Widgets.TextField(new Rect(rect.x + 100f, y, rect.width - 100f, 25f), editTag);
            y += 30f;
            
            // ⭐ Category dropdown (after tag)
            Widgets.Label(new Rect(rect.x, y, 100f, 25f), "Категорія:");
            string catLabel = CommonKnowledgeUIHelpers.GetExplicitCategoryLabel(editCategory);
            if (editCategory == KnowledgeEntryCategory.None && !string.IsNullOrEmpty(editTag))
            {
                var inferred = CommonKnowledgeUIHelpers.GetEntryCategory(
                    new CommonKnowledgeEntry(editTag, "") { category = KnowledgeEntryCategory.None });
                string inferredName = CommonKnowledgeUIHelpers.GetCategoryLabel(inferred);
                catLabel = "Автовизначення → " + inferredName;
            }
            if (Widgets.ButtonText(new Rect(rect.x + 100f, y, rect.width - 100f, 25f), catLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (KnowledgeEntryCategory cat in Enum.GetValues(typeof(KnowledgeEntryCategory)))
                {
                    KnowledgeEntryCategory localCat = cat;
                    options.Add(new FloatMenuOption(
                        CommonKnowledgeUIHelpers.GetExplicitCategoryLabel(cat),
                        delegate { editCategory = localCat; }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            y += 30f;
            
            // Importance
            Widgets.Label(new Rect(rect.x, y, 100f, 25f), 
                CommonKnowledgeUIHelpers.EnsureColon(CommonKnowledgeTranslationKeys.Importance.Translate()));
            editImportance = Widgets.HorizontalSlider(
                new Rect(rect.x + 100f, y, rect.width - 150f, 25f), 
                editImportance, 0f, 1f
            );
            Widgets.Label(new Rect(rect.xMax - 40f, y, 40f, 25f), editImportance.ToString("F1"));
            y += 30f;
            
            // Pawn selection (simplified)
            Widgets.Label(new Rect(rect.x, y, 100f, 25f), 
                CommonKnowledgeUIHelpers.EnsureColon(CommonKnowledgeTranslationKeys.Visibility.Translate()));
            string pawnLabel = editTargetPawnId == -1 
                ? CommonKnowledgeTranslationKeys.Global.Translate().ToString() 
                : $"Pawn #{editTargetPawnId}";
            if (Widgets.ButtonText(new Rect(rect.x + 100f, y, rect.width - 100f, 25f), pawnLabel))
            {
                CommonKnowledgeUIHelpers.ShowPawnSelectionMenu(pawnId => editTargetPawnId = pawnId);
            }
            y += 35f;
            
            // Match Mode
            Widgets.Label(new Rect(rect.x, y, 100f, 25f), "Режим зіставлення:");
            if (Widgets.ButtonText(new Rect(rect.x + 100f, y, rect.width - 100f, 25f), editMatchMode.ToString()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (KeywordMatchMode mode in Enum.GetValues(typeof(KeywordMatchMode)))
                {
                    options.Add(new FloatMenuOption(mode.ToString(), delegate { editMatchMode = mode; }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            y += 35f;
            
            // Content
            Widgets.Label(new Rect(rect.x, y, 100f, 25f), 
                CommonKnowledgeUIHelpers.EnsureColon(CommonKnowledgeTranslationKeys.Content.Translate()));
            y += 27f;
            
            float contentHeight = rect.yMax - y - 80f;
            Rect contentRect = new Rect(rect.x, y, rect.width, contentHeight);
            Widgets.DrawBoxSolid(contentRect, new Color(0.1f, 0.1f, 0.1f, 0.8f));
            Rect textAreaRect = contentRect.ContractedBy(5f);
            editContent = GUI.TextArea(textAreaRect, editContent ?? "");
            y += contentHeight + 10f;
            
            // Buttons
            float buttonY = rect.yMax - 65f;
            if (Widgets.ButtonText(new Rect(rect.x, buttonY, (rect.width - 5f) / 2f, BUTTON_HEIGHT), 
                CommonKnowledgeTranslationKeys.Save.Translate()))
            {
                Owner.SaveEntry();
            }
            
            if (Widgets.ButtonText(new Rect(rect.x + (rect.width + 5f) / 2f, buttonY, (rect.width - 5f) / 2f, BUTTON_HEIGHT), 
                CommonKnowledgeTranslationKeys.Cancel.Translate()))
            {
                editMode = false;
            }
        }
    }
}
