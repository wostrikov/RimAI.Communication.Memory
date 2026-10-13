namespace Ustas.RimAI.Communication.Memory.Policy
{
    public enum MemoryCacheLookupAction
    {
        Disabled,
        Miss,
        Expired,
        Hit
    }

    /// <summary>
    /// Authoritative large-colony capacity: ring overwrite of oldest,
    /// conversation-cache hit/expire/LRU evict, and snapshot reuse.
    /// Characterization only — not an expensive colony bench.
    /// </summary>
    public static class MemoryColonyCapacityPolicy
    {
        public static void AfterAdd(
            int head,
            int count,
            int capacity,
            out int writeIndex,
            out int newHead,
            out int newCount)
        {
            if (capacity <= 0)
            {
                writeIndex = 0;
                newHead = 0;
                newCount = 0;
                return;
            }

            int mask = capacity - 1;
            writeIndex = (head + count) & mask;
            if (count < capacity)
            {
                newCount = count + 1;
                newHead = head;
            }
            else
            {
                newCount = count;
                newHead = (head + 1) & mask;
            }
        }

        /// <summary>
        /// The size a ring buffer is built with: the smallest power of two that holds
        /// what was asked for, and never less than one slot, because the buffer wraps
        /// its index with a mask.
        ///
        /// This was Unity.Mathematics' math.ceilpow2. RimWorld 1.7 does not run on
        /// Unity and its stand-in has no such method, so the round memory - a game
        /// component - could not be constructed at all, and a save loaded without it.
        /// </summary>
        public static int RingCapacity(int requested)
        {
            int capacity = 1;
            while (capacity < requested && capacity < (1 << 30))
                capacity <<= 1;
            return capacity;
        }

        public static MemoryCacheLookupAction DecideLookup(bool enabled, bool found, bool expired)
        {
            if (!enabled)
                return MemoryCacheLookupAction.Disabled;
            if (!found)
                return MemoryCacheLookupAction.Miss;
            if (expired)
                return MemoryCacheLookupAction.Expired;
            return MemoryCacheLookupAction.Hit;
        }

        public static int LruEvictCount(int count, int maxSize)
        {
            if (maxSize < 0 || count <= maxSize)
                return 0;
            return count - maxSize;
        }

        public static bool CanReuseSnapshot(bool sameGeneration, bool cachedPresent) =>
            sameGeneration && cachedPresent;
    }
}
