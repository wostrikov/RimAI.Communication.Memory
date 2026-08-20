using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory
{
    public static class EventRecordKnowledgeGenerator
    {
        internal static HashSet<int> processedLogIDs = new HashSet<int>();
        
        internal static readonly Dictionary<string, float> ImportantKeywords = new Dictionary<string, float>
        {
            { "死亡", 1.0f }, { "倒下", 1.0f }, { "被杀", 1.0f }, { "击杀", 1.0f }, { "牺牲", 1.0f },
            { "died", 1.0f }, { "killed", 1.0f }, { "death", 1.0f }, { "dead", 1.0f },
            
            { "袭击", 0.9f }, { "进攻", 0.9f }, { "防御", 0.9f }, { "raid", 0.9f }, { "attack", 0.9f },
            { "击退", 0.85f }, { "战胜", 0.85f }, { "defeated", 0.85f },
            
            { "葬礼", 0.9f }, { "葬", 0.9f }, { "埋葬", 0.9f }, { "funeral", 0.9f }, { "burial", 0.9f },
            { "举行葬礼", 0.9f }, { "安葬", 0.9f },
            
            { "结婚", 0.85f }, { "订婚", 0.85f }, { "married", 0.85f }, { "engaged", 0.85f },
            { "婚礼", 0.85f }, { "wedding", 0.85f }, { "举行婚礼", 0.85f },
            { "分手", 0.75f }, { "离婚", 0.75f }, { "breakup", 0.75f },
            
            { "生日", 0.7f }, { "birthday", 0.7f }, { "庆祝", 0.6f }, { "celebration", 0.6f },
            { "过生日", 0.7f }, { "庆祝生日", 0.7f },
            
            { "突破", 0.8f }, { "breakthrough", 0.8f }, { "完成研究", 0.8f }, { "research complete", 0.8f },
            { "研究完成", 0.8f }, { "发明", 0.8f }, { "invention", 0.8f },
            
            { "周年", 0.7f }, { "anniversary", 0.7f }, { "周年纪念", 0.7f },
            
            { "加入", 0.8f }, { "逃跑", 0.8f }, { "离开", 0.8f }, { "joined", 0.8f }, { "fled", 0.8f },
            { "招募", 0.75f }, { "recruited", 0.75f }, { "新成员", 0.8f },
            
            { "爆炸", 0.85f }, { "烟雾", 0.85f }, { "火灾", 0.85f }, { "explosion", 0.85f }, { "fire", 0.85f },
            { "毒船", 0.85f }, { "龙卷风", 0.85f }, { "tornado", 0.85f },
            { "疾病", 0.85f }, { "饥荒", 0.8f }, { "饿死", 0.8f }, { "starvation", 0.8f },
            
            { "日食", 0.75f }, { "eclipse", 0.75f },
            { "虫族", 0.85f }, { "infestation", 0.85f },
            { "贸易", 0.6f }, { "caravan", 0.6f }, { "visitor", 0.6f },
            { "任务", 0.65f }, { "quest", 0.65f },
        };
        
        public static void ScanRecentPlayLog()
        {
            EventRecordPlayLogExtractor.ScanRecentPlayLog();
        }

        
        private static string ExtractEventInfo(LogEntry logEntry)
        {
            return EventRecordPlayLogExtractor.ExtractEventInfo(logEntry);
        }

        
        private static string ExtractKeyInformation(string fullText)
        {
            return EventRecordPlayLogExtractor.ExtractKeyInformation(fullText);
        }

        
        private static string CleanBillText(string text)
        {
            return EventRecordPlayLogExtractor.CleanBillText(text);
        }

        
        private static string ExtractMainPerson(string text)
        {
            return EventRecordPlayLogExtractor.ExtractMainPerson(text);
        }

        
        private static string ExtractMainAction(string text)
        {
            return EventRecordPlayLogExtractor.ExtractMainAction(text);
        }

        
        private static string ExtractTarget(string text, string mainPerson)
        {
            return EventRecordPlayLogExtractor.ExtractTarget(text, mainPerson);
        }

        
        private static string ExtractQuantity(string text)
        {
            return EventRecordPlayLogExtractor.ExtractQuantity(text);
        }

        
        public static void CleanupProcessedRecords()
        {
            if (processedLogIDs.Count > 2000)
            {
                var toRemove = processedLogIDs.Take(1000).ToList();
                foreach (var id in toRemove)
                {
                    processedLogIDs.Remove(id);
                }
            }
        }
        
        internal static void UpdateEventKnowledgeTimePrefix(CommonKnowledgeLibrary library)
        {
            if (library == null)
                return;
            
            try
            {
                int currentTick = Find.TickManager.TicksGame;
                int updatedCount = 0;
                
                var eventEntries = library.Entries
                    .Where(e => e.tag != null && (e.tag.Contains("事件") || e.tag.Contains("历史")))
                    .Where(e => e.creationTick >= 0)
                    .ToList();
                
                foreach (var entry in eventEntries)
                {
                    entry.UpdateEventTimePrefix(currentTick);
                    updatedCount++;
                }
                
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
        
        private static string ExtractOriginalEventText(string eventText)
        {
            return EventRecordPlayLogExtractor.ExtractOriginalEventText(eventText);
        }

        
        private static bool IsConversationContent(string text)
        {
            return EventRecordPlayLogExtractor.IsConversationContent(text);
        }

        
        private static bool IsBoringMessage(string text)
        {
            return EventRecordPlayLogExtractor.IsBoringMessage(text);
        }

        
        private static float CalculateImportance(string text)
        {
            return EventRecordPlayLogExtractor.CalculateImportance(text);
        }

    }
}
