using System;
using System.IO;
using Ustas.RimAI.Communication.Memory.Policy;

internal static class MemoryHostWiringGuardTests
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

        string embedding = Read("EmbeddingService.cs.src");
        string semantic = Read("SemanticScoringSystem.cs.src");
        string personal = Read("FourLayerMemoryComp.cs.src");
        string decay = Read("FourLayerDecay.cs.src");
        string summary = Read("FourLayerSummarization.cs.src");
        string entry = Read("MemoryEntry.cs.src");
        string world = Read("MemoryManager.cs.src");
        string rounds = Read("RoundMemoryManager.cs.src");
        string knowledge = Read("CommonKnowledgeLibrary.cs.src");

        T(embedding.Contains("return true;"), "embedding-available");
        T(!ContainsHardFalseAvailable(embedding), "embedding-not-hard-false");
        T(embedding.Contains("DeterministicEmbedding.Embed"), "embedding-uses-local-vectors");
        T(!embedding.Contains("CallEmbeddingAPIAsync(text)"), "embedding-no-paid-default-path");
        T(semantic.Contains("bool useSemantics = AI.EmbeddingService.IsAvailable();"), "semantic-follows-availability");
        T(!semantic.Contains("bool useSemantics = false;"), "semantic-not-hard-disabled");

        T(personal.Contains("\"" + MemoryScopeKeys.PersonalActive + "\""), "scribe-personal-abm");
        T(personal.Contains("\"" + MemoryScopeKeys.PersonalSituational + "\""), "scribe-personal-scm");
        T(personal.Contains("\"" + MemoryScopeKeys.PersonalEventLog + "\""), "scribe-personal-els");
        T(personal.Contains("\"" + MemoryScopeKeys.PersonalArchive + "\""), "scribe-personal-clpa");
        T(world.Contains("\"" + MemoryScopeKeys.WorldKnowledge + "\""), "scribe-world-knowledge");
        T(rounds.Contains("\"" + MemoryScopeKeys.GameRounds + "\""), "scribe-game-rounds");
        T(rounds.Contains("\"" + MemoryScopeKeys.GameNextRoundId + "\""), "scribe-game-next-id");

        T(entry.Contains("MemoryLayerMaintenance.DecayActivity"), "entry-decay-uses-policy");
        T(decay.Contains("MemoryLayerMaintenance.RemoveLowActivity"), "decay-cleanup-uses-policy");
        T(decay.Contains("MemoryLayerMaintenance.EnforceLimit"), "decay-limits-use-policy");
        T(summary.Contains("MemoryConsolidationPolicy.ApplyAfterSummary"), "summary-uses-policy");
        T(knowledge.Contains("KnowledgeMatchPolicy.Matches"), "knowledge-match-uses-policy");
        T(knowledge.Contains("KnowledgeMatchPolicy.Classify"), "knowledge-score-uses-policy");
        return n;
    }

    static bool ContainsHardFalseAvailable(string source)
    {
        int index = source.IndexOf("public static bool IsAvailable()", StringComparison.Ordinal);
        if (index < 0)
            return true;
        string body = source.Substring(index, Math.Min(160, source.Length - index));
        return body.Contains("return false;");
    }

    static string Read(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, name);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
