using System.Collections.Generic;

namespace Ustas.RimAI.Communication.Memory.Policy
{
    public enum MemoryArchiveCadenceAction
    {
        SkipDisabled,
        ArmFirstDay,
        NotDue,
        ArchiveDue
    }

    /// <summary>
    /// Authoritative auto-archive cycle: interval cadence, oldest 25% of
    /// non-pinned event-log entries, pinned kept. AI summarization is optional
    /// and must not block the deterministic archive write.
    /// </summary>
    public static class MemoryArchiveCyclePolicy
    {
        public const float ArchiveRatio = 0.25f;

        public static MemoryArchiveCadenceAction DecideCadence(
            bool enabled,
            int lastArchiveDay,
            int currentDay,
            int intervalDays)
        {
            if (!enabled)
                return MemoryArchiveCadenceAction.SkipDisabled;
            if (lastArchiveDay < 0)
                return MemoryArchiveCadenceAction.ArmFirstDay;
            if (intervalDays <= 0 || currentDay - lastArchiveDay < intervalDays)
                return MemoryArchiveCadenceAction.NotDue;
            return MemoryArchiveCadenceAction.ArchiveDue;
        }

        public static int ArchiveCount(int nonPinnedCount)
        {
            if (nonPinnedCount <= 0)
                return 0;
            int count = (int)(nonPinnedCount * ArchiveRatio);
            return count < 1 ? 1 : count;
        }

        public static List<int> SelectOldestIndexes(IList<int> ticks, IList<bool> pinned, int take)
        {
            var selected = new List<int>();
            if (ticks == null || take <= 0)
                return selected;

            var candidates = new List<int>();
            for (int i = 0; i < ticks.Count; i++)
            {
                if (pinned != null && i < pinned.Count && pinned[i])
                    continue;
                candidates.Add(i);
            }

            candidates.Sort((a, b) => ticks[a].CompareTo(ticks[b]));
            int n = take < candidates.Count ? take : candidates.Count;
            for (int i = 0; i < n; i++)
                selected.Add(candidates[i]);
            return selected;
        }

        public static bool UseOptionalAiSummary(bool useAiSummarization, bool summarizerAvailable) =>
            useAiSummarization && summarizerAvailable;
    }
}
