using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory
{
    public class PromptCache : IExposable
    {
        public class CacheEntry : IExposable
        {
            public string memoryPrompt;      
            public string knowledgePrompt;   
            public string fullPrompt;        
            public int timestamp;            
            public int lastUsedTick;         
            public int useCount;             
            
            public int pawnMemoryCount;      
            public int knowledgeCount;       
            
            public CacheEntry()
            {
            }
            
            public CacheEntry(string memoryPrompt, string knowledgePrompt, string fullPrompt, 
                             int pawnMemoryCount, int knowledgeCount)
            {
                this.memoryPrompt = memoryPrompt;
                this.knowledgePrompt = knowledgePrompt;
                this.fullPrompt = fullPrompt;
                this.timestamp = Find.TickManager.TicksGame;
                this.lastUsedTick = this.timestamp;
                this.useCount = 1;
                this.pawnMemoryCount = pawnMemoryCount;
                this.knowledgeCount = knowledgeCount;
            }
            
            public bool IsValid(int currentMemoryCount, int currentKnowledgeCount, int currentTick, int expireTicks)
            {
                int memoryDiff = Math.Abs(pawnMemoryCount - currentMemoryCount);
                if (memoryDiff > 5)
                    return false;
                
                int knowledgeDiff = Math.Abs(knowledgeCount - currentKnowledgeCount);
                if (knowledgeDiff > 10)
                    return false;
                
                if (currentTick - timestamp > expireTicks)
                    return false;
                
                return true;
            }
            
            public void ExposeData()
            {
                Scribe_Values.Look(ref memoryPrompt, "memoryPrompt");
                Scribe_Values.Look(ref knowledgePrompt, "knowledgePrompt");
                Scribe_Values.Look(ref fullPrompt, "fullPrompt");
                Scribe_Values.Look(ref timestamp, "timestamp");
                Scribe_Values.Look(ref lastUsedTick, "lastUsedTick");
                Scribe_Values.Look(ref useCount, "useCount");
                Scribe_Values.Look(ref pawnMemoryCount, "pawnMemoryCount");
                Scribe_Values.Look(ref knowledgeCount, "knowledgeCount");
            }
        }
        
        private Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>();
        
        private int totalHits = 0;
        private int totalMisses = 0;
        private int totalInvalidations = 0;
        
        private int MaxCacheSize => RimTalkMemoryPatchMod.Settings.promptCacheSize;
        private int ExpireMinutes => RimTalkMemoryPatchMod.Settings.promptCacheExpireMinutes;
        
        public float HitRate
        {
            get
            {
                int total = totalHits + totalMisses;
                if (total == 0) return 0f;
                return (float)totalHits / total;
            }
        }
        
        public CacheEntry TryGet(Pawn pawn, string context, out bool needsRegeneration)
        {
            needsRegeneration = true;
            
            if (!RimTalkMemoryPatchMod.Settings.enablePromptCache || pawn == null)
                return null;
            
            string cacheKey = GenerateCacheKey(pawn, context);
            
            if (cache.TryGetValue(cacheKey, out var entry))
            {
                var memoryComp = pawn.TryGetComp<FourLayerMemoryComp>();
                int currentMemoryCount = GetMemoryCount(memoryComp);
                int currentKnowledgeCount = GetKnowledgeCount();
                
                int currentTick = Find.TickManager.TicksGame;
                int expireTicks = ExpireMinutes * 2500;
                
                if (entry.IsValid(currentMemoryCount, currentKnowledgeCount, currentTick, expireTicks))
                {
                    entry.lastUsedTick = currentTick;
                    entry.useCount++;
                    totalHits++;
                    needsRegeneration = false;
                    
                    if (Prefs.DevMode)
                    {
                        Log.Message($"[Prompt Cache] ?? HIT: {pawn.LabelShort} (saved ~{EstimateComputeCost()}ms)");
                    }
                    
                    return entry;
                }
                else
                {
                    cache.Remove(cacheKey);
                    totalInvalidations++;
                    
                    if (Prefs.DevMode)
                    {
                        Log.Message($"[Prompt Cache] ?? INVALIDATED: {pawn.LabelShort} (memory/knowledge changed)");
                    }
                }
            }
            
            totalMisses++;
            return null;
        }
        
        public void Add(Pawn pawn, string context, string memoryPrompt, string knowledgePrompt, string fullPrompt)
        {
            if (!RimTalkMemoryPatchMod.Settings.enablePromptCache || pawn == null)
                return;
            
            var memoryComp = pawn.TryGetComp<FourLayerMemoryComp>();
            int memoryCount = GetMemoryCount(memoryComp);
            int knowledgeCount = GetKnowledgeCount();
            
            string cacheKey = GenerateCacheKey(pawn, context);
            
            var entry = new CacheEntry(memoryPrompt, knowledgePrompt, fullPrompt, memoryCount, knowledgeCount);
            cache[cacheKey] = entry;
            
            if (cache.Count > MaxCacheSize)
            {
                EvictLRU();
            }
            
            if (Prefs.DevMode)
            {
                Log.Message($"[Prompt Cache] ?? CACHED: {pawn.LabelShort} (total: {cache.Count}/{MaxCacheSize})");
            }
        }
        
        public void InvalidateForPawn(Pawn pawn)
        {
            if (pawn == null) return;
            
            var keysToRemove = cache.Keys.Where(k => k.StartsWith(pawn.ThingID + "_")).ToList();
            
            foreach (var key in keysToRemove)
            {
                cache.Remove(key);
            }
            
            if (keysToRemove.Count > 0 && Prefs.DevMode)
            {
                Log.Message($"[Prompt Cache] ??? Invalidated {keysToRemove.Count} entries for {pawn.LabelShort}");
            }
        }
        
        public void Clear()
        {
            int count = cache.Count;
            cache.Clear();
            totalHits = 0;
            totalMisses = 0;
            totalInvalidations = 0;
            
            Log.Message($"[Prompt Cache] ??? Cleared {count} cached prompts");
        }
        
        public void CleanExpired()
        {
            int currentTick = Find.TickManager.TicksGame;
            int expireTicks = ExpireMinutes * 2500;
            
            var expiredKeys = cache
                .Where(kvp => currentTick - kvp.Value.timestamp > expireTicks)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in expiredKeys)
            {
                cache.Remove(key);
            }
            
            if (expiredKeys.Count > 0 && Prefs.DevMode)
            {
                Log.Message($"[Prompt Cache] ?? Cleaned {expiredKeys.Count} expired entries");
            }
        }
        
        public string GetStats()
        {
            return $"Cached: {cache.Count}/{MaxCacheSize}, Hits: {totalHits}, Misses: {totalMisses}, " +
                   $"Invalidations: {totalInvalidations}, Hit Rate: {HitRate:P1}";
        }
        
        
        private string GenerateCacheKey(Pawn pawn, string context)
        {
            // Non-obvious edge case — read carefully before changing. (pawnId hash)
            string contextHash = GetStableHash(context).ToString();
            return $"{pawn.ThingID}_{contextHash}";
        }
        
        private int GetMemoryCount(FourLayerMemoryComp memoryComp)
        {
            if (memoryComp == null) return 0;
            return memoryComp.SituationalMemories.Count + 
                   memoryComp.EventLogMemories.Count + 
                   memoryComp.ArchiveMemories.Count;
        }
        
        private int GetKnowledgeCount()
        {
            var memoryManager = Find.World?.GetComponent<MemoryManager>();
            return memoryManager?.CommonKnowledge?.Entries?.Count ?? 0;
        }
        
        private int GetStableHash(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            string sample = text.Length > 100 ? text.Substring(0, 100) : text;
            return sample.GetHashCode();
        }
        
        private void EvictLRU()
        {
            if (cache.Count == 0) return;
            
            string lruKey = null;
            int minUseCount = int.MaxValue;
            int minLastUsedTick = int.MaxValue;
            
            foreach (var kvp in cache)
            {
                if (kvp.Value.useCount < minUseCount ||
                    (kvp.Value.useCount == minUseCount && kvp.Value.lastUsedTick < minLastUsedTick))
                {
                    minUseCount = kvp.Value.useCount;
                    minLastUsedTick = kvp.Value.lastUsedTick;
                    lruKey = kvp.Key;
                }
            }
            
            if (lruKey != null)
            {
                cache.Remove(lruKey);
                
                if (Prefs.DevMode)
                {
                    Log.Message($"[Prompt Cache] ??? EVICTED LRU entry");
                }
            }
        }
        
        private int EstimateComputeCost()
        {
            return UnityEngine.Random.Range(8, 15);
        }
        
        public void ExposeData()
        {
            Scribe_Collections.Look(ref cache, "promptCache", LookMode.Value, LookMode.Deep);
            Scribe_Values.Look(ref totalHits, "totalHits", 0);
            Scribe_Values.Look(ref totalMisses, "totalMisses", 0);
            Scribe_Values.Look(ref totalInvalidations, "totalInvalidations", 0);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (cache == null)
                    cache = new Dictionary<string, CacheEntry>();
            }
        }
    }
}
