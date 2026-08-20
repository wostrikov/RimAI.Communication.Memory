using System.Collections.Generic;
using System.Text;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.Injection
{
    public static class MemoryFormatter
    {
        public static string Format(List<MemoryEntry> memories, int startIndex = 1)
        {
            if (memories == null || memories.Count == 0)
                return string.Empty;
            
            var sb = new StringBuilder();
            int index = startIndex;
            
            foreach (var memory in memories)
            {
                string typeTag = GetMemoryTypeTag(memory.Type);
                string timeStr = GetTimeString(memory);
                
                sb.AppendLine($"{index}. [{typeTag}] {memory.Content} ({timeStr})");
                index++;
            }
            
            return sb.ToString().TrimEnd();
        }
        
        public static string FormatSingle(MemoryEntry memory, int index)
        {
            if (memory == null)
                return string.Empty;
            
            string typeTag = GetMemoryTypeTag(memory.Type);
            string timeStr = GetTimeString(memory);
            
            return $"{index}. [{typeTag}] {memory.Content} ({timeStr})";
        }
        
        private static string GetTimeString(MemoryEntry memory)
        {
            return memory.AgeString;
        }
        
        public static string GetMemoryTypeTag(MemoryType type)
        {
            switch (type)
            {
                case MemoryType.Conversation:
                    return "Conversation";
                case MemoryType.Action:
                    return "Action";
                case MemoryType.Observation:
                    return "Observation";
                case MemoryType.Event:
                    return "Event";
                case MemoryType.Emotion:
                    return "Emotion";
                case MemoryType.Relationship:
                    return "Relationship";
                default:
                    return "Memory";
            }
        }
    }
}