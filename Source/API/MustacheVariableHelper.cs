using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.API;
using Ustas.RimAI.Communication.Prompt;
using Ustas.RimAI.Communication.Memory.Diagnostics;

namespace Ustas.RimAI.Communication.Memory.API
{
    public static class MustacheVariableHelper
    {
        #region 缓存
        
        private static Dictionary<string, List<(string name, string description)>> _cachedVariables;
        private static HashSet<string> _cachedPawnProperties;
        
        #endregion
        
        #region 初始化
        
        #endregion

        #region 公共 API
        
        public static Dictionary<string, List<(string name, string description)>> GetBuiltinVariables()
        {
            if (_cachedVariables != null) return _cachedVariables;
            try
            {
                _cachedVariables = VariableDefinitions.GetScribanVariables();
                if (_cachedVariables != null && _cachedVariables.Count > 0)
                    return _cachedVariables;
            }
            catch (Exception ex)
            {
                Log.Warning($"[MemoryPatch] Failed to get builtin variables: {ex.Message}");
            }

            _cachedVariables = GetFallbackVariables();
            return _cachedVariables;
        }
        
        public static Dictionary<string, List<(string name, string description, bool isPawnProperty)>> GetCategorizedMatchingSources()
        {
            var result = new Dictionary<string, List<(string, string, bool)>>();
            var builtins = GetBuiltinVariables();
            
            foreach (var category in builtins)
            {
                var items = new List<(string, string, bool)>();
                
                foreach (var v in category.Value)
                {
                    if (v.name == "json.format" || v.name == "chat.history" || v.name.StartsWith("#"))
                        continue;
                    
                    if (v.name == "knowledge" || v.name.StartsWith("knowledge."))
                        continue;
                    
                    bool isPawn = v.name.StartsWith("pawn.") && !v.name.StartsWith("pawn.memory");
                    string varName = isPawn ? v.name.Substring(5) : v.name;
                    
                    items.Add((varName, v.description, isPawn));
                }
                
                if (items.Count > 0)
                {
                    result[category.Key] = items;
                }
            }
            
            return result;
        }
        
        public static bool IsPawnProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return false;
            
            if (_cachedPawnProperties == null)
            {
                _cachedPawnProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                var builtins = GetBuiltinVariables();
                foreach (var category in builtins)
                {
                    foreach (var v in category.Value)
                    {
                        if (v.name.StartsWith("pawn.") && !v.name.StartsWith("pawn.memory"))
                        {
                            _cachedPawnProperties.Add(v.name.Substring(5));
                        }
                    }
                }
            }
            
            return _cachedPawnProperties.Contains(propertyName);
        }
        
        public static bool TryGetPawnPropertyValue(string propertyName, Pawn pawn, out string value)
        {
            value = null;
            if (pawn == null || string.IsNullOrEmpty(propertyName)) return false;
            
            try
            {
                if (ContextHookRegistry.TryGetPawnVariable(propertyName, pawn, out value) && !string.IsNullOrEmpty(value))
                    return true;

                var ctx = new PromptContext(pawn);
                string template = "{{pawn." + propertyName + "}}";
                string parsed = ScribanParser.Render(template, ctx, false);
                if (!string.IsNullOrEmpty(parsed) && parsed != template)
                {
                    value = parsed;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning($"[MemoryPatch] TryGetPawnPropertyValue failed: {ex.Message}");
                }
                return false;
            }
        }
        
        public static void ClearCache()
        {
            _cachedVariables = null;
            _cachedPawnProperties = null;
        }
        
        #endregion
        
        #region 内部方法
        
        private static Dictionary<string, List<(string, string)>> ConvertDictionaryResult(object result)
        {
            var converted = new Dictionary<string, List<(string, string)>>();
            if (result == null) return converted;
            
            try
            {
                if (result is Dictionary<string, List<(string, string)>> typedResult)
                {
                    return typedResult;
                }
                
                if (result is System.Collections.IDictionary dict)
                {
                    foreach (var key in dict.Keys)
                    {
                        string keyStr = key?.ToString() ?? "";
                        if (string.IsNullOrEmpty(keyStr)) continue;
                        
                        var value = dict[key];
                        if (value is System.Collections.IEnumerable list)
                        {
                            var tuples = new List<(string, string)>();
                            foreach (var item in list)
                            {
                                var itemType = item.GetType();
                                string item1 = itemType.GetField("Item1")?.GetValue(item)?.ToString() ?? "";
                                string item2 = itemType.GetField("Item2")?.GetValue(item)?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(item1))
                                {
                                    tuples.Add((item1, item2));
                                }
                            }
                            if (tuples.Count > 0)
                            {
                                converted[keyStr] = tuples;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[MemoryPatch] Failed to convert result: {ex.Message}");
            }
            
            return converted;
        }
        
        #endregion
        
        #region Fallback
        
        private static Dictionary<string, List<(string, string)>> GetFallbackVariables()
        {
            return new Dictionary<string, List<(string, string)>>
            {
                ["Context"] = new List<(string, string)>
                {
                    ("prompt", "Full dialogue prompt"),
                    ("context", "Pawn context"),
                    ("ctx.DialogueType", "Dialogue type"),
                    ("ctx.DialogueStatus", "Dialogue status")
                },
                ["Pawn Shorthands"] = new List<(string, string)>
                {
                    ("pawn.name", "Name"),
                    ("pawn.backstory", "Backstory"),
                    ("pawn.traits", "Traits"),
                    ("pawn.mood", "Mood"),
                    ("pawn.job", "Current job"),
                    ("pawn.health", "Health"),
                    ("pawn.skills", "Skills")
                },
                ["System"] = new List<(string, string)>
                {
                    ("hour", "Current hour"),
                    ("season", "Current season"),
                    ("weather", "Current weather")
                }
            };
        }
        
        #endregion
        
        #region 兼容性 API
        
        public static List<(string name, string category, string description)> GetFlattenedVariables()
        {
            var result = new List<(string, string, string)>();
            foreach (var category in GetBuiltinVariables())
            {
                foreach (var v in category.Value)
                {
                    result.Add((v.name, category.Key, v.description));
                }
            }
            return result;
        }
        
        public static List<(string propertyName, string description, bool isPawnProperty)> GetMatchingPropertyCategories()
        {
            var result = new List<(string, string, bool)>();
            foreach (var category in GetCategorizedMatchingSources())
            {
                result.AddRange(category.Value);
            }
            return result;
        }
        
        public static List<(string name, string description)> GetMatchingSourceVariables()
        {
            var result = new List<(string, string)>();
            foreach (var category in GetBuiltinVariables())
            {
                foreach (var v in category.Value)
                {
                    if (!v.name.StartsWith("#") && v.name != "json.format" && v.name != "chat.history")
                    {
                        result.Add(v);
                    }
                }
            }
            return result;
        }
        
        public static List<(string name, string description)> GetExtensionPawnVariables()
        {
            var result = new List<(string, string)>();
            try
            {
                foreach (var item in ContextHookRegistry.GetAllCustomVariables())
                {
                    if (item.Type != "Pawn" || string.IsNullOrEmpty(item.Name))
                        continue;
                    string name = item.Name;
                    if (name.StartsWith("pawn.", StringComparison.OrdinalIgnoreCase))
                        name = name.Substring(5);
                    result.Add((name, item.Description));
                }
            }
            // RimAI.catch-boundary: ALLOWED_TOP_LEVEL_BOUNDARY - a variable source was skipped from the suggestion list
            catch (System.Exception ex)
            {
                ModuleLog.Message("[RimAI.Memory] a variable source was skipped from the suggestion list: " + ex.Message);
            }

            return result;
        }

        public static List<(string name, string description)> GetExtensionContextVariables()
        {
            var result = new List<(string, string)>();
            try
            {
                foreach (var item in ContextHookRegistry.GetAllCustomVariables())
                {
                    if (item.Type == "Context" && !string.IsNullOrEmpty(item.Name))
                        result.Add((item.Name, item.Description));
                }
            }
            // RimAI.catch-boundary: ALLOWED_TOP_LEVEL_BOUNDARY - a variable source was skipped from the suggestion list
            catch (System.Exception ex)
            {
                ModuleLog.Message("[RimAI.Memory] a variable source was skipped from the suggestion list: " + ex.Message);
            }

            return result;
        }
        
        #endregion
    }
}