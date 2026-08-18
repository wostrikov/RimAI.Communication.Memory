
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
    internal sealed class InjectionPreviewMemory : Dialog_InjectionPreviewCollaborator
    {
        internal InjectionPreviewMemory(Dialog_InjectionPreview owner) : base(owner) { }

internal void DrawMemoryPanel(Rect rect)
        {
            // 面板标题
            Widgets.DrawBoxSolid(rect, new Color(0.15f, 0.15f, 0.18f, 0.8f));
            
            Rect titleRect = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 25f);
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.8f, 0.9f, 1f);
            
            string memoryTitle = showMemoryPanel ? "▼ " : "▶ ";
            memoryTitle += "RimTalk_Preview_MemorySection".Translate(cachedMemoryCount);
            
            if (Widgets.ButtonText(titleRect, memoryTitle, false))
            {
                showMemoryPanel = !showMemoryPanel;
            }
            
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            
            if (!showMemoryPanel)
                return;
            
            float yPos = rect.y + 35f;
            
            // 记忆统计
            if (selectedPawn != null)
            {
                var memoryComp = selectedPawn.TryGetComp<FourLayerMemoryComp>();
                if (memoryComp != null)
                {
                    Rect statsRect = new Rect(rect.x + 5f, yPos, rect.width - 10f, 50f);
                    DrawMemoryStats(statsRect, memoryComp);
                    yPos += 55f;
                }
            }
            
            // 记忆预览内容
            Rect previewRect = new Rect(rect.x + 5f, yPos, rect.width - 10f, rect.yMax - yPos - 10f);
            DrawMemoryPreviewContent(previewRect);
        }

internal void DrawMemoryStats(Rect rect, FourLayerMemoryComp memoryComp)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.12f, 0.5f));
            
            float x = rect.x + 5f;
            float lineHeight = 22f;
            
            // 第一行 - 记忆层级统计
            GUI.color = new Color(0.7f, 0.7f, 1f);
            Widgets.Label(new Rect(x, rect.y + 3f, 100f, lineHeight), "RimTalk_Preview_MemoryLayers".Translate());
            GUI.color = Color.white;
            
            x += 100f;
            Widgets.Label(new Rect(x, rect.y + 3f, 120f, lineHeight), 
                $"ABM: {memoryComp.ActiveMemories.Count}");
            
            x += 100f;
            Widgets.Label(new Rect(x, rect.y + 3f, 120f, lineHeight), 
                $"SCM: {memoryComp.SituationalMemories.Count}");
            
            x += 100f;
            Widgets.Label(new Rect(x, rect.y + 3f, 120f, lineHeight), 
                $"ELS: {memoryComp.EventLogMemories.Count}");
            
            x += 100f;
            Widgets.Label(new Rect(x, rect.y + 3f, 120f, lineHeight), 
                $"CLPA: {memoryComp.ArchiveMemories.Count}");
            
            // 第二行 - 注入配置
            var settings = RimTalkMemoryPatchMod.Settings;
            if (settings != null)
            {
                x = rect.x + 5f;
                GUI.color = new Color(0.8f, 0.8f, 1f);
                string configText = "RimTalk_Preview_InjectionConfig".Translate(
                    settings.maxInjectedMemories, 
                    settings.maxABMInjectionRounds);
                Widgets.Label(new Rect(x, rect.y + 25f, rect.width - 10f, lineHeight), configText);
                GUI.color = Color.white;
            }
        }

internal void DrawMemoryPreviewContent(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.08f, 0.1f, 0.6f));
            
            if (string.IsNullOrEmpty(memoryPreviewText))
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "RimTalk_Preview_ClickRefresh".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }
            
            Rect innerRect = rect.ContractedBy(5f);
            float contentHeight = Text.CalcHeight(memoryPreviewText, innerRect.width - 20f);
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 20f, contentHeight + 20f);
            
            Widgets.BeginScrollView(innerRect, ref Owner.scrollPositionRight, viewRect);
            
            GUI.color = new Color(0.9f, 0.9f, 0.9f);
            Widgets.Label(new Rect(0f, 0f, viewRect.width, contentHeight), memoryPreviewText);
            GUI.color = Color.white;
            
            Widgets.EndScrollView();
        }

internal string GetLayerTag(MemoryLayer layer)
        {
            switch (layer)
            {
                case MemoryLayer.Active: return "[ABM]";
                case MemoryLayer.Situational: return "[SCM]";
                case MemoryLayer.EventLog: return "[ELS]";
                case MemoryLayer.Archive: return "[CLPA]";
                default: return "[???]";
            }
        }

internal string GetTypeTag(MemoryType type)
        {
            switch (type)
            {
                case MemoryType.Conversation: return "💬";
                case MemoryType.Action: return "⚡";
                default: return "📝";
            }
        }

internal void ShowHelpDialog()
        {
            string helpContent = "RimTalk_Preview_HelpContent".Translate();
            Find.WindowStack.Add(new Dialog_MessageBox(helpContent, "RimTalk_Close".Translate(), null, null, null, null, false, null, null, WindowLayer.Dialog));
        }
    }
}
