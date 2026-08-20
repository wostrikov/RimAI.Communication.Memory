using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

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
            
            string factionName = "未知敌人";
            if (parms.faction != null && !string.IsNullOrEmpty(parms.faction.Name))
            {
                factionName = parms.faction.Name;
            }
            
            string raidType = GetRaidType(incidentDef);
            
            string eventText = $"今天{factionName}发动了{raidType}";
            
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
                    Log.Message($"[EventRecord] ?? Raid started: {eventText} (ID: {raidId})");
                }
            }
        }
        
        private static string GetRaidType(IncidentDef incidentDef)
        {
            string defName = incidentDef.defName;
            
            if (defName.Contains("Siege"))
                return "围城";
            else if (defName.Contains("Mech"))
                return "机械族攻击";
            else if (defName.Contains("Sapper"))
                return "工兵袭击";
            else if (defName.Contains("Breacher"))
                return "破坏者袭击";
            else
                return "袭击";
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
                entry.content = $"{raidInfo.initialText}，殖民地成功击退了进攻";
                entry.importance = 0.95f;
            }
            else
            {
                entry.content = $"{raidInfo.initialText}，造成了严重损失";
                entry.importance = 1.0f;
            }
            
            if (Prefs.DevMode)
            {
                Log.Message($"[EventRecord] ? Updated raid outcome: {entry.content}");
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
                entry = new CommonKnowledgeEntry("事件,历史", eventText)
                {
                    importance = importance,
                    isEnabled = true,
                    isUserEdited = false
                };
                
                library.AddEntry(entry);
                
                if (Prefs.DevMode)
                {
                    Log.Message($"[EventRecord] ? Created knowledge: {eventText} (importance: {importance:F2})");
                }
            }
            
            return entry;
        }
        
        private static float CalculateIncidentImportance(IncidentDef incidentDef)
        {
            string defName = incidentDef.defName;
            string label = incidentDef.label;
            
            
            if (defName.Contains("Death") || defName.Contains("Dead") || 
                label.Contains("死") || label.Contains("death"))
                return 1.0f;
            
            if (defName.Contains("Marriage") || defName.Contains("Wedding") || 
                label.Contains("结婚") || label.Contains("婚"))
                return 0.85f;
            
            if (defName.Contains("Funeral") || defName.Contains("Burial") || 
                label.Contains("葬礼") || label.Contains("葬") || label.Contains("埋葬"))
                return 0.9f;
            
            if (defName.Contains("Birthday") || label.Contains("生日"))
                return 0.7f;
            
            if (defName.Contains("Breakthrough") || defName.Contains("Research") && defName.Contains("Complete") ||
                label.Contains("突破") || label.Contains("完成研究"))
                return 0.8f;
            
            if (defName.Contains("Anniversary") || label.Contains("周年"))
                return 0.7f;
            
            if (defName.Contains("Join") || defName.Contains("Refugee") || 
                defName.Contains("WandererJoin") || 
                label.Contains("加入") || label.Contains("难民"))
                return 0.8f;
            
            if (defName.Contains("Infestation") || label.Contains("虫"))
                return 0.85f;
            
            if (defName.Contains("Fire") || defName.Contains("Explosion") || 
                defName.Contains("Tornado") || defName.Contains("Eclipse") ||
                label.Contains("火") || label.Contains("爆炸") || label.Contains("龙卷风"))
                return 0.85f;
            
            if (defName.Contains("Caravan") || defName.Contains("Visitor") || 
                defName.Contains("Trade") ||
                label.Contains("贸易") || label.Contains("访客"))
                return 0.6f;
            
            if (defName.Contains("Disease") || label.Contains("疾病") || label.Contains("瘟疫"))
                return 0.75f;
            
            if (defName.Contains("Quest") || label.Contains("任务"))
                return 0.65f;
            
            return 0.3f;
        }
        
        private static string GenerateEventDescription(IncidentDef incidentDef, IncidentParms parms)
        {
            string label = incidentDef.label;
            string defName = incidentDef.defName;
            
            string timePrefix = "今天";
            
            if (defName.Contains("Marriage") || defName.Contains("Wedding"))
            {
                return $"{timePrefix}举行了婚礼";
            }
            else if (defName.Contains("Funeral") || defName.Contains("Burial"))
            {
                return $"{timePrefix}举行了葬礼";
            }
            else if (defName.Contains("Birthday"))
            {
                return $"{timePrefix}庆祝了生日";
            }
            else if (defName.Contains("Breakthrough") || defName.Contains("Research") && defName.Contains("Complete"))
            {
                return $"{timePrefix}取得了研究突破";
            }
            else if (defName.Contains("Anniversary"))
            {
                return $"{timePrefix}庆祝了周年纪念";
            }
            else if (defName.Contains("WandererJoin") || defName.Contains("RefugeeJoin"))
            {
                return $"{timePrefix}有新成员加入殖民地";
            }
            else if (defName.Contains("Infestation"))
            {
                return $"{timePrefix}发生了虫族入侵";
            }
            else if (defName.Contains("Fire"))
            {
                return $"{timePrefix}发生了火灾";
            }
            else if (defName.Contains("Explosion"))
            {
                return $"{timePrefix}发生了爆炸";
            }
            else if (defName.Contains("Tornado"))
            {
                return $"{timePrefix}遭遇了龙卷风";
            }
            else if (defName.Contains("Eclipse"))
            {
                return $"{timePrefix}发生了日食";
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
