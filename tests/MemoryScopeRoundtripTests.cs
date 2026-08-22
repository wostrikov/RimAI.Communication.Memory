using System;
using Ustas.RimAI.Communication.Memory.Policy;

internal static class MemoryScopeRoundtripTests
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

        var source = new ThreeScopeSnapshot { NextRoundMemoryId = 7 };
        source.Personal.Add(new MemoryEntrySnapshot
        {
            Id = "mem-1",
            GameTick = 42,
            Content = "colonist remembered the raid",
            Layer = "Active",
            Importance = 0.7f,
            Activity = 0.9f,
            Pinned = true
        });
        source.WorldKnowledge.Add(new KnowledgeEntrySnapshot
        {
            Id = "ck-1",
            Tag = "raid",
            Content = "raiders prefer night attacks",
            Importance = 0.8f,
            Enabled = true
        });
        source.GameRounds.Add(new RoundMemorySnapshot
        {
            Id = 7,
            Tick = 42,
            Content = "Alice, Bob: raid talk"
        });
        source.GameRounds[0].ParticipantPawnIds.Add(11);
        source.GameRounds[0].ParticipantPawnIds.Add(12);
        source.AbmRoundPointers[11] = 7;
        source.AbmRoundPointers[99] = 404;

        ThreeScopeSnapshot copy = MemoryScopeRoundtrip.Clone(source);
        T(copy != source, "roundtrip-new-instance");
        T(copy.NextRoundMemoryId == 7, "roundtrip-game-next-id");
        T(copy.Personal.Count == 1 && copy.Personal[0].Content == source.Personal[0].Content, "roundtrip-thing-personal");
        T(copy.Personal[0] != source.Personal[0], "roundtrip-personal-cloned");
        T(copy.WorldKnowledge.Count == 1 && copy.WorldKnowledge[0].Tag == "raid", "roundtrip-world-knowledge");
        T(copy.GameRounds.Count == 1 && copy.GameRounds[0].Id == 7, "roundtrip-game-rounds");
        T(copy.AbmRoundPointers[11] == 7, "roundtrip-abm-pointer");

        int repaired = MemoryScopeRoundtrip.RepairAbmPointers(copy);
        T(repaired == 1, "repair-drops-orphan-pointer");
        T(copy.AbmRoundPointers.ContainsKey(11), "repair-keeps-live-pointer");
        T(!copy.AbmRoundPointers.ContainsKey(99), "repair-removes-missing-round");
        T(source.AbmRoundPointers.ContainsKey(99), "repair-does-not-mutate-source");

        T(MemoryScopeKeys.PersonalActive == "activeMemories", "key-personal-abm");
        T(MemoryScopeKeys.PersonalSituational == "situationalMemories", "key-personal-scm");
        T(MemoryScopeKeys.PersonalEventLog == "eventLogMemories", "key-personal-els");
        T(MemoryScopeKeys.PersonalArchive == "archiveMemories", "key-personal-clpa");
        T(MemoryScopeKeys.WorldKnowledge == "commonKnowledge", "key-world");
        T(MemoryScopeKeys.GameRounds == "RoundMemories", "key-game-rounds");
        T(MemoryScopeKeys.GameNextRoundId == "NextRoundMemoryId", "key-game-next-id");
        return n;
    }
}
