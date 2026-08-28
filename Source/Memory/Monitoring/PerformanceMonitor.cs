using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory.Persistence;
using Ustas.RimAI.Core.Diagnostics;
using Ustas.RimAI.Communication.Memory.Diagnostics;

namespace Ustas.RimAI.Communication.Memory.Monitoring
{
    public static class PerformanceMonitor
    {
        private static Dictionary<string, ModuleStats> moduleStats = new Dictionary<string, ModuleStats>();
        private static int sessionStartTick = 0;
        
        private static int totalAPIRequests = 0;
        private static int successfulRequests = 0;
        private static int failedRequests = 0;
        private static double estimatedCostYuan = 0.0;
        
        private static Dictionary<string, PerformanceMetric> performanceMetrics = new Dictionary<string, PerformanceMetric>();
        
        public static void Initialize()
        {
            sessionStartTick = Find.TickManager?.TicksGame ?? 0;
            
            RegisterModule("Embedding", "Семантичні вбудовування");
            RegisterModule("VectorDB", "Векторна база даних");
            RegisterModule("RAG", "Пошук RAG");
            RegisterModule("AIDatabase", "База даних ШІ");
            RegisterModule("Injection", "Динамічне додавання");
            
            if (Prefs.DevMode)
            {
                ModuleLog.Message("[Performance Monitor] Initialized");
            }
        }
        
        private static void RegisterModule(string id, string name)
        {
            if (!moduleStats.ContainsKey(id))
            {
                moduleStats[id] = new ModuleStats
                {
                    ModuleId = id,
                    ModuleName = name
                };
            }
        }
        
        public static void RecordAPIRequest(string module, bool success, double costYuan = 0.0)
        {
            totalAPIRequests++;
            
            if (success)
            {
                successfulRequests++;
            }
            else
            {
                failedRequests++;
            }
            
            estimatedCostYuan += costYuan;
            
            if (moduleStats.TryGetValue(module, out ModuleStats stats))
            {
                stats.APIRequests++;
                if (success)
                {
                    stats.SuccessfulRequests++;
                }
                else
                {
                    stats.FailedRequests++;
                }
                stats.TotalCostYuan += costYuan;
            }
        }
        
        public static void RecordPerformance(string operation, long durationMs)
        {
            if (!performanceMetrics.TryGetValue(operation, out PerformanceMetric metric))
            {
                metric = new PerformanceMetric { OperationName = operation };
                performanceMetrics[operation] = metric;
            }
            
            metric.ExecutionCount++;
            metric.TotalDurationMs += durationMs;
            
            if (durationMs > metric.MaxDurationMs)
            {
                metric.MaxDurationMs = durationMs;
            }
            
            if (metric.MinDurationMs == 0 || durationMs < metric.MinDurationMs)
            {
                metric.MinDurationMs = durationMs;
            }
        }
        
        public static void RecordCacheHit(string module, bool hit)
        {
            if (moduleStats.TryGetValue(module, out ModuleStats stats))
            {
                stats.CacheRequests++;
                if (hit)
                {
                    stats.CacheHits++;
                }
            }
        }
        
        public static string GetFullReport()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine("  Звіт продуктивності RimTalk ExpandMemory");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine();
            
            int sessionDuration = (Find.TickManager?.TicksGame ?? 0) - sessionStartTick;
            int sessionDays = sessionDuration / GenDate.TicksPerDay;
            sb.AppendLine($"Тривалість сеансу: {sessionDays} дн. ({sessionDuration} тіків)");
            sb.AppendLine();
            
            sb.AppendLine("─── Статистика викликів API ───");
            sb.AppendLine($"Усього запитів: {totalAPIRequests}");
            sb.AppendLine($"  Успішно: {successfulRequests} ({GetPercentage(successfulRequests, totalAPIRequests)})");
            sb.AppendLine($"  Невдало: {failedRequests} ({GetPercentage(failedRequests, totalAPIRequests)})");
            sb.AppendLine($"Орієнтовна вартість: ¥{estimatedCostYuan:F4}");
            sb.AppendLine();
            
            sb.AppendLine("─── Статистика модулів ───");
            foreach (var kvp in moduleStats.OrderByDescending(k => k.Value.APIRequests))
            {
                var stats = kvp.Value;
                sb.AppendLine($"\n【{stats.ModuleName}】");
                sb.AppendLine($"  Запити API: {stats.APIRequests}");
                
                if (stats.APIRequests > 0)
                {
                    sb.AppendLine($"    Успішність: {GetPercentage(stats.SuccessfulRequests, stats.APIRequests)}");
                    sb.AppendLine($"    Вартість: ¥{stats.TotalCostYuan:F4}");
                }
                
                if (stats.CacheRequests > 0)
                {
                    sb.AppendLine($"  Запити кешу: {stats.CacheRequests}");
                    sb.AppendLine($"    Частка влучень: {GetPercentage(stats.CacheHits, stats.CacheRequests)}");
                }
            }
            sb.AppendLine();
            
            if (performanceMetrics.Count > 0)
            {
                sb.AppendLine("─── Показники продуктивності ───");
                foreach (var kvp in performanceMetrics.OrderByDescending(k => k.Value.TotalDurationMs))
                {
                    var metric = kvp.Value;
                    long avgMs = metric.ExecutionCount > 0 ? metric.TotalDurationMs / metric.ExecutionCount : 0;
                    
                    sb.AppendLine($"\n{metric.OperationName}:");
                    sb.AppendLine($"  Кількість виконань: {metric.ExecutionCount}");
                    sb.AppendLine($"  Середній час: {avgMs} мс");
                    sb.AppendLine($"  Мін./макс.: {metric.MinDurationMs} мс / {metric.MaxDurationMs} мс");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════");
            
            return sb.ToString();
        }
        
        public static string GetSummary()
        {
            double successRate = totalAPIRequests > 0 ? 
                (double)successfulRequests / totalAPIRequests * 100 : 0;
            
            return $"API: {totalAPIRequests} ({successRate:F1}% успішно) | Вартість: ¥{estimatedCostYuan:F4}";
        }
        
        public static void Reset()
        {
            totalAPIRequests = 0;
            successfulRequests = 0;
            failedRequests = 0;
            estimatedCostYuan = 0.0;
            
            moduleStats.Clear();
            performanceMetrics.Clear();
            
            Initialize();
            
            ModuleLog.Message("[Performance Monitor] Statistics reset");
        }
        
        public static void ExportReport(string filePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = MemorySidecarStorage.BuildPerformanceReportPath(
                        DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                }
                
                string report = GetFullReport();
                MemorySidecarStorage.WriteAllText(filePath, report);
                
                Messages.Message($"Звіт продуктивності експортовано: {filePath}", MessageTypeDefOf.PositiveEvent);
                RimAiLog.Info(RimAiLogCategory.Memory, $"[Performance Monitor] Report exported to: {filePath}");
            }
            catch (Exception ex)
            {
                // RimAI.exception: TEMPORARY_EXPLICIT_EXCEPTION — residual UI/export boundary; keep containment.
                RimAiLog.Error(RimAiLogCategory.Memory, "[Performance Monitor] Failed to export report.", exception: ex);
            }
        }
        
        private static string GetPercentage(int count, int total)
        {
            if (total == 0) return "0%";
            double percentage = (double)count / total * 100;
            return $"{percentage:F1}%";
        }
    }
    
    #region 数据结构
    
    public class ModuleStats
    {
        public string ModuleId;
        public string ModuleName;
        
        public int APIRequests;
        public int SuccessfulRequests;
        public int FailedRequests;
        public double TotalCostYuan;
        
        public int CacheRequests;
        public int CacheHits;
    }
    
    public class PerformanceMetric
    {
        public string OperationName;
        public int ExecutionCount;
        public long TotalDurationMs;
        public long MinDurationMs;
        public long MaxDurationMs;
    }
    
    #endregion
}
