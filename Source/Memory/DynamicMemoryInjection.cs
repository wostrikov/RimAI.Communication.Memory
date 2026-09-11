using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Diagnostics;

namespace Ustas.RimAI.Communication.Memory
{
    public static class DynamicMemoryInjection
    {
        public static class Weights
        {
            public static float LayerBonus = 0.2f;      
            public static float PinnedBonus = 0.5f;     
            public static float UserEditedBonus = 0.3f; 
        }


        public static string InjectMemoriesWithDetails(
            FourLayerMemoryComp memoryComp,
            string context,
            int maxMemories,
            out List<MemoryScore> scores,
            MemoryLayer? layerA = null,
            MemoryLayer? layerB = null)
        {
            scores = new List<MemoryScore>();

            if (memoryComp == null)
                return string.Empty;

            var pawn = memoryComp.parent as Pawn;
            if (pawn == null)
                return string.Empty;

            SceneType sceneType = DetermineScene(pawn, context);

            var analysis = SceneAnalyzer.AnalyzeScene(context);
            DynamicWeights sceneWeights = SceneAnalyzer.GetDynamicWeights(sceneType, analysis.Confidence);

            if (Prefs.DevMode)
            {
                ModuleLog.Message($"[Memory Injection] Scene: {SceneAnalyzer.GetSceneDisplayName(sceneType)}");
                ModuleLog.Message($"[Memory Injection] Confidence: {analysis.Confidence:P0}");
                ModuleLog.Message($"[Memory Injection] Weights: {sceneWeights}");
            }

            List<string> contextKeywords = ExtractKeywords(context);

            var allMemories = new List<MemoryEntry>();
            bool IsMatch(MemoryLayer target) => (layerA ?? layerB) == null || layerA == target || layerB == target;

            if (IsMatch(MemoryLayer.Situational)) allMemories.AddRange(memoryComp.SituationalMemories);
            if (IsMatch(MemoryLayer.EventLog)) allMemories.AddRange(memoryComp.EventLogMemories);

            if (IsMatch(MemoryLayer.Archive))
            {
                if (sceneType == SceneType.Social || sceneType == SceneType.Event || ShouldIncludeArchive(context))
                {
                    allMemories.AddRange(memoryComp.ArchiveMemories.Take(20));
                }
            }

            if (allMemories.Count == 0)
                return string.Empty;

            float threshold = RimTalkMemoryPatchMod.Settings?.memoryScoreThreshold ?? 0.15f;

            var scoredMemories = allMemories
                .Select(m => new ScoredMemory
                {
                    Memory = m,
                    Score = CalculateMemoryScore(m, contextKeywords, sceneWeights, memoryComp)
                })
                .Where(sm => sm.Score >= threshold)
                .OrderByDescending(sm => sm.Score)
                .Take(maxMemories)
                .ToList();

            if (scoredMemories.Count == 0)
            {
                if (Prefs.DevMode)
                {
                    ModuleLog.Message($"[Memory Injection] No memories met threshold ({threshold:F2}), returning null");
                }
                return null;
            }

            foreach (var scored in scoredMemories)
            {
                float timeScore = CalculateTimeDecayScore(scored.Memory, sceneWeights.TimeDecay, sceneWeights.RecencyWindow);
                float importanceScore = scored.Memory.Importance * sceneWeights.Importance;
                float keywordScore = CalculateKeywordMatchScore(scored.Memory, contextKeywords) * sceneWeights.KeywordMatch;

                float bonusScore = GetLayerBonus(scored.Memory.Layer) * Weights.LayerBonus;

                float relationshipScore = CalculateRelationshipBonus(scored.Memory, pawn) * sceneWeights.RelationshipBonus;
                bonusScore += relationshipScore;

                if (scored.Memory.IsPinned)
                    bonusScore += Weights.PinnedBonus;
                if (scored.Memory.IsUserEdited)
                    bonusScore += Weights.UserEditedBonus;

                scores.Add(new MemoryScore
                {
                    Memory = scored.Memory,
                    TotalScore = scored.Score,
                    TimeScore = timeScore,
                    ImportanceScore = importanceScore,
                    KeywordScore = keywordScore,
                    BonusScore = bonusScore
                });
            }

            return string.Empty;
        }

        /// <summary>
        /// enemyTarget is sticky: RimWorld does not reliably clear it when a fight ends and it
        /// survives save/reload, so a bare null check puts anyone who ever fought in the combat
        /// scene for good. It only counts while the target is still a live threat nearby.
        /// </summary>
        private static bool HasLiveEnemyTarget(Pawn pawn)
        {
            Thing target = pawn.mindState?.enemyTarget;
            if (target == null || target.Destroyed || !target.Spawned) return false;
            if (target.Map != pawn.Map) return false;
            if (target is Pawn targetPawn && (targetPawn.Dead || targetPawn.Downed)) return false;
            return pawn.Position.DistanceTo(target.Position) <= 30f;
        }

        private static SceneType DetermineScene(Pawn pawn, string context)
        {
            if (pawn == null)
                return SceneType.Neutral;


            if (pawn.Drafted || HasLiveEnemyTarget(pawn))
            {
                return SceneType.Combat;
            }

            if (pawn.Downed || pawn.health?.State == PawnHealthState.Down)
            {
                return SceneType.Medical;
            }

            if (pawn.CurJob?.def?.defName != null && pawn.CurJob.def.defName.Contains("Lovin"))
            {
                return SceneType.Social;
            }

            if (pawn.CurJob?.def != null)
            {
                string jobDefName = pawn.CurJob.def.defName;

                if (jobDefName.Contains("Research"))
                {
                    return SceneType.Research;
                }

                if (jobDefName.Contains("Doctor") || jobDefName.Contains("Tend") || jobDefName.Contains("Surgery"))
                {
                    return SceneType.Medical;
                }

                if (jobDefName.Contains("Construct") || jobDefName.Contains("Plant") ||
                    jobDefName.Contains("Haul") || jobDefName.Contains("Cook") ||
                    jobDefName.Contains("Mine") || jobDefName.Contains("Clean"))
                {
                    return SceneType.Work;
                }
            }

            if (!string.IsNullOrEmpty(context))
            {
                var analysis = SceneAnalyzer.AnalyzeScene(context);
                return analysis.PrimaryScene;
            }

            return SceneType.Neutral;
        }

        private static float CalculateMemoryScore(
            MemoryEntry memory,
            List<string> contextKeywords,
            DynamicWeights sceneWeights,
            FourLayerMemoryComp memoryComp)
        {
            float score = 0f;

            float timeScore = CalculateTimeDecayScore(memory, sceneWeights.TimeDecay, sceneWeights.RecencyWindow);
            score += timeScore;

            score += memory.Importance * sceneWeights.Importance;

            float keywordScore = CalculateKeywordMatchScore(memory, contextKeywords);
            score += keywordScore * sceneWeights.KeywordMatch;

            float layerBonus = GetLayerBonus(memory.Layer);
            score += layerBonus * Weights.LayerBonus;

            var pawn = memoryComp.parent as Pawn;
            float relationshipBonus = CalculateRelationshipBonus(memory, pawn);
            score += relationshipBonus * sceneWeights.RelationshipBonus;

            if (memory.IsPinned)
                score += Weights.PinnedBonus;

            if (memory.IsUserEdited)
                score += Weights.UserEditedBonus;

            score += memory.Activity * 0.1f;

            return score;
        }

        private static float CalculateTimeDecayScore(MemoryEntry memory, float decayRate, int recencyWindow)
        {
            int currentTick = Find.TickManager.TicksGame;
            int age = currentTick - memory.GameTick;

            if (age > recencyWindow)
            {
                float excessAge = (age - recencyWindow) / 60000f;
                return UnityEngine.Mathf.Exp(-excessAge * 2.0f) * 0.1f;
            }

            float normalizedAge = age / 60000f;
            return UnityEngine.Mathf.Exp(-normalizedAge * decayRate);
        }

        private static float CalculateKeywordMatchScore(MemoryEntry memory, List<string> contextKeywords)
        {
            if (contextKeywords == null || contextKeywords.Count == 0)
                return 0f;

            if (memory.keywords == null || memory.keywords.Count == 0)
                return 0f;

            var intersection = memory.keywords.Intersect(contextKeywords).Count();
            var union = memory.keywords.Union(contextKeywords).Count();

            if (union == 0)
                return 0f;

            float jaccardSimilarity = (float)intersection / union;

            float contentMatch = 0f;
            foreach (var keyword in contextKeywords)
            {
                if (memory.Content.Contains(keyword))
                    contentMatch += 0.2f;
            }

            return UnityEngine.Mathf.Min(jaccardSimilarity + contentMatch, 1f);
        }

        private static float CalculateRelationshipBonus(MemoryEntry memory, Pawn currentPawn)
        {
            if (string.IsNullOrEmpty(memory.relatedPawnId) || currentPawn == null)
                return 0f;

            Pawn relatedPawn = null;
            foreach (var map in Find.Maps)
            {
                relatedPawn = map.mapPawns.AllPawns.FirstOrDefault(p =>
                    p.ThingID == memory.relatedPawnId ||
                    p.LabelShort == memory.relatedPawnName
                );

                if (relatedPawn != null)
                    break;
            }

            if (relatedPawn == null)
                return 0f;

            if (currentPawn.relations == null || relatedPawn.relations == null)
                return 0f;

            var directRelations = currentPawn.relations.DirectRelations
                .Where(r => r.otherPawn == relatedPawn)
                .ToList();

            if (directRelations.Any())
            {
                if (directRelations.Any(r => r.def == PawnRelationDefOf.Spouse || r.def == PawnRelationDefOf.Lover))
                    return 1.0f;

                if (directRelations.Any(r => r.def == PawnRelationDefOf.Parent ||
                                            r.def == PawnRelationDefOf.Child ||
                                            r.def == PawnRelationDefOf.Sibling))
                    return 0.6f;

                return 0.3f;
            }

            int opinion = currentPawn.relations.OpinionOf(relatedPawn);
            if (opinion > 50)
                return 0.5f;
            else if (opinion > 20)
                return 0.3f;
            else if (opinion < -20)
                return 0.2f;

            return 0f;
        }

        private static float GetLayerBonus(MemoryLayer layer)
        {
            switch (layer)
            {
                case MemoryLayer.Active:
                    return 1.0f;
                case MemoryLayer.Situational:
                    return 0.7f;
                case MemoryLayer.EventLog:
                    return 0.4f;
                case MemoryLayer.Archive:
                    return 0.2f;
                default:
                    return 0f;
            }
        }

        private static bool ShouldIncludeArchive(string context)
        {
            if (string.IsNullOrEmpty(context))
                return false;

            string[] archiveKeywords = { "підійти", "раніше", "колись", "памʼятаю", "спогад", "історія", "у той час", "тоді" };

            foreach (var keyword in archiveKeywords)
            {
                if (context.Contains(keyword))
                    return true;
            }

            return false;
        }

        private static List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            const int MAX_TEXT_LENGTH = 500;
            if (text.Length > MAX_TEXT_LENGTH)
            {
                text = text.Substring(0, MAX_TEXT_LENGTH);
            }

            var weightedKeywords = SuperKeywordEngine.ExtractKeywords(text, 100);

            if (weightedKeywords.Count == 0)
            {
                return new List<string>();
            }

            var sortedByLength = weightedKeywords
                .OrderByDescending(kw => kw.Word.Length)
                .ThenBy(kw => kw.Word, StringComparer.Ordinal)
                .ToList();

            var coreKeywords = sortedByLength.Take(10).ToList();

            var remainingPool = sortedByLength.Skip(10).ToList();
            var fuzzyKeywords = new List<WeightedKeyword>();

            if (remainingPool.Count > 0)
            {
                fuzzyKeywords = remainingPool
                    .OrderBy(kw => kw.Word, StringComparer.Ordinal)
                    .Take(10)
                    .ToList();
            }

            var finalKeywords = new List<string>();
            finalKeywords.AddRange(coreKeywords.Select(kw => kw.Word));
            finalKeywords.AddRange(fuzzyKeywords.Select(kw => kw.Word));

            return finalKeywords;
        }


        private class ScoredMemory
        {
            public MemoryEntry Memory;
            public float Score;
        }

        public class MemoryScore
        {
            public MemoryEntry Memory;
            public float TotalScore;
            public float TimeScore;
            public float ImportanceScore;
            public float KeywordScore;
            public float BonusScore;
        }
    }
}
