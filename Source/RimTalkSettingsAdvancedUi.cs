using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ustas.RimAI.Communication.Memory
{
    internal sealed class RimTalkSettingsAdvancedUi : RimTalkMemoryPatchSettingsCollaborator
    {
        internal RimTalkSettingsAdvancedUi(RimTalkMemoryPatchSettings owner) : base(owner) { }

internal void DrawCollapsibleSection(Listing_Standard listing, string title, ref bool expanded, System.Action drawContent)
        {
            Rect headerRect = listing.GetRect(30f);
            Widgets.DrawBoxSolid(headerRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            
            Text.Font = GameFont.Medium;
            Rect labelRect = new Rect(headerRect.x + 30f, headerRect.y + 3f, headerRect.width - 30f, headerRect.height);
            Widgets.Label(labelRect, title);
            Text.Font = GameFont.Small;
            
            Rect iconRect = new Rect(headerRect.x + 5f, headerRect.y + 7f, 20f, 20f);
            if (Widgets.ButtonImage(iconRect, expanded ? TexButton.Collapse : TexButton.Reveal))
            {
                expanded = !expanded;
            }
            
            listing.Gap(3f);
            
            if (expanded)
            {
                listing.Gap(3f);
                drawContent?.Invoke();
                listing.Gap(6f);
            }
            
            listing.GapLine();
        }

internal void DrawDynamicInjectionSettings(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RimTalk_Settings_EnableDynamicInjection".Translate(), ref Owner.useDynamicInjection);
            
            if (useDynamicInjection)
            {
                GUI.color = new Color(0.8f, 1f, 0.8f);
                listing.Label("  " + "RimTalk_Settings_DynamicInjectionDesc".Translate());
                GUI.color = Color.white;
                
                listing.Gap();
                
                // ⭐ v4.0: ABM 注入轮数设置
                listing.Label("RimTalk_Settings_MaxABMInjectionRoundsLabel".Translate(maxABMInjectionRounds));
                maxABMInjectionRounds = (int)listing.Slider(maxABMInjectionRounds, 1, 10);
                GUI.color = Color.gray;
                listing.Label("  " + "RimTalk_Settings_MaxABMInjectionRoundsDesc".Translate());
                GUI.color = Color.white;
                
                listing.Gap();
                
                // ⭐ 是否注入玩家发言
                listing.CheckboxLabeled("RimTalk_Settings_IsPlayerDialogueInject".Translate(), ref Owner.IsPlayerDialogueInject);
                GUI.color = Color.gray;
                listing.Label("  " + "RimTalk_Settings_IsPlayerDialogueInjectDesc".Translate());
                GUI.color = Color.white;
                
                listing.Gap();

                // ⭐ 是否启用轮次记忆
                // 防呆设计：拨动开关时会自动调整ABM注入轮数
                bool oldIsActive = IsRoundMemoryActive;
                listing.CheckboxLabeled("RimTalk_Settings_IsRoundMemoryActive".Translate(), ref Owner.IsRoundMemoryActive);
                GUI.color = Color.gray;
                listing.Label("  " + "RimTalk_Settings_IsRoundMemoryActiveDesc".Translate());
                GUI.color = Color.white;
                if (oldIsActive != IsRoundMemoryActive)
                {
                    // 如果开关状态发生了改变，即时改变ABM注入轮数
                    if (IsRoundMemoryActive)
                    {
                        // 开关被【打开】时，设为默认值3
                        maxABMInjectionRounds = 3;
                    }
                    else
                    {
                        // 开关被【关闭】时，设为0
                        maxABMInjectionRounds = 0;
                    }
                }

                listing.Gap();

                listing.Label("RimTalk_Settings_MaxInjectedMemoriesLabel".Translate(maxInjectedMemories));
                maxInjectedMemories = (int)listing.Slider(maxInjectedMemories, 1, 20);
                
                listing.Label("RimTalk_Settings_MaxInjectedKnowledgeLabel".Translate(maxInjectedKnowledge));
                
                // 滑条和输入框组合
                Rect knowledgeSliderRect = listing.GetRect(28f);
                Rect sliderRect = new Rect(knowledgeSliderRect.x, knowledgeSliderRect.y, knowledgeSliderRect.width - 70f, 28f);
                Rect inputRect = new Rect(knowledgeSliderRect.xMax - 60f, knowledgeSliderRect.y, 60f, 24f);
                
                // 滑条
                maxInjectedKnowledge = (int)Widgets.HorizontalSlider(sliderRect, maxInjectedKnowledge, 0f, 100f, true);
                
                // 输入框
                string knowledgeInput = maxInjectedKnowledge.ToString();
                knowledgeInput = Widgets.TextField(inputRect, knowledgeInput);
                if (int.TryParse(knowledgeInput, out int parsedKnowledge))
                {
                    maxInjectedKnowledge = Mathf.Clamp(parsedKnowledge, 0, 100);
                }
                
                listing.Gap();
                
                listing.Label("RimTalk_Settings_MemoryScoreThresholdLabel".Translate(memoryScoreThreshold.ToString("P0")));
                memoryScoreThreshold = listing.Slider(memoryScoreThreshold, 0f, 1f);
                
                listing.Label("RimTalk_Settings_KnowledgeScoreThresholdLabel".Translate(knowledgeScoreThreshold.ToString("P0")));
                knowledgeScoreThreshold = listing.Slider(knowledgeScoreThreshold, 0f, 1f);
            }
        }

internal void DrawMemoryCapacitySettings(Listing_Standard listing)
        {
            listing.Label("RimTalk_Settings_SCMCapacityLabel".Translate(maxSituationalMemories));
            maxSituationalMemories = (int)listing.Slider(maxSituationalMemories, 10, 50);
            
            listing.Label("RimTalk_Settings_ELSCapacityLabel".Translate(maxEventLogMemories));
            maxEventLogMemories = (int)listing.Slider(maxEventLogMemories, 20, 100);
        }

internal void DrawDecaySettings(Listing_Standard listing)
        {
            listing.Label("RimTalk_Settings_SCMDecayLabel".Translate(scmDecayRate.ToString("P1")));
            scmDecayRate = listing.Slider(scmDecayRate, 0.001f, 0.05f);
            
            listing.Label("RimTalk_Settings_ELSDecayLabel".Translate(elsDecayRate.ToString("P1")));
            elsDecayRate = listing.Slider(elsDecayRate, 0.0005f, 0.02f);
            
            listing.Label("RimTalk_Settings_CLPADecayLabel".Translate(clpaDecayRate.ToString("P1")));
            clpaDecayRate = listing.Slider(clpaDecayRate, 0.0001f, 0.01f);
        }

internal void DrawSummarizationSettings(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RimTalk_Settings_EnableDailySummarization".Translate(), ref Owner.enableDailySummarization);
            
            if (enableDailySummarization)
            {
                listing.Label("RimTalk_Settings_TriggerTimeLabel".Translate(summarizationHour));
                summarizationHour = (int)listing.Slider(summarizationHour, 0, 23);
            }
            
            listing.CheckboxLabeled("RimTalk_Settings_EnableAutoArchive".Translate(), ref Owner.enableAutoArchive);
        }

internal void DrawMemoryTypesSettings(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RimTalk_Settings_ActionMemory".Translate(), ref Owner.enableActionMemory);
            listing.CheckboxLabeled("RimTalk_Settings_ConversationMemory".Translate(), ref Owner.enableConversationMemory);
        }

internal void DrawExperimentalFeaturesSettings(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RimTalk_Settings_EnableProactiveRecall".Translate(), ref Owner.enableProactiveRecall);
            
            if (enableProactiveRecall)
            {
                listing.Label("RimTalk_Settings_TriggerChanceLabel".Translate(recallTriggerChance.ToString("P0")));
                recallTriggerChance = listing.Slider(recallTriggerChance, 0.05f, 0.60f);
            }
            
            listing.Gap();
            listing.GapLine();
            
            // ⭐ v4.0: 知识匹配源选择（动态从 RimTalk 获取 Mustache 变量）
            SettingsUIDrawers.DrawKnowledgeMatchingSourcesSettings(listing, Owner);
            
            // ⭐ 常识链设置
            SettingsUIDrawers.DrawKnowledgeChainingSettings(listing, Owner);
        }

internal void DrawVectorEnhancementSettings(Listing_Standard listing)
        {
            // ⭐ SiliconFlow向量服务设置
            SettingsUIDrawers.DrawSiliconFlowSettings(listing, Owner);
        }

internal void DrawPromptNormalizationSettings(Listing_Standard listing)
        {
            // ⭐ 使用辅助类绘制
            SettingsUIDrawers.DrawPromptNormalizationSettings(listing, Owner);
        }
    }
}
