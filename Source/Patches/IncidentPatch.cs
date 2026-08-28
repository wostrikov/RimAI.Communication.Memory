using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Diagnostics;

namespace Ustas.RimAI.Communication.Memory.Patches
{
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute))]
    public static class IncidentPatch
    {
        private static Dictionary<int, RaidEventInfo> activeRaids = new Dictionary<int, RaidEventInfo>();
        
        [HarmonyPostfix]
        public static void Postfix(IncidentWorker __instance, IncidentParms parms, bool __result)
        {
            if (!__result)
                return;
            
            if (!RimTalkMemoryPatchMod.Settings.enableEventRecordKnowledge)
                return;
            
            try
            {
                var incidentDef = __instance.def;
                if (incidentDef == null)
                    return;
                
                if (IsRaidIncident(incidentDef))
                {
                    HandleRaidStart(incidentDef, parms);
                    return;
                }
                
                float importance = CalculateIncidentImportance(incidentDef);
                
                if (importance < 0.5f)
                    return;
                
                string eventText = GenerateEventDescription(incidentDef, parms);
                
                if (string.IsNullOrEmpty(eventText))
                    return;
                
                AddOrUpdateKnowledge(null, eventText, importance);
            }
            catch (Exception ex)
            {
                Log.Error($"[EventRecord] Error in IncidentPatch: {ex.Message}");
            }
        }
        
        private static bool IsRaidIncident(IncidentDef incidentDef)
        {
            string defName = incidentDef.defName;
            return defName.Contains("Raid") || 
                   defName.Contains("Siege") || 
                   defName.Contains("Mech") && defName.Contains("Cluster") ||
                   incidentDef.category == IncidentCategoryDefOf.ThreatBig;
        }
        
        private static void HandleRaidStart(IncidentDef incidentDef, IncidentParms parms)
        {
            int raidId = GenTicks.TicksGame;
            
            string factionName = "Невідомий ворог";
            if (parms.faction != null && !string.IsNullOrEmpty(parms.faction.Name))
            {
                factionName = parms.faction.Name;
            }
            
            string raidType = GetRaidType(incidentDef);
            
            string eventText = $"Сьогодні {factionName} вчинила {raidType}";
            
            var entry = AddOrUpdateKnowledge(null, eventText, 0.9f);
            
            if (entry != null)
            {
                activeRaids[raidId] = new RaidEventInfo
                {
                    entryId = entry.id,
                    factionName = factionName,
                    raidType = raidType,
                    startTick = GenTicks.TicksGame,
                    initialText = eventText
                };
                
                if (!raidCheckActive)
                {
                    raidCheckActive = true;
                }
                
                if (Prefs.DevMode)
                {
                    ModuleLog.Message($"[EventRecord] ?? Raid started: {eventText} (ID: {raidId})");
                }
            }
        }
        
        private static string GetRaidType(IncidentDef incidentDef)
        {
            string defName = incidentDef.defName;
            
            if (defName.Contains("Siege"))
                return "облога";
            else if (defName.Contains("Mech"))
                return "напад механітів";
            else if (defName.Contains("Sapper"))
                return "напад саперів";
            else if (defName.Contains("Breacher"))
                return "напад руйнівників";
            else
                return "напад";
        }
        
        private static bool raidCheckActive = false;
        
        public static void CheckRaidStatus()
        {
            if (!raidCheckActive || activeRaids.Count == 0)
                return;
            
            try
            {
                var library = MemoryManager.GetCommonKnowledge();
                if (library == null)
                    return;
                
                int currentTick = GenTicks.TicksGame;
                var completedRaids = new List<int>();
                
                foreach (var kvp in activeRaids)
                {
                    int raidId = kvp.Key;
                    var raidInfo = kvp.Value;
                    
                    int elapsedTicks = currentTick - raidInfo.startTick;
                    if (elapsedTicks > 10000)
                    {
                        UpdateRaidOutcome(library, raidInfo, true);
                        completedRaids.Add(raidId);
                    }
                    else
                    {
                        bool hasEnemies = CheckForEnemies();
                        
                        if (!hasEnemies && elapsedTicks > 1000)
                        {
                            UpdateRaidOutcome(library, raidInfo, true);
                            completedRaids.Add(raidId);
                        }
                    }
                }
                
                foreach (var raidId in completedRaids)
                {
                    activeRaids.Remove(raidId);
                }
                
                if (activeRaids.Count == 0)
                {
                    raidCheckActive = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[EventRecord] Error checking raid status: {ex.Message}");
            }
        }
        
        private static bool CheckForEnemies()
        {
            if (Find.CurrentMap == null)
                return false;
            
            foreach (var pawn in Find.CurrentMap.mapPawns.AllPawnsSpawned)
            {
                if (pawn.HostileTo(Faction.OfPlayer) && !pawn.Dead && !pawn.Downed)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private static void UpdateRaidOutcome(CommonKnowledgeLibrary library, RaidEventInfo raidInfo, bool defeated)
        {
            var entry = library.Entries.FirstOrDefault(e => e.id == raidInfo.entryId);
            
            if (entry == null)
            {
                return;
            }
            
            if (defeated)
            {
                entry.content = $"{raidInfo.initialText}, колонія успішно відбила напад";
                entry.importance = 0.95f;
            }
            else
            {
                entry.content = $"{raidInfo.initialText}, завдавши тяжких втрат";
                entry.importance = 1.0f;
            }
            
            if (Prefs.DevMode)
            {
                ModuleLog.Message($"[EventRecord] ? Updated raid outcome: {entry.content}");
            }
        }
        
        private static CommonKnowledgeEntry AddOrUpdateKnowledge(string existingId, string eventText, float importance)
        {
            var library = MemoryManager.GetCommonKnowledge();
            if (library == null)
                return null;
            
            CommonKnowledgeEntry entry = null;
            
            if (!string.IsNullOrEmpty(existingId))
            {
                entry = library.Entries.FirstOrDefault(e => e.id == existingId);
                if (entry != null)
                {
                    entry.content = eventText;
                    entry.importance = importance;
                    return entry;
                }
            }
            
            bool exists = library.Entries.Any(e => 
                e.content.Contains(eventText.Substring(0, Math.Min(15, eventText.Length)))
            );
            
            if (!exists)
            {
                entry = new CommonKnowledgeEntry("події,історія", eventText)
                {
                    importance = importance,
                    isEnabled = true,
                    isUserEdited = false
                };
                
                library.AddEntry(entry);
                
                if (Prefs.DevMode)
                {
                    ModuleLog.Message($"[EventRecord] ? Created knowledge: {eventText} (importance: {importance:F2})");
                }
            }
            
            return entry;
        }
        
        private static float CalculateIncidentImportance(IncidentDef incidentDef)
        {
            string defName = incidentDef.defName;
            string label = incidentDef.label;
            
            
            if (defName.Contains("Death") || defName.Contains("Dead") || 
                label.Contains("смерть") || label.Contains("death"))
                return 1.0f;
            
            if (defName.Contains("Marriage") || defName.Contains("Wedding") || 
                label.Contains("одруження") || label.Contains("шлюб"))
                return 0.85f;
            
            if (defName.Contains("Funeral") || defName.Contains("Burial") || 
                label.Contains("похорон") || label.Contains("похорон") || label.Contains("поховати"))
                return 0.9f;
            
            if (defName.Contains("Birthday") || label.Contains("день народження"))
                return 0.7f;
            
            if (defName.Contains("Breakthrough") || defName.Contains("Research") && defName.Contains("Complete") ||
                label.Contains("прорив") || label.Contains("завершити дослідження"))
                return 0.8f;
            
            if (defName.Contains("Anniversary") || label.Contains("річниця"))
                return 0.7f;
            
            if (defName.Contains("Join") || defName.Contains("Refugee") || 
                defName.Contains("WandererJoin") || 
                label.Contains("приєднання") || label.Contains("біженці"))
                return 0.8f;
            
            if (defName.Contains("Infestation") || label.Contains("комаха"))
                return 0.85f;
            
            if (defName.Contains("Fire") || defName.Contains("Explosion") || 
                defName.Contains("Tornado") || defName.Contains("Eclipse") ||
                label.Contains("вогонь") || label.Contains("вибух") || label.Contains("торнадо"))
                return 0.85f;
            
            if (defName.Contains("Caravan") || defName.Contains("Visitor") || 
                defName.Contains("Trade") ||
                label.Contains("торг") || label.Contains("гість"))
                return 0.6f;
            
            if (defName.Contains("Disease") || label.Contains("хвороба") || label.Contains("чума"))
                return 0.75f;
            
            if (defName.Contains("Quest") || label.Contains("завдання"))
                return 0.65f;
            
            return 0.3f;
        }
        
        private static string GenerateEventDescription(IncidentDef incidentDef, IncidentParms parms)
        {
            string label = incidentDef.label;
            string defName = incidentDef.defName;
            
            string timePrefix = "сьогодні";
            
            if (defName.Contains("Marriage") || defName.Contains("Wedding"))
            {
                return $"{timePrefix}справили весілля";
            }
            else if (defName.Contains("Funeral") || defName.Contains("Burial"))
            {
                return $"{timePrefix}провели похорон";
            }
            else if (defName.Contains("Birthday"))
            {
                return $"{timePrefix}святкували день народження";
            }
            else if (defName.Contains("Breakthrough") || defName.Contains("Research") && defName.Contains("Complete"))
            {
                return $"{timePrefix}стався дослідницький прорив";
            }
            else if (defName.Contains("Anniversary"))
            {
                return $"{timePrefix}святкували річницю";
            }
            else if (defName.Contains("WandererJoin") || defName.Contains("RefugeeJoin"))
            {
                return $"{timePrefix}до колонії приєднався новий член";
            }
            else if (defName.Contains("Infestation"))
            {
                return $"{timePrefix}сталася навала комах";
            }
            else if (defName.Contains("Fire"))
            {
                return $"{timePrefix}сталася пожежа";
            }
            else if (defName.Contains("Explosion"))
            {
                return $"{timePrefix}стався вибух";
            }
            else if (defName.Contains("Tornado"))
            {
                return $"{timePrefix}налетів торнадо";
            }
            else if (defName.Contains("Eclipse"))
            {
                return $"{timePrefix}сталося затемнення";
            }
            else if (defName.Contains("TraderCaravan") || defName.Contains("VisitorGroup"))
            {
                return null;
            }
            
            if (!string.IsNullOrEmpty(label))
            {
                return $"{timePrefix}{label}";
            }
            
            return null;
        }
        
        private class RaidEventInfo
        {
            public string entryId;         
            public string factionName;     
            public string raidType;        
            public int startTick;          
            public string initialText;     
        }
    }
}
