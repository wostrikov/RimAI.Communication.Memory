using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Patches;
using Ustas.RimAI.Communication.Memory.Diagnostics;

namespace Ustas.RimAI.Communication.Memory
{
    public class MemoryManager : WorldComponent
    {
        internal MemoryManagerParts Parts;

        static MemoryManager()
        {
        }
        
        internal int lastDecayTick = 0;
        internal const int DecayInterval = 2500; // Every in-game hour
        
        internal int lastSummarizationDay = -1;
        internal int lastArchiveDay = -1;       
        
        internal int sessionStartTick = -1;
        internal const int COLD_START_DELAY = 200;

        internal Queue<Pawn> summarizationQueue = new Queue<Pawn>();
        internal int nextSummarizationTick = 0;
        internal const int SUMMARIZATION_DELAY_TICKS = 900;
        
        internal Queue<Pawn> manualSummarizationQueue = new Queue<Pawn>();
        internal int nextManualSummarizationTick = 0;
        internal const int MANUAL_SUMMARIZATION_DELAY_TICKS = 60;

        internal CommonKnowledgeLibrary commonKnowledge;
        public CommonKnowledgeLibrary CommonKnowledge
        {
            get
            {
                if (commonKnowledge == null)
                    commonKnowledge = new CommonKnowledgeLibrary();
                return commonKnowledge;
            }
        }
        
        internal ConversationCache conversationCache;
        public ConversationCache ConversationCache
        {
            get
            {
                if (conversationCache == null)
                    conversationCache = new ConversationCache();
                return conversationCache;
            }
        }
        
        internal PromptCache promptCache;
        public PromptCache PromptCache
        {
            get
            {
                if (promptCache == null)
                    promptCache = new PromptCache();
                return promptCache;
            }
        }

        public static CommonKnowledgeLibrary GetCommonKnowledge()
        {
            if (Current.Game == null) return new CommonKnowledgeLibrary();
            
            var manager = Find.World.GetComponent<MemoryManager>();
            return manager?.CommonKnowledge ?? new CommonKnowledgeLibrary();
        }
        
        public static ConversationCache GetConversationCache()
        {
            if (Current.Game == null) return new ConversationCache();
            
            var manager = Find.World.GetComponent<MemoryManager>();
            return manager?.ConversationCache ?? new ConversationCache();
        }
        
        public static PromptCache GetPromptCache()
        {
            if (Current.Game == null) return new PromptCache();
            
            var manager = Find.World.GetComponent<MemoryManager>();
            return manager?.PromptCache ?? new PromptCache();
        }

        public MemoryManager(World world) : base(world)
        {
            Parts = new MemoryManagerParts(this);
            commonKnowledge = new CommonKnowledgeLibrary();
        }

        
        
        
        
        

        

        

        
        
        
        
        
        
        
        
        

        

        

        
        
        
        
        public override void ExposeData()
        {
            base.ExposeData();
            
            Scribe_Values.Look(ref lastDecayTick, "lastDecayTick", 0);
            Scribe_Values.Look(ref lastSummarizationDay, "lastSummarizationDay", -1);
            Scribe_Values.Look(ref lastArchiveDay, "lastArchiveDay", -1);
            Scribe_Values.Look(ref nextSummarizationTick, "nextSummarizationTick", 0);
            
            Scribe_Deep.Look(ref commonKnowledge, "commonKnowledge");
            Scribe_Deep.Look(ref conversationCache, "conversationCache");
            Scribe_Deep.Look(ref promptCache, "promptCache");
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // A save written before one of these existed comes back with it
                // null, and creating it is the whole of the repair. That is an
                // ordinary thing for a save to need, not a fault - so it is one
                // message naming what was created rather than three warnings,
                // each of which opened the debug window on its own.
                //
                // Still said out loud. If these start appearing on saves that
                // are not old, something is failing to write them and the line
                // is the only place that would show it.
                List<string> created = null;
                if (commonKnowledge == null)
                {
                    commonKnowledge = new CommonKnowledgeLibrary();
                    (created = created ?? new List<string>()).Add("commonKnowledge");
                }
                if (conversationCache == null)
                {
                    conversationCache = new ConversationCache();
                    (created = created ?? new List<string>()).Add("conversationCache");
                }
                if (promptCache == null)
                {
                    promptCache = new PromptCache();
                    (created = created ?? new List<string>()).Add("promptCache");
                }
                if (created != null)
                {
                    ModuleLog.Message("[RimAI.Memory] This save had no " + string.Join(", ", created)
                        + "; created fresh. Normal for a save made before that part existed.");
                }
                
                if (summarizationQueue == null)
                    summarizationQueue = new Queue<Pawn>();
                if (manualSummarizationQueue == null)
                    manualSummarizationQueue = new Queue<Pawn>();
                
                int currentDay = GenDate.DaysPassed;
                
                if (lastArchiveDay == -1)
                {
                    lastArchiveDay = currentDay;
                    Log.Warning($"[RimAI.Memory] ⚠️ Old save detected! Initialized lastArchiveDay to {currentDay} to prevent immediate archive.");
                }
                
                if (lastSummarizationDay == -1)
                {
                    lastSummarizationDay = currentDay;
                    Log.Warning($"[RimAI.Memory] ⚠️ Old save detected! Initialized lastSummarizationDay to {currentDay} to prevent immediate summarization.");
                }
                
                ModuleLog.Message($"[RimAI.Memory] MemoryManager loaded successfully.");
            }
        }
        
        #region 辅助方法
        
        #endregion
    
        #region Cluster forwards
        public override void WorldComponentTick() => Parts.Tick.WorldComponentTick();
        internal void UpdateEventKnowledgeTimePrefixes() => Parts.Tick.UpdateEventKnowledgeTimePrefixes();
        internal void CheckDailySummarization() => Parts.Tick.CheckDailySummarization();
        internal static bool IsColonyAnimalWithVocalLink(Pawn pawn) => MemoryManagerTick.IsColonyAnimalWithVocalLink(pawn);
        internal void SummarizeAllMemories() => Parts.Summarization.SummarizeAllMemories();
        internal void ProcessSummarizationQueue() => Parts.Summarization.ProcessSummarizationQueue();
        internal void ProcessManualSummarizationQueue() => Parts.Summarization.ProcessManualSummarizationQueue();
        public void QueueManualSummarization(List<Pawn> pawns) => Parts.Summarization.QueueManualSummarization(pawns);
        internal void DecayAllMemories() => Parts.Summarization.DecayAllMemories();
        internal string FormatConversationText(PendingConversation record) => Parts.Conversation.FormatConversationText(record);
        internal List<Pawn> FindPawnsByThingIds(List<string> thingIds) => Parts.Conversation.FindPawnsByThingIds(thingIds);
        internal void CheckArchiveInterval(int currentDay) => Parts.Archive.CheckArchiveInterval(currentDay);
        internal string CreateArchiveSummary(List<MemoryEntry> memories, MemoryType type) => Parts.Archive.CreateArchiveSummary(memories, type);
        #endregion
}
    internal sealed class MemoryManagerTick : MemoryManagerCollaborator
    {
        internal MemoryManagerTick(MemoryManager owner) : base(owner) { }

public void WorldComponentTick()
        {
            if (sessionStartTick == -1) sessionStartTick = Find.TickManager.TicksGame;
            if (Find.TickManager.TicksGame - sessionStartTick < COLD_START_DELAY) return;

            if (Find.TickManager.TicksGame - lastDecayTick >= DecayInterval)
            {
                Owner.DecayAllMemories();
                lastDecayTick = Find.TickManager.TicksGame;
                
                
                if (RimTalkMemoryPatchMod.Settings.enablePawnStatusKnowledge)
                {
                    PawnStatusKnowledgeGenerator.UpdateAllColonistStatus();
                }
                
                
                PawnStatusKnowledgeGenerator.CleanupUpdateRecords();
            }
            
            Owner.ProcessSummarizationQueue();
            
            Owner.ProcessManualSummarizationQueue();
            
            
            CheckDailySummarization();
        }

internal void UpdateEventKnowledgeTimePrefixes()
        {
            if (commonKnowledge == null || commonKnowledge.Entries == null)
                return;
            
            int currentTick = Find.TickManager.TicksGame;
            int updatedCount = 0;
            
            foreach (var entry in commonKnowledge.Entries)
            {
                if (entry.creationTick >= 0 && !string.IsNullOrEmpty(entry.originalEventText))
                {
                    string oldContent = entry.content;
                    
                    entry.UpdateEventTimePrefix(currentTick);
                    
                    if (entry.content != oldContent)
                    {
                        updatedCount++;
                    }
                }
            }
            
            if (Prefs.DevMode && updatedCount > 0 && UnityEngine.Random.value < 0.1f)
            {
                ModuleLog.Message($"[RimAI.Memory] Updated {updatedCount} event knowledge time prefixes");
            }
        }

internal void CheckDailySummarization()
        {
            if (Current.Game == null || Find.CurrentMap == null) return;
            
            if (!RimTalkMemoryPatchMod.Settings.enableDailySummarization)
                return;
            
            int currentDay = GenDate.DaysPassed;
            int currentHour = GenLocalDate.HourOfDay(Find.CurrentMap);
            int targetHour = RimTalkMemoryPatchMod.Settings.summarizationHour;
            
            if (currentDay != lastSummarizationDay && currentHour == targetHour)
            {
                ModuleLog.Message($"[RimAI.Memory] 🌙 Day {currentDay}, Hour {currentHour}: Triggering daily ELS summarization");
                
                foreach (var map in Find.Maps)
                {
                    foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                    {
                        if (pawn.IsColonist || IsColonyAnimalWithVocalLink(pawn))
                        {
                            summarizationQueue.Enqueue(pawn);
                        }
                    }
                }
                
                lastSummarizationDay = currentDay;
            }
            
            Owner.CheckArchiveInterval(currentDay);
        }

internal static bool IsColonyAnimalWithVocalLink(Pawn pawn)
        {
            if (pawn == null || pawn.Faction != Faction.OfPlayer) return false;
            if (pawn.RaceProps?.Humanlike == true) return false;
            
            try
            {
                var vocalLinkDef = DefDatabase<HediffDef>.GetNamed("VocalLinkImplant", false);
                return vocalLinkDef != null && pawn.health?.hediffSet?.HasHediff(vocalLinkDef) == true;
            }
            catch
            {
                return false;
            }
        }
    }

    internal sealed class MemoryManagerParts
    {
        internal readonly MemoryManager Owner;
        internal readonly MemoryManagerTick Tick;
        internal readonly MemoryManagerSummarization Summarization;
        internal readonly MemoryManagerConversation Conversation;
        internal readonly MemoryManagerArchive Archive;
        internal MemoryManagerParts(MemoryManager owner)
        {
            Owner = owner;
            Tick = new MemoryManagerTick(owner);
            Summarization = new MemoryManagerSummarization(owner);
            Conversation = new MemoryManagerConversation(owner);
            Archive = new MemoryManagerArchive(owner);
        }
    }

}
