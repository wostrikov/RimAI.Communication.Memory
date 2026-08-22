using System;
using System.Collections.Generic;

namespace Ustas.RimAI.Communication.Memory.Policy
{
    public enum KnowledgeTagMatchMode
    {
        Any,
        All
    }

    public enum KnowledgeMatchKind
    {
        None,
        Keyword,
        Vector,
        Mixed
    }

    public sealed class KnowledgeCandidate
    {
        public string Id;
        public string Content;
        public float Importance;
        public bool Enabled = true;
        public KnowledgeTagMatchMode Mode = KnowledgeTagMatchMode.Any;
        public readonly List<string> Tags = new List<string>();
    }

    public sealed class KnowledgeRankedMatch
    {
        public KnowledgeCandidate Entry;
        public float Score;
        public KnowledgeMatchKind Kind;
    }

    public static class KnowledgeMatchPolicy
    {
        public const float VectorThreshold = 0.28f;

        public static bool MatchesKeywords(string text, IList<string> tags, KnowledgeTagMatchMode mode)
        {
            if (string.IsNullOrEmpty(text) || tags == null || tags.Count == 0)
                return false;

            if (mode == KnowledgeTagMatchMode.All)
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    string tag = tags[i];
                    if (string.IsNullOrWhiteSpace(tag))
                        continue;
                    if (text.IndexOf(tag, StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
                return HasUsableTag(tags);
            }

            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (string.IsNullOrWhiteSpace(tag))
                    continue;
                if (text.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        public static float VectorScore(string query, string content, IList<string> extraTerms)
        {
            string right = content ?? string.Empty;
            if (extraTerms != null && extraTerms.Count > 0)
                right = right + " " + string.Join(" ", extraTerms);
            return DeterministicEmbedding.Similarity(query ?? string.Empty, right);
        }

        public static bool MatchesVector(string query, string content, IList<string> extraTerms, float threshold = VectorThreshold)
        {
            return VectorScore(query, content, extraTerms) >= threshold;
        }

        public static KnowledgeMatchKind Classify(string query, string content, IList<string> tags, KnowledgeTagMatchMode mode)
        {
            bool keyword = MatchesKeywords(query, tags, mode);
            bool vector = MatchesVector(query, content, tags);
            if (keyword && vector)
                return KnowledgeMatchKind.Mixed;
            if (keyword)
                return KnowledgeMatchKind.Keyword;
            if (vector)
                return KnowledgeMatchKind.Vector;
            return KnowledgeMatchKind.None;
        }

        public static bool Matches(string query, string content, IList<string> tags, KnowledgeTagMatchMode mode)
        {
            return Classify(query, content, tags, mode) != KnowledgeMatchKind.None;
        }

        public static List<KnowledgeRankedMatch> Rank(string query, IList<KnowledgeCandidate> entries, int maxEntries)
        {
            var ranked = new List<KnowledgeRankedMatch>();
            if (entries == null || string.IsNullOrEmpty(query))
                return ranked;

            for (int i = 0; i < entries.Count; i++)
            {
                KnowledgeCandidate entry = entries[i];
                if (entry == null || !entry.Enabled)
                    continue;

                KnowledgeMatchKind kind = Classify(query, entry.Content, entry.Tags, entry.Mode);
                if (kind == KnowledgeMatchKind.None)
                    continue;

                float keywordBonus = kind == KnowledgeMatchKind.Keyword || kind == KnowledgeMatchKind.Mixed ? 0.5f : 0f;
                float vector = VectorScore(query, entry.Content, entry.Tags);
                ranked.Add(new KnowledgeRankedMatch
                {
                    Entry = entry,
                    Kind = kind,
                    Score = keywordBonus + (vector * 0.4f) + entry.Importance
                });
            }

            ranked.Sort((a, b) => b.Score.CompareTo(a.Score));
            if (maxEntries >= 0 && ranked.Count > maxEntries)
                ranked.RemoveRange(maxEntries, ranked.Count - maxEntries);
            return ranked;
        }

        static bool HasUsableTag(IList<string> tags)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(tags[i]))
                    return true;
            }
            return false;
        }
    }
}
