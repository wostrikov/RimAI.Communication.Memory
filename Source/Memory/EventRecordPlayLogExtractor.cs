using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory;

internal static class EventRecordPlayLogExtractor
{
        
        internal static void ScanRecentPlayLog()
        {
            EventRecordPlayLogScan.ScanRecentPlayLog();
        }

        
        internal static string ExtractEventInfo(LogEntry logEntry)
        {
            return EventRecordPlayLogScan.ExtractEventInfo(logEntry);
        }

        
        internal static string ExtractKeyInformation(string fullText)
        {
            if (string.IsNullOrEmpty(fullText))
                return null;
            
            try
            {
                fullText = CleanBillText(fullText);
                
                string mainPerson = ExtractMainPerson(fullText);
                
                string action = ExtractMainAction(fullText);
                
                string target = ExtractTarget(fullText, mainPerson);
                
                string quantity = ExtractQuantity(fullText);
                
                var parts = new List<string>();
                
                if (!string.IsNullOrEmpty(mainPerson))
                    parts.Add(mainPerson);
                
                if (!string.IsNullOrEmpty(action))
                    parts.Add(action);
                
                if (!string.IsNullOrEmpty(target))
                    parts.Add(target);
                
                if (!string.IsNullOrEmpty(quantity))
                    parts.Add(quantity);
                
                if (parts.Count < 2)
                {
                    return fullText.Length > 40 ? fullText.Substring(0, 40) : fullText;
                }
                
                return string.Join("", parts);
            }
            catch (Exception ex)
            {
                if (Prefs.DevMode)
                    Log.Warning($"[EventRecord] Key info extraction failed: {ex.Message}");
                
                return fullText.Length > 40 ? fullText.Substring(0, 40) : fullText;
            }
        }
        
        internal static string CleanBillText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            try
            {
                var billPattern = System.Text.RegularExpressions.Regex.Match(
                    text, 
                    @"завершено (target\.[A-Za-z0-9]+|.+?) перелік[－\-](.+?)(?:[,.\s]|$)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                
                if (billPattern.Success)
                {
                    string workbenchName = billPattern.Groups[2].Value.Trim();
                    
                    workbenchName = System.Text.RegularExpressions.Regex.Replace(
                        workbenchName, 
                        @"[（\(].*?[）\)]", 
                        ""
                    ).Trim();
                    
                    string[] words = text.Split(new[] { '完', '成', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string person = words.Length > 0 ? words[0].Trim() : "";
                    
                    if (!string.IsNullOrEmpty(person) && !string.IsNullOrEmpty(workbenchName))
                    {
                        return $"{person} виготовляє щось за {workbenchName}";
                    }
                    else if (!string.IsNullOrEmpty(workbenchName))
                    {
                        return $"виготовляє щось за {workbenchName}";
                    }
                }
                
                text = System.Text.RegularExpressions.Regex.Replace(
                    text,
                    @"target\.[A-Za-z0-9]+",
                    "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                
                return text.Trim();
            }
            catch (Exception ex)
            {
                if (Prefs.DevMode)
                    Log.Warning($"[EventRecord] Bill text cleaning failed: {ex.Message}");
                
                return text;
            }
        }
        
        internal static string ExtractTarget(string text, string mainPerson)
        {
            string remaining = text;
            if (!string.IsNullOrEmpty(mainPerson))
            {
                int index = text.IndexOf(mainPerson);
                if (index >= 0)
                {
                    remaining = text.Substring(index + mainPerson.Length);
                }
            }
            
            var targetMarkers = new[] { "вбивство", "атакувати", "садити", "будувати", "виготовити", "завершено", "лікувати", "вже" };
            
            foreach (var marker in targetMarkers)
            {
                int markerIndex = remaining.IndexOf(marker);
                if (markerIndex >= 0)
                {
                    int start = markerIndex + marker.Length;
                    if (start < remaining.Length)
                    {
                        string targetText = remaining.Substring(start, Math.Min(15, remaining.Length - start));
                        
                        targetText = targetText.Split(new[] { '，', '。', '、', '；', '的', '在', '和', ' ' })[0].Trim();
                        
                        if (targetText.Length >= 2 && targetText.Length <= 10)
                        {
                            return targetText;
                        }
                    }
                }
            }
            
            return null;
        }
        
        internal static string ExtractMainPerson(string text)
        {
            var separators = new[] { "в", "це", "буде", "був", "до", "із", "та", "щодо", " " };
            
            foreach (var sep in separators)
            {
                int index = text.IndexOf(sep);
                if (index > 0 && index < 15)
                {
                    string candidate = text.Substring(0, index).Trim();
                    
                    if (candidate.Length >= 2 && candidate.Length <= 8 && 
                        !candidate.Any(c => char.IsPunctuation(c) || char.IsDigit(c)))
                    {
                        return candidate;
                    }
                }
            }
            
            if (text.Length >= 2)
            {
                string candidate = text.Substring(0, Math.Min(5, text.Length)).Trim();
                if (!candidate.Any(c => char.IsPunctuation(c)))
                    return candidate;
            }
            
            return null;
        }
        
        internal static string ExtractMainAction(string text)
        {
            var matchedKeywords = EventRecordKnowledgeGenerator.ImportantKeywords.Keys
                .Where(k => text.Contains(k))
                .OrderByDescending(k => EventRecordKnowledgeGenerator.ImportantKeywords[k])
                .ThenByDescending(k => k.Length)
                .ToList();
            
            if (matchedKeywords.Any())
            {
                return matchedKeywords.First();
            }
            
            var commonActions = new[] { "садити", "будувати", "завершено", "вбивство", "атакувати", "оборона", "виготовити", "куховарство", 
                                         "дослідження", "лікувати", "приєднання", "відхід", "смерть", "поранення" };
            
            foreach (var action in commonActions)
            {
                if (text.Contains(action))
                    return action;
            }
            
            return null;
        }
        
        internal static string ExtractOriginalEventText(string eventText)
        {
            if (string.IsNullOrEmpty(eventText))
                return eventText;
            
            string[] timePrefixes = { "сьогодні", "1 дн. тому", "2 дн. тому", "3 дн. тому", "4 дн. тому", "5 дн. тому", "6 дн. тому",
                                     "близько 3 дн. тому", "близько 4 дн. тому", "близько 5 дн. тому", "близько 6 дн. тому", "близько 7 дн. тому" };
            
            foreach (var prefix in timePrefixes)
            {
                if (eventText.StartsWith(prefix))
                {
                    return eventText.Substring(prefix.Length);
                }
            }
            
            return eventText;
        }
        
        internal static float CalculateImportance(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0.5f;
            
            var matched = EventRecordKnowledgeGenerator.ImportantKeywords
                .Where(kv => text.Contains(kv.Key))
                .OrderByDescending(kv => kv.Value)
                .FirstOrDefault();
            
            if (matched.Key != null)
            {
                return matched.Value;
            }
            
            return 0.4f;
        }
        
        internal static string ExtractQuantity(string text)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, @"\d+");
            
            if (match.Success)
            {
                int number = int.Parse(match.Value);
                
                if (number > 1 && number < 10000)
                {
                    return $"×{number}";
                }
            }
            
            return null;
        }
        
        internal static bool IsConversationContent(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            
            string[] conversationMarkers = { 
                "каже:", "said:", "мовить:", "сказав:", "сказала:",
                "питає:", "asked:", "запитує:", "запитав:", "запитала:",
                "відповідь:", "replied:", "відповіді:", "відповів:", "відповіла:",
                "гукнув:", "shouted:", "гукнула:", "крикнув:", "крикнула:"
            };
            
            return conversationMarkers.Any(marker => text.Contains(marker));
        }
        
        internal static bool IsBoringMessage(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;
            
            var boringKeywords = new[] 
            { 
                "ходьба", "поїсти", "спати", "розваги", "тинятися", "відпочинок",
                "walking", "eating", "sleeping", "recreation", "wandering"
            };
            
            return boringKeywords.Any(k => text.Contains(k));
        }
}
