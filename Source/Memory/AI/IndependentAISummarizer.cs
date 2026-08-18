using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using RimWorld;
using Ustas.RimAI.Communication;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Core.AI;
using Ustas.RimAI.Core.Configuration;
using Ustas.RimAI.Core.Player2;

namespace Ustas.RimAI.Communication.Memory.AI
{
    // DTO 类已提取到独立文件：
    // - DTO/OpenAITypes.cs (OpenAIRequest, OpenAIMessage, CacheControl)
    // - DTO/GeminiTypes.cs (GeminiRequest, GeminiContent, GeminiPart, GeminiGenerationConfig, GeminiThinkingConfig)
    
    public static class IndependentAISummarizer
    {
        // ? v3.3.2.35: 优化正则表达式 - 提升为静态编译字段
        internal static readonly Regex GoogleResponseRegex = new Regex(
            @"""text""\s*:\s*""(.*?)""",
            RegexOptions.Compiled | RegexOptions.Singleline
        );
        
        internal static readonly Regex OpenAIResponseRegex = new Regex(
            @"""content""\s*:\s*""(.*?)""",
            RegexOptions.Compiled | RegexOptions.Singleline
        );
        
        internal static bool isInitialized = false;
        internal static bool useRimTalkAdapter = false;
        internal static string apiKey, apiUrl, model, provider;
        
        // ? 修复1: 添加缓存大小限制，防止内存泄漏
        internal const int MAX_CACHE_SIZE = 100; // 最多缓存100个总结
        internal const int CACHE_CLEANUP_THRESHOLD = 120; // 达到120个时清理
        
        internal static readonly Dictionary<string, string> completedSummaries = new Dictionary<string, string>();
        internal static readonly HashSet<string> pendingSummaries = new HashSet<string>();
        internal static readonly Dictionary<string, List<Action<string>>> callbackMap = new Dictionary<string, List<Action<string>>>();
        internal static readonly Queue<Action> mainThreadActions = new Queue<Action>();

        

        

        

        /// <summary>
        /// ? 修复：添加强制重新初始化方法
        /// </summary>
        
        
        /// <summary>
        /// ? v3.3.3: 清除所有API配置和缓存
        /// </summary>
        
        
        
        
        /// <summary>
        /// ? v3.3.3: 验证API配置
        /// </summary>
        
        
        /// <summary>
        /// 尝试从 RimTalk 加载配置（兼容模式）
        /// </summary>
        

        

        

        

        

        /// <summary>
        /// ? v3.3.2.34: 重构版 - 使用 DTO 类和手动序列化（安全）
        /// 彻底修复特殊字符导致的 JSON 格式错误
        /// </summary>
        
        
        /// <summary>
        /// ? v3.3.2.34: 安全的 JSON 字符串转义
        /// 处理所有特殊字符：引号、换行、反斜杠等
        /// </summary>
        

        

        /// <summary>
        /// ? v3.3.2.35: 优化版 - 使用静态编译的正则表达式
        /// </summary>
        

        

        
        
        
    
        #region Cluster forwards
        public static string ComputeCacheKey(Pawn pawn, List<MemoryEntry> memories) => AISummarizerLifecycle.ComputeCacheKey(pawn, memories);
        public static void RegisterCallback(string cacheKey, Action<string> callback) => AISummarizerLifecycle.RegisterCallback(cacheKey, callback);
        public static void ProcessPendingCallbacks(int maxPerTick = 5) => AISummarizerLifecycle.ProcessPendingCallbacks(maxPerTick);
        public static void ForceReinitialize() => AISummarizerLifecycle.ForceReinitialize();
        public static void ClearAllConfiguration() => AISummarizerLifecycle.ClearAllConfiguration();
        public static void Initialize() => AISummarizerLifecycle.Initialize();
        internal static bool ValidateConfiguration() => AISummarizerLifecycle.ValidateConfiguration();
        internal static bool TryLoadFromRimTalk() => AISummarizerLifecycle.TryLoadFromRimTalk();
        internal static void ApplyDefaultUrlIfMissing() => AISummarizerLifecycle.ApplyDefaultUrlIfMissing();
        public static bool IsAvailable() => AISummarizerLifecycle.IsAvailable();
        public static void TryDetectPlayer2LocalApp() => AISummarizerLifecycle.TryDetectPlayer2LocalApp();
        public static string SummarizeMemories(Pawn pawn, List<MemoryEntry> memories, string promptTemplate) => AISummarizerPrompt.SummarizeMemories(pawn, memories, promptTemplate);
        internal static string BuildPrompt(Pawn pawn, List<MemoryEntry> memories, string template) => AISummarizerPrompt.BuildPrompt(pawn, memories, template);
        internal static string BuildJsonRequest(string prompt) => AISummarizerPrompt.BuildJsonRequest(prompt);
        internal static string EscapeJsonString(string text) => AISummarizerPrompt.EscapeJsonString(text);
        internal static Task<string> CallAIAsync(string prompt) => AISummarizerHttp.CallAIAsync(prompt);
        internal static string ParseResponse(string responseText) => AISummarizerHttp.ParseResponse(responseText);
        internal static string ReadStreamAsText(System.IO.Stream stream) => AISummarizerHttp.ReadStreamAsText(stream);
        internal static Task<string> ReadStreamAsTextAsync(System.IO.Stream stream) => AISummarizerHttp.ReadStreamAsTextAsync(stream);
        #endregion
}
    internal static class AISummarizerLifecycle
    {
public static string ComputeCacheKey(Pawn pawn, List<MemoryEntry> memories)
        {
            var ids = memories.Select(m => m.Id ?? m.Content.GetHashCode().ToString()).ToArray();
            string joinedIds = string.Join("|", ids);
            return $"{pawn.ThingID}_{memories.Count}_{joinedIds.GetHashCode()}";
        }

public static void RegisterCallback(string cacheKey, Action<string> callback)
        {
            lock (IndependentAISummarizer.callbackMap)
            {
                if (!IndependentAISummarizer.callbackMap.TryGetValue(cacheKey, out var callbacks))
                {
                    callbacks = new List<Action<string>>();
                    IndependentAISummarizer.callbackMap[cacheKey] = callbacks;
                }
                callbacks.Add(callback);
            }
        }

public static void ProcessPendingCallbacks(int maxPerTick = 5)
        {
            int processed = 0;
            lock (IndependentAISummarizer.mainThreadActions)
            {
                while (IndependentAISummarizer.mainThreadActions.Count > 0 && processed < maxPerTick)
                {
                    try
                    {
                        IndependentAISummarizer.mainThreadActions.Dequeue()?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[AI Summarizer] Callback error: {ex.Message}");
                    }
                    processed++;
                }
            }
        }

public static void ForceReinitialize()
        {
            IndependentAISummarizer.isInitialized = false;
            Initialize();
        }

public static void ClearAllConfiguration()
        {
            // 清除静态变量
            IndependentAISummarizer.apiKey = "";
            IndependentAISummarizer.apiUrl = "";
            IndependentAISummarizer.model = "";
            IndependentAISummarizer.provider = "";
            IndependentAISummarizer.useRimTalkAdapter = false;
            IndependentAISummarizer.isInitialized = false;
            
            // 清除所有缓存
            lock (IndependentAISummarizer.completedSummaries)
            {
                IndependentAISummarizer.completedSummaries.Clear();
            }
            
            lock (IndependentAISummarizer.pendingSummaries)
            {
                IndependentAISummarizer.pendingSummaries.Clear();
            }
            
            lock (IndependentAISummarizer.callbackMap)
            {
                IndependentAISummarizer.callbackMap.Clear();
            }
            
            lock (IndependentAISummarizer.mainThreadActions)
            {
                IndependentAISummarizer.mainThreadActions.Clear();
            }
            
            Log.Message("[AI] ?? All API configuration and cache cleared");
        }

public static void Initialize()
        {
            try
            {
                var settings = RimTalkMemoryPatchMod.Settings;
                
                // ? 修复：严格按照用户设置决定是否跟随RimTalk
                if (settings.useRimTalkAIConfig)
                {
                    if (TryLoadFromRimTalk())
                    {
                        Log.Message($"[AI] Loaded from RimTalk ({IndependentAISummarizer.provider}/{IndependentAISummarizer.model})");
                        IndependentAISummarizer.isInitialized = true;
                        return;
                    }
                    Log.Warning("[AI] Конфігурацію RimTalk не налаштовано; незалежний fallback вимкнено для inherited mode");
                    IndependentAISummarizer.isInitialized = false;
                    return;
                }
                
                // 使用独立配置
                IndependentAISummarizer.apiKey = settings.independentProvider == "OpenAI"
                    ? AiCredentialResolver.Resolve().Value
                    : settings.independentApiKey;
                IndependentAISummarizer.apiUrl = settings.independentApiUrl;
                IndependentAISummarizer.model = settings.independentModel;
                IndependentAISummarizer.provider = settings.independentProvider;
                
                // ? v3.3.6: Player2 特殊处理 - 优先使用本地应用
                if (IndependentAISummarizer.provider == "Player2")
                {
                    var session = Player2Session.Current.EnsureAuthenticated(new Player2AuthRequest
                    {
                        FallbackApiKey = IndependentAISummarizer.apiKey,
                        RequestGameKey = Player2GameKeys.Memory
                    });
                    if (session.Succeeded)
                    {
                        IndependentAISummarizer.apiKey = session.ApiKey;
                        IndependentAISummarizer.apiUrl = Player2Endpoints.ChatCompletions(session.BaseUrl);
                        Log.Message(session.IsLocal
                            ? "[AI] Using Player2 local app connection"
                            : "[AI] Using Player2 remote API with manual key");
                    }
                    else
                    {
                        Log.Message("[AI] Player2 selected but no key, trying to detect local app...");
                        TryDetectPlayer2LocalApp();
                    }
                }
                
                // 如果 URL 为空，根据提供商设置默认值
                if (string.IsNullOrEmpty(IndependentAISummarizer.apiUrl))
                {
                    if (IndependentAISummarizer.provider == "OpenAI")
                    {
                        IndependentAISummarizer.apiUrl = "https://api.openai.com/v1/chat/completions";
                    }
                    else if (IndependentAISummarizer.provider == "DeepSeek")
                    {
                        IndependentAISummarizer.apiUrl = "https://api.deepseek.com/v1/chat/completions";
                    }
                    else if (IndependentAISummarizer.provider == "Google")
                    {
                        IndependentAISummarizer.apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/MODEL_PLACEHOLDER:generateContent?key=API_KEY_PLACEHOLDER";
                    }
                    else if (IndependentAISummarizer.provider == "Player2")
                    {
                        IndependentAISummarizer.apiUrl = Player2Endpoints.ChatCompletions(Player2EndpointKind.CloudGame);
                    }
                }
                
                // ? 详细验证配置
                if (!ValidateConfiguration())
                {
                    IndependentAISummarizer.isInitialized = false;
                    return;
                }
                
                Log.Message($"[AI] ? Initialized with independent config ({IndependentAISummarizer.provider}/{IndependentAISummarizer.model})");
                Log.Message($"[AI]    Credential source: {(IndependentAISummarizer.provider == "OpenAI" ? AiCredentialResolver.Resolve().Display : "IndependentAISummarizer.provider-specific setting")}");
                Log.Message($"[AI]    API URL: {IndependentAISummarizer.apiUrl}");
                IndependentAISummarizer.isInitialized = true;
            }
            catch (Exception ex)
            {
                Log.Error($"[AI] ? Init failed: {ex.Message}");
                IndependentAISummarizer.isInitialized = false;
            }
        }

internal static bool ValidateConfiguration()
        {
            // 检查API Key
            if (string.IsNullOrEmpty(IndependentAISummarizer.apiKey))
            {
                Log.Error("[AI] ? API Key is empty!");
                Log.Error("[AI]    Налаштуйте доступ у: Параметри → Налаштування модів → RimTalk-Expand Memory → AI");
                return false;
            }
            
            // 检查API Key长度
            if (IndependentAISummarizer.apiKey.Length < 10)
            {
                Log.Error($"[AI] ? API Key too short (length: {IndependentAISummarizer.apiKey.Length})!");
                Log.Error("[AI]    Valid API Keys are usually 20+ characters");
                Log.Error("[AI]    Credential is invalid or too short");
                return false;
            }
            
            // ? v3.3.6: Player2/Custom模式不强制检查格式
            if (IndependentAISummarizer.provider != "Custom" && IndependentAISummarizer.provider != "Player2" && IndependentAISummarizer.provider != "Google")
            {
                // 检查API Key格式（OpenAI/DeepSeek建议以sk-开头，但只是警告）
                if ((IndependentAISummarizer.provider == "OpenAI" || IndependentAISummarizer.provider == "DeepSeek") && !IndependentAISummarizer.apiKey.StartsWith("sk-"))
                {
                    Log.Warning($"[AI] ?? API Key doesn't start with 'sk-' for {IndependentAISummarizer.provider}");
                    Log.Warning("[AI]    Credential format is unusual");
                    Log.Warning("[AI]    If using third-party proxy, select 'Custom' or 'Player2' IndependentAISummarizer.provider");
                }
            }
            
            // 检查API URL
            if (string.IsNullOrEmpty(IndependentAISummarizer.apiUrl))
            {
                Log.Error("[AI] ? API URL is empty!");
                return false;
            }
            
            // 检查Model
            if (string.IsNullOrEmpty(IndependentAISummarizer.model))
            {
                Log.Error("[AI] Назву моделі не налаштовано");
                return false;
            }
            
            return true;
        }

internal static bool TryLoadFromRimTalk()
        {
            try
            {
                var snapshot = SharedTextAiAccess.Current;
                string snapshotProvider = snapshot?.Provider;
                string snapshotModel = snapshot?.Model;
                string snapshotUrl = snapshot?.BaseUrl;
                if (snapshot is { HasActive: true } && !string.IsNullOrEmpty(snapshotModel))
                {
                    IndependentAISummarizer.apiKey = null;
                    IndependentAISummarizer.provider = snapshotProvider ?? "";
                    IndependentAISummarizer.apiUrl = snapshotUrl ?? "";
                    IndependentAISummarizer.model = snapshotModel;
                    if (string.Equals(IndependentAISummarizer.model, "Custom", StringComparison.OrdinalIgnoreCase))
                        IndependentAISummarizer.model = snapshot.CustomModel;
                    ApplyDefaultUrlIfMissing();
                    if (!string.IsNullOrEmpty(IndependentAISummarizer.model))
                    {
                        IndependentAISummarizer.useRimTalkAdapter = true;
                        IndependentAISummarizer.isInitialized = true;
                        Log.Message($"[AI] Loaded from shared text AI ({IndependentAISummarizer.provider}/{IndependentAISummarizer.model})");
                        return true;
                    }
                }

                var config = Settings.Get()?.GetActiveConfig();
                if (config == null) return false;
                IndependentAISummarizer.apiKey = null;
                IndependentAISummarizer.apiUrl = config.BaseUrl;
                IndependentAISummarizer.provider = config.Provider.ToString();
                IndependentAISummarizer.model = config.SelectedModel;
                if (IndependentAISummarizer.model == "Custom")
                    IndependentAISummarizer.model = config.CustomModelName;
                ApplyDefaultUrlIfMissing();
                if (string.IsNullOrEmpty(IndependentAISummarizer.model)) return false;
                IndependentAISummarizer.useRimTalkAdapter = true;
                IndependentAISummarizer.isInitialized = true;
                Log.Message($"[AI] Loaded from Communication ({IndependentAISummarizer.provider}/{IndependentAISummarizer.model})");
                return true;
            }
            catch (Exception ex)
            {
                if (Prefs.DevMode)
                    Log.Warning($"[AI Summarizer] TryLoadFromRimTalk failed: {ex.Message}");
                return false;
            }
        }

internal static void ApplyDefaultUrlIfMissing()
        {
            if (!string.IsNullOrEmpty(IndependentAISummarizer.apiUrl)) return;
            if (!IndependentAISummarizer.useRimTalkAdapter && IndependentAISummarizer.provider == "Google")
            {
                IndependentAISummarizer.apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/MODEL_PLACEHOLDER:generateContent?key=API_KEY_PLACEHOLDER";
                return;
            }

            if (!IndependentAISummarizer.useRimTalkAdapter && IndependentAISummarizer.provider == "Player2")
            {
                IndependentAISummarizer.apiUrl = Player2Endpoints.ChatCompletions(Player2EndpointKind.CloudGame);
                return;
            }

            IndependentAISummarizer.apiUrl = GameplayTextAiProviderCatalog.ChatEndpoint(IndependentAISummarizer.provider);
        }

public static bool IsAvailable()
        {
            if (!IndependentAISummarizer.isInitialized) Initialize();
            return IndependentAISummarizer.isInitialized;
        }

public static void TryDetectPlayer2LocalApp()
        {
            Task.Run(() =>
            {
                try
                {
                    Log.Message("[AI] Checking for local Player2 app...");
                    var session = Player2Session.Current.EnsureAuthenticated(new Player2AuthRequest
                    {
                        RequestGameKey = Player2GameKeys.Memory
                    });
                    if (session.Succeeded && session.IsLocal)
                    {
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            Messages.Message("RimTalk_Settings_Player2Detected".Translate(), MessageTypeDefOf.PositiveEvent, false);
                        });
                        return;
                    }

                    Log.Message("[AI] Player2 local app not found, will use remote API");
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        Messages.Message("RimTalk_Settings_Player2NotFound".Translate(), MessageTypeDefOf.NeutralEvent, false);
                    });
                }
                catch (Exception ex)
                {
                    Log.Warning($"[AI] Player2 detection error: {ex.Message}");
                }
            });
        }
    }

    internal static class AISummarizerPrompt
    {
public static string SummarizeMemories(Pawn pawn, List<MemoryEntry> memories, string promptTemplate)
        {
            if (!IndependentAISummarizer.IsAvailable()) return null;

            string cacheKey = IndependentAISummarizer.ComputeCacheKey(pawn, memories);

            lock (IndependentAISummarizer.completedSummaries)
            {
                if (IndependentAISummarizer.completedSummaries.TryGetValue(cacheKey, out string summary))
                {
                    return summary; // Return cached result directly if available
                }
            }

            lock (IndependentAISummarizer.pendingSummaries)
            {
                if (IndependentAISummarizer.pendingSummaries.Contains(cacheKey)) return null; // Already processing
                IndependentAISummarizer.pendingSummaries.Add(cacheKey);
            }

            string prompt = BuildPrompt(pawn, memories, promptTemplate);

            Task.Run(async () =>
            {
                try
                {
                    string result = await IndependentAISummarizer.CallAIAsync(prompt);
                    if (result != null)
                    {
                        lock (IndependentAISummarizer.completedSummaries)
                        {
                            // ? 修改1: 增加缓存上限，防止内存泄漏
                            if (IndependentAISummarizer.completedSummaries.Count >= IndependentAISummarizer.CACHE_CLEANUP_THRESHOLD)
                            {
                                // ? v3.3.2.29: 确定性清理 - 按 key 字母顺序升序排序后删除前50%
                                // 使用字母顺序排序代替随机 Take()，确保相同的缓存状态总是删除相同的条目
                                var toRemove = IndependentAISummarizer.completedSummaries.Keys
                                    .OrderBy(k => k, StringComparer.Ordinal) // 字母顺序升序
                                    .Take(IndependentAISummarizer.MAX_CACHE_SIZE / 2)
                                    .ToList();
                                
                                foreach (var key in toRemove)
                                {
                                    IndependentAISummarizer.completedSummaries.Remove(key);
                                }
                                

                                if (Prefs.DevMode)
                                {
                                    Log.Message($"[AI Summarizer] ?? Cleaned cache: {toRemove.Count} entries removed (deterministic by key order), {IndependentAISummarizer.completedSummaries.Count} remaining");
                                }
                            }
                            
                            IndependentAISummarizer.completedSummaries[cacheKey] = result;
                        }
                        lock (IndependentAISummarizer.callbackMap)
                        {
                            if (IndependentAISummarizer.callbackMap.TryGetValue(cacheKey, out var callbacks))
                            {
                                foreach (var cb in callbacks)
                                {
                                    lock (IndependentAISummarizer.mainThreadActions)
                                    {
                                        IndependentAISummarizer.mainThreadActions.Enqueue(() => cb(result));
                                    }
                                }
                                IndependentAISummarizer.callbackMap.Remove(cacheKey);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[AI Summarizer] Task failed: {ex.Message}");
                }
                finally
                {
                    lock (IndependentAISummarizer.pendingSummaries)
                    {
                        IndependentAISummarizer.pendingSummaries.Remove(cacheKey);
                    }
                }
            });

            return null; // Indicates that the process is async
        }

internal static string BuildPrompt(Pawn pawn, List<MemoryEntry> memories, string template)
        {
            var settings = RimTalkMemoryPatchMod.Settings;
            
            // 构建记忆列表
            var memoryListSb = new StringBuilder();
            int maxMemories = (template == "deep_archive") ? 15 : 20;
            int i = 1;
            foreach (var m in memories.OrderBy(m => m.GameTick))
            {
                memoryListSb.AppendLine($"{i}. {m.Content}");
                i++;
            }
            string memoryList = memoryListSb.ToString().TrimEnd();
            
            // 使用自定义提示词或默认提示词
            string promptTemplate;
            
            if (template == "deep_archive")
            {
                // 深度归档
                if (!string.IsNullOrEmpty(settings.deepArchivePrompt))
                {
                    // 使用自定义提示词
                    promptTemplate = settings.deepArchivePrompt;
                }
                else
                {
                    // 使用默认提示词
                    promptTemplate = 
                        "Архів спогадів колоніста {0}\n\n" +
                        "Список спогадів\n" +
                        "{1}\n\n" +
                        "Виділи основні риси та ключові події\n" +
                        "Об'єднай подібний досвід і підкресли довгострокові тенденції\n" +
                        "Виклади вкрай стисло, не більше 60 слів\n" +
                        "Виведи лише текст підсумку без додаткового форматування";
                }
            }
            else
            {
                // 每日总结
                if (!string.IsNullOrEmpty(settings.dailySummaryPrompt))
                {
                    // 使用自定义提示词
                    promptTemplate = settings.dailySummaryPrompt;
                }
                else
                {
                    // 使用默认提示词
                    promptTemplate = 
                        "Підсумок спогадів колоніста {0}\n\n" +
                        "Список спогадів\n" +
                        "{1}\n\n" +
                        "Виділи місця, персонажів і події\n" +
                        "Об'єднай подібні події та зазнач їхню частоту\n" +
                        "Виклади вкрай стисло, не більше 80 слів\n" +
                        "Виведи лише текст підсумку без додаткового форматування";
                }
            }
            
            // ⭐ 修复：先转义花括号，防止 string.Format 报错
            // 将用户自定义提示词中的 { 和 } 转义为 {{ 和 }}
            string escapedTemplate = promptTemplate.Replace("{", "{{").Replace("}", "}}");
            
            // 然后把占位符 {{{0}}} 和 {{{1}}} 替换回 {0} 和 {1}
            escapedTemplate = escapedTemplate.Replace("{{0}}", "{0}").Replace("{{1}}", "{1}");
            
            // 替换占位符
            string result = string.Format(escapedTemplate, pawn.LabelShort, memoryList);
            
            return result;
        }

internal static string BuildJsonRequest(string prompt)
        {
            bool isGoogle = (IndependentAISummarizer.provider == "Google");
            var settings = RimTalkMemoryPatchMod.Settings;
            bool enableCaching = settings != null && settings.enablePromptCaching;
            
            if (isGoogle)
            {
                // ? Google Gemini 格式
                string escapedPrompt = EscapeJsonString(prompt);
                
                var sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"contents\":[{");
                sb.Append("\"parts\":[{");
                sb.Append($"\"text\":\"{escapedPrompt}\"");
                sb.Append("}]");
                sb.Append("}],");
                sb.Append("\"generationConfig\":{");
                sb.Append("\"temperature\":0.7,");
                int maxTokens = settings != null ? settings.summaryMaxTokens : 200;
                sb.Append($"\"maxOutputTokens\":{maxTokens}");
                
                if (IndependentAISummarizer.model.Contains("flash"))
                {
                    sb.Append(",\"thinkingConfig\":{\"thinkingBudget\":0}");
                }
                
                sb.Append("}");
                sb.Append("}");
                
                return sb.ToString();
            }
            else
            {
                // ? OpenAI/DeepSeek/Player2/Custom - 统一使用OpenAI兼容格式
                
                // 固定的系统指令（可缓存）
                string systemPrompt = "Ти — помічник зі стислого узагальнення спогадів колонії RimWorld.\n" +
                                    "Узагальнюй вміст спогадів гранично лаконічно.\n" +
                                    "Виводь лише текст підсумку без додаткового форматування.";
                
                string escapedSystem = EscapeJsonString(systemPrompt);
                string escapedPrompt = EscapeJsonString(prompt);
                
                var sb = new StringBuilder();
                sb.Append("{");
                sb.Append($"\"IndependentAISummarizer.model\":\"{IndependentAISummarizer.model}\",");
                sb.Append("\"messages\":[");
                
                // system消息（带缓存控制）
                sb.Append("{\"role\":\"system\",");
                sb.Append($"\"content\":\"{escapedSystem}\"");
                
                if (enableCaching)
                {
                    // ? OpenAI/Custom/Player2 都尝试使用 cache_control
                    if ((IndependentAISummarizer.provider == "OpenAI" || IndependentAISummarizer.provider == "Custom" || IndependentAISummarizer.provider == "Player2") && 
                        (IndependentAISummarizer.model.Contains("gpt-4") || IndependentAISummarizer.model.Contains("gpt-3.5")))
                    {
                        // OpenAI Prompt Caching
                        sb.Append(",\"cache_control\":{\"type\":\"ephemeral\"}");
                    }
                    else if (IndependentAISummarizer.provider == "DeepSeek")
                    {
                        // DeepSeek缓存控制
                        sb.Append(",\"cache\":true");
                    }
                }
                
                sb.Append("},");
                
                // user消息（变化的内容）
                sb.Append("{\"role\":\"user\",");
                sb.Append($"\"content\":\"{escapedPrompt}\"");
                sb.Append("}],");
                
                sb.Append("\"temperature\":0.7,");
                int maxTokens = settings != null ? settings.summaryMaxTokens : 200;
                sb.Append($"\"max_tokens\":{maxTokens}");

                
                if (enableCaching && IndependentAISummarizer.provider == "DeepSeek")
                {
                    sb.Append(",\"enable_prompt_cache\":true");
                }
                
                sb.Append("}");
                
                return sb.ToString();
            }
        }

internal static string EscapeJsonString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            
            var sb = new StringBuilder(text.Length + 20);
            
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    default:
                        // 其他控制字符使用 Unicode 转义
                        if (c < 32)
                        {
                            sb.Append("\\u");
                            sb.Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            
            return sb.ToString();
        }
    }

    internal static class AISummarizerHttp
    {
internal static async Task<string> CallAIAsync(string prompt)
        {
            if (IndependentAISummarizer.useRimTalkAdapter)
            {
                var client = await global::Ustas.RimAI.Communication.Client.AIClientFactory.GetAIClientAsync();
                if (client == null) return null;
                var payload = await client.GetChatCompletionAsync(
                    new List<(global::Ustas.RimAI.Communication.Data.Role role, string message)>(),
                    new List<(global::Ustas.RimAI.Communication.Data.Role role, string message)>
                    {
                        (global::Ustas.RimAI.Communication.Data.Role.User, prompt)
                    });
                return payload?.Response;
            }

            if (IndependentAISummarizer.provider == "OpenAI")
            {
                var shared = await Task.Run(() => SharedTextAiOrchestrator.Complete(new TextAiRequest
                {
                    Messages = new[] { new TextAiMessage("user", prompt) },
                    Model = IndependentAISummarizer.model,
                    ApiShape = TextAiApiShape.Responses,
                    UseSharedGameplayCredential = true,
                    Caller = "memory-summarizer",
                    Arbitration = AiRequestMetadata.FromCaller("memory-summarizer")
                }));
                return shared.Succeeded ? shared.Text : null;
            }

            if (IndependentAISummarizer.provider != "Google")
            {
                var shared = await Task.Run(() => SharedTextAiOrchestrator.Complete(new TextAiRequest
                {
                    Messages = new[] { new TextAiMessage("user", prompt) },
                    Model = IndependentAISummarizer.model,
                    BaseUrl = IndependentAISummarizer.apiUrl,
                    ApiKey = IndependentAISummarizer.apiKey,
                    UseSharedGameplayCredential = false,
                    ApiShape = TextAiApiShape.ChatCompletions,
                    PrebuiltJson = IndependentAISummarizer.BuildJsonRequest(prompt),
                    Caller = "memory-summarizer",
                    Arbitration = AiRequestMetadata.FromCaller("memory-summarizer")
                }));
                if (!shared.Succeeded)
                    return null;
                return ParseResponse(shared.RawPayload) ?? shared.Text;
            }

            const int MAX_RETRIES = 3;
            const int RETRY_DELAY_MS = 2000; // 2秒重试延迟
            
            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                try
                {
                    string actualUrl = IndependentAISummarizer.apiUrl;
                    if (IndependentAISummarizer.provider == "Google")
                    {
                        actualUrl = IndependentAISummarizer.apiUrl.Replace("MODEL_PLACEHOLDER", IndependentAISummarizer.model).Replace("API_KEY_PLACEHOLDER", IndependentAISummarizer.apiKey);
                    }

                    if (attempt > 1)
                    {
                        Log.Message($"[AI Summarizer] Retry attempt {attempt}/{MAX_RETRIES}...");
                    }
                    else
                    {
                        Log.Message($"[AI Summarizer] Calling API: {actualUrl.Substring(0, Math.Min(60, actualUrl.Length))}...");
                        Log.Message($"[AI Summarizer]   Provider: {IndependentAISummarizer.provider}");
                        Log.Message($"[AI Summarizer]   Model: {IndependentAISummarizer.model}");
                        Log.Message($"[AI Summarizer]   Credential source: {(IndependentAISummarizer.provider == "OpenAI" ? AiCredentialResolver.Resolve().Display : "IndependentAISummarizer.provider-specific setting")}");
                    }

                    var request = (HttpWebRequest)WebRequest.Create(actualUrl);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    
                    // ? v3.3.3: Google API不使用Bearer token（Key在URL中）
                    if (IndependentAISummarizer.provider != "Google")
                    {
                        if (IndependentAISummarizer.provider == "Player2")
                        {
                            foreach (var header in Player2Session.Current.AuthHeaders(Player2GameKeys.Memory))
                                request.Headers[header.Key] = header.Value;
                        }
                        else
                        {
                            request.Headers["Authorization"] = $"Bearer {IndependentAISummarizer.apiKey}";
                        }
                    }
                    
                    // ? 增加超时时间到120秒（2分钟）
                    request.Timeout = 120000; // 原来是30000（30秒）

                    string json = IndependentAISummarizer.BuildJsonRequest(prompt);

                    // 显式使用更宽容的转码方式
                    var tolerantUtf8 = new UTF8Encoding(false, false);
                    byte[] bodyRaw;
                    try
                    {
                        bodyRaw = tolerantUtf8.GetBytes(json);
                    }
                    catch
                    {
                        // 如果依然报错，使用正则表达式强制剔除无效的代理对字符(Surrogate pairs)
                        string safeJson = Regex.Replace(json, @"\p{Cs}", "?");
                        bodyRaw = Encoding.UTF8.GetBytes(safeJson);
                    }

                    request.ContentLength = bodyRaw.Length;

                    using (var stream = await request.GetRequestStreamAsync())
                    {
                        await stream.WriteAsync(bodyRaw, 0, bodyRaw.Length);
                    }

                    using (var response = (HttpWebResponse)await request.GetResponseAsync())
                    {
                        string responseText = await ReadStreamAsTextAsync(response.GetResponseStream());
                        
                        // ? v3.3.7: 添加响应接收确认日志
                        Log.Message($"[AI Summarizer] ✅ Response received, length: {responseText.Length} chars");
                        
                        string result = ParseResponse(responseText);
                        
                        // ? v3.3.7: 添加解析结果确认日志
                        if (result != null)
                        {
                            Log.Message($"[AI Summarizer] ✅ Parse successful, result length: {result.Length} chars");
                        }
                        else
                        {
                            Log.Warning($"[AI Summarizer] ⚠️ Parse returned null!");
                        }
                        
                        if (attempt > 1)
                        {
                            Log.Message($"[AI Summarizer] ? Retry successful on attempt {attempt}");
                        }
                        
                        return result;
                    }
                }
                catch (WebException ex)
                {
                    bool shouldRetry = false;
                    string errorDetail = "";
                    HttpStatusCode statusCode = 0; // ? v3.3.3: 保存状态码到外部变量
                    


	                if (ex.Response != null)
	                {
	                    using (var errorResponse = (HttpWebResponse)ex.Response)
	                    {
	                        statusCode = errorResponse.StatusCode; // ? 保存状态码
	                        string errorText = ReadStreamAsText(errorResponse.GetResponseStream());
	                        
	                        // ? v3.3.3: 根据错误类型显示完整或截断的错误信息
	                        if (errorResponse.StatusCode == HttpStatusCode.Unauthorized || // 401
	                            errorResponse.StatusCode == HttpStatusCode.Forbidden)      // 403
	                        {
	                            // 认证错误：显示完整错误信息（帮助调试）
	                            errorDetail = errorText;
	                            Log.Error($"[AI Summarizer] ? Authentication Error ({errorResponse.StatusCode}):");
	                            Log.Error("[AI Summarizer]    Credential rejected by IndependentAISummarizer.provider");
	                            Log.Error($"[AI Summarizer]    Provider: {IndependentAISummarizer.provider}");
	                            Log.Error($"[AI Summarizer]    Response: {errorText}");
	                            Log.Error("[AI Summarizer] ");
	                            Log.Error("[AI Summarizer] ?? Possible solutions:");
	                            Log.Error("[AI Summarizer]    1. Check if API Key is correct");
	                            Log.Error("[AI Summarizer]    2. Verify Provider selection matches your key");
	                            Log.Error("[AI Summarizer]    3. Check if API Key has sufficient credits");
	                            Log.Error("[AI Summarizer]    4. Try regenerating your API Key");
	                        }
	                        else
	                        {
	                            // 其他错误：截断显示
	                            errorDetail = errorText.Substring(0, Math.Min(200, errorText.Length));
	                        }
	                        
	                        // 判断是否应该重试
	                        if (errorResponse.StatusCode == HttpStatusCode.ServiceUnavailable || // 503
	                            errorResponse.StatusCode == (HttpStatusCode)429 ||              // Too Many Requests
	                            errorResponse.StatusCode == HttpStatusCode.GatewayTimeout ||    // 504
	                            errorText.Contains("overloaded") ||
	                            errorText.Contains("UNAVAILABLE"))
	                        {
	                            shouldRetry = true;
	                        }
	                        
	                        if (errorResponse.StatusCode != HttpStatusCode.Unauthorized &&
	                            errorResponse.StatusCode != HttpStatusCode.Forbidden)
	                        {
	                            Log.Warning($"[AI Summarizer] ?? API Error (attempt {attempt}/{MAX_RETRIES}): {errorResponse.StatusCode} - {errorDetail}");
	                        }
	                    }
	                }
	                else
	                {
	                    errorDetail = ex.Message;
	                    Log.Warning($"[AI Summarizer] ?? Network Error (attempt {attempt}/{MAX_RETRIES}): {errorDetail}");
	                    shouldRetry = true; // 网络错误也重试
	                }
	                
	                // 如果是最后一次尝试或不应该重试，则失败
	                if (attempt >= MAX_RETRIES || !shouldRetry)
	                {
	                    // ? v3.3.3: 使用保存的状态码判断
	                    if (statusCode != HttpStatusCode.Unauthorized && 
	                        statusCode != HttpStatusCode.Forbidden)
	                    {
	                        Log.Error($"[AI Summarizer] ? Failed after {attempt} attempts. Last error: {errorDetail}");
	                    }
	                    return null;
	                }
	                
	                // 等待后重试
	                await Task.Delay(RETRY_DELAY_MS * attempt); // 递增延迟：2s, 4s, 6s
                }
                catch (Exception ex)
                {
                    Log.Error($"[AI Summarizer] ? Unexpected error: {ex.GetType().Name} - {ex.Message}");
                    Log.Error($"[AI Summarizer]    Stack trace: {ex.StackTrace}");
                    return null;
                }
            }
            
            return null;
        }

internal static string ParseResponse(string responseText)
        {
            // ? v3.3.7: 方法入口日志（确保方法被调用）
            Log.Message($"[AI Summarizer] 🔍 ParseResponse called, IndependentAISummarizer.provider={IndependentAISummarizer.provider}");
            
            try
            {
                // ? 调试日志：输出完整响应
                Log.Message($"[AI Summarizer] Full API Response (Length: {responseText.Length}):\n{responseText}");

                // 必须配合之前给你的那个能跳过转义引号的正则
                var regex = IndependentAISummarizer.provider == "Google" ? IndependentAISummarizer.GoogleResponseRegex : IndependentAISummarizer.OpenAIResponseRegex;

                // ⭐ 核心修改：使用 Matches (复数) 抓取所有数据包
                var matches = regex.Matches(responseText);
                
                Log.Message($"[AI Summarizer] Regex matched {matches.Count} fragments");

                if (matches.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (Match match in matches)
                    {
                        string fragment = match.Groups[1].Value;
                        // 把每个数据包里的碎片拼起来
                        sb.Append(fragment);
                    }
                    // 拼完之后再统一反转义
                    string result = Regex.Unescape(sb.ToString());
                    Log.Message($"[AI Summarizer] Final parsed result: {result}");
                    return result;
                }
                else
                {
                    Log.Warning("[AI Summarizer] No matches found in response text!");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[AI Summarizer] ?? Parse error: {ex.Message}");
            }
            return null;
        }

internal static string ReadStreamAsText(System.IO.Stream stream)
        {
            if (stream == null) return "";
            
            using (var memoryStream = new System.IO.MemoryStream())
            {
                stream.CopyTo(memoryStream);
                byte[] bytes = memoryStream.ToArray();
                
                try
                {
                    return Encoding.UTF8.GetString(bytes);
                }
                catch (ArgumentException)
                {
                    Log.Warning("[AI Summarizer] Failed to decode stream as UTF-8, falling back to default encoding.");
                    return Encoding.Default.GetString(bytes);
                }
            }
        }

internal static async Task<string> ReadStreamAsTextAsync(System.IO.Stream stream)
        {
            if (stream == null) return "";
            
            using (var memoryStream = new System.IO.MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                byte[] bytes = memoryStream.ToArray();
                
                try
                {
                    return Encoding.UTF8.GetString(bytes);
                }
                catch (ArgumentException)
                {
                    Log.Warning("[AI Summarizer] Failed to decode stream as UTF-8, falling back to default encoding.");
                    return Encoding.Default.GetString(bytes);
                }
            }
        }
    }

}
