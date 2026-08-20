using Ustas.RimAI.Communication.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    public static class ProactiveMemoryRecall
    {
        public static class TriggerProbability
        {
            public static float BaseChance = 0.15f;          
            public static float HighImportanceBonus = 0.20f; 
            public static float RecentMemoryBonus = 0.15f;   
            public static float EmotionalBonus = 0.10f;      
        }

        public static string TryRecallMemory(Pawn pawn, string context, Pawn listener = null)
        {
            var settings = RimTalkMemoryPatchMod.Settings;
            if (settings?.enableProactiveRecall != true)
                return null;

            var memoryComp = pawn?.TryGetComp<FourLayerMemoryComp>();
            if (memoryComp == null)
                return null;

            var candidates = new List<MemoryEntry>();
            candidates.AddRange(memoryComp.SituationalMemories.Take(10));
            candidates.AddRange(memoryComp.EventLogMemories.Take(5));

            if (candidates.Count == 0)
                return null;

            var contextKeywords = ExtractKeywords(context);
            if (contextKeywords.Count == 0)
                return null;

            var scored = candidates
                .Select(m => new
                {
                    Memory = m,
                    Score = CalculateRecallScore(m, contextKeywords, listener)
                })
                .Where(s => s.Score > 0.3f)
                .OrderByDescending(s => s.Score)
                .ToList();

            if (scored.Count == 0)
                return null;

            var best = scored.First();
            float triggerChance = CalculateTriggerChance(best.Memory);

            if (UnityEngine.Random.value > triggerChance)
                return null;

            string recallPrompt = GenerateRecallPrompt(best.Memory, context, listener);

            if (Prefs.DevMode)
            {
                Log.Message($"[Proactive Recall] {pawn.LabelShort} recalled memory: {best.Memory.Content.Substring(0, Math.Min(50, best.Memory.Content.Length))} (Score: {best.Score:F2}, Chance: {triggerChance:P0})");
            }

            return recallPrompt;
        }

        private static float CalculateRecallScore(MemoryEntry memory, List<string> contextKeywords, Pawn listener)
        {
            float score = 0f;

            float keywordMatch = 0f;
            if (memory.keywords != null && contextKeywords != null)
            {
                int matches = memory.keywords.Intersect(contextKeywords).Count();
                if (matches > 0)
                {
                    keywordMatch = Math.Min((float)matches / contextKeywords.Count, 1f);
                }
            }
            score += keywordMatch * 0.4f;

            score += memory.Importance * 0.3f;

            int age = Find.TickManager.TicksGame - memory.GameTick;
            float freshness = UnityEngine.Mathf.Exp(-age / 120000f);
            score += freshness * 0.2f;

            if (listener != null && !string.IsNullOrEmpty(memory.relatedPawnName))
            {
                if (memory.relatedPawnName == listener.LabelShort)
                {
                    score += 0.1f;
                }
            }

            if (memory.Type == MemoryType.Emotion)
                score += 0.15f;

            if (memory.IsPinned)
                score += 0.2f;

            return score;
        }

        private static float CalculateTriggerChance(MemoryEntry memory)
        {
            float chance = TriggerProbability.BaseChance;

            if (memory.Importance > 0.7f)
                chance += TriggerProbability.HighImportanceBonus;

            int age = Find.TickManager.TicksGame - memory.GameTick;
            if (age < 60000)
                chance += TriggerProbability.RecentMemoryBonus;

            if (memory.Type == MemoryType.Emotion)
                chance += TriggerProbability.EmotionalBonus;

            return Math.Min(chance, 0.6f);
        }

        private static string GenerateRecallPrompt(MemoryEntry memory, string context, Pawn listener)
        {
            var sb = new StringBuilder();

            sb.AppendLine("## ?? Active Memory Recall");
            sb.AppendLine("(AI Instruction: The character spontaneously recalls this memory. Naturally mention or reference it in the response.)");
            sb.AppendLine();

            string typeTag = GetMemoryTypeTag(memory.Type);
            string timeStr = memory.AgeString;
            
            sb.AppendLine($"**Recalled Memory:** [{typeTag}] {memory.Content}");
            sb.AppendLine($"**When:** {timeStr}");

            if (!string.IsNullOrEmpty(memory.relatedPawnName))
            {
                sb.AppendLine($"**Related to:** {memory.relatedPawnName}");
            }

            if (memory.Type == MemoryType.Emotion)
            {
                sb.AppendLine($"**Emotional weight:** High (importance: {memory.Importance:P0})");
            }

            sb.AppendLine();
            sb.AppendLine("(Use this memory to add depth and continuity to your response. Don't just repeat it - weave it naturally into the conversation.)");

            return sb.ToString();
        }

        private static string GetMemoryTypeTag(MemoryType type)
        {
            switch (type)
            {
                case MemoryType.Conversation:
                    return "Conversation";
                case MemoryType.Action:
                    return "Action";
                case MemoryType.Event:
                    return "Event";
                case MemoryType.Emotion:
                    return "Emotion";
                case MemoryType.Relationship:
                    return "Relationship";
                default:
                    return "Memory";
            }
        }

        private static List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            var weightedKeywords = SuperKeywordEngine.ExtractKeywords(text, 15);
            return weightedKeywords.Select(k => k.Word).ToList();
        }

        public static RecallDiagnostics GetDiagnostics(Pawn pawn)
        {
            var diagnostics = new RecallDiagnostics
            {
                PawnName = pawn?.LabelShort ?? "Unknown",
                IsEnabled = RimTalkMemoryPatchMod.Settings?.enableProactiveRecall ?? false
            };

            var memoryComp = pawn?.TryGetComp<FourLayerMemoryComp>();
            if (memoryComp != null)
            {
                diagnostics.CandidateMemories = memoryComp.SituationalMemories.Count + memoryComp.EventLogMemories.Count;
                
                var highScore = memoryComp.SituationalMemories
                    .Concat(memoryComp.EventLogMemories)
                    .Where(m => m.Importance > 0.7f)
                    .Count();
                
                diagnostics.HighImportanceMemories = highScore;
            }

            return diagnostics;
        }

        public class RecallDiagnostics
        {
            public string PawnName;
            public bool IsEnabled;
            public int CandidateMemories;
            public int HighImportanceMemories;
            public float EstimatedTriggerRate;
        }
    }
}
