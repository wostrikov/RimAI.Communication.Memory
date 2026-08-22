using System;
using System.Collections.Generic;
using Ustas.RimAI.Communication.Memory.Policy;

internal static class MemoryConsolidationTests
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

        var active = new List<MemoryRecord>
        {
            new MemoryRecord { Id = "abm-1", Content = "talked about raid", GameTick = 100, Importance = 0.4f },
            new MemoryRecord { Id = "abm-2", Content = "heard thunder", GameTick = 120, Importance = 0.2f }
        };
        var situational = new List<MemoryRecord>
        {
            new MemoryRecord { Id = "scm-1", Content = "raiders nearby", GameTick = 80, Importance = 0.6f },
            new MemoryRecord { Id = "scm-pin", Content = "keep this", GameTick = 90, Importance = 0.9f, IsPinned = true }
        };
        var eventLog = new List<MemoryRecord>();

        var result = MemoryConsolidationPolicy.Consolidate(active, situational, eventLog);
        T(result.Applied, "consolidate-applied");
        T(result.SummarizedCount == 4, "consolidate-selected-includes-pinned-unsummarized");
        T(result.ActiveCleared == 2, "consolidate-clears-abm-count");
        T(active.Count == 0, "consolidate-abm-empty");
        T(situational.Count == 1 && situational[0].Id == "scm-pin", "consolidate-keeps-pinned-scm");
        T(result.SituationalRemoved == 1, "consolidate-removed-unpinned-scm");
        T(eventLog.Count == 1 && eventLog[0].Layer == "EventLog", "consolidate-writes-els");
        T(eventLog[0].GameTick == 120, "consolidate-uses-latest-tick");
        T(situational[0].IsSummarized, "consolidate-marks-pinned-summarized");

        var empty = MemoryConsolidationPolicy.Consolidate(new List<MemoryRecord>(), new List<MemoryRecord>(), new List<MemoryRecord>());
        T(!empty.Applied, "consolidate-noop-when-empty");
        return n;
    }
}
