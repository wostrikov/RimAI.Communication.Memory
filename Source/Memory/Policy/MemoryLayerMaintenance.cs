using System;
using System.Collections.Generic;

namespace Ustas.RimAI.Communication.Memory.Policy
{
    public static class MemoryLayerMaintenance
    {
        public const float ActivityCleanupThreshold = 0.01f;

        public static float DecayActivity(float activity, float rate, bool pinned)
        {
            if (pinned)
                return activity;
            if (rate <= 0f)
                return activity;
            return activity * (1f - rate);
        }

        public static int DecayList<T>(IList<T> list, float rate, Func<T, float> getActivity, Action<T, float> setActivity, Func<T, bool> pinned)
        {
            if (list == null)
                return 0;
            int changed = 0;
            for (int i = 0; i < list.Count; i++)
            {
                T item = list[i];
                if (item == null)
                    continue;
                float next = DecayActivity(getActivity(item), rate, pinned(item));
                if (next != getActivity(item))
                {
                    setActivity(item, next);
                    changed++;
                }
            }
            return changed;
        }

        public static int RemoveLowActivity<T>(IList<T> list, Func<T, float> activity, Func<T, bool> pinned, float threshold = ActivityCleanupThreshold)
        {
            if (list == null)
                return 0;
            int removed = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                T item = list[i];
                if (item == null)
                    continue;
                if (!pinned(item) && activity(item) < threshold)
                {
                    list.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        public static int EnforceLimit<T>(IList<T> list, int maxNonPinned, Func<T, float> activity, Func<T, int> tick, Func<T, bool> pinned)
        {
            if (list == null || maxNonPinned < 0)
                return 0;

            int nonPinned = 0;
            for (int i = 0; i < list.Count; i++)
            {
                T item = list[i];
                if (item != null && !pinned(item))
                    nonPinned++;
            }

            if (nonPinned <= maxNonPinned)
                return 0;

            int toRemove = nonPinned - maxNonPinned;
            var candidates = new List<int>(nonPinned);
            for (int i = 0; i < list.Count; i++)
            {
                T item = list[i];
                if (item != null && !pinned(item))
                    candidates.Add(i);
            }

            candidates.Sort((a, b) =>
            {
                int byActivity = activity(list[a]).CompareTo(activity(list[b]));
                if (byActivity != 0)
                    return byActivity;
                return tick(list[a]).CompareTo(tick(list[b]));
            });

            var remove = new List<T>(toRemove);
            for (int i = 0; i < toRemove && i < candidates.Count; i++)
                remove.Add(list[candidates[i]]);

            int removed = 0;
            for (int i = 0; i < remove.Count; i++)
            {
                if (list.Remove(remove[i]))
                    removed++;
            }
            return removed;
        }
    }
}
