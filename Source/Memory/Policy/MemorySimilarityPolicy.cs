using System;

namespace Ustas.RimAI.Communication.Memory.Policy
{
    /// <summary>
    /// When two memories are the same memory. On a played save's 461 memories, unrelated pairs
    /// never scored above 0.79 and nine in ten twins scored 0.95 or more, so at 0.85 only
    /// copies merge - a different remark on the same subject stays its own memory.
    /// </summary>
    public static class MemorySimilarityPolicy
    {
        public const float NearDuplicateThreshold = 0.85f;

        public static bool IsNearDuplicate(string left, string right, float threshold = NearDuplicateThreshold)
        {
            string a = Comparable(left);
            string b = Comparable(right);
            if (a.Length == 0 || b.Length == 0)
                return false;
            if (string.Equals(a, b, StringComparison.Ordinal))
                return true;
            return DeterministicEmbedding.Similarity(a, b) >= threshold;
        }

        /// <summary>
        /// The text that says what happened. A round memory opens with a "[Participants: ...]"
        /// roster line, which would make two different talks between the same pawns look alike.
        /// </summary>
        public static string Comparable(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;
            string text = content.Trim();
            if (text.StartsWith("[", StringComparison.Ordinal))
            {
                int newline = text.IndexOf('\n');
                text = newline < 0 ? string.Empty : text.Substring(newline + 1);
            }
            return text.Trim();
        }
    }
}
