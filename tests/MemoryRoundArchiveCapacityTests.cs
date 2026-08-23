using System;
using System.Collections.Generic;
using System.IO;
using Ustas.RimAI.Communication.Memory.Policy;

internal static class MemoryRoundArchiveCapacityTests
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

        T(!MemoryRoundConversationPolicy.ShouldWriteThrough(false), "round-skip-null");
        T(MemoryRoundConversationPolicy.ShouldWriteThrough(true), "round-write-silent");
        T(MemoryRoundConversationPolicy.ComposeRoster(new[] { "Ada", "Bob", "Cara" }) == "Ada, Bob, Cara", "round-roster");
        string record = MemoryRoundConversationPolicy.ComposeRecord(new[] { "Ada", "Bob" }, "Ada: the raid is coming");
        T(record.Contains(MemoryRoundConversationPolicy.ParticipantsPrefix), "round-header");
        T(MemoryRoundConversationPolicy.IncludesSilentParticipant(record, "Bob", "Ada"), "round-silent-included");
        T(MemoryRoundConversationPolicy.ComposeRecord(Array.Empty<string>(), "Ada: hi") == null, "round-fail-closed");

        T(MemoryArchiveCyclePolicy.DecideCadence(false, 1, 10, 3) == MemoryArchiveCadenceAction.SkipDisabled, "arch-off");
        T(MemoryArchiveCyclePolicy.DecideCadence(true, -1, 4, 3) == MemoryArchiveCadenceAction.ArmFirstDay, "arch-arm");
        T(MemoryArchiveCyclePolicy.DecideCadence(true, 1, 3, 3) == MemoryArchiveCadenceAction.NotDue, "arch-not-due");
        T(MemoryArchiveCyclePolicy.DecideCadence(true, 1, 4, 3) == MemoryArchiveCadenceAction.ArchiveDue, "arch-due");
        T(MemoryArchiveCyclePolicy.ArchiveCount(0) == 0, "arch-empty");
        T(MemoryArchiveCyclePolicy.ArchiveCount(1) == 1, "arch-min-one");
        T(MemoryArchiveCyclePolicy.ArchiveCount(8) == 2, "arch-quarter");
        var ticks = new List<int> { 40, 10, 30, 20 };
        var pinned = new List<bool> { false, false, true, false };
        var oldest = MemoryArchiveCyclePolicy.SelectOldestIndexes(ticks, pinned, 2);
        T(oldest.Count == 2 && oldest[0] == 1 && oldest[1] == 3, "arch-oldest-unpinned");
        T(!MemoryArchiveCyclePolicy.UseOptionalAiSummary(true, false), "arch-ai-unavailable");
        T(MemoryArchiveCyclePolicy.UseOptionalAiSummary(true, true), "arch-ai-optional");

        MemoryColonyCapacityPolicy.AfterAdd(0, 0, 4, out int w0, out int h0, out int c0);
        T(w0 == 0 && h0 == 0 && c0 == 1, "ring-first");
        MemoryColonyCapacityPolicy.AfterAdd(0, 4, 4, out int wFull, out int hFull, out int cFull);
        T(wFull == 0 && hFull == 1 && cFull == 4, "ring-overwrite-oldest");
        T(MemoryColonyCapacityPolicy.DecideLookup(false, true, false) == MemoryCacheLookupAction.Disabled, "cache-off");
        T(MemoryColonyCapacityPolicy.DecideLookup(true, false, false) == MemoryCacheLookupAction.Miss, "cache-miss");
        T(MemoryColonyCapacityPolicy.DecideLookup(true, true, true) == MemoryCacheLookupAction.Expired, "cache-expired");
        T(MemoryColonyCapacityPolicy.DecideLookup(true, true, false) == MemoryCacheLookupAction.Hit, "cache-hit");
        T(MemoryColonyCapacityPolicy.LruEvictCount(8, 8) == 0, "lru-at-cap");
        T(MemoryColonyCapacityPolicy.LruEvictCount(9, 8) == 1, "lru-over");
        T(MemoryColonyCapacityPolicy.CanReuseSnapshot(true, true), "snapshot-reuse");
        T(!MemoryColonyCapacityPolicy.CanReuseSnapshot(false, true), "snapshot-stale");

        string rounds = Read("RoundMemoryManager.cs.src");
        string round = Read("RoundMemory.cs.src");
        string archive = Read("MemoryManagerArchive.cs.src");
        string ring = Read("RimRingBuffer.cs.src");
        string cache = Read("ConversationCache.cs.src");
        T(rounds.Contains("MemoryRoundConversationPolicy.ShouldWriteThrough"), "host-round-write");
        T(round.Contains("MemoryRoundConversationPolicy.ComposeRoster"), "host-round-roster");
        T(archive.Contains("MemoryArchiveCyclePolicy.DecideCadence"), "host-arch-cadence");
        T(archive.Contains("MemoryArchiveCyclePolicy.ArchiveCount"), "host-arch-count");
        T(archive.Contains("MemoryArchiveCyclePolicy.UseOptionalAiSummary"), "host-arch-ai");
        T(ring.Contains("MemoryColonyCapacityPolicy.AfterAdd"), "host-ring");
        T(cache.Contains("MemoryColonyCapacityPolicy.DecideLookup"), "host-cache-lookup");
        T(cache.Contains("MemoryColonyCapacityPolicy.LruEvictCount"), "host-cache-lru");
        return n;
    }

    static string Read(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, name);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
