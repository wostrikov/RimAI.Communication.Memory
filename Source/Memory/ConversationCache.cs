using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Policy;
using Ustas.RimAI.Communication.Memory.Diagnostics;

namespace Ustas.RimAI.Communication.Memory
{
    public class ConversationCache : IExposable
    {
        public class CacheEntry : IExposable
        {
            public string cacheKey;          
            public string dialogue;          
            public int timestamp;            
            public int lastUsedTick;         
            public int useCount;             
            
            public CacheEntry()
            {
                // Serialization / save-load constraint — keep field identity stable.
            }
            
            public CacheEntry(string cacheKey, string dialogue, int timestamp)
            {
                this.cacheKey = cacheKey;
                this.dialogue = dialogue;
                this.timestamp = timestamp;
                this.lastUsedTick = timestamp;
                this.useCount = 1;
            }
            
            public bool IsExpired(int currentTick, int expireTicks)
            {
                return (currentTick - timestamp) > expireTicks;
            }
            
            public void ExposeData()
            {
                Scribe_Values.Look(ref cacheKey, "cacheKey");
                Scribe_Values.Look(ref dialogue, "dialogue");
                Scribe_Values.Look(ref timestamp, "timestamp");
                Scribe_Values.Look(ref lastUsedTick, "lastUsedTick");
                Scribe_Values.Look(ref useCount, "useCount");
            }
        }
        
        private LinkedList<CacheEntry> lruList = new LinkedList<CacheEntry>();
        
        private Dictionary<string, LinkedListNode<CacheEntry>> cacheMap = new Dictionary<string, LinkedListNode<CacheEntry>>();
        
        private int totalHits = 0;
        private int totalMisses = 0;
        
        private int MaxCacheSize => RimTalkMemoryPatchMod.Settings.conversationCacheSize;
        private int ExpireDays => RimTalkMemoryPatchMod.Settings.conversationCacheExpireDays;
        
        public float HitRate
        {
            get
            {
                int total = totalHits + totalMisses;
                if (total == 0) return 0f;
                return (float)totalHits / total;
            }
        }
        
        public string TryGet(string cacheKey)
        {
            if (MemoryColonyCapacityPolicy.DecideLookup(
                    RimTalkMemoryPatchMod.Settings.enableConversationCache,
                    found: false,
                    expired: false) == MemoryCacheLookupAction.Disabled)
                return null;
            
            if (UnityEngine.Random.value < 0.1f)
            {
                CleanExpiredEntries();
            }
            
            if (cacheMap.TryGetValue(cacheKey, out var node))
            {
                int currentTick = Find.TickManager.TicksGame;
                int expireTicks = ExpireDays * 60000;
                
                var entry = node.Value;
                
                var lookup = MemoryColonyCapacityPolicy.DecideLookup(true, found: true, expired: entry.IsExpired(currentTick, expireTicks));
                if (lookup == MemoryCacheLookupAction.Hit)
                {
                    entry.lastUsedTick = currentTick;
                    entry.useCount++;
                    totalHits++;
                    
                    lruList.Remove(node);
                    lruList.AddFirst(node);
                    
                    if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                    {
                        ModuleLog.Message($"[Cache] 🎯 HIT: {cacheKey.Substring(0, Math.Min(30, cacheKey.Length))}... (uses: {entry.useCount})");
                    }
                    
                    return entry.dialogue;
                }
                else
                {
                    lruList.Remove(node);
                    cacheMap.Remove(cacheKey);
                    
                    if (Prefs.DevMode)
                    {
                        ModuleLog.Message($"[Cache] ⏰ EXPIRED: {cacheKey.Substring(0, Math.Min(30, cacheKey.Length))}...");
                    }
                }
            }
            
            totalMisses++;
            return null;
        }
        
        public void Add(string cacheKey, string dialogue)
        {
            if (!RimTalkMemoryPatchMod.Settings.enableConversationCache)
                return;
            
            if (string.IsNullOrEmpty(dialogue))
                return;
            
            int currentTick = Find.TickManager.TicksGame;
            
            if (cacheMap.TryGetValue(cacheKey, out var existingNode))
            {
                var entry = existingNode.Value;
                entry.dialogue = dialogue;
                entry.timestamp = currentTick;
                entry.lastUsedTick = currentTick;
                entry.useCount = 1;
                
                lruList.Remove(existingNode);
                lruList.AddFirst(existingNode);
            }
            else
            {
                var newEntry = new CacheEntry(cacheKey, dialogue, currentTick);
                var newNode = new LinkedListNode<CacheEntry>(newEntry);
                
                lruList.AddFirst(newNode);
                cacheMap[cacheKey] = newNode;
                
                if (MemoryColonyCapacityPolicy.LruEvictCount(cacheMap.Count, MaxCacheSize) > 0)
                {
                    EvictLRU_O1();
                }
            }
            
            if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
            {
                ModuleLog.Message($"[Cache] 💾 ADD: {cacheKey.Substring(0, Math.Min(30, cacheKey.Length))}... (total: {cacheMap.Count})");
            }
        }
        
        private void EvictLRU_O1()
        {
            if (lruList.Count == 0) return;
            
            var lruNode = lruList.Last;
            var lruEntry = lruNode.Value;
            
            lruList.RemoveLast();
            cacheMap.Remove(lruEntry.cacheKey);
            
            if (Prefs.DevMode)
            {
                ModuleLog.Message($"[Cache] 🗑️ EVICT: {lruEntry.cacheKey.Substring(0, Math.Min(30, lruEntry.cacheKey.Length))}... (uses: {lruEntry.useCount})");
            }
        }
        
        private void CleanExpiredEntries()
        {
            int currentTick = Find.TickManager.TicksGame;
            int expireTicks = ExpireDays * 60000;
            int cleanedCount = 0;
            
            while (lruList.Count > 0)
            {
                var tailNode = lruList.Last;
                var tailEntry = tailNode.Value;
                
                if (tailEntry.IsExpired(currentTick, expireTicks))
                {
                    lruList.RemoveLast();
                    cacheMap.Remove(tailEntry.cacheKey);
                    cleanedCount++;
                }
                else
                {
                    break;
                }
            }
            
            if (cleanedCount > 0 && Prefs.DevMode)
            {
                ModuleLog.Message($"[Cache] 🧹 Cleaned {cleanedCount} expired entries");
            }
        }
        
        public void Clear()
        {
            int count = cacheMap.Count;
            lruList.Clear();
            cacheMap.Clear();
            totalHits = 0;
            totalMisses = 0;
            
            ModuleLog.Message($"[Cache] 🗑️ Cleared {count} cached conversations");
        }
        
        public string GetStats()
        {
            return $"Cached: {cacheMap.Count}/{MaxCacheSize}, Hits: {totalHits}, Misses: {totalMisses}, Hit Rate: {HitRate:P1}";
        }
        
        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var cacheList = new List<CacheEntry>(lruList);
                Scribe_Collections.Look(ref cacheList, "conversationCache", LookMode.Deep);
                Scribe_Values.Look(ref totalHits, "totalHits", 0);
                Scribe_Values.Look(ref totalMisses, "totalMisses", 0);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                var cacheList = new List<CacheEntry>();
                Scribe_Collections.Look(ref cacheList, "conversationCache", LookMode.Deep);
                Scribe_Values.Look(ref totalHits, "totalHits", 0);
                Scribe_Values.Look(ref totalMisses, "totalMisses", 0);
                
                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    lruList = new LinkedList<CacheEntry>();
                    cacheMap = new Dictionary<string, LinkedListNode<CacheEntry>>();
                    
                    if (cacheList != null)
                    {
                        foreach (var entry in cacheList)
                        {
                            if (!string.IsNullOrEmpty(entry.cacheKey))
                            {
                                var node = new LinkedListNode<CacheEntry>(entry);
                                lruList.AddLast(node);
                                cacheMap[entry.cacheKey] = node;
                            }
                        }
                        
                        ModuleLog.Message($"[Cache] Loaded {cacheMap.Count} cached conversations");
                    }
                }
            }
            else if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (lruList == null)
                    lruList = new LinkedList<CacheEntry>();
                if (cacheMap == null)
                    cacheMap = new Dictionary<string, LinkedListNode<CacheEntry>>();
            }
        }
    }
    
    public static class CacheKeyGenerator
    {
        public static string Generate(Pawn speaker, Pawn listener, string topic)
        {
            if (speaker == null || listener == null)
                return null;
            
            string speakerName = speaker.LabelShort;
            string listenerName = listener.LabelShort;
            
            string moodLevel = GetMoodLevel(speaker.needs?.mood?.CurLevel ?? 0.5f);
            
            string relationLevel = GetRelationLevel(speaker, listener);
            
            
            return $"{speakerName}_{listenerName}_{moodLevel}_{relationLevel}";
        }
        
        private static string GetMoodLevel(float mood)
        {
            if (mood > 0.4f) return "positive";
            return "negative";
        }
        
        private static string GetRelationLevel(Pawn speaker, Pawn listener)
        {
            if (speaker.relations == null || listener.relations == null)
                return "neutral";
            
            int opinion = speaker.relations.OpinionOf(listener);
            
            if (opinion > 0) return "positive";
            return "negative";
        }
    }
}
