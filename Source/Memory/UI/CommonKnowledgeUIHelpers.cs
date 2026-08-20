using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.UI
{
    public static class CommonKnowledgeUIHelpers
    {
        private static readonly Color ColorInstructions = new Color(0.3f, 0.8f, 0.3f);
        private static readonly Color ColorLore = new Color(0.8f, 0.6f, 0.3f);
        private static readonly Color ColorPawnStatus = new Color(0.3f, 0.6f, 0.9f);
        private static readonly Color ColorHistory = new Color(0.7f, 0.5f, 0.7f);
        private static readonly Color ColorOther = Color.white;
        
        
        public static string GetCategoryLabel(KnowledgeCategory category)
        {
            switch (category)
            {
                case KnowledgeCategory.All:
                    return CommonKnowledgeTranslationKeys.CategoryAll.Translate();
                case KnowledgeCategory.Instructions:
                    return CommonKnowledgeTranslationKeys.CategoryInstructions.Translate();
                case KnowledgeCategory.Lore:
                    return CommonKnowledgeTranslationKeys.CategoryLore.Translate();
                case KnowledgeCategory.PawnStatus:
                    return CommonKnowledgeTranslationKeys.CategoryPawnStatus.Translate();
                case KnowledgeCategory.History:
                    return CommonKnowledgeTranslationKeys.CategoryHistory.Translate();
                case KnowledgeCategory.Other:
                    return CommonKnowledgeTranslationKeys.CategoryOther.Translate();
                default:
                    return CommonKnowledgeTranslationKeys.CategoryUnknown.Translate();
            }
        }
        
        public static Color GetCategoryColor(CommonKnowledgeEntry entry)
        {
            var category = GetEntryCategory(entry);
            switch (category)
            {
                case KnowledgeCategory.Instructions:
                    return ColorInstructions;
                case KnowledgeCategory.Lore:
                    return ColorLore;
                case KnowledgeCategory.PawnStatus:
                    return ColorPawnStatus;
                case KnowledgeCategory.History:
                    return ColorHistory;
                default:
                    return ColorOther;
            }
        }
        
        public static KnowledgeCategory GetEntryCategory(CommonKnowledgeEntry entry)
        {
            if (entry.category != KnowledgeEntryCategory.None)
            {
                return ExplicitCategoryToKnowledgeCategory(entry.category);
            }
            
            if (string.IsNullOrEmpty(entry.tag))
                return KnowledgeCategory.Other;

            string tagLower = entry.tag.ToLower();

            
            if (tagLower.Contains("规则") || tagLower.Contains("instructions") || 
                tagLower.Contains("instruction") || tagLower.Contains("rule"))
            {
                return KnowledgeCategory.Instructions;
            }

            if (tagLower.Contains("殖民者状态") || tagLower.Contains("pawnstatus") || 
                tagLower.Contains("colonist") || tagLower.Contains("状态"))
            {
                return KnowledgeCategory.PawnStatus;
            }

            if (tagLower.Contains("历史") || tagLower.Contains("history") || 
                tagLower.Contains("past") || tagLower.Contains("记录"))
            {
                return KnowledgeCategory.History;
            }

            if (tagLower.Contains("世界观") || tagLower.Contains("lore") || 
                tagLower.Contains("background") || tagLower.Contains("背景") ||
                tagLower.Contains("设定"))
            {
                return KnowledgeCategory.Lore;
            }

            return KnowledgeCategory.Other;
        }
        
        public static KnowledgeCategory ExplicitCategoryToKnowledgeCategory(KnowledgeEntryCategory cat)
        {
            switch (cat)
            {
                case KnowledgeEntryCategory.Instructions: return KnowledgeCategory.Instructions;
                case KnowledgeEntryCategory.Lore: return KnowledgeCategory.Lore;
                case KnowledgeEntryCategory.PawnStatus: return KnowledgeCategory.PawnStatus;
                case KnowledgeEntryCategory.History: return KnowledgeCategory.History;
                case KnowledgeEntryCategory.Other: return KnowledgeCategory.Other;
                default: return KnowledgeCategory.Other;
            }
        }
        
        public static KnowledgeEntryCategory KnowledgeCategoryToExplicit(KnowledgeCategory cat)
        {
            switch (cat)
            {
                case KnowledgeCategory.Instructions: return KnowledgeEntryCategory.Instructions;
                case KnowledgeCategory.Lore: return KnowledgeEntryCategory.Lore;
                case KnowledgeCategory.PawnStatus: return KnowledgeEntryCategory.PawnStatus;
                case KnowledgeCategory.History: return KnowledgeEntryCategory.History;
                case KnowledgeCategory.Other: return KnowledgeEntryCategory.Other;
                default: return KnowledgeEntryCategory.None;
            }
        }
        
        public static string GetExplicitCategoryLabel(KnowledgeEntryCategory cat)
        {
            switch (cat)
            {
                case KnowledgeEntryCategory.None: return "自动推断";
                case KnowledgeEntryCategory.Instructions: return "指令规则";
                case KnowledgeEntryCategory.Lore: return "世界观设定";
                case KnowledgeEntryCategory.PawnStatus: return "殖民者状态";
                case KnowledgeEntryCategory.History: return "历史记录";
                case KnowledgeEntryCategory.Other: return "其他";
                default: return "未知";
            }
        }
        
        
        public static string GetVisibilityText(CommonKnowledgeEntry entry)
        {
            if (entry.targetPawnId == -1)
                return CommonKnowledgeTranslationKeys.VisibilityGlobal.Translate();
            
            var pawn = Find.Maps?
                .SelectMany(m => m.mapPawns.FreeColonists)
                .FirstOrDefault(p => p.thingIDNumber == entry.targetPawnId);
            
            return pawn != null 
                ? CommonKnowledgeTranslationKeys.VisibilityExclusive.Translate(pawn.LabelShort) 
                : CommonKnowledgeTranslationKeys.VisibilityDeleted.Translate(entry.targetPawnId);
        }
        
        
        public static void DrawDetailField(Rect rect, string label, string value)
        {
            float labelWidth = 100f;
            
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), EnsureColon(label));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            
            Widgets.Label(new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height), value);
        }

        public static string EnsureColon(string label)
        {
            if (string.IsNullOrEmpty(label)) return ":";
            return label.EndsWith(":") || label.EndsWith("：") ? label : label + ":";
        }

        
        
        public static void DrawColoredCheckbox(Rect rect, string label, ref bool value, Color color)
        {
            Rect colorRect = new Rect(rect.x, rect.y + 2f, 3f, rect.height - 4f);
            Widgets.DrawBoxSolid(colorRect, color);
            
            Rect checkboxRect = new Rect(rect.x + 8f, rect.y, rect.width - 8f, rect.height);
            Widgets.CheckboxLabeled(checkboxRect, label, ref value);
        }
        
        
        public static bool DrawCategoryButton(Rect rect, KnowledgeCategory category, bool isSelected, int count)
        {
            string categoryLabel = GetCategoryLabel(category);
            string label = $"{categoryLabel} ({count})";
            
            if (isSelected)
            {
                Widgets.DrawHighlightSelected(rect);
            }
            else if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }
            
            return Widgets.ButtonText(rect, label, drawBackground: false);
        }
        
        
        public static void DrawAutoGenerateSettings(Rect rect, Action onGeneratePawnStatus, Action onGenerateEventRecord)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.3f));
            Rect innerRect = rect.ContractedBy(5f);
            float y = innerRect.y;
            
            var settings = RimTalkMemoryPatchMod.Settings;
            
            bool enablePawnStatus = settings.enablePawnStatusKnowledge;
            Widgets.CheckboxLabeled(new Rect(innerRect.x, y, innerRect.width, 25f), 
                CommonKnowledgeTranslationKeys.PawnStatus.Translate(), ref enablePawnStatus);
            settings.enablePawnStatusKnowledge = enablePawnStatus;
            y += 30f;
            
            if (Widgets.ButtonText(new Rect(innerRect.x, y, innerRect.width, 25f), 
                CommonKnowledgeTranslationKeys.GenerateNow.Translate()))
            {
                onGeneratePawnStatus?.Invoke();
            }
            y += 30f;
            
            bool enableEventRecord = settings.enableEventRecordKnowledge;
            Widgets.CheckboxLabeled(new Rect(innerRect.x, y, innerRect.width, 25f), 
                CommonKnowledgeTranslationKeys.EventRecord.Translate(), ref enableEventRecord);
            settings.enableEventRecordKnowledge = enableEventRecord;
            y += 30f;
            
            if (Widgets.ButtonText(new Rect(innerRect.x, y, innerRect.width, 25f), 
                CommonKnowledgeTranslationKeys.GenerateNow.Translate()))
            {
                onGenerateEventRecord?.Invoke();
            }
        }
        
        
        public static void ShowPawnSelectionMenu(Action<int> onSelected)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            
            options.Add(new FloatMenuOption(
                CommonKnowledgeTranslationKeys.GlobalAll.Translate(), 
                delegate { onSelected(-1); }
            ));
            
            var colonists = Find.Maps?.SelectMany(m => m.mapPawns.FreeColonists).ToList();
            if (colonists != null && colonists.Count > 0)
            {
                foreach (var pawn in colonists.OrderBy(p => p.LabelShort))
                {
                    int pawnId = pawn.thingIDNumber;
                    options.Add(new FloatMenuOption(
                        CommonKnowledgeTranslationKeys.ExclusiveTo.Translate(pawn.LabelShort), 
                        delegate { onSelected(pawnId); }
                    ));
                }
            }
            
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
