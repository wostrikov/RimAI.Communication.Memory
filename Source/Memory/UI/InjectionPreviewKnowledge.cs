
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.API;
using Ustas.RimAI.Communication.Prompt;

namespace Ustas.RimAI.Communication.Memory.Debug
{
    internal sealed class InjectionPreviewKnowledge : Dialog_InjectionPreviewCollaborator
    {
        internal InjectionPreviewKnowledge(Dialog_InjectionPreview owner) : base(owner) { }

internal void DrawKnowledgePanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
            
            Rect titleRect = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 25f);
            Text.Font = GameFont.Medium;
            GUI.color = new Color(1f, 1f, 0.8f);
            
            string knowledgeTitle = showKnowledgePanel ? "▼ " : "▶ ";
            knowledgeTitle += "RimTalk_Preview_KnowledgeSection".Translate();
            
            if (Widgets.ButtonText(titleRect, knowledgeTitle, false))
            {
                showKnowledgePanel = !showKnowledgePanel;
            }
            
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            
            if (!showKnowledgePanel)
                return;
            
            float yPos = rect.y + 35f;
            float contentWidth = rect.width - 10f;
            
            Rect matchSourceHeaderRect = new Rect(rect.x + 5f, yPos, contentWidth, 25f);
            GUI.color = new Color(0.8f, 0.9f, 1f);
            
            string matchSourceTitle = showMatchSourcePanel ? "▼ " : "▶ ";
            matchSourceTitle += "RimTalk_Preview_MatchingSource".Translate();
            
            if (Widgets.ButtonText(matchSourceHeaderRect, matchSourceTitle, false))
            {
                showMatchSourcePanel = !showMatchSourcePanel;
            }
            GUI.color = Color.white;
            yPos += 28f;
            
            if (showMatchSourcePanel)
            {
                float matchSourceHeight = 150f;
                Rect matchSourceRect = new Rect(rect.x + 5f, yPos, contentWidth, matchSourceHeight);
                DrawMatchingSourceSelector(matchSourceRect);
                yPos += matchSourceHeight + 5f;
            }
            
            Rect parsedTitleRect = new Rect(rect.x + 5f, yPos, contentWidth, 25f);
            GUI.color = new Color(0.9f, 1f, 0.8f);
            Widgets.Label(parsedTitleRect, "RimTalk_Preview_ParsedMatchText".Translate());
            GUI.color = Color.white;
            yPos += 28f;
            
            float parsedTextHeight = 180f;
            Rect parsedTextRect = new Rect(rect.x + 5f, yPos, contentWidth, parsedTextHeight);
            Widgets.DrawBoxSolid(parsedTextRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            
            if (string.IsNullOrEmpty(parsedMatchText))
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(parsedTextRect, "RimTalk_Preview_NoParsedText".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else
            {
                Rect innerRect = parsedTextRect.ContractedBy(5f);
                float textHeight = Text.CalcHeight(parsedMatchText, innerRect.width - 20f);
                Rect viewRect = new Rect(0f, 0f, innerRect.width - 20f, Mathf.Max(textHeight + 10f, innerRect.height));
                
                Widgets.BeginScrollView(innerRect, ref Owner.scrollPositionParsedText, viewRect);
                GUI.color = new Color(0.85f, 0.85f, 0.85f);
                Widgets.Label(new Rect(0f, 0f, viewRect.width, textHeight), parsedMatchText);
                GUI.color = Color.white;
                Widgets.EndScrollView();
            }
            yPos += parsedTextHeight + 5f;
            
            Rect matchedTitleRect = new Rect(rect.x + 5f, yPos, contentWidth, 25f);
            GUI.color = new Color(1f, 0.9f, 0.7f);
            string matchedTitle = "RimTalk_Preview_MatchedKnowledge".Translate(matchedKnowledge.Count);
            Widgets.Label(matchedTitleRect, matchedTitle);
            GUI.color = Color.white;
            yPos += 28f;
            
            Rect knowledgeListRect = new Rect(rect.x + 5f, yPos, contentWidth, rect.yMax - yPos - 10f);
            DrawMatchedKnowledgeList(knowledgeListRect);
        }

internal void DrawMatchingSourceSelector(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.12f, 0.12f, 0.6f));
            
            if (availableMatchingSources == null || availableMatchingSources.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(rect.ContractedBy(5f), "RimTalk_Preview_NoMatchingSources".Translate());
                GUI.color = Color.white;
                return;
            }
            
            var settings = RimTalkMemoryPatchMod.Settings;
            if (settings == null) return;
            
            Rect innerRect = rect.ContractedBy(5f);
            float lineHeight = 22f;
            int columns = 3;
            float columnWidth = innerRect.width / columns;
            
            float totalHeight = Mathf.Ceil(availableMatchingSources.Count / (float)columns) * lineHeight;
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 20f, totalHeight);
            
            Widgets.BeginScrollView(innerRect, ref Owner.scrollPositionMatchSource, viewRect);
            
            int index = 0;
            foreach (var source in availableMatchingSources)
            {
                int col = index % columns;
                int row = index / columns;
                
                Rect checkboxRect = new Rect(col * columnWidth, row * lineHeight, columnWidth - 5f, lineHeight);
                
                bool isSelected = settings.knowledgeMatchingSources.Contains(source.name);
                string label = source.isPawnProperty ? $"[P] {source.name}" : source.name;
                
                TooltipHandler.TipRegion(checkboxRect, source.description);
                
                bool newSelected = isSelected;
                Widgets.CheckboxLabeled(checkboxRect, label, ref newSelected);
                
                if (newSelected != isSelected)
                {
                    if (newSelected)
                    {
                        if (!settings.knowledgeMatchingSources.Contains(source.name))
                            settings.knowledgeMatchingSources.Add(source.name);
                    }
                    else
                    {
                        settings.knowledgeMatchingSources.Remove(source.name);
                    }
                    
                    Owner.RefreshParsedMatchText();
                }
                
                index++;
            }
            
            Widgets.EndScrollView();
        }

internal void DrawMatchedKnowledgeList(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            
            if (matchedKnowledge.Count == 0)
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "RimTalk_Preview_NoMatchedKnowledge".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }
            
            Rect innerRect = rect.ContractedBy(5f);
            float entryHeight = 60f;
            float totalHeight = matchedKnowledge.Count * entryHeight;
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 20f, totalHeight);
            
            Widgets.BeginScrollView(innerRect, ref Owner.scrollPositionLeft, viewRect);
            
            for (int i = 0; i < matchedKnowledge.Count; i++)
            {
                var ks = matchedKnowledge[i];
                Rect entryRect = new Rect(0f, i * entryHeight, viewRect.width, entryHeight - 5f);
                
                Widgets.DrawBoxSolid(entryRect, new Color(0.15f, 0.18f, 0.15f, 0.4f));
                
                GUI.color = new Color(0.9f, 0.9f, 0.6f);
                string headerText = $"[{i + 1}] [{ks.Entry.tag}] " + "RimTalk_Preview_Score".Translate(ks.Score.ToString("F2"));
                Widgets.Label(new Rect(entryRect.x + 5f, entryRect.y + 2f, entryRect.width - 10f, 20f), headerText);
                GUI.color = Color.white;
                
                GUI.color = new Color(0.85f, 0.85f, 0.85f);
                string contentPreview = ks.Entry.content.Length > 100 
                    ? ks.Entry.content.Substring(0, 100) + "..." 
                    : ks.Entry.content;
                Widgets.Label(new Rect(entryRect.x + 5f, entryRect.y + 22f, entryRect.width - 10f, 35f), contentPreview);
                GUI.color = Color.white;
            }
            
            Widgets.EndScrollView();
        }
    }
}
