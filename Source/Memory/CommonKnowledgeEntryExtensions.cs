using System;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    public static class CommonKnowledgeEntryExtensions
    {
        public static bool IsCompleteWordMatch(string text, string word)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(word))
                return false;
            
            if (string.Equals(text, word, StringComparison.OrdinalIgnoreCase))
                return true;
            
            int index = 0;
            while ((index = text.IndexOf(word, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                // Host/domain boundary constraint.
                bool frontBoundary = (index == 0) || IsWordBoundary(text[index - 1]);
                
                // Host/domain boundary constraint.
                int endIndex = index + word.Length;
                bool backBoundary = (endIndex == text.Length) || IsWordBoundary(text[endIndex]);
                
                // Host/domain boundary constraint.
                if (frontBoundary && backBoundary)
                    return true;
                
                index += word.Length;
            }
            
            return false;
        }
        
        public static bool IsWordBoundary(char c)
        {
            if (char.IsWhiteSpace(c) || c == ',' || c == '，' || c == '、' || c == ';' || c == '；' ||
                c == '.' || c == '。' || c == '!' || c == '！' || c == '?' || c == '？' ||
                c == ':' || c == '：' || c == '-' || c == '_' || c == '/' || c == '\\' ||
                c == '(' || c == ')' || c == '（' || c == '）' || c == '[' || c == ']' ||
                c == '{' || c == '}' || c == '<' || c == '>' || c == '「' || c == '」' ||
                c == '『' || c == '』' || c == '【' || c == '】')
            {
                return true;
            }
            
            if (char.IsPunctuation(c) || char.IsSymbol(c))
            {
                return true;
            }
            
            return false;
        }
        
        public static bool IsRuleKnowledge(this CommonKnowledgeEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.tag))
                return false;
            
            string lowerTag = entry.tag.ToLower();
            return lowerTag.Contains("规则") || 
                   lowerTag.Contains("instructions") || 
                   lowerTag.Contains("rule");
        }
    }
}
