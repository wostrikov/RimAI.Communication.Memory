using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.UI;
using Ustas.RimAI.Communication.Prompt;
using Ustas.RimAI.Core.Memory;

namespace Ustas.RimAI.Communication.Memory.API
{
    /// <summary>
    /// Supplies {{knowledge}} template values for Communication Scriban rendering.
    /// Keyword matching is synchronous; vector enhancement runs off the talk-generation path.
    /// </summary>
    public static class KnowledgeVariableProvider
    {
        #region 位置追踪（供 Patch 使用）
        
        // Threading/concurrency constraint — do not race this state. (summary knowledge summary)
        public class KnowledgeInjectionContext
        {
            public string MatchText { get; set; }          
            public string DialogueType { get; set; }       
            public string KeywordKnowledge { get; set; }   
            public Pawn Speaker { get; set; }              
            public Pawn Listener { get; set; }             
            public int Tick { get; set; }                  
        }
        
        private static KnowledgeInjectionContext _lastContext;
        private static readonly object _contextLock = new object();
        
        public static KnowledgeInjectionContext GetLastContext()
        {
            lock (_contextLock)
            {
                return _lastContext;
            }
        }
        
        public static void ClearContext()
        {
            lock (_contextLock)
            {
                _lastContext = null;
            }
        }
        
        #endregion
        
        public static string GetMatchedKnowledge(object promptContext)
        {
            if (promptContext == null)
            {
                return "";
            }
            
            try
            {
                // OPTION A knowledge path: present Attach's one conversation Projection.
                if (promptContext is PromptContext typedCtx
                    && typedCtx.TryGetTypedKnowledgeProjection(out var precomputedKnowledge)
                    && precomputedKnowledge != null)
                {
                    if (!string.IsNullOrEmpty(precomputedKnowledge))
                    {
                        lock (_contextLock)
                        {
                            _lastContext = new KnowledgeInjectionContext
                            {
                                MatchText = typedCtx.DialoguePrompt ?? typedCtx.DialogueType,
                                DialogueType = typedCtx.DialogueType,
                                KeywordKnowledge = precomputedKnowledge,
                                Speaker = typedCtx.CurrentPawn,
                                Tick = Find.TickManager?.TicksGame ?? 0
                            };
                        }
                        return PromptNormalizer.Normalize(precomputedKnowledge);
                    }

                    return "";
                }

                var settings = RimTalkMemoryPatchMod.Settings;
                
                string matchText = BuildMatchText(promptContext, settings);
                var typedKnowledge = MemoryContextAccess.Knowledge;
                if (typedKnowledge != null && !string.IsNullOrEmpty(matchText))
                {
                    var typed = typedKnowledge.GetKnowledge(new MemoryContextRequest
                    {
                        Query = matchText,
                        PawnId = GetPropertyValue<Pawn>(promptContext, "CurrentPawn")?.ThingID
                    });
                    if (!string.IsNullOrEmpty(typed.Projection))
                    {
                        lock (_contextLock)
                        {
                            _lastContext = new KnowledgeInjectionContext
                            {
                                MatchText = matchText,
                                DialogueType = GetVariableValue(promptContext, "dialogue.type"),
                                KeywordKnowledge = typed.Projection,
                                Speaker = GetPropertyValue<Pawn>(promptContext, "CurrentPawn"),
                                Tick = Find.TickManager?.TicksGame ?? 0
                            };
                        }
                        return PromptNormalizer.Normalize(typed.Projection);
                    }
                }
                
                if (string.IsNullOrEmpty(matchText))
                {
                    return "(Немає контексту для зіставлення)";
                }
                
                Pawn speaker = GetPropertyValue<Pawn>(promptContext, "CurrentPawn");
                Pawn listener = null;
                
                var allPawns = GetPropertyValue<List<Pawn>>(promptContext, "AllPawns");
                if (allPawns?.Count > 1)
                {
                    listener = allPawns[1];
                }
                
                var memoryManager = Find.World?.GetComponent<MemoryManager>();
                if (memoryManager?.CommonKnowledge == null)
                {
                    return "(Знання про світ відсутні)";
                }
                
                List<KnowledgeScore> matchedScores;
                string keywordKnowledge = memoryManager.CommonKnowledge.InjectKnowledgeWithDetails(
                    matchText,
                    settings.maxInjectedKnowledge,
                    out matchedScores,
                    speaker,  
                    listener   // targetPawn
                );
                
                string dialogueType = GetVariableValue(promptContext, "dialogue.type");
                
                lock (_contextLock)
                {
                    _lastContext = new KnowledgeInjectionContext
                    {
                        MatchText = matchText,
                        DialogueType = dialogueType,
                        KeywordKnowledge = keywordKnowledge,
                        Speaker = speaker,
                        Listener = listener,
                        Tick = Find.TickManager?.TicksGame ?? 0
                    };
                }
                
                if (string.IsNullOrEmpty(keywordKnowledge))
                {
                    return "(Відповідних знань не знайдено)";
                }
                
                return PromptNormalizer.Normalize(keywordKnowledge);
            }
            catch (Exception ex)
            {
                Log.Warning($"[MemoryPatch] Error getting knowledge: {ex.Message}");
                return "";
            }
        }

        public static string GetGroupedKnowledge(object promptContext)
        {
            if (promptContext == null) return "";

            try
            {
                var matchedScores = GetMatchedScores(promptContext);
                if (matchedScores == null || matchedScores.Count == 0)
                    return "(Відповідних знань не знайдено)";

                return PromptNormalizer.Normalize(FormatGroupedKnowledge(matchedScores));
            }
            catch (Exception ex)
            {
                Log.Warning($"[MemoryPatch] Error getting grouped knowledge: {ex.Message}");
                return "";
            }
        }

        public static string GetKnowledgeByCategory(object promptContext, KnowledgeCategory category)
        {
            if (promptContext == null) return "";

            try
            {
                var matchedScores = GetMatchedScores(promptContext);
                if (matchedScores == null || matchedScores.Count == 0)
                    return "";

                var filtered = matchedScores
                    .Where(s => CommonKnowledgeUIHelpers.GetEntryCategory(s.Entry) == category)
                    .ToList();

                if (filtered.Count == 0) return "";

                return PromptNormalizer.Normalize(FormatCategoryEntries(filtered));
            }
            catch (Exception ex)
            {
                Log.Warning($"[MemoryPatch] Error getting knowledge for category {category}: {ex.Message}");
                return "";
            }
        }

        public static string GetKnowledgeRules(object ctx) => GetKnowledgeByCategory(ctx, KnowledgeCategory.Instructions);
        public static string GetKnowledgeLore(object ctx) => GetKnowledgeByCategory(ctx, KnowledgeCategory.Lore);
        public static string GetKnowledgeStatus(object ctx) => GetKnowledgeByCategory(ctx, KnowledgeCategory.PawnStatus);
        public static string GetKnowledgeHistory(object ctx) => GetKnowledgeByCategory(ctx, KnowledgeCategory.History);
        public static string GetKnowledgeOther(object ctx) => GetKnowledgeByCategory(ctx, KnowledgeCategory.Other);

        private static List<KnowledgeScore> _cachedScores;
        private static int _cachedTick = -1;
        private static readonly object _cacheLock = new object();
        
        private static List<KnowledgeScore> GetMatchedScores(object promptContext)
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            
            lock (_cacheLock)
            {
                if (_cachedScores != null && _cachedTick == currentTick)
                    return _cachedScores;
            }
            
            var settings = RimTalkMemoryPatchMod.Settings;

            string matchText = BuildMatchText(promptContext, settings);
            if (string.IsNullOrEmpty(matchText))
                return null;

            Pawn speaker = GetPropertyValue<Pawn>(promptContext, "CurrentPawn");
            Pawn listener = null;
            var allPawns = GetPropertyValue<List<Pawn>>(promptContext, "AllPawns");
            if (allPawns != null && allPawns.Count > 1)
                listener = allPawns[1];

            var memoryManager = Find.World?.GetComponent<MemoryManager>();
            if (memoryManager?.CommonKnowledge == null)
                return null;

            List<KnowledgeScore> matchedScores;
            memoryManager.CommonKnowledge.InjectKnowledgeWithDetails(
                matchText,
                settings.maxInjectedKnowledge,
                out matchedScores,
                speaker,
                listener
            );

            string dialogueType = GetVariableValue(promptContext, "dialogue.type");
            lock (_contextLock)
            {
                _lastContext = new KnowledgeInjectionContext
                {
                    MatchText = matchText,
                    DialogueType = dialogueType,
                    KeywordKnowledge = FormatCategoryEntries(matchedScores),
                    Speaker = speaker,
                    Listener = listener,
                    Tick = currentTick
                };
            }
            
            lock (_cacheLock)
            {
                _cachedScores = matchedScores;
                _cachedTick = currentTick;
            }

            return matchedScores;
        }

        private static string GetCategoryDisplayName(KnowledgeCategory category)
        {
            switch (category)
            {
                case KnowledgeCategory.Instructions: return "规则";
                case KnowledgeCategory.Lore: return "世界观";
                case KnowledgeCategory.PawnStatus: return "殖民者状态";
                case KnowledgeCategory.History: return "历史";
                case KnowledgeCategory.Other: return "其他";
                default: return "未知";
            }
        }

        private static string GetFirstTag(CommonKnowledgeEntry entry)
        {
            if (string.IsNullOrEmpty(entry.tag)) return "";
            var tags = entry.GetTags();
            return tags.Count > 0 ? tags[0] : entry.tag;
        }

        private static string FormatGroupedKnowledge(List<KnowledgeScore> scores)
        {
            if (scores == null || scores.Count == 0) return "";

            var groups = new Dictionary<KnowledgeCategory, List<KnowledgeScore>>();
            foreach (var score in scores)
            {
                var cat = CommonKnowledgeUIHelpers.GetEntryCategory(score.Entry);
                if (!groups.ContainsKey(cat))
                    groups[cat] = new List<KnowledgeScore>();
                groups[cat].Add(score);
            }

            var order = new KnowledgeCategory[]
            {
                KnowledgeCategory.Instructions,
                KnowledgeCategory.Lore,
                KnowledgeCategory.PawnStatus,
                KnowledgeCategory.History,
                KnowledgeCategory.Other
            };

            var sb = new StringBuilder();
            foreach (var cat in order)
            {
                if (!groups.ContainsKey(cat)) continue;
                var entries = groups[cat];

                sb.AppendLine($"## {GetCategoryDisplayName(cat)}");
                foreach (var score in entries)
                {
                    sb.AppendLine($"- {score.Entry.content}");
                }
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatCategoryEntries(List<KnowledgeScore> scores)
        {
            if (scores == null || scores.Count == 0) return "";

            var sb = new StringBuilder();
            foreach (var score in scores)
            {
                sb.AppendLine($"- {score.Entry.content}");
            }
            return sb.ToString().TrimEnd();
        }

        
        private static string BuildMatchText(object promptContext, RimTalkMemoryPatchSettings settings)
        {
            var matchTextBuilder = new StringBuilder();
            var sources = settings.knowledgeMatchingSources;
            
            if (sources == null || sources.Count == 0)
            {
                sources = new List<string> { "prompt" };
            }
            
            var allPawns = GetPropertyValue<List<Pawn>>(promptContext, "AllPawns");
            int pawnCount = allPawns?.Count ?? 0;
            
            foreach (var source in sources)
            {
                if (source.Equals("knowledge", StringComparison.OrdinalIgnoreCase) ||
                    source.StartsWith("knowledge_", StringComparison.OrdinalIgnoreCase) ||
                    source.StartsWith("knowledge.", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                if (MustacheVariableHelper.IsPawnProperty(source))
                {
                    if (allPawns != null)
                    {
                        foreach (var pawn in allPawns)
                        {
                            if (pawn == null) continue;
                            
                            if (MustacheVariableHelper.TryGetPawnPropertyValue(source, pawn, out string value)
                                && !string.IsNullOrEmpty(value))
                            {
                                if (matchTextBuilder.Length > 0)
                                {
                                    matchTextBuilder.AppendLine();
                                }
                                matchTextBuilder.Append(value);
                            }
                        }
                    }
                }
                else
                {
                    string value = GetVariableValue(promptContext, source);
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (matchTextBuilder.Length > 0)
                        {
                            matchTextBuilder.AppendLine();
                        }
                        matchTextBuilder.Append(value);
                    }
                }
            }
            
            return matchTextBuilder.ToString();
        }
        
        private static string GetVariableValue(object ctx, string variableName)
        {
            try
            {
                string directValue = GetDirectContextProperty(ctx, variableName);
                if (!string.IsNullOrEmpty(directValue))
                {
                    return directValue;
                }
                
                string parsedValue = TryParseScribanVariable(ctx, variableName);
                if (!string.IsNullOrEmpty(parsedValue))
                {
                    return parsedValue;
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        private static string GetDirectContextProperty(object ctx, string variableName)
        {
            switch (variableName)
            {
                case "dialogue.prompt":
                case "prompt":
                    return GetPropertyValue<string>(ctx, "DialoguePrompt");
                case "dialogue.type":
                    return GetPropertyValue<string>(ctx, "DialogueType");
                case "dialogue.status":
                    return GetPropertyValue<string>(ctx, "DialogueStatus");
                case "pawn.context":
                case "context":
                    return GetPropertyValue<string>(ctx, "PawnContext");
                default:
                    return null;
            }
        }
        
        private static string TryParseScribanVariable(object ctx, string variableName)
        {
            try
            {
                if (ctx is not PromptContext promptContext)
                    return null;

                string template = "{{" + variableName + "}}";
                string parsed = ScribanParser.Render(template, promptContext, false);
                if (!string.IsNullOrEmpty(parsed) && parsed != template)
                    return parsed;
                return null;
            }
            catch (Exception ex)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning($"[MemoryPatch] TryParseScribanVariable failed: {ex.Message}");
                }
                return null;
            }
        }
        
        
        #region Helper Methods
        
        private static T GetPropertyValue<T>(object obj, string propertyName) where T : class
        {
            if (obj == null) return null;
            
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop != null)
            {
                return prop.GetValue(obj) as T;
            }
            return null;
        }
        
        private static Pawn GetCurrentPawn(object ctx)
        {
            return GetPropertyValue<Pawn>(ctx, "CurrentPawn");
        }
        
        private static Pawn GetPawn2(object ctx)
        {
            var allPawns = GetPropertyValue<List<Pawn>>(ctx, "AllPawns");
            return allPawns?.Count > 1 ? allPawns[1] : null;
        }
        
        private static Map GetMap(object ctx)
        {
            return GetPropertyValue<Map>(ctx, "Map");
        }
        
        #endregion
    }
}
