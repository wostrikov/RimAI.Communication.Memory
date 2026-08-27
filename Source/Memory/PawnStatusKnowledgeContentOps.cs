using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    /// <summary>
    /// Pawn status knowledge content and race-info builders.
    /// </summary>
    internal static class PawnStatusKnowledgeContentOps
    {
        internal static string GenerateStatusContent(Pawn pawn, int daysInColony, int joinTick, string existingJoinDate = null)
        {
            string name = pawn.LabelShort;
            
            string joinDate;
            if (!string.IsNullOrEmpty(existingJoinDate))
            {
                joinDate = existingJoinDate;
            }
            else
            {
                int tile = pawn.Map?.Tile ?? (Find.AnyPlayerHomeMap?.Tile ?? 0);
                float longitude = Find.WorldGrid.LongLatOf(tile).x;

                int joinDay = GenDate.DayOfQuadrum(joinTick, longitude) + 1;
                Quadrum joinQuadrum = GenDate.Quadrum(joinTick, longitude);
                int joinYear = GenDate.Year(joinTick, longitude);
                
                joinDate = $"{joinQuadrum.Label()} {joinDay}日, {joinYear}年";
            }
            
            string raceInfo = GetCompleteRaceInfo(pawn);
            
            string baseDescription = "";
            
            if (daysInColony < 7)
            {
                if (daysInColony == 0)
                {
                    baseDescription = $"{name} — новачок у колонії, приєднався сьогодні ({joinDate})";
                }
                else if (daysInColony == 1)
                {
                    baseDescription = $"{name} — новачок у колонії, приєднався вчора ({joinDate})";
                }
                else
                {
                    baseDescription = $"{name} — новачок у колонії, приєднався {daysInColony} дн. тому ({joinDate})";
                }
            }
            else
            {
                baseDescription = $"{name} — досвідчений член колонії, у ній уже {daysInColony} дн. (приєднався {joinDate}), добре знає історію колонії та стосунки між її людьми";
            }
            
            if (!string.IsNullOrEmpty(raceInfo))
            {
                if (daysInColony < 7)
                {
                    return $"{baseDescription}. {raceInfo}. Ще погано знає історію колонії та стосунки між її людьми";
                }
                else
                {
                    return $"{baseDescription}。{raceInfo}";
                }
            }
            else
            {
                return baseDescription;
            }
        }
        
        internal static string ExtractJoinDateFromContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;
            
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    content, 
                    @"\(([^)]+季\s*\d+日,\s*\d+年)\)"
                );
                
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                
                match = System.Text.RegularExpressions.Regex.Match(
                    content,
                    @"加入于([^，。）]+季\s*\d+日,\s*\d+年)"
                );
                
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                if (Prefs.DevMode)
                    Log.Warning($"[PawnStatus] Failed to extract join date from content: {ex.Message}");
            }
            
            return null;
        }
        
        internal static string GetCompleteRaceInfo(Pawn pawn)
        {
            if (pawn?.def == null)
                return "";
            
            try
            {
                string pawnName = pawn.LabelShort;
                
                string raceName = pawn.def.label ?? pawn.def.defName;
                
                string xenotypeName = "";
                
                if (pawn.genes != null && pawn.genes.Xenotype != null)
                {
                    xenotypeName = pawn.genes.Xenotype.label ?? pawn.genes.Xenotype.defName;
                }
                
                if (string.IsNullOrEmpty(xenotypeName) && pawn.story != null)
                {
                    var xenotypeField = pawn.story.GetType().GetField("xenotype");
                    if (xenotypeField != null)
                    {
                        var xenotype = xenotypeField.GetValue(pawn.story);
                        if (xenotype != null)
                        {
                            var labelProp = xenotype.GetType().GetProperty("label");
                            if (labelProp != null)
                            {
                                xenotypeName = labelProp.GetValue(xenotype) as string;
                            }
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(xenotypeName) && pawn.genes != null)
                {
                    var customXenotypeField = pawn.genes.GetType().GetField("xenotypeName");
                    if (customXenotypeField != null)
                    {
                        xenotypeName = customXenotypeField.GetValue(pawn.genes) as string;
                    }
                }
                
                if (!string.IsNullOrEmpty(xenotypeName))
                {
                    if (xenotypeName.Equals(raceName, StringComparison.OrdinalIgnoreCase))
                    {
                        return $"{pawnName}的种族是{raceName}";
                    }
                    else
                    {
                        return $"{pawnName}的种族是{raceName}-{xenotypeName}";
                    }
                }
                else
                {
                    return $"{pawnName}的种族是{raceName}";
                }
            }
            catch (Exception ex)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning($"[PawnStatus] Failed to extract race info for {pawn.LabelShort}: {ex.Message}");
                }
                
                return $"{pawn.LabelShort}的种族是{pawn.def?.label ?? "未知"}";
            }
        }
        
    }
}
