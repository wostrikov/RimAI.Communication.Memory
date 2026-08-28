using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory
{
    public static class PawnStatusKnowledgeGenerator
    {
        private static Dictionary<int, int> lastUpdateTicks = new Dictionary<int, int>();
        private const int UPDATE_INTERVAL_TICKS = 60000;
        
        private static HashSet<int> userDeletedPawns = new HashSet<int>();
        
        private const int NEW_COLONIST_THRESHOLD_DAYS = 7;
        
        public static void MarkAsUserDeleted(int pawnId)
        {
            userDeletedPawns.Add(pawnId);
            lastUpdateTicks.Remove(pawnId);
            
            if (Prefs.DevMode)
                Log.Message($"[PawnStatus] Marked pawn {pawnId} as user-deleted, will not regenerate");
        }
        
        public static bool IsUserDeleted(int pawnId)
        {
            return userDeletedPawns.Contains(pawnId);
        }
        
        public static void ClearUserDeletedMark(int pawnId)
        {
            userDeletedPawns.Remove(pawnId);
            
            if (Prefs.DevMode)
                Log.Message($"[PawnStatus] Cleared user-deleted mark for pawn {pawnId}");
        }
        
        public static void UpdateAllColonistStatus()
        {
            if (!RimTalkMemoryPatchMod.Settings.enablePawnStatusKnowledge)
                return;
            
            var library = MemoryManager.GetCommonKnowledge();
            if (library == null) return;

            int currentTick = Find.TickManager.TicksGame;
            int updatedCount = 0;
            
            var allColonists = new List<Pawn>();
            
            foreach (var map in Find.Maps)
            {
                if (map.mapPawns != null)
                {
                    allColonists.AddRange(map.mapPawns.FreeColonists);
                }
            }
            
            foreach (var caravan in Find.WorldObjects.Caravans)
            {
                if (caravan.IsPlayerControlled && caravan.pawns != null)
                {
                    foreach (var pawn in caravan.pawns.InnerListForReading)
                    {
                        if (pawn.IsColonist && !allColonists.Contains(pawn))
                        {
                            allColonists.Add(pawn);
                        }
                    }
                }
            }
            
            foreach (var pawn in allColonists)
            {
                try
                {
                    int pawnID = pawn.thingIDNumber;
                    
                    if (IsUserDeleted(pawnID))
                    {
                        continue;
                    }
                    
                    if (!lastUpdateTicks.TryGetValue(pawnID, out int lastUpdate))
                    {
                        lastUpdate = 0;
                    }
                    
                    int ticksSinceUpdate = currentTick - lastUpdate;
                    
                    if (ticksSinceUpdate >= UPDATE_INTERVAL_TICKS)
                    {
                        UpdatePawnStatusKnowledge(pawn, library, currentTick);
                        lastUpdateTicks[pawnID] = currentTick;
                        updatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    if (Prefs.DevMode && UnityEngine.Random.value < 0.2f)
                    {
                        Log.Error($"[PawnStatus] Error updating status for {pawn.LabelShort}: {ex.Message}");
                    }
                }
            }
            
            if (updatedCount > 0 && Prefs.DevMode && UnityEngine.Random.value < 0.1f)
            {
                Log.Message($"[PawnStatus] Updated {updatedCount} colonist status knowledge entries");
            }
        }

        public static void UpdatePawnStatusKnowledge(Pawn pawn, CommonKnowledgeLibrary library, int currentTick)
        {
            if (pawn == null || library == null) return;

            try
            {
                if (pawn.RaceProps != null && pawn.RaceProps.Humanlike)
                {
                    float ageYears = pawn.ageTracker.AgeBiologicalYearsFloat;
                    if (ageYears < 3f)
                    {
                        CleanupPawnStatusKnowledge(pawn, library);
                        lastUpdateTicks.Remove(pawn.thingIDNumber);
                        return;
                    }
                }
                
                int joinTick = CalculateJoinTick(pawn, currentTick);
                int daysInColony = CalculateDaysInColony(joinTick, currentTick);
                
                if (Prefs.DevMode && UnityEngine.Random.value < 0.05f)
                {
                    Log.Message($"[PawnStatus] {pawn.LabelShort}: joinTick={joinTick}, currentTick={currentTick}, daysInColony={daysInColony}");
                }

                string statusTag = $"Стан колоніста,{pawn.LabelShort}";
                
                var existingEntry = library.Entries.FirstOrDefault(e => 
                    (e.targetPawnId == pawn.thingIDNumber && e.tag.Contains("Стан колоніста")) ||
                    (e.tag.Contains(pawn.LabelShort) && e.tag.Contains("Стан колоніста"))
                );

                float defaultImportance = 0.5f;

                if (existingEntry != null)
                {
                    if (existingEntry.isUserEdited)
                    {
                        return;
                    }
                    
                    bool isAutoGenerated = IsAutoGeneratedContent(existingEntry.content);
                    
                    if (isAutoGenerated)
                    {
                        string existingJoinDate = ExtractJoinDateFromContent(existingEntry.content);
                        string newContent = GenerateStatusContent(pawn, daysInColony, joinTick, existingJoinDate);
                        
                        existingEntry.content = newContent;
                        existingEntry.importance = defaultImportance;
                        existingEntry.targetPawnId = pawn.thingIDNumber;
                        if (!existingEntry.tag.Contains(pawn.LabelShort))
                        {
                             existingEntry.tag = statusTag;
                        }
                        
                        if (Prefs.DevMode && UnityEngine.Random.value < 0.05f)
                        {
                            Log.Message($"[PawnStatus] Updated: {pawn.LabelShort} (days: {daysInColony}) -> {newContent}");
                        }
                    }
                }
                else
                {
                    string newContent = GenerateStatusContent(pawn, daysInColony, joinTick, null);
                    
                    var newEntry = new CommonKnowledgeEntry(statusTag, newContent)
                    {
                        importance = defaultImportance,
                        isEnabled = true,
                        isUserEdited = false,
                        targetPawnId = pawn.thingIDNumber
                    };
                    
                    library.AddEntry(newEntry);
                    
                    if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                    {
                        Log.Message($"[PawnStatus] Created: {pawn.LabelShort} (days: {daysInColony}, importance: {defaultImportance:F2})");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[PawnStatus] Failed to update status for {pawn?.LabelShort ?? "Unknown"}: {ex.Message}");
            }
        }
        
        private static int CalculateJoinTick(Pawn pawn, int currentTick)
        {
            try
            {
                if (pawn.records == null)
                    return currentTick;
                
                
                var recordDef = RecordDefOf.TimeAsColonistOrColonyAnimal;
                
                if (recordDef == null)
                {
                    if (Prefs.DevMode)
                        Log.Warning($"[PawnStatus] RecordDef 'TimeAsColonistOrColonyAnimal' not found (this should never happen with strong reference)");
                    return currentTick;
                }
                
                float timeAsColonist = pawn.records.GetValue(recordDef);
                
                if (timeAsColonist <= 0)
                {
                    return currentTick;
                }
                
                int joinTick = currentTick - (int)timeAsColonist;
                
                if (joinTick < 0)
                {
                    joinTick = 0;
                }
                
                return joinTick;
            }
            catch (Exception ex)
            {
                Log.Error($"[PawnStatus] Error calculating join tick for {pawn?.LabelShort}: {ex.Message}");
                return currentTick;
            }
        }
        
        private static int CalculateDaysInColony(int joinTick, int currentTick)
        {
            int ticksInColony = currentTick - joinTick;
            int daysInColony = ticksInColony / GenDate.TicksPerDay;
            
            if (daysInColony < 0)
            {
                Log.Warning($"[PawnStatus] Negative days detected: {daysInColony}, resetting to 0");
                daysInColony = 0;
            }
            
            return daysInColony;
        }

        internal static string GenerateStatusContent(Pawn pawn, int daysInColony, int joinTick, string existingJoinDate = null)
        {
            return PawnStatusKnowledgeContentOps.GenerateStatusContent(pawn, daysInColony, joinTick, null);
        }

        internal static string ExtractJoinDateFromContent(string content)
        {
            return PawnStatusKnowledgeContentOps.ExtractJoinDateFromContent(content);
        }

        internal static string GetCompleteRaceInfo(Pawn pawn)
        {
            return PawnStatusKnowledgeContentOps.GetCompleteRaceInfo(pawn);
        }

        private static bool IsAutoGeneratedContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;
            
            var autoKeywords = new[] 
            { 
                "щойно приєднався", "новий член", "Досвідчений член", "вже в колонії" 
            };
            
            return autoKeywords.Any(k => content.Contains(k));
        }
        
        public static void CleanupPawnStatusKnowledge(Pawn pawn, CommonKnowledgeLibrary library)
        {
            if (pawn == null || library == null) return;

            var entry = library.Entries.FirstOrDefault(e => 
                e.tag.Contains(pawn.LabelShort) && 
                e.tag.Contains("Стан колоніста")
            );
            
            if (entry != null)
            {
                library.RemoveEntry(entry);
                
                lastUpdateTicks.Remove(pawn.thingIDNumber);
                
                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    Log.Message($"[PawnStatus] Removed status for {pawn.LabelShort}");
                }
            }
        }
        
        public static void CleanupUpdateRecords()
        {
            var allLivingColonists = new List<Pawn>();
            
            foreach (var map in Find.Maps)
            {
                if (map.mapPawns != null)
                {
                    allLivingColonists.AddRange(map.mapPawns.FreeColonists);
                }
            }
            
            foreach (var caravan in Find.WorldObjects.Caravans)
            {
                if (caravan.IsPlayerControlled && caravan.pawns != null)
                {
                    foreach (var pawn in caravan.pawns.InnerListForReading)
                    {
                        if (pawn.IsColonist && !allLivingColonists.Contains(pawn))
                        {
                            allLivingColonists.Add(pawn);
                        }
                    }
                }
            }
            
            var allColonistIDs = new HashSet<int>(allLivingColonists.Select(p => p.thingIDNumber));
            
            var toRemove = new List<int>();
            
            foreach (var pawnID in lastUpdateTicks.Keys.ToList())
            {
                if (allColonistIDs.Contains(pawnID))
                    continue;
                
                Pawn pawn = null;
                
                foreach (var map in Find.Maps)
                {
                    pawn = map.mapPawns.AllPawns.FirstOrDefault(p => p.thingIDNumber == pawnID);
                    if (pawn != null) break;
                }
                
                if (pawn == null && Find.WorldPawns != null)
                {
                    pawn = Find.WorldPawns.AllPawnsAlive.FirstOrDefault(p => p.thingIDNumber == pawnID);
                }
                
                bool shouldRemove = false;
                
                if (pawn == null)
                {
                    shouldRemove = true;
                }
                else
                {
                    if (pawn.Dead)
                    {
                        shouldRemove = true;
                        if (Prefs.DevMode)
                            Log.Message($"[PawnStatus] Removing dead pawn: {pawn.LabelShort}");
                    }
                    else if (pawn.Faction != Faction.OfPlayer)
                    {
                        shouldRemove = true;
                        if (Prefs.DevMode)
                            Log.Message($"[PawnStatus] Removing non-player pawn: {pawn.LabelShort}");
                    }
                }
                
                if (shouldRemove)
                {
                    toRemove.Add(pawnID);
                }
            }
            
            foreach (var id in toRemove)
            {
                lastUpdateTicks.Remove(id);
            }
            
            if (toRemove.Count > 0 && Prefs.DevMode)
            {
                Log.Message($"[PawnStatus] Cleaned up {toRemove.Count} update records");
            }
        }
    }
}
