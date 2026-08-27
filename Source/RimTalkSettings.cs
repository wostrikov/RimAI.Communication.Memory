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
    public class RimTalkMemoryPatchSettings : ModSettings
    {
        internal RimTalkMemoryPatchSettingsParts Parts;

        public RimTalkMemoryPatchSettings() { Parts = new RimTalkMemoryPatchSettingsParts(this); }

        public class ReplacementRule : IExposable
        {
            public string pattern = "";
            public string replacement = "";
            public bool isEnabled = true;
            
            public ReplacementRule() { }
            
            public ReplacementRule(string pattern, string replacement, bool isEnabled = true)
            {
                this.pattern = pattern;
                this.replacement = replacement;
                this.isEnabled = isEnabled;
            }
            
            public void ExposeData()
            {
                Scribe_Values.Look(ref pattern, "pattern", "");
                Scribe_Values.Look(ref replacement, "replacement", "");
                Scribe_Values.Look(ref isEnabled, "isEnabled", true);
            }
        }
        
        public List<ReplacementRule> normalizationRules = new List<ReplacementRule>();

        public int maxActiveMemories = 6;
        public int maxSituationalMemories = 20;
        public int maxEventLogMemories = 50;

        public bool IsPlayerDialogueInject = true;
        public bool IsRoundMemoryActive = true;

        public int maxABMInjectionRounds = 3; 
        
        public float scmDecayRate = 0.01f;
        public float elsDecayRate = 0.005f;
        public float clpaDecayRate = 0.001f;
        
        public bool enableDailySummarization = true;
        public int summarizationHour = 0;
        public bool useAISummarization = true;
        public int maxSummaryLength = 80;
        
        public bool enableAutoArchive = true;
        public int archiveIntervalDays = 7;
        public int maxArchiveMemories = 50;

        public bool useRimTalkAIConfig = true;
        public string independentApiKey = "";
        public string independentApiUrl = "";
        public string independentModel = "";
        public string independentProvider = "OpenAI";
        public bool enablePromptCaching = true;
        
        public string dailySummaryPrompt = ""; 
        public string deepArchivePrompt = "";  
        public int summaryMaxTokens = 8000; 

        public bool enableMemoryUI = true;
        
        public bool enableActionMemory = true;
        public bool enableConversationMemory = true;
        
        public bool enablePawnStatusKnowledge = false;
        
        public bool enableEventRecordKnowledge = false;

        public bool enableConversationCache = true;
        public int conversationCacheSize = 200;
        public int conversationCacheExpireDays = 14;
        
        public bool enablePromptCache = true;
        public int promptCacheSize = 100;
        public int promptCacheExpireMinutes = 60;

        public bool useDynamicInjection = true;
        public int maxInjectedMemories = 10;
        public int maxInjectedKnowledge = 5;
        
        public float weightTimeDecay = 0.3f;
        public float weightImportance = 0.3f;
        public float weightKeywordMatch = 0.4f;
        
        public float memoryScoreThreshold = 0.15f;
        public float knowledgeScoreThreshold = 0.1f;
        
        public bool enableAdaptiveThreshold = false;
        public bool autoApplyAdaptiveThreshold = false;
        
        public bool enableProactiveRecall = false;
        public float recallTriggerChance = 0.15f;
        
        // Vector Enhancement Settings
        public bool enableVectorEnhancement = false;
        public float vectorSimilarityThreshold = 0.75f;
        public int maxVectorResults = 5;
        
        // Cloud Embedding Settings
        public string embeddingApiKey = "";
        public string embeddingApiUrl = "https://api.siliconflow.cn/v1/embeddings";
        public string embeddingModel = "BAAI/bge-m3";
        
        // Knowledge Matching Settings
        public bool enableKnowledgeChaining = false;
        public int maxChainingRounds = 2;
        
        public List<string> knowledgeMatchingSources = new List<string> { "prompt", "fullname", "role", "age", "gender", "backstory", "traits", "skills", "relations" };

        internal static bool expandDynamicInjection = true;
        internal static bool expandMemoryCapacity = false;
        internal static bool expandDecayRates = false;
        internal static bool expandSummarization = false;
        internal static bool expandAIConfig = true;
        internal static bool expandMemoryTypes = false;
        internal static bool expandVectorEnhancement = true;
        internal static bool expandExperimentalFeatures = true;
        
        internal static Vector2 scrollPosition = Vector2.zero;

        public override void ExposeData()
        {
            base.ExposeData();
            
            // Serialization / save-load constraint — keep field identity stable.
            Scribe_Collections.Look(ref normalizationRules, "normalizationRules", LookMode.Deep);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit && normalizationRules == null)
            {
                normalizationRules = new List<ReplacementRule>();
            }
            
            int _legacyMaxActive = 6;
            Scribe_Values.Look(ref _legacyMaxActive, "fourLayer_maxActiveMemories", 6);
            
            Scribe_Values.Look(ref maxSituationalMemories, "fourLayer_maxSituationalMemories", 20);
            Scribe_Values.Look(ref maxEventLogMemories, "fourLayer_maxEventLogMemories", 50);
            
            Scribe_Values.Look(ref maxABMInjectionRounds, "fourLayer_maxABMInjectionRounds", 0);
            
            Scribe_Values.Look(ref IsPlayerDialogueInject, "fourLayer_isPlayerDialogueInject", true);
            Scribe_Values.Look(ref IsRoundMemoryActive, "fourLayer_IsRoundMemoryActive", false);

            Scribe_Values.Look(ref scmDecayRate, "fourLayer_scmDecayRate", 0.01f);
            Scribe_Values.Look(ref elsDecayRate, "fourLayer_elsDecayRate", 0.005f);
            Scribe_Values.Look(ref clpaDecayRate, "fourLayer_clpaDecayRate", 0.001f);
            
            Scribe_Values.Look(ref enableDailySummarization, "fourLayer_enableDailySummarization", true);
            Scribe_Values.Look(ref summarizationHour, "fourLayer_summarizationHour", 0);
            Scribe_Values.Look(ref useAISummarization, "fourLayer_useAISummarization", true);
            Scribe_Values.Look(ref maxSummaryLength, "fourLayer_maxSummaryLength", 80);
            
            Scribe_Values.Look(ref enableAutoArchive, "fourLayer_enableAutoArchive", true);
            Scribe_Values.Look(ref archiveIntervalDays, "fourLayer_archiveIntervalDays", 7);
            Scribe_Values.Look(ref maxArchiveMemories, "fourLayer_maxArchiveMemories", 50);

            Scribe_Values.Look(ref useRimTalkAIConfig, "ai_useRimTalkConfig", true);
            Scribe_Values.Look(ref independentApiKey, "ai_independentApiKey", "");
            Scribe_Values.Look(ref independentApiUrl, "ai_independentApiUrl", "");
            Scribe_Values.Look(ref independentModel, "ai_independentModel", "");
            Scribe_Values.Look(ref independentProvider, "ai_independentProvider", "OpenAI");
            Scribe_Values.Look(ref enablePromptCaching, "ai_enablePromptCaching", true);
            
            Scribe_Values.Look(ref dailySummaryPrompt, "ai_dailySummaryPrompt", "");
            Scribe_Values.Look(ref deepArchivePrompt, "ai_deepArchivePrompt", "");
            Scribe_Values.Look(ref summaryMaxTokens, "ai_summaryMaxTokens", 8000); 

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                const string legacyDaily = "Підсумок памʼяті колоніста {0}\n\nСписок спогадів\n{1}\n\nВимоги: виділити місця, людей, події\nСхожі події обʼєднати й позначити частоту\nГранично стисло, не більше 80 слів\nВивести лише текст підсумку, без жодного оформлення";
                const string legacyArchive = "Архів памʼяті колоніста {0}\n\nСписок спогадів\n{1}\n\nВимоги: виділити ключові риси й переломні події\nОбʼєднати схожий досвід, підкреслити довгі тенденції\nГранично стисло, не більше 60 слів\nВивести лише текст підсумку, без жодного оформлення";
                if (dailySummaryPrompt == legacyDaily) dailySummaryPrompt = "";
                if (deepArchivePrompt == legacyArchive) deepArchivePrompt = "";
                if (independentModel == "gpt-3.5-turbo") independentModel = "";
                if ((useRimTalkAIConfig || independentProvider == "OpenAI") && !string.IsNullOrEmpty(independentApiKey))
                    independentApiKey = "";
            }

            Scribe_Values.Look(ref enableMemoryUI, "memoryPatch_enableMemoryUI", true);
            Scribe_Values.Look(ref enableActionMemory, "memoryPatch_enableActionMemory", true);
            Scribe_Values.Look(ref enableConversationMemory, "memoryPatch_enableConversationMemory", true);
            Scribe_Values.Look(ref enablePawnStatusKnowledge, "pawnStatus_enablePawnStatusKnowledge", false);
            Scribe_Values.Look(ref enableEventRecordKnowledge, "eventRecord_enableEventRecordKnowledge", false);

            Scribe_Values.Look(ref enableConversationCache, "cache_enableConversationCache", true);
            Scribe_Values.Look(ref conversationCacheSize, "cache_conversationCacheSize", 200);
            Scribe_Values.Look(ref conversationCacheExpireDays, "cache_conversationCacheExpireDays", 14);
            Scribe_Values.Look(ref enablePromptCache, "cache_enablePromptCache", true);
            Scribe_Values.Look(ref promptCacheSize, "cache_promptCacheSize", 100);
            Scribe_Values.Look(ref promptCacheExpireMinutes, "cache_promptCacheExpireMinutes", 60);
            
            Scribe_Values.Look(ref useDynamicInjection, "dynamic_useDynamicInjection", true);
            Scribe_Values.Look(ref maxInjectedMemories, "dynamic_maxInjectedMemories", 10);
            Scribe_Values.Look(ref maxInjectedKnowledge, "dynamic_maxInjectedKnowledge", 5);
            Scribe_Values.Look(ref weightTimeDecay, "dynamic_weightTimeDecay", 0.3f);
            Scribe_Values.Look(ref weightImportance, "dynamic_weightImportance", 0.3f);
            Scribe_Values.Look(ref weightKeywordMatch, "dynamic_weightKeywordMatch", 0.4f);
            Scribe_Values.Look(ref memoryScoreThreshold, "dynamic_memoryScoreThreshold", 0.15f);
            Scribe_Values.Look(ref knowledgeScoreThreshold, "dynamic_knowledgeScoreThreshold", 0.1f);
            
            Scribe_Values.Look(ref enableAdaptiveThreshold, "adaptive_enableAdaptiveThreshold", false);
            Scribe_Values.Look(ref autoApplyAdaptiveThreshold, "adaptive_autoApplyAdaptiveThreshold", false);
            Scribe_Values.Look(ref enableProactiveRecall, "recall_enableProactiveRecall", false);
            Scribe_Values.Look(ref recallTriggerChance, "recall_triggerChance", 0.15f);

            // Vector Enhancement
            Scribe_Values.Look(ref enableVectorEnhancement, "vector_enableVectorEnhancement", false);
            Scribe_Values.Look(ref vectorSimilarityThreshold, "vector_vectorSimilarityThreshold", 0.75f);
            Scribe_Values.Look(ref maxVectorResults, "vector_maxVectorResults", 5);
            
            Scribe_Values.Look(ref embeddingApiKey, "vector_embeddingApiKey", "");
            Scribe_Values.Look(ref embeddingApiUrl, "vector_embeddingApiUrl", "https://api.siliconflow.cn/v1/embeddings");
            Scribe_Values.Look(ref embeddingModel, "vector_embeddingModel", "BAAI/bge-m3");

            // Knowledge Matching
            Scribe_Values.Look(ref enableKnowledgeChaining, "knowledge_enableKnowledgeChaining", false);
            Scribe_Values.Look(ref maxChainingRounds, "knowledge_maxChainingRounds", 2);
            
            Scribe_Collections.Look(ref knowledgeMatchingSources, "knowledgeMatchingSources", LookMode.Value);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit && (knowledgeMatchingSources == null || knowledgeMatchingSources.Count == 0))
            {
                knowledgeMatchingSources = new List<string> { "prompt", "fullname", "role", "age", "gender", "backstory", "traits", "skills", "relations" };
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit && knowledgeMatchingSources != null)
            {
                int removedCount = knowledgeMatchingSources.RemoveAll(s =>
                    s == "knowledge" || s.StartsWith("knowledge.", StringComparison.OrdinalIgnoreCase));
                if (removedCount > 0)
                {
                    Log.Message($"[MemoryPatch] Removed {removedCount} 'knowledge' entries from matching sources to prevent self-referencing.");
                }
            }
        }

        

        

        

        

        

        

        

        

        

        

        
        
        

        

        
        
        

        
        
    
        #region Cluster forwards
        public void DoSettingsWindowContents(Rect inRect) => Parts.MainUi.DoSettingsWindowContents(inRect);
        internal void DrawPresetConfiguration(Listing_Standard listing) => Parts.MainUi.DrawPresetConfiguration(listing);
        internal void DrawPresetCard(Rect rect, string title, int memoryCount, int knowledgeCount, int tokenEstimate) => Parts.MainUi.DrawPresetCard(rect, title, memoryCount, knowledgeCount, tokenEstimate);
        internal void DrawQuickActionButtons(Listing_Standard listing) => Parts.MainUi.DrawQuickActionButtons(listing);
        internal void DrawActionButton(Rect rect, string label, string tip, System.Action onClick) => Parts.MainUi.DrawActionButton(rect, label, tip, onClick);
        internal void DrawAIConfigSettings(Listing_Standard listing) => Parts.MainUi.DrawAIConfigSettings(listing);
        internal void ValidateAIConfig() => Parts.MainUi.ValidateAIConfig();
        internal void OpenCommonKnowledgeDialog() => Parts.MainUi.OpenCommonKnowledgeDialog();
        internal void DrawCollapsibleSection(Listing_Standard listing, string title, ref bool expanded, System.Action drawContent) => Parts.AdvancedUi.DrawCollapsibleSection(listing, title, ref expanded, drawContent);
        internal void DrawDynamicInjectionSettings(Listing_Standard listing) => Parts.AdvancedUi.DrawDynamicInjectionSettings(listing);
        internal void DrawMemoryCapacitySettings(Listing_Standard listing) => Parts.AdvancedUi.DrawMemoryCapacitySettings(listing);
        internal void DrawDecaySettings(Listing_Standard listing) => Parts.AdvancedUi.DrawDecaySettings(listing);
        internal void DrawSummarizationSettings(Listing_Standard listing) => Parts.AdvancedUi.DrawSummarizationSettings(listing);
        internal void DrawMemoryTypesSettings(Listing_Standard listing) => Parts.AdvancedUi.DrawMemoryTypesSettings(listing);
        internal void DrawExperimentalFeaturesSettings(Listing_Standard listing) => Parts.AdvancedUi.DrawExperimentalFeaturesSettings(listing);
        internal void DrawVectorEnhancementSettings(Listing_Standard listing) => Parts.AdvancedUi.DrawVectorEnhancementSettings(listing);
        internal void DrawPromptNormalizationSettings(Listing_Standard listing) => Parts.AdvancedUi.DrawPromptNormalizationSettings(listing);
        #endregion
}
    internal sealed class RimTalkSettingsMainUi : RimTalkMemoryPatchSettingsCollaborator
    {
        internal RimTalkSettingsMainUi(RimTalkMemoryPatchSettings owner) : base(owner) { }

public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, 1400f);
            Widgets.BeginScrollView(inRect, ref RimTalkMemoryPatchSettings.scrollPosition, viewRect);
            // Verse wraps a Listing into a second column, off the visible view, as soon as
            // content passes the rect height, and CurHeight then reports that new column.
            // A scrolling settings page never wants that; see validate_scrollable_listings.
            listingStandard.maxOneColumn = true;
            listingStandard.Begin(viewRect);

            DrawPresetConfiguration(listingStandard);
            listingStandard.Gap();
            DrawQuickActionButtons(listingStandard);
            listingStandard.GapLine();

            Text.Font = GameFont.Medium;
            listingStandard.Label("RimTalk_Settings_APIConfigTitle".Translate());
            Text.Font = GameFont.Small;
            DrawAIConfigSettings(listingStandard);

            listingStandard.GapLine();
            Rect advancedButtonRect = listingStandard.GetRect(40f);
            if (Widgets.ButtonText(advancedButtonRect, "RimTalk_Settings_AdvancedSettings".Translate()))
            {
                Find.WindowStack.Add(new AdvancedSettingsWindow(Owner));
            }

            listingStandard.End();
            Widgets.EndScrollView();
        }

internal void DrawPresetConfiguration(Listing_Standard listing)
        {
            Text.Font = GameFont.Medium;
            listing.Label("RimTalk_Settings_PresetConfig".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            listing.Label("RimTalk_Settings_PresetConfigDesc".Translate());
            GUI.color = Color.white;
            listing.Gap(6f);

            Rect rowRect = listing.GetRect(95f);
            float spacing = 10f;
            float cardWidth = (rowRect.width - spacing * 2f) / 3f;
            float cardHeight = rowRect.height;

            DrawPresetCard(new Rect(rowRect.x, rowRect.y, cardWidth, cardHeight), "RimTalk_Settings_PresetLight".Translate(), 3, 2, 250);
            DrawPresetCard(new Rect(rowRect.x + cardWidth + spacing, rowRect.y, cardWidth, cardHeight), "RimTalk_Settings_PresetBalanced".Translate(), 6, 4, 520);
            DrawPresetCard(new Rect(rowRect.x + 2f * (cardWidth + spacing), rowRect.y, cardWidth, cardHeight), "RimTalk_Settings_PresetEnhanced".Translate(), 10, 6, 850);

            listing.GapLine();
        }

internal void DrawPresetCard(Rect rect, string title, int memoryCount, int knowledgeCount, int tokenEstimate)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.18f, 0.18f, 0.18f, 0.6f));
            Widgets.DrawHighlightIfMouseover(rect);

            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(0.8f, 0.9f, 1f);
            Widgets.Label(rect, "RimTalk_Settings_PresetCardContent".Translate(title, memoryCount, knowledgeCount, tokenEstimate));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            TooltipHandler.TipRegion(rect, "RimTalk_Settings_PresetCardTooltip".Translate(memoryCount, knowledgeCount, tokenEstimate));

            if (Widgets.ButtonInvisible(rect))
            {
                useDynamicInjection = true;
                maxInjectedMemories = memoryCount;
                maxInjectedKnowledge = knowledgeCount;
                Messages.Message("RimTalk_Settings_PresetApplied".Translate(title, memoryCount, knowledgeCount), MessageTypeDefOf.PositiveEvent);
            }
        }

internal void DrawQuickActionButtons(Listing_Standard listing)
        {
            Text.Font = GameFont.Medium;
            listing.Label("RimTalk_Settings_FeatureEntries".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            Rect rowRect = listing.GetRect(60f);
            float spacing = 10f;
            float buttonWidth = (rowRect.width - spacing * 2f) / 3f;
            float buttonHeight = rowRect.height;

            DrawActionButton(new Rect(rowRect.x, rowRect.y, buttonWidth, buttonHeight), "RimTalk_Settings_KnowledgeLibrary".Translate(), "RimTalk_Settings_KnowledgeLibraryTip".Translate(), delegate
            {
                OpenCommonKnowledgeDialog();
            });

            DrawActionButton(new Rect(rowRect.x + buttonWidth + spacing, rowRect.y, buttonWidth, buttonHeight), "RimTalk_Settings_PromptReplacement".Translate(), "RimTalk_Settings_PromptReplacementTip".Translate(), delegate
            {
                Find.WindowStack.Add(new PromptNormalizationWindow(Owner));
            });

            DrawActionButton(new Rect(rowRect.x + 2f * (buttonWidth + spacing), rowRect.y, buttonWidth, buttonHeight), "RimTalk_Settings_InjectionPreviewer".Translate(), "RimTalk_Settings_InjectionPreviewerTip".Translate(), delegate
            {
                Find.WindowStack.Add(new Memory.Debug.Dialog_InjectionPreview());
            });
        }

internal void DrawActionButton(Rect rect, string label, string tip, System.Action onClick)
        {
            if (Widgets.ButtonText(rect, label))
            {
                onClick?.Invoke();
            }
            TooltipHandler.TipRegion(rect, tip);
        }

internal void DrawAIConfigSettings(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RimTalk_Settings_PreferRimTalkAI".Translate(), ref Owner.useRimTalkAIConfig);
            
            if (useRimTalkAIConfig)
            {
                GUI.color = new Color(0.8f, 1f, 0.8f);
                listing.Label("  " + "RimTalk_Settings_WillFollowRimTalkConfig".Translate());
                GUI.color = Color.white;
                listing.Gap();
                return;
            }
            
            listing.Gap();
            
            SettingsUIDrawers.DrawAIProviderSelection(listing, Owner);
            
            listing.Gap();
            
            listing.Label("RimTalk_Settings_APIKey".Translate() + ":");
            if (independentProvider == "OpenAI")
            {
                listing.Label("OPENAI_RIMAI ✓");
                independentApiKey = "";
            }
            else
            {
                independentApiKey = GUI.PasswordField(listing.GetRect(30f), independentApiKey ?? "", '•');
            }
            
            listing.Label("RimTalk_Settings_APIURL".Translate() + ":");
            independentApiUrl = listing.TextEntry(independentApiUrl);
            
            listing.Label("RimTalk_Settings_ModelName".Translate() + ":");
            independentModel = listing.TextEntry(independentModel);
            
            listing.Gap();
            
            bool canToggleCaching = (independentProvider == "OpenAI" || independentProvider == "DeepSeek");
            
            if (canToggleCaching)
            {
                listing.CheckboxLabeled("RimTalk_Settings_EnablePromptCaching".Translate(), ref Owner.enablePromptCaching);
            }
            else
            {
                enablePromptCaching = false;
                GUI.color = Color.gray;
                bool disabledCache = false;
                listing.CheckboxLabeled("RimTalk_Settings_EnablePromptCachingUnavailable".Translate(), ref disabledCache);
                GUI.color = Color.white;
            }
            
            if (enablePromptCaching || !canToggleCaching)
            {
                if (independentProvider == "OpenAI")
                {
                    GUI.color = new Color(0.8f, 1f, 0.8f);
                    listing.Label("  " + "RimTalk_Settings_OpenAICachingSupport".Translate());
                    listing.Label("  " + "RimTalk_Settings_OpenAICachingModels".Translate());
                    GUI.color = Color.white;
                }
                else if (independentProvider == "DeepSeek")
                {
                    GUI.color = new Color(0.8f, 1f, 0.8f);
                    listing.Label("  " + "RimTalk_Settings_DeepSeekCachingSupport".Translate());
                    listing.Label("  " + "RimTalk_Settings_DeepSeekCachingSavings".Translate());
                    GUI.color = Color.white;
                }
                else if (independentProvider == "Player2")
                {
                    GUI.color = Color.gray;
                    listing.Label("  " + "RimTalk_Settings_Player2NoCaching".Translate());
                    listing.Label("  " + "RimTalk_Settings_Player2LocalNoCache".Translate());
                    GUI.color = Color.white;
                }
                else if (independentProvider == "Google")
                {
                    GUI.color = Color.gray;
                    listing.Label("  " + "RimTalk_Settings_GoogleNoCaching".Translate());
                    GUI.color = Color.white;
                }
                else if (independentProvider == "Custom")
                {
                    GUI.color = Color.gray;
                    listing.Label("  " + "RimTalk_Settings_CustomNoCaching".Translate());
                    listing.Label("  " + "RimTalk_Settings_CustomCachingDepends".Translate());
                    GUI.color = Color.white;
                }
            }
            
            listing.Gap();
            
            Rect validateButtonRect = listing.GetRect(35f);
            if (Widgets.ButtonText(validateButtonRect, "RimTalk_Settings_ValidateConfig".Translate()))
            {
                ValidateAIConfig();
            }
            
            GUI.color = Color.gray;
            listing.Label("RimTalk_Settings_ValidateConfigTip".Translate());
            GUI.color = Color.white;
        }

internal void ValidateAIConfig()
        {
            if (useRimTalkAIConfig)
            {
                Messages.Message("RimTalk_Settings_UsingRimTalkConfigNoValidation".Translate(), MessageTypeDefOf.NeutralEvent);
                return;
            }
            
            if (string.IsNullOrEmpty(independentApiKey))
            {
                Messages.Message("RimTalk_Settings_PleaseEnterAPIKey".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            
            if (string.IsNullOrEmpty(independentApiUrl))
            {
                Messages.Message("RimTalk_Settings_PleaseEnterAPIURL".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            
            if (string.IsNullOrEmpty(independentModel))
            {
                Messages.Message("RimTalk_Settings_PleaseEnterModel".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            
            Messages.Message("RimTalk_Settings_Validating".Translate(), MessageTypeDefOf.NeutralEvent);
            
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Memory.AI.IndependentAISummarizer.ForceReinitialize();
                    
                    if (Memory.AI.IndependentAISummarizer.IsAvailable())
                    {
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            Messages.Message("RimTalk_Settings_ValidationSuccess".Translate(independentProvider), MessageTypeDefOf.PositiveEvent);
                        });
                    }
                    else
                    {
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            Messages.Message("RimTalk_Settings_ValidationFailed".Translate(), MessageTypeDefOf.RejectInput);
                        });
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"AI Config validation failed: {ex.Message}");
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        Messages.Message("RimTalk_Settings_ValidationError".Translate(ex.Message), MessageTypeDefOf.RejectInput);
                    });
                }
            });
        }

internal void OpenCommonKnowledgeDialog()
        {
            if (Current.Game == null)
            {
                Messages.Message("RimTalk_Settings_MustEnterGame".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            var memoryManager = Find.World.GetComponent<MemoryManager>();
            if (memoryManager == null)
            {
                Messages.Message("RimTalk_Settings_CannotFindManager".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            Find.WindowStack.Add(new Dialog_CommonKnowledge(memoryManager.CommonKnowledge));
        }
    }

    internal sealed class RimTalkMemoryPatchSettingsParts
    {
        internal readonly RimTalkMemoryPatchSettings Owner;
        internal readonly RimTalkSettingsMainUi MainUi;
        internal readonly RimTalkSettingsAdvancedUi AdvancedUi;
        internal RimTalkMemoryPatchSettingsParts(RimTalkMemoryPatchSettings owner)
        {
            Owner = owner;
            MainUi = new RimTalkSettingsMainUi(owner);
            AdvancedUi = new RimTalkSettingsAdvancedUi(owner);
        }
    }

}
