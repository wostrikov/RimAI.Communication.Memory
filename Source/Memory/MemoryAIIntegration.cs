using System.Text;
using System.Collections.Generic;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory
{
    /// <summary>
    /// Integration helper for AI conversation generation with memory context
    /// </summary>
    public static class MemoryAIIntegration
    {
        private static HashSet<string> recordedConversations = new HashSet<string>();
        private static int lastCleanupTick = 0;
        private const int CleanupInterval = 2500;
        
        /// <summary>
        /// Generate AI prompt with pawn's memory context
        /// </summary>
        public static string GeneratePromptWithMemory(Pawn pawn, string basePrompt)
        {
            if (pawn == null)
                return basePrompt;

            var memoryComp = pawn.TryGetComp<PawnMemoryComp>();
            if (memoryComp == null)
                return basePrompt;

            StringBuilder promptBuilder = new StringBuilder();
            
            // Add memory context
            string memoryContext = memoryComp.GetMemoryContext();
            if (!string.IsNullOrEmpty(memoryContext))
            {
                promptBuilder.AppendLine(memoryContext);
                promptBuilder.AppendLine();
            }

            // Add base prompt
            promptBuilder.Append(basePrompt);

            return promptBuilder.ToString();
        }

        /// <summary>
        /// Get a summary of pawn's current mental state based on memories
        /// </summary>
        public static string GetMentalStateSummary(Pawn pawn)
        {
            var memoryComp = pawn.TryGetComp<PawnMemoryComp>();
            if (memoryComp == null)
                return "";

            var recentMemories = memoryComp.GetRelevantMemories(3);
            if (recentMemories.Count == 0)
                return "";

            StringBuilder summary = new StringBuilder();
            summary.AppendLine("Recent experiences:");

            foreach (var memory in recentMemories)
            {
                string emotionalTag = GetEmotionalTag(memory);
                summary.AppendLine("- " + emotionalTag + memory.Content);
            }

            return summary.ToString();
        }

        private static string GetEmotionalTag(MemoryEntry memory)
        {
            if (memory.Importance > 0.8f)
                return "[Important] ";
            if (memory.Importance < 0.3f)
                return "[Minor] ";
            return "";
        }

        /// <summary>
        /// Record AI-generated conversation as memory
        /// </summary>
        public static void RecordConversation(Pawn speaker, Pawn listener, string content)
        {
            // Check if conversation memory is enabled
            if (!RimTalkMemoryPatchMod.Settings.enableConversationMemory)
            {
                if (Prefs.DevMode)
                    Log.Message("[RimAI.Memory] ⚠️ Conversation memory is DISABLED in settings!");
                return;
            }
            
            if (Find.TickManager != null && Find.TickManager.TicksGame - lastCleanupTick > CleanupInterval)
            {
                recordedConversations.Clear();
                lastCleanupTick = Find.TickManager.TicksGame;
                if (Prefs.DevMode)
                    Log.Message("[RimAI.Memory] Cleaned conversation cache");
            }
            
            int tick = Find.TickManager?.TicksGame ?? 0;
            int contentHash = content?.GetHashCode() ?? 0;
            string speakerId = speaker?.ThingID ?? "null";
            string listenerId = listener?.ThingID ?? "null";
            
            string conversationId = $"{tick}_{speakerId}_{listenerId}_{contentHash}";
            
            if (recordedConversations.Contains(conversationId))
            {
                if (Prefs.DevMode)
                    Log.Message($"[RimAI.Memory] ⏭️ Skipped duplicate in RecordConversation: {conversationId}");
                return;
            }
            
            recordedConversations.Add(conversationId);
                
            var speakerMemory = speaker != null ? speaker.TryGetComp<PawnMemoryComp>() : null;
            if (speakerMemory != null)
            {
                string listenerName = listener != null ? listener.LabelShort : "self";
                string memoryContent = "Said to " + listenerName + ": " + content;
                speakerMemory.AddActiveMemory(memoryContent, MemoryType.Conversation, 0.6f, listenerName);
            }

            if (listener != null && listener != speaker)
            {
                var listenerMemory = listener.TryGetComp<PawnMemoryComp>();
                if (listenerMemory != null)
                {
                    string speakerName = speaker != null ? speaker.LabelShort : "someone";
                    string memoryContent = speakerName + " said: " + content;
                    listenerMemory.AddActiveMemory(memoryContent, MemoryType.Conversation, 0.5f, speakerName);
                }
            }
            
            string speakerLabel = speaker != null ? speaker.LabelShort : "Unknown";
            string listenerLabel = listener != null && listener != speaker ? listener.LabelShort : "self";
            string previewContent = content != null && content.Length > 50 ? content.Substring(0, 50) + "..." : content;
            Log.Message($"[RimAI.Memory] ✅ RECORDED: {speakerLabel} -> {listenerLabel}: {previewContent}");
        }
    }
}
