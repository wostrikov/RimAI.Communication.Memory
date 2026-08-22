using System;
using System.Collections.Generic;
using Ustas.RimAI.Communication.Memory.Policy;

internal static class KnowledgeMatchPolicyTests
{
    public static int Run()
    {
        int n = 0;
        void T(bool x, string s)
        {
            if (!x)
                throw new Exception("FAILED " + s);
            n++;
        }

        var raidTags = new List<string> { "raid", "attack" };
        T(KnowledgeMatchPolicy.MatchesKeywords("raid on the colony", raidTags, KnowledgeTagMatchMode.Any), "keyword-any");
        T(!KnowledgeMatchPolicy.MatchesKeywords("harvest wheat", raidTags, KnowledgeTagMatchMode.Any), "keyword-miss");
        T(KnowledgeMatchPolicy.MatchesKeywords("raid attack tonight", raidTags, KnowledgeTagMatchMode.All), "keyword-all");
        T(!KnowledgeMatchPolicy.MatchesKeywords("raid tonight", raidTags, KnowledgeTagMatchMode.All), "keyword-all-incomplete");

        float raidVsRaid = KnowledgeMatchPolicy.VectorScore(
            "raiders attacked the colony at night",
            "night raid on the colony by raiders",
            raidTags);
        float raidVsWheat = KnowledgeMatchPolicy.VectorScore(
            "raiders attacked the colony at night",
            "colonists harvested wheat in the fields",
            new List<string> { "harvest", "wheat" });
        T(raidVsRaid > raidVsWheat, "vector-ranks-related-above-unrelated");
        T(raidVsRaid >= KnowledgeMatchPolicy.VectorThreshold, "vector-hits-threshold");
        T(KnowledgeMatchPolicy.MatchesVector(
            "raiders attacked the colony at night",
            "night raid on the colony by raiders",
            raidTags), "vector-match");
        var vectorOnlyTags = new List<string> { "nomatch-tag" };
        const string paraphrase = "hostile band stormed the settlement after dusk";
        T(!KnowledgeMatchPolicy.MatchesKeywords(paraphrase, vectorOnlyTags, KnowledgeTagMatchMode.Any), "keyword-miss-on-paraphrase");
        T(KnowledgeMatchPolicy.Matches(paraphrase, paraphrase, vectorOnlyTags, KnowledgeTagMatchMode.Any), "vector-recovers-paraphrase");

        KnowledgeMatchKind kind = KnowledgeMatchPolicy.Classify(
            paraphrase,
            paraphrase,
            vectorOnlyTags,
            KnowledgeTagMatchMode.Any);
        T(kind == KnowledgeMatchKind.Vector, "classify-vector-without-keyword");

        var entries = new List<KnowledgeCandidate>
        {
            new KnowledgeCandidate { Id = "raid", Content = "raiders attacked the colony at night", Importance = 0.2f },
            new KnowledgeCandidate { Id = "wheat", Content = "colonists harvested wheat in the fields", Importance = 0.9f }
        };
        entries[0].Tags.Add("raid");
        entries[1].Tags.Add("harvest");
        var ranked = KnowledgeMatchPolicy.Rank("raiders attacked the colony at night", entries, 2);
        T(ranked.Count >= 1, "rank-returns-matches");
        T(ranked[0].Entry.Id == "raid", "rank-prefers-semantic-raid-over-high-importance-wheat");
        T(ranked[0].Kind != KnowledgeMatchKind.None, "rank-typed");

        float[] a = DeterministicEmbedding.Embed("raid colony night");
        float[] b = DeterministicEmbedding.Embed("raid colony night");
        T(a.Length == DeterministicEmbedding.Dimension, "embed-dimension");
        T(Math.Abs(DeterministicEmbedding.Cosine(a, b) - 1f) < 0.0001f, "embed-deterministic");
        T(DeterministicEmbedding.Embed(null).Length == DeterministicEmbedding.Dimension, "embed-null-safe");
        return n;
    }
}
