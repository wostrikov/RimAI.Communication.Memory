using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory
{
    /// <summary>
    /// 事件记录常识生成器 (PlayLog扫描 - 补充监听)
    /// ? 职责：捕获IncidentPatch无法监听的事件
    /// - 死亡通知（非Incident触发的死亡）
    /// - 关系变化细节（包含参与者名字）
    /// - 其他重要日志（兜底机制）
    /// </summary>
    public static class EventRecordKnowledgeGenerator
    {
        // 已处理的记录ID（防止重复）
        internal static HashSet<int> processedLogIDs = new HashSet<int>();
        
        // 重要事件关键词（优先级）
        internal static readonly Dictionary<string, float> ImportantKeywords = new Dictionary<string, float>
        {
            // 死亡相关（最重要1.0）
            { "死亡", 1.0f }, { "倒下", 1.0f }, { "被杀", 1.0f }, { "击杀", 1.0f }, { "牺牲", 1.0f },
            { "died", 1.0f }, { "killed", 1.0f }, { "death", 1.0f }, { "dead", 1.0f },
            
            // 战斗相关（重要性0.9）
            { "袭击", 0.9f }, { "进攻", 0.9f }, { "防御", 0.9f }, { "raid", 0.9f }, { "attack", 0.9f },
            { "击退", 0.85f }, { "战胜", 0.85f }, { "defeated", 0.85f },
            
            // ? 新增：葬礼相关（重要性0.9）
            { "葬礼", 0.9f }, { "葬", 0.9f }, { "埋葬", 0.9f }, { "funeral", 0.9f }, { "burial", 0.9f },
            { "举行葬礼", 0.9f }, { "安葬", 0.9f },
            
            // 关系相关（重要性0.85）
            { "结婚", 0.85f }, { "订婚", 0.85f }, { "married", 0.85f }, { "engaged", 0.85f },
            { "婚礼", 0.85f }, { "wedding", 0.85f }, { "举行婚礼", 0.85f },
            { "分手", 0.75f }, { "离婚", 0.75f }, { "breakup", 0.75f },
            
            // ? 新增：生日相关（重要性0.7）
            { "生日", 0.7f }, { "birthday", 0.7f }, { "庆祝", 0.6f }, { "celebration", 0.6f },
            { "过生日", 0.7f }, { "庆祝生日", 0.7f },
            
            // ? 新增：研究突破（重要性0.8）
            { "突破", 0.8f }, { "breakthrough", 0.8f }, { "完成研究", 0.8f }, { "research complete", 0.8f },
            { "研究完成", 0.8f }, { "发明", 0.8f }, { "invention", 0.8f },
            
            // ? 新增：周年纪念（重要性0.7）
            { "周年", 0.7f }, { "anniversary", 0.7f }, { "周年纪念", 0.7f },
            
            // 成员变动（重要性0.8）
            { "加入", 0.8f }, { "逃跑", 0.8f }, { "离开", 0.8f }, { "joined", 0.8f }, { "fled", 0.8f },
            { "招募", 0.75f }, { "recruited", 0.75f }, { "新成员", 0.8f },
            
            // 灾害相关（重要性0.85）
            { "爆炸", 0.85f }, { "烟雾", 0.85f }, { "火灾", 0.85f }, { "explosion", 0.85f }, { "fire", 0.85f },
            { "毒船", 0.85f }, { "龙卷风", 0.85f }, { "tornado", 0.85f },
            { "疾病", 0.85f }, { "饥荒", 0.8f }, { "饿死", 0.8f }, { "starvation", 0.8f },
            
            // ? 新增：其他重要事件
            { "日食", 0.75f }, { "eclipse", 0.75f },
            { "虫族", 0.85f }, { "infestation", 0.85f },
            { "贸易", 0.6f }, { "caravan", 0.6f }, { "visitor", 0.6f },
            { "任务", 0.65f }, { "quest", 0.65f },
        };
        
        /// <summary>
        /// 每小时扫描PlayLog事件
        /// 生成全局公共殖民地历史常识
        /// ⚠️ v3.4.4: 性能优化 - 从尾部倒序扫描，避免全量遍历
        /// </summary>
        public static void ScanRecentPlayLog()
        {
            EventRecordPlayLogExtractor.ScanRecentPlayLog();
        }

        
        /// <summary>
        /// 从LogEntry提取事件信息
        /// ? v3.3.4: 优化为提取关键信息，减少token消耗
        /// </summary>
        private static string ExtractEventInfo(LogEntry logEntry)
        {
            return EventRecordPlayLogExtractor.ExtractEventInfo(logEntry);
        }

        
        /// <summary>
        /// ? v3.3.10: 提取关键信息，压缩事件文本（修复制造清单问题）
        /// 目标：将详细事件描述压缩为最重要的部分，减少token浪费
        /// 示例：
        /// - "小明在机械加工台上种植了12株玉米" → "小明种植玉米x12"
        /// - "张三击杀了袭击者（机械虫）" → "张三击杀机械虫"
        /// - "李四运输物资送到仓库" → "李四运输物资"
        /// - "王五完成target.A清单-手工制作台" → "王五在手工制作台制造物品"
        /// </summary>
        private static string ExtractKeyInformation(string fullText)
        {
            return EventRecordPlayLogExtractor.ExtractKeyInformation(fullText);
        }

        
        /// <summary>
        /// ? v3.3.10: 清理制造清单相关的无用文本
        /// "完成target.A清单-手工制作台" → "在手工制作台制造物品"
        /// </summary>
        private static string CleanBillText(string text)
        {
            return EventRecordPlayLogExtractor.CleanBillText(text);
        }

        
        /// <summary>
        /// 提取主要人物（通常是事件的主体）
        /// </summary>
        private static string ExtractMainPerson(string text)
        {
            return EventRecordPlayLogExtractor.ExtractMainPerson(text);
        }

        
        /// <summary>
        /// 提取核心动作（基于重要关键词）
        /// </summary>
        private static string ExtractMainAction(string text)
        {
            return EventRecordPlayLogExtractor.ExtractMainAction(text);
        }

        
        /// <summary>
        /// 提取目标/对象（通常是动作的接受者）
        /// </summary>
        private static string ExtractTarget(string text, string mainPerson)
        {
            return EventRecordPlayLogExtractor.ExtractTarget(text, mainPerson);
        }

        
        /// <summary>
        /// 提取数量信息（如 "×12"）
        /// </summary>
        private static string ExtractQuantity(string text)
        {
            return EventRecordPlayLogExtractor.ExtractQuantity(text);
        }

        
        /// <summary>
        /// 用于清理记录和维护性能
        /// </summary>
        public static void CleanupProcessedRecords()
        {
            // 控制集合大小
            if (processedLogIDs.Count > 2000)
            {
                var toRemove = processedLogIDs.Take(1000).ToList();
                foreach (var id in toRemove)
                {
                    processedLogIDs.Remove(id);
                }
            }
        }
        
        /// <summary>
        /// ? v3.3.3: 更新事件常识的时间前缀（动态更新"今天" → "3天前"）
        /// </summary>
        internal static void UpdateEventKnowledgeTimePrefix(CommonKnowledgeLibrary library)
        {
            if (library == null)
                return;
            
            try
            {
                int currentTick = Find.TickManager.TicksGame;
                int updatedCount = 0;
                
                // 查找所有事件常识
                var eventEntries = library.Entries
                    .Where(e => e.tag != null && (e.tag.Contains("事件") || e.tag.Contains("历史")))
                    .Where(e => e.creationTick >= 0) // 只更新有时间戳的
                    .ToList();
                
                foreach (var entry in eventEntries)
                {
                    // 更新时间前缀
                    entry.UpdateEventTimePrefix(currentTick);
                    updatedCount++;
                }
                
                // ? 日志：记录更新操作（仅DevMode且低频率）
                if (updatedCount > 0 && Prefs.DevMode && UnityEngine.Random.value < 0.05f)
                {
                    Log.Message($"[EventRecord] Updated time prefix for {updatedCount} event knowledge entries");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[EventRecord] Error updating event time prefix: {ex.Message}");
            }
        }
        
        /// <summary>
        /// ? v3.3.3: 从带时间前缀的事件文本中提取原始文本
        /// </summary>
        private static string ExtractOriginalEventText(string eventText)
        {
            return EventRecordPlayLogExtractor.ExtractOriginalEventText(eventText);
        }

        
        /// <summary>
        /// ? 新增：检测是否是对话内容（避免记录对话）
        /// </summary>
        private static bool IsConversationContent(string text)
        {
            return EventRecordPlayLogExtractor.IsConversationContent(text);
        }

        
        /// <summary>
        /// 过滤无聊事件
        /// </summary>
        private static bool IsBoringMessage(string text)
        {
            return EventRecordPlayLogExtractor.IsBoringMessage(text);
        }

        
        /// <summary>
        /// 计算事件重要性
        /// </summary>
        private static float CalculateImportance(string text)
        {
            return EventRecordPlayLogExtractor.CalculateImportance(text);
        }

    }
}
