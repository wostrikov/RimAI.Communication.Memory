using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory;

internal static class EventRecordPlayLogScan
{
        
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
                
                EventRecordKnowledgeGenerator.UpdateEventKnowledgeTimePrefix(library);
                
                int processedCount = 0;
                int currentTick = Find.TickManager.TicksGame;
                int hourThreshold = currentTick - GenDate.TicksPerHour;
                
                var allEntries = gameHistory.AllEntries;
                int totalCount = allEntries.Count;
                int scannedCount = 0;
                int maxScan = Math.Min(50, totalCount);
                
                for (int i = totalCount - 1; i >= 0 && scannedCount < maxScan; i--)
                {
                    try
                    {
                        var logEntry = allEntries[i];
                        if (logEntry == null)
                            continue;
                        
                        scannedCount++;
                        
                        if (logEntry.Age < hourThreshold)
                        {
                            break;
                        }
                        
                        int logID = logEntry.GetHashCode();
                        
                        if (EventRecordKnowledgeGenerator.processedLogIDs.Contains(logID))
                            continue;
                        
                        EventRecordKnowledgeGenerator.processedLogIDs.Add(logID);
                        
                        if (EventRecordKnowledgeGenerator.processedLogIDs.Count > 2000)
                        {
                            var toRemove = EventRecordKnowledgeGenerator.processedLogIDs.Take(1000).ToList();
                            foreach (var id in toRemove)
                            {
                                EventRecordKnowledgeGenerator.processedLogIDs.Remove(id);
                            }
                        }
                        
                        string eventText = ExtractEventInfo(logEntry);
                        
                        if (!string.IsNullOrEmpty(eventText))
                        {
                            bool exists = library.Entries.Any(e => 
                                e.content.Contains(eventText.Substring(0, Math.Min(15, eventText.Length)))
                            );
                            
                            if (!exists)
                            {
                                float importance = EventRecordPlayLogExtractor.CalculateImportance(eventText);
                                
                                string originalText = EventRecordPlayLogExtractor.ExtractOriginalEventText(eventText);
                                
                                var entry = new CommonKnowledgeEntry("事件,历史", eventText)
                                {
                                    importance = importance,
                                    isEnabled = true,
                                    isUserEdited = false,
                                    creationTick = currentTick,          
                                    originalEventText = originalText     
                                };
                                
                                library.AddEntry(entry);
                                processedCount++;
                                
                                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                                {
                                    Log.Message($"[EventRecord] Created event knowledge: {eventText.Substring(0, Math.Min(50, eventText.Length))}...");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Prefs.DevMode && UnityEngine.Random.value < 0.2f)
                        {
                            Log.Warning($"[EventRecord] Error processing log entry: {ex.Message}");
                        }
                    }
                }
                
                if (processedCount > 0 && Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    Log.Message($"[EventRecord] Scanned {scannedCount} entries, processed {processedCount} new events (total: {totalCount})");
                }
            }
            catch (Exception ex)
            {
                if (Prefs.DevMode && UnityEngine.Random.value < 0.2f)
                {
                    Log.Error($"[EventRecord] Error scanning PlayLog: {ex.Message}");
                }
            }
        }
        
        internal static string ExtractEventInfo(LogEntry logEntry)
        {
            if (logEntry == null)
                return null;
            
            try
            {
                if (logEntry.GetType().Name == "PlayLogEntry_Interaction")
                {
                    return null;
                }
                
                if (logEntry.GetType().Name == "PlayLogEntry_Incident")
                {
                    string previewText = logEntry.ToGameStringFromPOV(null, false);
                    if (!string.IsNullOrEmpty(previewText))
                    {
                        bool isImportantEvent = 
                            previewText.Contains("死亡") || previewText.Contains("died") || previewText.Contains("killed") || 
                            previewText.Contains("dead") || previewText.Contains("death") ||
                            previewText.Contains("葬礼") || previewText.Contains("葬") || previewText.Contains("埋葬") ||
                            previewText.Contains("结婚") || previewText.Contains("婚礼") || previewText.Contains("married") ||
                            previewText.Contains("生日") || previewText.Contains("birthday") ||
                            previewText.Contains("突破") || previewText.Contains("breakthrough");
                        
                        if (!isImportantEvent)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                
                string text = logEntry.ToGameStringFromPOV(null, false);
                
                if (string.IsNullOrEmpty(text))
                    return null;
                
                if (text.Length < 10 || text.Length > 200)
                    return null;
                
                if (EventRecordPlayLogExtractor.IsBoringMessage(text))
                    return null;
                
                bool hasImportantKeyword = EventRecordKnowledgeGenerator.ImportantKeywords.Keys.Any(k => text.Contains(k));
                
                if (!hasImportantKeyword)
                {
                    if (logEntry.GetType().Name == "PlayLogEntry_Incident")
                    {
                    }
                    else
                    {
                        return null;  // Hard constraint — changing this breaks an invariant. (Incident)
                    }
                }
                
                if (EventRecordPlayLogExtractor.IsConversationContent(text))
                    return null;
                
                string compressedText = EventRecordPlayLogExtractor.ExtractKeyInformation(text);
                
                if (string.IsNullOrEmpty(compressedText))
                    return null;
                
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
                    return null;
                }
                
                return $"{timePrefix}{compressedText}";
            }
            catch (Exception)
            {
                return null;
            }
        }
}
