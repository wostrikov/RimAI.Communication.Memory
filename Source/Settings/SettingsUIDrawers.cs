using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.API;
using Ustas.RimAI.Core.Player2;
using System.Linq;
using System.Collections.Generic;

namespace Ustas.RimAI.Communication.Memory
{
    public static class SettingsUIDrawers
    {
        
        public static void DrawAIProviderSelection(Listing_Standard listing, RimTalkMemoryPatchSettings settings)
        {
            listing.Label("RimTalk_Settings_AIProvider".Translate() + ":");
            GUI.color = Color.gray;
            listing.Label("  " + "RimTalk_Settings_CurrentProvider".Translate(settings.independentProvider));
            GUI.color = Color.white;
            
            Rect providerHeaderRect = listing.GetRect(25f);
            Widgets.DrawBoxSolid(providerHeaderRect, new Color(0.15f, 0.15f, 0.15f, 0.5f));
            Widgets.Label(providerHeaderRect.ContractedBy(5f), "RimTalk_Settings_SelectProvider".Translate());
            
            Rect providerButtonRect1 = listing.GetRect(30f);
            float buttonWidth = (providerButtonRect1.width - 20f) / 3f;
            
            DrawProviderButton(new Rect(providerButtonRect1.x, providerButtonRect1.y, buttonWidth, 30f), 
                "OpenAI", settings, "OpenAI", "", "https://api.openai.com/v1/responses",
                new Color(0.5f, 1f, 0.5f));
            
            DrawProviderButton(new Rect(providerButtonRect1.x + buttonWidth + 10f, providerButtonRect1.y, buttonWidth, 30f),
                "DeepSeek", settings, "DeepSeek", "deepseek-chat", "https://api.deepseek.com/v1/chat/completions",
                new Color(0.5f, 0.7f, 1f));
            
            DrawProviderButton(new Rect(providerButtonRect1.x + 2 * (buttonWidth + 10f), providerButtonRect1.y, buttonWidth, 30f),
                "Player2", settings, "Player2", "", Player2Endpoints.ChatCompletions(Player2EndpointKind.CloudGame),
                new Color(1f, 0.8f, 0.5f));
            
            Rect providerButtonRect2 = listing.GetRect(30f);
            
            DrawProviderButton(new Rect(providerButtonRect2.x, providerButtonRect2.y, buttonWidth, 30f),
                "Google", settings, "Google", "gemini-2.0-flash-exp", "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent",
                new Color(1f, 0.5f, 0.5f));
            
            DrawProviderButton(new Rect(providerButtonRect2.x + buttonWidth + 10f, providerButtonRect2.y, buttonWidth, 30f),
                "Custom", settings, "Custom", "custom-model", "https://your-api-url.com/v1/chat/completions",
                new Color(0.7f, 0.7f, 0.7f));
            
            GUI.color = Color.white;
            listing.Gap();
            
            DrawProviderDescription(listing, settings.independentProvider);
        }
        
        private static void DrawProviderButton(Rect rect, string label, RimTalkMemoryPatchSettings settings, 
            string provider, string model, string url, Color highlightColor)
        {
            bool isSelected = settings.independentProvider == provider;
            GUI.color = isSelected ? highlightColor : Color.white;
            
            if (Widgets.ButtonText(rect, label))
            {
                settings.independentProvider = provider;
                settings.independentModel = model;
                settings.independentApiUrl = url;
            }
            
            GUI.color = Color.white;
        }
        
        private static void DrawProviderDescription(Listing_Standard listing, string provider)
        {
            GUI.color = new Color(0.7f, 0.9f, 1f);
            
            switch (provider)
            {
                case "OpenAI":
                    listing.Label("RimTalk_Settings_OpenAIDesc1".Translate());
                    listing.Label("   " + "RimTalk_Settings_OpenAIDesc2".Translate());
                    break;
                case "DeepSeek":
                    listing.Label("RimTalk_Settings_DeepSeekDesc1".Translate());
                    listing.Label("   " + "RimTalk_Settings_DeepSeekDesc2".Translate());
                    break;
                case "Player2":
                    listing.Label("RimTalk_Settings_Player2Desc1".Translate());
                    listing.Label("   " + "RimTalk_Settings_Player2Desc2".Translate());
                    break;
                case "Google":
                    listing.Label("RimTalk_Settings_GoogleDesc1".Translate());
                    listing.Label("   " + "RimTalk_Settings_GoogleDesc2".Translate());
                    break;
                case "Custom":
                    listing.Label("RimTalk_Settings_CustomDesc1".Translate());
                    listing.Label("   " + "RimTalk_Settings_CustomDesc2".Translate());
                    break;
            }
            
            GUI.color = Color.white;
        }
        
        
        private static Dictionary<string, List<(string name, string description, bool isPawnProperty)>> _cachedCategorizedSources = null;
        
        private static Vector2 _matchingSourcesScrollPosition = Vector2.zero;
        
        public static void DrawKnowledgeMatchingSourcesSettings(Listing_Standard listing, RimTalkMemoryPatchSettings settings)
        {
            GUI.color = new Color(0.8f, 1f, 0.8f);
            listing.Label("RimTalk_Settings_KnowledgeMatchingSources".Translate());
            GUI.color = Color.white;
            
            GUI.color = Color.gray;
            listing.Label("  " + "RimTalk_Settings_KnowledgeMatchingSourcesDesc".Translate());
            GUI.color = Color.white;
            listing.Gap();
            
            Rect refreshRect = listing.GetRect(25f);
            if (Widgets.ButtonText(new Rect(refreshRect.x, refreshRect.y, 120f, 25f), "RimTalk_Settings_RefreshList".Translate()))
            {
                MustacheVariableHelper.ClearCache();
                _cachedCategorizedSources = null;
                Messages.Message("RimTalk_Settings_ListRefreshed".Translate(), MessageTypeDefOf.NeutralEvent);
            }
            
            var categorizedSources = GetCachedCategorizedSources();
            
            int totalVars = categorizedSources.Sum(c => c.Value.Count);
            int pawnProps = categorizedSources.Sum(c => c.Value.Count(v => v.isPawnProperty));
            Rect countRect = new Rect(refreshRect.x + 130f, refreshRect.y, 250f, 25f);
            GUI.color = Color.gray;
            Widgets.Label(countRect, "RimTalk_Settings_VariableCount".Translate(totalVars, pawnProps));
            GUI.color = Color.white;
            
            listing.Gap();
            
            if (settings.knowledgeMatchingSources == null || settings.knowledgeMatchingSources.Count == 0)
            {
                settings.knowledgeMatchingSources = new List<string> { "dialogue", "context" };
            }
            
            float rowHeight = 22f;
            float categoryHeaderHeight = 26f;
            float contentHeight = 0f;
            foreach (var category in categorizedSources)
            {
                contentHeight += categoryHeaderHeight;
                contentHeight += category.Value.Count * rowHeight + 2f;
            }
            
            float displayHeight = Mathf.Min(contentHeight + 10f, 320f);
            Rect outerRect = listing.GetRect(displayHeight);
            Widgets.DrawBoxSolid(outerRect, new Color(0.12f, 0.12f, 0.12f, 0.5f));
            
            Rect innerRect = outerRect.ContractedBy(5f);
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, contentHeight);
            
            Widgets.BeginScrollView(innerRect, ref _matchingSourcesScrollPosition, viewRect);
            
            float curY = 0f;
            float width = viewRect.width;
            
            foreach (var category in categorizedSources)
            {
                Rect headerRect = new Rect(0f, curY, width, categoryHeaderHeight - 2f);
                Widgets.DrawBoxSolid(headerRect, new Color(0.2f, 0.25f, 0.3f, 0.8f));
                GUI.color = new Color(0.9f, 0.95f, 1f);
                Widgets.Label(new Rect(8f, curY + 3f, width - 16f, 20f), $"{category.Key} ({category.Value.Count})");
                GUI.color = Color.white;
                curY += categoryHeaderHeight;
                
                foreach (var variable in category.Value)
                {
                    bool isSelected = settings.knowledgeMatchingSources.Contains(variable.name);
                    bool newValue = isSelected;
                    
                    Rect rowRect = new Rect(0f, curY, width, rowHeight - 2f);
                    
                    if (isSelected)
                    {
                        Widgets.DrawBoxSolid(rowRect, new Color(0.2f, 0.4f, 0.2f, 0.3f));
                    }
                    
                    if (Mouse.IsOver(rowRect))
                    {
                        Widgets.DrawHighlight(rowRect);
                    }
                    
                    Widgets.Checkbox(new Vector2(8f, curY + 1f), ref newValue);
                    
                    float nameStartX = 32f;
                    if (variable.isPawnProperty)
                    {
                        Rect tagRect = new Rect(nameStartX, curY + 2f, 45f, 18f);
                        Widgets.DrawBoxSolid(tagRect, new Color(0.3f, 0.5f, 0.7f, 0.5f));
                        GUI.color = new Color(0.7f, 0.9f, 1f);
                        Text.Font = GameFont.Tiny;
                        Widgets.Label(new Rect(tagRect.x + 3f, curY + 3f, 40f, 16f), "Pawn");
                        Text.Font = GameFont.Small;
                        GUI.color = Color.white;
                        nameStartX += 50f;
                    }
                    
                    GUI.color = isSelected ? new Color(0.5f, 1f, 0.5f) : new Color(0.9f, 0.9f, 0.7f);
                    Widgets.Label(new Rect(nameStartX, curY + 1f, 140f, 20f), variable.name);
                    GUI.color = Color.white;
                    
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    string shortDesc = variable.description.Length > 30
                        ? variable.description.Substring(0, 27) + "..."
                        : variable.description;
                    Widgets.Label(new Rect(nameStartX + 145f, curY + 2f, width - nameStartX - 150f, 18f), shortDesc);
                    Text.Font = GameFont.Small;
                    GUI.color = Color.white;
                    
                    string tooltip = variable.isPawnProperty
                        ? "RimTalk_Settings_PawnPropertyTooltip".Translate(variable.name, variable.description)
                        : "RimTalk_Settings_VariableTooltip".Translate(variable.name, variable.description);
                    TooltipHandler.TipRegion(rowRect, tooltip);
                    
                    if (newValue != isSelected)
                    {
                        if (newValue)
                        {
                            if (!settings.knowledgeMatchingSources.Contains(variable.name))
                            {
                                settings.knowledgeMatchingSources.Add(variable.name);
                            }
                        }
                        else
                        {
                            settings.knowledgeMatchingSources.Remove(variable.name);
                            if (settings.knowledgeMatchingSources.Count == 0)
                            {
                                settings.knowledgeMatchingSources.Add("dialogue");
                                Messages.Message("RimTalk_Settings_AtLeastOneSource".Translate(), MessageTypeDefOf.RejectInput);
                            }
                        }
                    }
                    
                    curY += rowHeight;
                }
                
                curY += 2f;
            }
            
            Widgets.EndScrollView();
            
            listing.Gap();
            
            int selectedPawnProps = settings.knowledgeMatchingSources.Count(s =>
                categorizedSources.Any(c => c.Value.Any(v => v.name == s && v.isPawnProperty)));
            int selectedOther = settings.knowledgeMatchingSources.Count - selectedPawnProps;
            
            GUI.color = new Color(0.7f, 0.9f, 1f);
            listing.Label("RimTalk_Settings_SelectedCount".Translate(settings.knowledgeMatchingSources.Count, selectedPawnProps, selectedOther));
            
            string selectedStr = settings.knowledgeMatchingSources.Count <= 4
                ? string.Join(", ", settings.knowledgeMatchingSources)
                : $"{string.Join(", ", settings.knowledgeMatchingSources.Take(3))}... (+{settings.knowledgeMatchingSources.Count - 3})";
            GUI.color = Color.gray;
            listing.Label($"  {selectedStr}");
            GUI.color = Color.white;
            
            listing.Gap();
            listing.GapLine();
        }
        
        private static Dictionary<string, List<(string name, string description, bool isPawnProperty)>> GetCachedCategorizedSources()
        {
            if (_cachedCategorizedSources == null)
            {
                _cachedCategorizedSources = MustacheVariableHelper.GetCategorizedMatchingSources();
            }
            return _cachedCategorizedSources;
        }
        
        
        public static void DrawKnowledgeChainingSettings(Listing_Standard listing, RimTalkMemoryPatchSettings settings)
        {
            listing.CheckboxLabeled("RimTalk_Settings_EnableKnowledgeChaining".Translate(), ref settings.enableKnowledgeChaining);
            if (settings.enableKnowledgeChaining)
            {
                GUI.color = new Color(1f, 0.8f, 0.5f);
                listing.Label("  " + "RimTalk_Settings_KnowledgeChainingDesc".Translate());
                GUI.color = Color.white;
                listing.Gap();
                
                listing.Label("RimTalk_Settings_MaxChainingRoundsLabel".Translate(settings.maxChainingRounds));
                settings.maxChainingRounds = (int)listing.Slider(settings.maxChainingRounds, 1, 5);
            }
            
            listing.Gap();
        }
        
        
        public static void DrawPromptNormalizationSettings(Listing_Standard listing, RimTalkMemoryPatchSettings settings)
        {
            Rect sectionRect = listing.GetRect(300f);
            Widgets.DrawBoxSolid(sectionRect, new Color(0.15f, 0.15f, 0.15f, 0.5f));
            
            Listing_Standard inner = new Listing_Standard();
            inner.Begin(sectionRect.ContractedBy(10f));
            
            Text.Font = GameFont.Small;
            GUI.color = new Color(1f, 0.9f, 0.7f);
            inner.Label("RimTalk_Settings_ReplacementRulesList".Translate());
            GUI.color = Color.white;
            
            inner.Gap(5f);
            
            if (settings.normalizationRules == null)
            {
                settings.normalizationRules = new System.Collections.Generic.List<RimTalkMemoryPatchSettings.ReplacementRule>();
            }
            
            for (int i = 0; i < settings.normalizationRules.Count; i++)
            {
                var rule = settings.normalizationRules[i];
                
                Rect ruleRect = inner.GetRect(30f);
                
                Rect checkboxRect = new Rect(ruleRect.x, ruleRect.y, 24f, 24f);
                Widgets.Checkbox(checkboxRect.position, ref rule.isEnabled);
                
                Rect patternRect = new Rect(ruleRect.x + 30f, ruleRect.y, 200f, 25f);
                rule.pattern = Widgets.TextField(patternRect, rule.pattern ?? "");
                
                Rect arrowRect = new Rect(ruleRect.x + 235f, ruleRect.y, 30f, 25f);
                Widgets.Label(arrowRect, " → ");
                
                Rect replacementRect = new Rect(ruleRect.x + 270f, ruleRect.y, 150f, 25f);
                rule.replacement = Widgets.TextField(replacementRect, rule.replacement ?? "");
                
                Rect deleteRect = new Rect(ruleRect.x + 430f, ruleRect.y, 30f, 25f);
                GUI.color = new Color(1f, 0.3f, 0.3f);
                if (Widgets.ButtonText(deleteRect, "×"))
                {
                    settings.normalizationRules.RemoveAt(i);
                    i--;
                }
                GUI.color = Color.white;
                
                inner.Gap(3f);
            }
            
            Rect addButtonRect = inner.GetRect(30f);
            if (Widgets.ButtonText(addButtonRect, "RimTalk_Settings_AddNewRule".Translate()))
            {
                settings.normalizationRules.Add(new RimTalkMemoryPatchSettings.ReplacementRule("", "", true));
            }
            
            inner.Gap(5f);
            
            int enabledCount = settings.normalizationRules.Count(r => r.isEnabled);
            GUI.color = Color.gray;
            inner.Label("RimTalk_Settings_EnabledRulesCount".Translate(enabledCount, settings.normalizationRules.Count));
            GUI.color = Color.white;
            
            inner.Gap(3f);
            GUI.color = new Color(0.7f, 0.9f, 1f);
            inner.Label("RimTalk_Settings_RuleExample".Translate());
            inner.Label("   " + "RimTalk_Settings_RegexSupport".Translate());
            GUI.color = Color.white;
            
            inner.End();
        }
        
        
        public static void DrawSiliconFlowSettings(Listing_Standard listing, RimTalkMemoryPatchSettings settings)
        {
            listing.CheckboxLabeled("RimTalk_Settings_EnableVectorEnhancement".Translate(), ref settings.enableVectorEnhancement);
            if (settings.enableVectorEnhancement)
            {
                GUI.color = new Color(0.8f, 1f, 0.8f);
                listing.Label("  " + "RimTalk_Settings_VectorEnhancementDesc".Translate());
                GUI.color = Color.white;
                listing.Gap();
                
                listing.Label("RimTalk_Settings_SimilarityThresholdLabel".Translate(settings.vectorSimilarityThreshold.ToString("F2")));
                settings.vectorSimilarityThreshold = listing.Slider(settings.vectorSimilarityThreshold, 0.5f, 1.0f);
                
                listing.Label("RimTalk_Settings_MaxVectorResultsLabel".Translate(settings.maxVectorResults));
                settings.maxVectorResults = (int)listing.Slider(settings.maxVectorResults, 1, 15);
                
                listing.Gap();
                
                GUI.color = new Color(1f, 0.9f, 0.7f);
                listing.Label("RimTalk_Settings_CloudEmbeddingConfig".Translate());
                GUI.color = Color.white;
                
                listing.Label("RimTalk_Settings_EmbeddingAPIKey".Translate() + ":");
                settings.embeddingApiKey = GUI.PasswordField(listing.GetRect(30f), settings.embeddingApiKey ?? "", '•');
                
                listing.Label("RimTalk_Settings_EmbeddingAPIURL".Translate() + ":");
                settings.embeddingApiUrl = listing.TextEntry(settings.embeddingApiUrl);
                
                listing.Label("RimTalk_Settings_EmbeddingModel".Translate() + ":");
                settings.embeddingModel = listing.TextEntry(settings.embeddingModel);
                
                GUI.color = Color.gray;
                listing.Label("RimTalk_Settings_EmbeddingAPIKeyTip".Translate());
                GUI.color = Color.white;
            }
            
            listing.Gap();
        }
    }
}
