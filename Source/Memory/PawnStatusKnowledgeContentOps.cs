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
            
            // ⭐ 修复：如果已有加入日期，直接使用；否则计算新日期
            string joinDate;
            if (!string.IsNullOrEmpty(existingJoinDate))
            {
                joinDate = existingJoinDate;
            }
            else
            {
                // 计算加入日期（游戏内日期）
                int tile = pawn.Map?.Tile ?? (Find.AnyPlayerHomeMap?.Tile ?? 0);
                float longitude = Find.WorldGrid.LongLatOf(tile).x;

                // 使用 DayOfQuadrum (0-14) 并 +1
                int joinDay = GenDate.DayOfQuadrum(joinTick, longitude) + 1;
                Quadrum joinQuadrum = GenDate.Quadrum(joinTick, longitude);
                int joinYear = GenDate.Year(joinTick, longitude);
                
                // 格式化日期（例如：冬季 5日, 5500年）
                joinDate = $"{joinQuadrum.Label()} {joinDay}日, {joinYear}年";
            }
            
            // 获取完整种族信息（种族+亚种）
            string raceInfo = GetCompleteRaceInfo(pawn);
            
            // 根据天数生成不同描述
            string baseDescription = "";
            
            if (daysInColony < 7)
            {
                // < 7天：新成员描述
                if (daysInColony == 0)
                {
                    baseDescription = $"{name}是殖民地的新成员，今天({joinDate})刚加入";
                }
                else if (daysInColony == 1)
                {
                    baseDescription = $"{name}是殖民地的新成员，昨天({joinDate})加入";
                }
                else
                {
                    baseDescription = $"{name}是殖民地的新成员，{daysInColony}天前({joinDate})加入";
                }
            }
            else
            {
                // >= 7天：资深成员描述
                baseDescription = $"{name}是殖民地的资深成员，已加入殖民地 {daysInColony} 天（加入于{joinDate}），对殖民地的历史和成员关系较为熟悉";
            }
            
            // 附加种族信息和提示信息
            if (!string.IsNullOrEmpty(raceInfo))
            {
                if (daysInColony < 7)
                {
                    return $"{baseDescription}。{raceInfo}。对殖民地的历史和成员关系尚不熟悉";
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
        
        /// <summary>
        /// ⭐ 从已有内容中提取加入日期（避免日期漂移）
        /// </summary>
        internal static string ExtractJoinDateFromContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return null;
            
            try
            {
                // 匹配模式："今天(冬季 5日, 5500年)" 或 "加入于冬季 5日, 5500年"
                // 使用正则表达式提取括号内的日期
                var match = System.Text.RegularExpressions.Regex.Match(
                    content, 
                    @"\(([^)]+季\s*\d+日,\s*\d+年)\)"
                );
                
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                
                // 兼容旧格式："加入于冬季 5日, 5500年）"（没有括号）
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
        
        /// <summary>
        /// 获取完整种族信息（种族+亚种）
        /// </summary>
        internal static string GetCompleteRaceInfo(Pawn pawn)
        {
            if (pawn?.def == null)
                return "";
            
            try
            {
                string pawnName = pawn.LabelShort;
                
                // 1. 获取主种族名称
                string raceName = pawn.def.label ?? pawn.def.defName;
                
                // 2. 尝试获取亚种信息（优先从基因获得）
                string xenotypeName = "";
                
                // 方法A：检查pawn.genes.Xenotype（标准Biotech DLC）
                if (pawn.genes != null && pawn.genes.Xenotype != null)
                {
                    xenotypeName = pawn.genes.Xenotype.label ?? pawn.genes.Xenotype.defName;
                }
                
                // 方法B：检查pawn.story.xenotype（旧版API）
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
                
                // 方法C：检查CustomXenotype（自定义名字）
                if (string.IsNullOrEmpty(xenotypeName) && pawn.genes != null)
                {
                    var customXenotypeField = pawn.genes.GetType().GetField("xenotypeName");
                    if (customXenotypeField != null)
                    {
                        xenotypeName = customXenotypeField.GetValue(pawn.genes) as string;
                    }
                }
                
                // 3. 组合种族和亚种描述
                if (!string.IsNullOrEmpty(xenotypeName))
                {
                    // 避免重复（如"人类-人类"）
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
                    // 只有主种族
                    return $"{pawnName}的种族是{raceName}";
                }
            }
            catch (Exception ex)
            {
                // 容错：如果种族信息获取失败时，返回基础信息
                if (Prefs.DevMode)
                {
                    Log.Warning($"[PawnStatus] Failed to extract race info for {pawn.LabelShort}: {ex.Message}");
                }
                
                return $"{pawn.LabelShort}的种族是{pawn.def?.label ?? "未知"}";
            }
        }
        
        /// <summary>
        /// 检查内容是否为自动生成的（没有被用户编辑）
        /// </summary>
    }
}
