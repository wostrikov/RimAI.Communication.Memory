using Ustas.RimAI.Communication.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Ustas.RimAI.Communication.Memory.Diagnostics;

namespace Ustas.RimAI.Communication.Memory
{
    public static class AdaptiveThresholdManager
    {
        private static List<float> memoryScoreHistory = new List<float>();
        private static List<float> knowledgeScoreHistory = new List<float>();

        private static int logCounter = 0;
        private const int LOG_INTERVAL = 100;
        
        private const int MAX_HISTORY_SIZE = 1000; 
        private const int MIN_SAMPLES = 50;        
        private const float PERCENTILE_TARGET = 0.20f;

        private const float MIN_THRESHOLD = 0.05f;
        private const float MAX_THRESHOLD = 0.50f;
        private const float ADJUSTMENT_RATE = 0.05f;

        public static void RecordMemoryScore(float score)
        {
            if (score < 0 || score > 1)
                return;

            memoryScoreHistory.Add(score);

            if (memoryScoreHistory.Count > MAX_HISTORY_SIZE)
            {
                memoryScoreHistory.RemoveAt(0);
            }
        }

        public static void RecordKnowledgeScore(float score)
        {
            if (score < 0 || score > 1)
                return;

            knowledgeScoreHistory.Add(score);

            if (knowledgeScoreHistory.Count > MAX_HISTORY_SIZE)
            {
                knowledgeScoreHistory.RemoveAt(0);
            }
        }

        public static void RecordScores(
            List<ScoredItem<MemoryEntry>> memoryScores,
            List<ScoredItem<CommonKnowledgeEntry>> knowledgeScores)
        {
            if (memoryScores != null)
            {
                foreach (var item in memoryScores)
                {
                    RecordMemoryScore(item.Score);
                }
            }

            if (knowledgeScores != null)
            {
                foreach (var item in knowledgeScores)
                {
                    RecordKnowledgeScore(item.Score);
                }
            }
        }

        public static float GetRecommendedMemoryThreshold()
        {
            if (memoryScoreHistory.Count < MIN_SAMPLES)
            {
                return 0.20f;
            }

            return CalculateAdaptiveThreshold(memoryScoreHistory, "Memory");
        }

        public static float GetRecommendedKnowledgeThreshold()
        {
            if (knowledgeScoreHistory.Count < MIN_SAMPLES)
            {
                return 0.15f;
            }

            return CalculateAdaptiveThreshold(knowledgeScoreHistory, "Knowledge");
        }

        private static float CalculateAdaptiveThreshold(List<float> scores, string type)
        {
            var stats = CalculateStatistics(scores);

            float percentileThreshold = CalculatePercentile(scores, PERCENTILE_TARGET);

            float meanThreshold = stats.Mean - (stats.StdDev * 0.5f);

            float recommendedThreshold = (percentileThreshold * 0.7f) + (meanThreshold * 0.3f);

            recommendedThreshold = Math.Max(MIN_THRESHOLD, Math.Min(MAX_THRESHOLD, recommendedThreshold));

            float currentThreshold = type == "Memory" 
                ? GetCurrentMemoryThreshold() 
                : GetCurrentKnowledgeThreshold();

            float smoothedThreshold = SmoothAdjustment(currentThreshold, recommendedThreshold);

            logCounter++;
            if (Prefs.DevMode && logCounter % LOG_INTERVAL == 0)
            {
                ModuleLog.Message($"[Adaptive Threshold] {type} - Current: {currentThreshold:F3}, " +
                           $"Recommended: {recommendedThreshold:F3}, Smoothed: {smoothedThreshold:F3} " +
                           $"(Mean: {stats.Mean:F3}, StdDev: {stats.StdDev:F3}, Samples: {scores.Count})");
            }

            return smoothedThreshold;
        }

        private static float CalculatePercentile(List<float> scores, float percentile)
        {
            var sorted = scores.OrderByDescending(s => s).ToList();
            int index = (int)(sorted.Count * percentile);
            index = Math.Max(0, Math.Min(sorted.Count - 1, index));
            return sorted[index];
        }

        private static Statistics CalculateStatistics(List<float> scores)
        {
            if (scores.Count == 0)
                return new Statistics { Mean = 0.2f, StdDev = 0.1f };

            float mean = scores.Average();
            float variance = scores.Select(s => (s - mean) * (s - mean)).Average();
            float stdDev = (float)Math.Sqrt(variance);

            return new Statistics
            {
                Mean = mean,
                StdDev = stdDev,
                Min = scores.Min(),
                Max = scores.Max(),
                Count = scores.Count
            };
        }

        private static float SmoothAdjustment(float current, float target)
        {
            float difference = target - current;
            float adjustment = Math.Sign(difference) * Math.Min(Math.Abs(difference), ADJUSTMENT_RATE);
            return current + adjustment;
        }

        public static void ApplyRecommendedThresholds()
        {
            var settings = RimTalkMemoryPatchMod.Settings;
            if (settings == null)
                return;

            float memoryThreshold = GetRecommendedMemoryThreshold();
            float knowledgeThreshold = GetRecommendedKnowledgeThreshold();

            settings.memoryScoreThreshold = memoryThreshold;
            settings.knowledgeScoreThreshold = knowledgeThreshold;

            if (Prefs.DevMode)
            {
                ModuleLog.Message($"[Adaptive Threshold] Applied - Memory: {memoryThreshold:F3}, Knowledge: {knowledgeThreshold:F3}");
            }
        }

        private static float GetCurrentMemoryThreshold()
        {
            return RimTalkMemoryPatchMod.Settings?.memoryScoreThreshold ?? 0.20f;
        }

        private static float GetCurrentKnowledgeThreshold()
        {
            return RimTalkMemoryPatchMod.Settings?.knowledgeScoreThreshold ?? 0.15f;
        }

        public static ThresholdDiagnostics GetDiagnostics()
        {
            var memoryStats = CalculateStatistics(memoryScoreHistory);
            var knowledgeStats = CalculateStatistics(knowledgeScoreHistory);

            return new ThresholdDiagnostics
            {
                MemoryStats = memoryStats,
                KnowledgeStats = knowledgeStats,
                CurrentMemoryThreshold = GetCurrentMemoryThreshold(),
                CurrentKnowledgeThreshold = GetCurrentKnowledgeThreshold(),
                RecommendedMemoryThreshold = GetRecommendedMemoryThreshold(),
                RecommendedKnowledgeThreshold = GetRecommendedKnowledgeThreshold(),
                MemorySampleCount = memoryScoreHistory.Count,
                KnowledgeSampleCount = knowledgeScoreHistory.Count
            };
        }

        public static void ResetHistory()
        {
            memoryScoreHistory.Clear();
            knowledgeScoreHistory.Clear();
            
            if (Prefs.DevMode)
            {
                ModuleLog.Message("[Adaptive Threshold] History reset");
            }
        }

        public static ScoreDistribution GetScoreDistribution()
        {
            return new ScoreDistribution
            {
                MemoryScores = new List<float>(memoryScoreHistory),
                KnowledgeScores = new List<float>(knowledgeScoreHistory),
                MemoryBuckets = CreateHistogramBuckets(memoryScoreHistory, 10),
                KnowledgeBuckets = CreateHistogramBuckets(knowledgeScoreHistory, 10)
            };
        }

        private static Dictionary<string, int> CreateHistogramBuckets(List<float> scores, int bucketCount)
        {
            var buckets = new Dictionary<string, int>();
            if (scores.Count == 0)
                return buckets;

            float bucketSize = 1.0f / bucketCount;

            for (int i = 0; i < bucketCount; i++)
            {
                float lower = i * bucketSize;
                float upper = (i + 1) * bucketSize;
                string key = $"{lower:F2}-{upper:F2}";
                int count = scores.Count(s => s >= lower && s < upper);
                buckets[key] = count;
            }

            return buckets;
        }

        #region 数据结构

        public struct Statistics
        {
            public float Mean;
            public float StdDev;
            public float Min;
            public float Max;
            public int Count;
        }

        public class ThresholdDiagnostics
        {
            public Statistics MemoryStats;
            public Statistics KnowledgeStats;
            public float CurrentMemoryThreshold;
            public float CurrentKnowledgeThreshold;
            public float RecommendedMemoryThreshold;
            public float RecommendedKnowledgeThreshold;
            public int MemorySampleCount;
            public int KnowledgeSampleCount;
        }

        public class ScoreDistribution
        {
            public List<float> MemoryScores;
            public List<float> KnowledgeScores;
            public Dictionary<string, int> MemoryBuckets;
            public Dictionary<string, int> KnowledgeBuckets;
        }

        #endregion
    }
}
