using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    public static class AdvancedScoringSystem
    {
        public enum ConversationSceneType
        {
            Casual,        
            EmotionalTalk, 
            WorkDiscussion,
            HistoryRecall, 
            Emergency,     
            Introduction   
        }

        public class ScoringWeights
        {
            public float ContextRelevance = 0.40f; 
            public float Recency = 0.20f;          
            public float Importance = 0.20f;       
            public float Diversity = 0.10f;        
            public float LayerPriority = 0.10f;    

            public float EmotionalBoost = 1.3f;    
            public float RelationshipBoost = 1.2f; 
            public float ConversationBoost = 1.1f; 
        }

        private static ScoringWeights defaultWeights = new ScoringWeights();

        public static List<ScoredItem<MemoryEntry>> ScoreMemories(
            List<MemoryEntry> memories,
            string context,
            Pawn speaker = null,
            Pawn listener = null)
        {
            if (memories == null || memories.Count == 0)
                return new List<ScoredItem<MemoryEntry>>();

            ConversationSceneType scene = IdentifyScene(context);

            ScoringWeights weights = AdjustWeightsForScene(scene, defaultWeights);

            ContextFeatures features = ExtractContextFeatures(context, speaker, listener);

            var scored = new List<ScoredItem<MemoryEntry>>();
            foreach (var memory in memories)
            {
                float score = ScoreSingleMemory(memory, features, weights, speaker, listener);
                
                scored.Add(new ScoredItem<MemoryEntry>
                {
                    Item = memory,
                    Score = score,
                    Breakdown = new ScoreBreakdown
                    {
                        ContextRelevance = CalculateContextRelevance(memory, features),
                        Recency = CalculateRecency(memory),
                        Importance = memory.Importance,
                        Diversity = 0f,
                        LayerPriority = GetLayerPriority(memory.Layer)
                    }
                });
            }

            ApplyDiversityBoost(scored);

            return scored.OrderByDescending(s => s.Score).ToList();
        }

        public static List<ScoredItem<CommonKnowledgeEntry>> ScoreKnowledge(
            List<CommonKnowledgeEntry> knowledge,
            string context,
            Pawn speaker = null,
            Pawn listener = null)
        {
            if (knowledge == null || knowledge.Count == 0)
                return new List<ScoredItem<CommonKnowledgeEntry>>();

            ContextFeatures features = ExtractContextFeatures(context, speaker, listener);
            var scored = new List<ScoredItem<CommonKnowledgeEntry>>();

            foreach (var entry in knowledge)
            {
                if (!entry.isEnabled)
                    continue;

                float score = ScoreSingleKnowledge(entry, features, speaker);
                
                scored.Add(new ScoredItem<CommonKnowledgeEntry>
                {
                    Item = entry,
                    Score = score,
                    Breakdown = new ScoreBreakdown
                    {
                        ContextRelevance = CalculateKnowledgeRelevance(entry, features),
                        Importance = entry.importance,
                        Diversity = 0f
                    }
                });
            }

            ApplyDiversityBoost(scored);

            return scored.OrderByDescending(s => s.Score).ToList();
        }

        #region 场景识别

        private static ConversationSceneType IdentifyScene(string context)
        {
            if (string.IsNullOrEmpty(context))
                return ConversationSceneType.Casual;

            context = context.ToLower();

            if (ContainsAny(context, new[] { "袭击", "敌人", "危险", "受伤", "死", "快", "救" }))
                return ConversationSceneType.Emergency;

            if (ContainsAny(context, new[] { "过去", "以前", "曾经", "记得", "那时", "当时" }))
                return ConversationSceneType.HistoryRecall;

            if (ContainsAny(context, new[] { "感觉", "心情", "难过", "开心", "想", "喜欢", "讨厌" }))
                return ConversationSceneType.EmotionalTalk;

            if (ContainsAny(context, new[] { "工作", "任务", "建造", "种植", "研究", "搬运" }))
                return ConversationSceneType.WorkDiscussion;

            if (ContainsAny(context, new[] { "你是", "叫什么", "来自", "背景", "擅长" }))
                return ConversationSceneType.Introduction;

            return ConversationSceneType.Casual;
        }

        private static ScoringWeights AdjustWeightsForScene(ConversationSceneType scene, ScoringWeights baseWeights)
        {
            var adjusted = new ScoringWeights
            {
                ContextRelevance = baseWeights.ContextRelevance,
                Recency = baseWeights.Recency,
                Importance = baseWeights.Importance,
                Diversity = baseWeights.Diversity,
                LayerPriority = baseWeights.LayerPriority
            };

            switch (scene)
            {
                case ConversationSceneType.Emergency:
                    adjusted.Recency = 0.50f;
                    adjusted.ContextRelevance = 0.30f;
                    adjusted.Importance = 0.15f;
                    break;

                case ConversationSceneType.HistoryRecall:
                    adjusted.Recency = 0.10f;
                    adjusted.LayerPriority = 0.25f;
                    adjusted.ContextRelevance = 0.45f;
                    break;

                case ConversationSceneType.EmotionalTalk:
                    adjusted.EmotionalBoost = 1.5f;
                    adjusted.RelationshipBoost = 1.4f;
                    break;

                case ConversationSceneType.WorkDiscussion:
                    adjusted.ContextRelevance = 0.50f;
                    adjusted.Recency = 0.25f;
                    break;

                case ConversationSceneType.Introduction:
                    adjusted.LayerPriority = 0.30f;
                    adjusted.ContextRelevance = 0.35f;
                    adjusted.Importance = 0.25f;
                    break;
            }

            return adjusted;
        }

        #endregion

        #region 上下文特征提取

        public class ContextFeatures
        {
            public List<string> Keywords = new List<string>();     
            public List<string> Entities = new List<string>();     
            public List<string> Topics = new List<string>();       
            public HashSet<string> EmotionWords = new HashSet<string>();
            public ConversationSceneType Scene = ConversationSceneType.Casual;
        }

        private static ContextFeatures ExtractContextFeatures(string context, Pawn speaker, Pawn listener)
        {
            var features = new ContextFeatures
            {
                Scene = IdentifyScene(context)
            };

            if (string.IsNullOrEmpty(context))
                return features;

            features.Keywords = ExtractImportantKeywords(context);

            if (speaker != null)
                features.Entities.Add(speaker.LabelShort);
            if (listener != null && listener != speaker)
                features.Entities.Add(listener.LabelShort);

            features.Topics = ExtractTopics(context);

            features.EmotionWords = ExtractEmotionWords(context);

            return features;
        }

        private static List<string> ExtractImportantKeywords(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            var weightedKeywords = SuperKeywordEngine.ExtractKeywords(text, 30);
            return weightedKeywords.Select(k => k.Word).ToList();
        }

        private static List<string> ExtractTopics(string context)
        {
            var topics = new List<string>();

            if (ContainsAny(context, new[] { "工作", "任务", "建造", "种植" }))
                topics.Add("工作");

            if (ContainsAny(context, new[] { "战斗", "袭击", "敌人", "武器" }))
                topics.Add("战斗");

            if (ContainsAny(context, new[] { "聊天", "朋友", "关系", "喜欢" }))
                topics.Add("社交");

            if (ContainsAny(context, new[] { "受伤", "治疗", "生病", "健康" }))
                topics.Add("健康");

            return topics;
        }

        private static HashSet<string> ExtractEmotionWords(string context)
        {
            var emotions = new HashSet<string>();
            var emotionKeywords = new[] 
            { 
                "开心", "高兴", "快乐", "愉快",
                "难过", "悲伤", "伤心", "痛苦",
                "愤怒", "生气", "恼火", "讨厌",
                "害怕", "恐惧", "担心", "焦虑"
            };

            foreach (var word in emotionKeywords)
            {
                if (context.Contains(word))
                    emotions.Add(word);
            }

            return emotions;
        }

        #endregion

        #region 评分计算

        private static float ScoreSingleMemory(
            MemoryEntry memory,
            ContextFeatures features,
            ScoringWeights weights,
            Pawn speaker,
            Pawn listener)
        {
            float baseScore = 0f;

            float relevance = CalculateContextRelevance(memory, features);
            baseScore += relevance * weights.ContextRelevance;

            float recency = CalculateRecency(memory);
            baseScore += recency * weights.Recency;

            baseScore += memory.Importance * weights.Importance;

            float layerScore = GetLayerPriority(memory.Layer);
            baseScore += layerScore * weights.LayerPriority;

            float typeBoost = GetTypeBoost(memory.Type, weights);
            float finalScore = baseScore * typeBoost;

            if (memory.IsPinned)
                finalScore *= 1.5f;
            if (memory.IsUserEdited)
                finalScore *= 1.3f;

            if (!string.IsNullOrEmpty(memory.relatedPawnName))
            {
                if (listener != null && memory.relatedPawnName == listener.LabelShort)
                    finalScore *= 1.2f;
            }

            return finalScore;
        }

        private static float ScoreSingleKnowledge(
            CommonKnowledgeEntry entry,
            ContextFeatures features,
            Pawn speaker)
        {
            float score = 0f;

            float relevance = CalculateKnowledgeRelevance(entry, features);
            score += relevance * 0.6f;

            score += entry.importance * 0.4f;

            return score;
        }

        private static float CalculateContextRelevance(MemoryEntry memory, ContextFeatures features)
        {
            float relevance = 0f;
            int matches = 0;
            int total = 0;

            if (memory.keywords != null && memory.keywords.Count > 0)
            {
                var intersection = memory.keywords.Intersect(features.Keywords).Count();
                if (features.Keywords.Count > 0)
                {
                    relevance += (float)intersection / features.Keywords.Count * 0.4f;
                    matches += intersection;
                    total += features.Keywords.Count;
                }
            }

            foreach (var keyword in features.Keywords.Take(10))
            {
                if (memory.Content.Contains(keyword))
                {
                    relevance += 0.1f;
                }
            }

            foreach (var topic in features.Topics)
            {
                if (memory.tags.Contains(topic) || memory.Content.Contains(topic))
                {
                    relevance += 0.15f;
                }
            }

            if (memory.Type == MemoryType.Emotion)
            {
                foreach (var emotion in features.EmotionWords)
                {
                    if (memory.Content.Contains(emotion))
                    {
                        relevance += 0.2f;
                    }
                }
            }

            return Math.Min(relevance, 1.0f);
        }

        private static float CalculateKnowledgeRelevance(CommonKnowledgeEntry entry, ContextFeatures features)
        {
            float relevance = 0f;

            var tags = entry.GetTags();
            foreach (var tag in tags)
            {
                if (features.Topics.Any(t => t.Contains(tag) || tag.Contains(t)))
                {
                    relevance += 0.3f;
                }
            }

            int matchCount = 0;
            foreach (var keyword in features.Keywords.Take(15))
            {
                if (entry.content.Contains(keyword))
                {
                    matchCount++;
                }
            }

            if (features.Keywords.Count > 0)
            {
                relevance += (float)matchCount / features.Keywords.Count * 0.5f;
            }

            return Math.Min(relevance, 1.0f);
        }

        private static float CalculateRecency(MemoryEntry memory)
        {
            int currentTick = Find.TickManager.TicksGame;
            int age = currentTick - memory.GameTick;

            if (age < 2500)
                return 1.0f;
            else if (age < 15000)
                return 0.9f;
            else if (age < 60000)
                return 0.7f;
            else if (age < 300000)
                return 0.5f;
            else if (age < 900000)
                return 0.3f;
            else
                return 0.1f;
        }

        private static float GetLayerPriority(MemoryLayer layer)
        {
            switch (layer)
            {
                case MemoryLayer.Active:
                    return 1.0f;
                case MemoryLayer.Situational:
                    return 0.8f; // SCM
                case MemoryLayer.EventLog:
                    return 0.5f; // ELS
                case MemoryLayer.Archive:
                    return 0.3f; // CLPA
                default:
                    return 0f;
            }
        }

        private static float GetTypeBoost(MemoryType type, ScoringWeights weights)
        {
            switch (type)
            {
                case MemoryType.Emotion:
                    return weights.EmotionalBoost;
                case MemoryType.Relationship:
                    return weights.RelationshipBoost;
                case MemoryType.Conversation:
                    return weights.ConversationBoost;
                default:
                    return 1.0f;
            }
        }

        #endregion

        #region 多样性优化

        private static void ApplyDiversityBoost<T>(List<ScoredItem<T>> scored) where T : class
        {
            if (scored.Count == 0)
                return;

            var typeCount = new Dictionary<string, int>();

            foreach (var item in scored)
            {
                string type = GetItemType(item.Item);
                
                if (!typeCount.ContainsKey(type))
                    typeCount[type] = 0;

                typeCount[type]++;

                float diversityFactor = 1.0f / (1.0f + typeCount[type] * 0.1f);
                item.Breakdown.Diversity = diversityFactor - 1.0f;
                item.Score *= diversityFactor;
            }
        }

        private static string GetItemType(object item)
        {
            if (item is MemoryEntry memory)
                return memory.Type.ToString();
            else if (item is CommonKnowledgeEntry knowledge)
                return knowledge.GetTags().FirstOrDefault() ?? "unknown";
            else
                return "unknown";
        }

        #endregion

        #region 辅助方法

        private static bool ContainsAny(string text, string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                if (text.Contains(keyword))
                    return true;
            }
            return false;
        }

        #endregion
    }

    #region 数据结构

    public class ScoredItem<T> where T : class
    {
        public T Item;
        public float Score;
        public ScoreBreakdown Breakdown;
    }

    public class ScoreBreakdown
    {
        public float ContextRelevance; 
        public float Recency;          
        public float Importance;       
        public float Diversity;        
        public float LayerPriority;    
        public float TypeBoost;        
    }

    #endregion
}
