using System;
using System.Text;
using System.Text.RegularExpressions;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    public static class ContextCleaner
    {
        public static string CleanForVectorMatching(string context)
        {
            if (string.IsNullOrEmpty(context))
                return "";

            if (context.Contains(" said to '"))
            {
                var match = Regex.Match(context, @"said to '[^']*?: ([^']+)'");
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
                
                match = Regex.Match(context, @"said to '([^']+)'");
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            StringBuilder sb = new StringBuilder();

            int eventStart = context.IndexOf("[Ongoing events]");
            int eventEnd = context.IndexOf("[Event list end]");
            
            if (eventStart >= 0 && eventEnd > eventStart)
            {
                string events = context.Substring(eventStart, eventEnd - eventStart + "[Event list end]".Length);
                sb.AppendLine(events);
            }

            string[] lines = context.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            bool insideConversationBlock = false;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                
                if (string.IsNullOrEmpty(trimmed)) continue;
                
                if (trimmed.Contains("[Ongoing events]") || trimmed.Contains("[Event list end]")) continue;
                
                if (eventStart >= 0 && context.IndexOf(line) > eventStart && context.IndexOf(line) < eventEnd)
                    continue;
                
                if (trimmed.Contains("starts conversation") || trimmed.Contains("short monologue"))
                {
                    insideConversationBlock = true;
                    continue;
                }
                
                if (insideConversationBlock && IsPawnStatusLine(trimmed))
                {
                    insideConversationBlock = false;
                    continue;
                }
                
                if (insideConversationBlock)
                {
                    if (!IsNoiseLine(trimmed))
                    {
                        sb.AppendLine(trimmed);
                    }
                    continue;
                }
                

                if (trimmed.StartsWith("new good feeling:", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine(trimmed);
                    continue;
                }
                
                if (IsNoiseLine(trimmed))
                    continue;
                
                // Hard constraint — changing this breaks an invariant. (Pawn)
                if (!IsPawnStatusLine(trimmed))
                {
                    if (!trimmed.Contains("Generate dialogue starting after") &&
                        !trimmed.Contains("Do not generate any further lines"))
                    {
                        sb.AppendLine(trimmed);
                    }
                }
            }
            
            string result = sb.ToString().Trim();
            
            
            return result;
        }

        private static bool IsNoiseLine(string line)
        {
            if (line.StartsWith("Time:")) return true;
            if (line.StartsWith("Today:")) return true;
            if (line.StartsWith("Season:")) return true;
            if (line.StartsWith("Weather:")) return true;
            if (line.StartsWith("Location:")) return true;
            if (line.StartsWith("Terrain:")) return true;
            if (line.StartsWith("Wealth:")) return true;
            if (line.StartsWith("Nearby:")) return true;
            if (line.StartsWith("Nearby people:")) return true;
            if (line.StartsWith("in ChineseSimplified")) return true;
            
            return false;
        }

        private static bool IsPawnStatusLine(string line)
        {
            if (line.Contains("(Age:") && line.Contains(";ID:")) return true;
            
            return false;
        }
    }
}
