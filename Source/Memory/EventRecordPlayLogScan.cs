using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory;

internal static class EventRecordPlayLogScan
{
        
        /// <summary>
        /// 每小时扫描PlayLog事件
        /// 生成全局公共殖民地历史常识
        /// ⚠️ v3.4.4: 性能优化 - 从尾部倒序扫描，避免全量遍历
        /// </summary>
        internal static void ScanRecentPlayLog()
        {
            if (!RimTalkMemoryPatchMod.Settings.enableEventRecordKnowledge)
                return;
            
            try
            {
                var gameHistory = Find.PlayLog;
                if (gameHistory == null || gameHistory.AllEntries == null)
                    return;
                
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return;
                
                // ⭐ v3.3.3: 先更新已有事件常识的时间前缀
                EventRecordKnowledgeGenerator.UpdateEventKnowledgeTimePrefix(library);
                
                int processedCount = 0;
                int currentTick = Find.TickManager.TicksGame;
                int hourThreshold = currentTick - GenDate.TicksPerHour;
                
                // ⚠️ v3.4.4: 性能优化关键修复
                // 不再使用 LINQ Where 遍历全部历史，改为从尾部倒序扫描
                // PlayLog.AllEntries 是 List，最新的记录在尾部
                var allEntries = gameHistory.AllEntries;
                int totalCount = allEntries.Count;
                int scannedCount = 0;
                int maxScan = Math.Min(50, totalCount); // 最多扫描50条
                
                // 从尾部（最新）向前遍历
                for (int i = totalCount - 1; i >= 0 && scannedCount < maxScan; i--)
                {
                    try
                    {
                        var logEntry = allEntries[i];
                        if (logEntry == null)
                            continue;
                        
                        scannedCount++;
                        
                        // ⚠️ 关键优化：一旦遇到超过1小时的记录，立即终止
                        if (logEntry.Age < hourThreshold)
                        {
                            // 已经超过时间范围，因为 Age 是递减的，后面的更旧
                            break;
                        }
                        
                        // 使用LogEntry的ID去重
                        int logID = logEntry.GetHashCode();
                        
                        if (EventRecordKnowledgeGenerator.processedLogIDs.Contains(logID))
                            continue;
                        
                        EventRecordKnowledgeGenerator.processedLogIDs.Add(logID);
                        
                        // 控制集合大小
                        if (EventRecordKnowledgeGenerator.processedLogIDs.Count > 2000)
                        {
                            var toRemove = EventRecordKnowledgeGenerator.processedLogIDs.Take(1000).ToList();
                            foreach (var id in toRemove)
                            {
                                EventRecordKnowledgeGenerator.processedLogIDs.Remove(id);
                            }
                        }
                        
                        // 获取事件信息
                        string eventText = ExtractEventInfo(logEntry);
                        
                        if (!string.IsNullOrEmpty(eventText))
                        {
                            // 检查是否已存在
                            bool exists = library.Entries.Any(e => 
                                e.content.Contains(eventText.Substring(0, Math.Min(15, eventText.Length)))
                            );
                            
                            if (!exists)
                            {
                                // 计算重要性
                                float importance = EventRecordPlayLogExtractor.CalculateImportance(eventText);
                                
                                // ⭐ v3.3.3: 提取原始事件文本（移除时间前缀）
                                string originalText = EventRecordPlayLogExtractor.ExtractOriginalEventText(eventText);
                                
                                // ⭐ v3.3.3: 创建事件常识，保存创建时间和原始文本
                                var entry = new CommonKnowledgeEntry("事件,历史", eventText)
                                {
                                    importance = importance,
                                    isEnabled = true,
                                    isUserEdited = false,
                                    creationTick = currentTick,           // ⭐ 设置创建时间戳
                                    originalEventText = originalText      // ⭐ 保存原始文本
                                    // targetPawnId = -1 (默认全局)
                                };
                                
                                library.AddEntry(entry);
                                processedCount++;
                                
                                // ⭐ v3.3.2: 减少日志量 - 仅DevMode且10%概率
                                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                                {
                                    Log.Message($"[EventRecord] Created event knowledge: {eventText.Substring(0, Math.Min(50, eventText.Length))}...");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // ⭐ v3.3.2: 仅在DevMode且随机输出
                        if (Prefs.DevMode && UnityEngine.Random.value < 0.2f)
                        {
                            Log.Warning($"[EventRecord] Error processing log entry: {ex.Message}");
                        }
                    }
                }
                
                // ⭐ v3.3.2: 减少日志量 - 仅DevMode且10%概率
                if (processedCount > 0 && Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    Log.Message($"[EventRecord] Scanned {scannedCount} entries, processed {processedCount} new events (total: {totalCount})");
                }
            }
            catch (Exception ex)
            {
                // ⭐ v3.3.2: 减少日志量，降低错误频率
                if (Prefs.DevMode && UnityEngine.Random.value < 0.2f)
                {
                    Log.Error($"[EventRecord] Error scanning PlayLog: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 从LogEntry提取事件信息
        /// ? v3.3.4: 优化为提取关键信息，减少token消耗
        /// </summary>
        internal static string ExtractEventInfo(LogEntry logEntry)
        {
            if (logEntry == null)
                return null;
            
            try
            {
                // 跳过对话类型的日志（已由RimTalk对话记忆处理）
                if (logEntry.GetType().Name == "PlayLogEntry_Interaction")
                {
                    return null;
                }
                
                // ? 修改：允许处理一些重要的非Incident事件，特别是死亡和葬礼
                if (logEntry.GetType().Name == "PlayLogEntry_Incident")
                {
                    // 对于Incident事件，检查是否是需要补充的事件类型
                    string previewText = logEntry.ToGameStringFromPOV(null, false);
                    if (!string.IsNullOrEmpty(previewText))
                    {
                        // 允许处理：死亡、葬礼、结婚等重要事件（作为IncidentPatch的补充）
                        bool isImportantEvent = 
                            previewText.Contains("死亡") || previewText.Contains("died") || previewText.Contains("killed") || 
                            previewText.Contains("dead") || previewText.Contains("death") ||
                            previewText.Contains("葬礼") || previewText.Contains("葬") || previewText.Contains("埋葬") ||
                            previewText.Contains("结婚") || previewText.Contains("婚礼") || previewText.Contains("married") ||
                            previewText.Contains("生日") || previewText.Contains("birthday") ||
                            previewText.Contains("突破") || previewText.Contains("breakthrough");
                        
                        if (!isImportantEvent)
                        {
                            // 其他Incident事件：跳过，避免重复（IncidentPatch已处理）
                            return null;
                        }
                        // 重要事件：继续处理
                    }
                    else
                    {
                        return null;
                    }
                }
                
                string text = logEntry.ToGameStringFromPOV(null, false);
                
                if (string.IsNullOrEmpty(text))
                    return null;
                
                // 过滤长度
                if (text.Length < 10 || text.Length > 200)
                    return null;
                
                // 过滤无聊事件
                if (EventRecordPlayLogExtractor.IsBoringMessage(text))
                    return null;
                
                // ? 增强关键词检测：检查是否包含重要关键词
                bool hasImportantKeyword = EventRecordKnowledgeGenerator.ImportantKeywords.Keys.Any(k => text.Contains(k));
                
                if (!hasImportantKeyword)
                {
                    // ? 新增：如果没有匹配重要关键词，但这是Incident事件，也记录（宽松模式）
                    if (logEntry.GetType().Name == "PlayLogEntry_Incident")
                    {
                        // 移除调试日志
                        // 对于Incident事件，即使关键词不匹配也记录（但降低重要性）
                    }
                    else
                    {
                        return null; // 非Incident事件必须有关键词匹配
                    }
                }
                
                // ? 过滤对话内容：如果包含对话标记，跳过
                if (EventRecordPlayLogExtractor.IsConversationContent(text))
                    return null;
                
                // ? v3.3.4: 提取关键信息（压缩原文）
                string compressedText = EventRecordPlayLogExtractor.ExtractKeyInformation(text);
                
                if (string.IsNullOrEmpty(compressedText))
                    return null;
                
                // 添加时间前缀
                int ticksAgo = Find.TickManager.TicksGame - logEntry.Age;
                int daysAgo = ticksAgo / GenDate.TicksPerDay;
                
                string timePrefix = "";
                if (daysAgo < 1)
                {
                    timePrefix = "今天";
                }
                else if (daysAgo < 3)
                {
                    timePrefix = $"{daysAgo}天前";
                }
                else if (daysAgo < 7)
                {
                    timePrefix = $"约{daysAgo}天前";
                }
                else
                {
                    return null; // 超过7天的事件不记录
                }
                
                return $"{timePrefix}{compressedText}";
            }
            catch (Exception)
            {
                // ? v3.3.2: 移除调试日志
                // if (Prefs.DevMode) { ... }
                return null;
            }
        }
}
