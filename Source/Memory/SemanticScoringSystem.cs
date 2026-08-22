using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    public static class SemanticScoringSystem
    {
        public static List<ScoredItem<MemoryEntry>> ScoreMemoriesWithSemantics(
            List<MemoryEntry> memories,
            string context,
            Pawn speaker = null,
            Pawn listener = null)
        {
            if (memories == null || memories.Count == 0)
                return new List<ScoredItem<MemoryEntry>>();
            
            var quickScored = AdvancedScoringSystem.ScoreMemories(memories, context, speaker, listener);
            
            int keepCount = Math.Max(10, quickScored.Count / 2);
            var topCandidates = quickScored.Take(keepCount).ToList();
            
            bool useSemantics = AI.EmbeddingService.IsAvailable();
            
            if (!useSemantics)
            {
                return topCandidates;
            }
            
            var semanticTask = Task.Run(async () => await ApplySemanticScoringAsync(topCandidates, context));
            
            if (semanticTask.Wait(800))
            {
                var semanticScored = semanticTask.Result;
                
                if (semanticScored != null && semanticScored.Count > 0)
                {
                    if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                    {
                        Log.Message($"[Semantic Scoring] Success: {semanticScored.Count} memories");
                    }
                    
                    return semanticScored;
                }
            }
            else
            {
                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    Log.Warning("[Semantic Scoring] Timeout, using keyword fallback");
                }
            }
            
            return topCandidates;
        }
        
        private static async Task<List<ScoredItem<MemoryEntry>>> ApplySemanticScoringAsync(
            List<ScoredItem<MemoryEntry>> candidates,
            string context)
        {
            try
            {
                float[] contextEmbedding = await AI.EmbeddingService.GetEmbeddingAsync(context);
                
                if (contextEmbedding == null)
                {
                    Log.Warning("[Semantic Scoring] Failed to get context embedding");
                    return candidates;
                }
                
                foreach (var scored in candidates)
                {
                    try
                    {
                        float[] memoryEmbedding = await AI.EmbeddingService.GetEmbeddingAsync(scored.Item.Content);
                        
                        if (memoryEmbedding != null)
                        {
                            float semanticSimilarity = AI.EmbeddingService.CosineSimilarity(contextEmbedding, memoryEmbedding);
                            
                            float keywordScore = scored.Score;
                            float hybridScore = (keywordScore * 0.7f) + (semanticSimilarity * 0.3f);
                            
                            scored.Score = hybridScore;
                            
                            if (scored.Breakdown != null)
                            {
                                scored.Breakdown.TypeBoost = semanticSimilarity;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[Semantic Scoring] Failed to score memory: {ex.Message}");
                    }
                }
                
                return candidates.OrderByDescending(s => s.Score).ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"[Semantic Scoring] Error: {ex.Message}");
                return candidates;
            }
        }
        
        public static List<ScoredItem<CommonKnowledgeEntry>> ScoreKnowledgeWithSemantics(
            List<CommonKnowledgeEntry> knowledge,
            string context,
            Pawn speaker = null,
            Pawn listener = null)
        {
            if (knowledge == null || knowledge.Count == 0)
                return new List<ScoredItem<CommonKnowledgeEntry>>();
            
            var quickScored = AdvancedScoringSystem.ScoreKnowledge(knowledge, context, speaker, listener);
            
            int keepCount = Math.Max(5, quickScored.Count / 2);
            var topCandidates = quickScored.Take(keepCount).ToList();
            
            bool useSemantics = AI.EmbeddingService.IsAvailable();
            
            if (!useSemantics)
            {
                return topCandidates;
            }
            
            var semanticTask = Task.Run(async () => await ApplySemanticScoringToKnowledgeAsync(topCandidates, context));
            
            if (semanticTask.Wait(800))
            {
                var semanticScored = semanticTask.Result;
                
                if (semanticScored != null && semanticScored.Count > 0)
                {
                    if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                    {
                        Log.Message($"[Semantic Scoring] Success: {semanticScored.Count} knowledge");
                    }
                    
                    return semanticScored;
                }
            }
            else
            {
                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    Log.Warning("[Semantic Scoring] Timeout, using keyword fallback");
                }
            }
            
            return topCandidates;
        }
        
        private static async Task<List<ScoredItem<CommonKnowledgeEntry>>> ApplySemanticScoringToKnowledgeAsync(
            List<ScoredItem<CommonKnowledgeEntry>> candidates,
            string context)
        {
            try
            {
                float[] contextEmbedding = await AI.EmbeddingService.GetEmbeddingAsync(context);
                
                if (contextEmbedding == null)
                    return candidates;
                
                foreach (var scored in candidates)
                {
                    try
                    {
                        float[] knowledgeEmbedding = await AI.EmbeddingService.GetEmbeddingAsync(scored.Item.content);
                        
                        if (knowledgeEmbedding != null)
                        {
                            float semanticSimilarity = AI.EmbeddingService.CosineSimilarity(contextEmbedding, knowledgeEmbedding);
                            
                            float keywordScore = scored.Score;
                            float hybridScore = (keywordScore * 0.6f) + (semanticSimilarity * 0.4f);
                            
                            scored.Score = hybridScore;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[Semantic Scoring] Failed to score knowledge: {ex.Message}");
                    }
                }
                
                return candidates.OrderByDescending(s => s.Score).ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"[Semantic Scoring] Error: {ex.Message}");
                return candidates;
            }
        }
        
        public static async Task PrewarmEmbeddingCacheAsync(FourLayerMemoryComp memoryComp)
        {
            if (memoryComp == null || !AI.EmbeddingService.IsAvailable())
                return;
            
            try
            {
                var importantMemories = new List<MemoryEntry>();
                
                importantMemories.AddRange(memoryComp.SituationalMemories.Where(m => m.Importance > 0.7f));
                importantMemories.AddRange(memoryComp.EventLogMemories.Where(m => m.Importance > 0.7f));
                importantMemories.AddRange(memoryComp.ArchiveMemories.Take(10).Where(m => m.Importance > 0.7f));
                
                if (importantMemories.Count == 0)
                    return;
                
                var toBatch = importantMemories.Take(20).Select(m => m.Content).ToList();
                
                if (Prefs.DevMode)
                    Log.Message($"[Semantic Scoring] Prewarming {toBatch.Count} memory embeddings...");
                
                await AI.EmbeddingService.GetEmbeddingsBatchAsync(toBatch);
                
                if (Prefs.DevMode)
                    Log.Message($"[Semantic Scoring] Prewarm complete");
            }
            catch (Exception ex)
            {
                Log.Error($"[Semantic Scoring] Prewarm error: {ex.Message}");
            }
        }
    }
}
