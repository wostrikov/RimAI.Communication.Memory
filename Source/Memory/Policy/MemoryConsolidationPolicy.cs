using System;
using System.Collections.Generic;

namespace Ustas.RimAI.Communication.Memory.Policy
{
    public sealed class MemoryRecord
    {
        public string Id;
        public string Content;
        public int GameTick;
        public float Importance;
        public float Activity = 1f;
        public bool IsPinned;
        public bool IsSummarized;
        public string Layer;
        public string RelatedPawnName;
        public readonly List<string> Tags = new List<string>();
        public readonly List<string> Keywords = new List<string>();

        public bool CanBeSummarized => !IsSummarized;
    }

    public sealed class MemoryConsolidationResult
    {
        public MemoryRecord Summary;
        public int SummarizedCount;
        public int ActiveCleared;
        public int SituationalRemoved;
        public int SituationalPinnedKept;
        public bool Applied => SummarizedCount > 0;
    }

    public static class MemoryConsolidationPolicy
    {
        public static MemoryConsolidationResult Consolidate(
            IList<MemoryRecord> active,
            IList<MemoryRecord> situational,
            IList<MemoryRecord> eventLog)
        {
            var result = new MemoryConsolidationResult();
            if (active == null || situational == null || eventLog == null)
                return result;

            var selected = new List<MemoryRecord>();
            CollectSummarizable(active, selected);
            CollectSummarizable(situational, selected);
            if (selected.Count == 0)
                return result;

            float importanceSum = 0f;
            int latest = int.MinValue;
            for (int i = 0; i < selected.Count; i++)
            {
                importanceSum += selected[i].Importance;
                if (selected[i].GameTick > latest)
                    latest = selected[i].GameTick;
            }

            var summary = new MemoryRecord
            {
                Id = "els-summary",
                Content = "consolidated:" + selected.Count,
                GameTick = latest,
                Importance = (importanceSum / selected.Count) + 0.2f,
                Layer = "EventLog",
                Activity = 1f
            };
            eventLog.Add(summary);

            result.Summary = summary;
            result.SummarizedCount = selected.Count;
            result.ActiveCleared = active.Count;
            result.SituationalRemoved = ApplyAfterSummary(selected, active, situational, MarkSummarized, IsPinned);
            result.SituationalPinnedKept = situational.Count;
            return result;
        }

        public static int ApplyAfterSummary<T>(
            IEnumerable<T> selected,
            IList<T> active,
            IList<T> situational,
            Action<T> markSummarized,
            Func<T, bool> isPinned)
        {
            if (selected != null && markSummarized != null)
            {
                foreach (T item in selected)
                {
                    if (item != null)
                        markSummarized(item);
                }
            }

            active?.Clear();

            if (situational == null || isPinned == null)
                return 0;

            int removed = 0;
            for (int i = situational.Count - 1; i >= 0; i--)
            {
                T item = situational[i];
                if (item == null || isPinned(item))
                    continue;
                situational.RemoveAt(i);
                removed++;
            }
            return removed;
        }

        static void CollectSummarizable(IList<MemoryRecord> source, List<MemoryRecord> selected)
        {
            for (int i = 0; i < source.Count; i++)
            {
                MemoryRecord item = source[i];
                if (item != null && item.CanBeSummarized)
                    selected.Add(item);
            }
        }

        static void MarkSummarized(MemoryRecord memory)
        {
            memory.IsSummarized = true;
        }

        static bool IsPinned(MemoryRecord memory)
        {
            return memory.IsPinned;
        }
    }
}
