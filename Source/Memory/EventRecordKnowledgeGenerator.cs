using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Diagnostics;

namespace Ustas.RimAI.Communication.Memory
{
    public static class EventRecordKnowledgeGenerator
    {
        internal static HashSet<int> processedLogIDs = new HashSet<int>();
        
        internal static readonly Dictionary<string, float> ImportantKeywords = new Dictionary<string, float>
        {
            { "смерть", 1.0f }, { "впав", 1.0f }, { "убитий", 1.0f }, { "вбивство", 1.0f }, { "загинув", 1.0f },
            { "died", 1.0f }, { "killed", 1.0f }, { "death", 1.0f }, { "dead", 1.0f },
            
            { "напад", 0.9f }, { "наступ", 0.9f }, { "оборона", 0.9f }, { "raid", 0.9f }, { "attack", 0.9f },
            { "відбити", 0.85f }, { "перемогти", 0.85f }, { "defeated", 0.85f },
            
            { "похорон", 0.9f }, { "похорон", 0.9f }, { "поховати", 0.9f }, { "funeral", 0.9f }, { "burial", 0.9f },
            { "провести похорон", 0.9f }, { "поховання", 0.9f },
            
            { "одруження", 0.85f }, { "заручини", 0.85f }, { "married", 0.85f }, { "engaged", 0.85f },
            { "весілля", 0.85f }, { "wedding", 0.85f }, { "справити весілля", 0.85f },
            { "розрив", 0.75f }, { "розлучення", 0.75f }, { "breakup", 0.75f },
            
            { "день народження", 0.7f }, { "birthday", 0.7f }, { "святкування", 0.6f }, { "celebration", 0.6f },
            { "святкувати день народження", 0.7f }, { "святкувати день народження", 0.7f },
            
            { "прорив", 0.8f }, { "breakthrough", 0.8f }, { "завершити дослідження", 0.8f }, { "research complete", 0.8f },
            { "дослідження завершено", 0.8f }, { "винахід", 0.8f }, { "invention", 0.8f },
            
            { "річниця", 0.7f }, { "anniversary", 0.7f }, { "річниця", 0.7f },
            
            { "приєднання", 0.8f }, { "тікати", 0.8f }, { "відхід", 0.8f }, { "joined", 0.8f }, { "fled", 0.8f },
            { "вербувати", 0.75f }, { "recruited", 0.75f }, { "новий член", 0.8f },
            
            { "вибух", 0.85f }, { "дим", 0.85f }, { "пожежа", 0.85f }, { "explosion", 0.85f }, { "fire", 0.85f },
            { "отруйний корабель", 0.85f }, { "торнадо", 0.85f }, { "tornado", 0.85f },
            { "хвороба", 0.85f }, { "голод", 0.8f }, { "померти з голоду", 0.8f }, { "starvation", 0.8f },
            
            { "затемнення", 0.75f }, { "eclipse", 0.75f },
            { "комахи", 0.85f }, { "infestation", 0.85f },
            { "торг", 0.6f }, { "caravan", 0.6f }, { "visitor", 0.6f },
            { "завдання", 0.65f }, { "quest", 0.65f },
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
                    .Where(e => e.tag != null && (e.tag.Contains("подія") || e.tag.Contains("історія")))
                    .Where(e => e.creationTick >= 0)
                    .ToList();
                
                foreach (var entry in eventEntries)
                {
                    entry.UpdateEventTimePrefix(currentTick);
                    updatedCount++;
                }
                
                if (updatedCount > 0 && Prefs.DevMode && UnityEngine.Random.value < 0.05f)
                {
                    ModuleLog.Message($"[EventRecord] Updated time prefix for {updatedCount} event knowledge entries");
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
