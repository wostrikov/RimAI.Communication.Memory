using System;
using System.Collections.Generic;
using Ustas.RimAI.Communication.Memory.Policy;

internal static class MemoryLayerMaintenanceTests
{
    sealed class Item
    {
        public float Activity;
        public int Tick;
        public bool Pinned;
    }

    public static int Run()
    {
        int n = 0;
        void T(bool x, string s)
        {
            if (!x)
                throw new Exception("FAILED " + s);
            n++;
        }

        T(MemoryLayerMaintenance.DecayActivity(1f, 0.2f, false) == 0.8f, "decay-unpinned");
        T(MemoryLayerMaintenance.DecayActivity(1f, 0.2f, true) == 1f, "decay-skips-pinned");
        T(MemoryLayerMaintenance.DecayActivity(0.5f, 0f, false) == 0.5f, "decay-zero-rate");

        var decaying = new List<Item>
        {
            new Item { Activity = 1f, Pinned = false },
            new Item { Activity = 1f, Pinned = true }
        };
        int changed = MemoryLayerMaintenance.DecayList(
            decaying, 0.5f, i => i.Activity, (i, v) => i.Activity = v, i => i.Pinned);
        T(changed == 1, "decay-list-changed");
        T(Math.Abs(decaying[0].Activity - 0.5f) < 0.0001f, "decay-list-unpinned");
        T(decaying[1].Activity == 1f, "decay-list-pinned");

        var cleanup = new List<Item>
        {
            new Item { Activity = 0.005f, Pinned = false },
            new Item { Activity = 0.005f, Pinned = true },
            new Item { Activity = 0.4f, Pinned = false }
        };
        int removed = MemoryLayerMaintenance.RemoveLowActivity(cleanup, i => i.Activity, i => i.Pinned);
        T(removed == 1, "cleanup-removed");
        T(cleanup.Count == 2, "cleanup-kept");
        T(cleanup[0].Pinned, "cleanup-kept-pinned-low");
        T(cleanup[1].Activity == 0.4f, "cleanup-kept-active");

        var limited = new List<Item>
        {
            new Item { Activity = 0.1f, Tick = 1, Pinned = false },
            new Item { Activity = 0.9f, Tick = 2, Pinned = false },
            new Item { Activity = 0.05f, Tick = 3, Pinned = true },
            new Item { Activity = 0.2f, Tick = 4, Pinned = false }
        };
        int evicted = MemoryLayerMaintenance.EnforceLimit(limited, 2, i => i.Activity, i => i.Tick, i => i.Pinned);
        T(evicted == 1, "limit-removed-one");
        T(limited.Count == 3, "limit-remaining");
        T(limited.Exists(i => i.Pinned), "limit-keeps-pinned");
        T(!limited.Exists(i => i.Activity == 0.1f && !i.Pinned), "limit-drops-lowest-unpinned");
        return n;
    }
}
