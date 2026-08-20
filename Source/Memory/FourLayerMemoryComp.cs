using Ustas.RimAI.Communication.Memory.Capture;
using Ustas.RimAI.Communication.Memory.UI;
using Ustas.RimAI.Communication.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Ustas.RimAI.Communication.Memory
{
    public class FourLayerMemoryComp : ThingComp
    {
        internal FourLayerMemoryCompParts Parts;

        internal List<MemoryEntry> activeMemories = new();     
        internal List<MemoryEntry> situationalMemories = new();
        internal List<MemoryEntry> eventLogMemories = new();   
        internal List<MemoryEntry> archiveMemories = new();    

        internal readonly JobMemoryCapturer _jobCapturer;

        public List<MemoryEntry> ActiveMemories => activeMemories;

        public List<MemoryEntry> SituationalMemories => situationalMemories;

        public List<MemoryEntry> EventLogMemories => eventLogMemories;

        public List<MemoryEntry> ArchiveMemories => archiveMemories;

        public JobMemoryCapturer JobCapturer => _jobCapturer;

        public static bool IsRoundMemoryEnabled => RimTalkMemoryPatchMod.Settings?.IsRoundMemoryActive ?? false;
        internal int MaxABM => RimTalkMemoryPatchMod.Settings.maxActiveMemories;
        internal int MaxSCM => RimTalkMemoryPatchMod.Settings.maxSituationalMemories;
        internal int MaxELS => RimTalkMemoryPatchMod.Settings.maxEventLogMemories;

        public FourLayerMemoryComp()
        {
            Parts = new FourLayerMemoryCompParts(this);
            _jobCapturer = new JobMemoryCapturer(this);
        }


        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref activeMemories, "activeMemories", LookMode.Deep);
            Scribe_Collections.Look(ref situationalMemories, "situationalMemories", LookMode.Deep);
            Scribe_Collections.Look(ref eventLogMemories, "eventLogMemories", LookMode.Deep);
            Scribe_Collections.Look(ref archiveMemories, "archiveMemories", LookMode.Deep);

            activeMemories ??= new();
            situationalMemories ??= new();
            eventLogMemories ??= new();
            archiveMemories ??= new();
        }

        

        

        

        

        

        

        

        

        

        

        

        

        
        
        internal static MainTabWindow_Memory GetMemoryWindowInstance()
        {
            return Find.WindowStack.Windows
                .OfType<MainTabWindow_Memory>()
                .FirstOrDefault();
        }

        

        

        

        


        

        


        

        

        
    
        #region Cluster forwards
        public void DailySummarization() => Parts.Summarization.DailySummarization();
        public void ManualSummarization() => Parts.Summarization.ManualSummarization();
        internal void InsertMemoryByTimestamp(List<MemoryEntry> list, MemoryEntry entry) => Parts.Summarization.InsertMemoryByTimestamp(list, entry);
        internal string CreateSimpleSummary(List<MemoryEntry> memories, MemoryType type) => Parts.Summarization.CreateSimpleSummary(memories, type);
        internal void TrimEventLog() => Parts.Summarization.TrimEventLog();
        internal void ExtractKeywords(MemoryEntry memory) => Parts.Summarization.ExtractKeywords(memory);
        public void DecayActivity() => Parts.Decay.DecayActivity();
        internal void CleanupLowActivityMemories() => Parts.Decay.CleanupLowActivityMemories();
        internal void EnforceMemoryLimits() => Parts.Decay.EnforceMemoryLimits();
        internal void PromoteToSituational(MemoryEntry memory) => Parts.Decay.PromoteToSituational(memory);
        public List<MemoryEntry> RetrieveMemories(MemoryQuery query) => Parts.Query.RetrieveMemories(query);
        internal bool MatchesQuery(MemoryEntry memory, MemoryQuery query) => Parts.Query.MatchesQuery(memory, query);
        public List<MemoryEntry> GetAllMemories() => Parts.Query.GetAllMemories();
        public string GetMemoryContext(int count = 5) => Parts.Query.GetMemoryContext(count);
        public List<MemoryEntry> GetRelevantMemories(int count = 5) => Parts.Query.GetRelevantMemories(count);
        internal MemoryEntry FindMemoryById(string id) => Parts.Query.FindMemoryById(id);
        public void EditMemory(string memoryId, string newContent, string notes = null) => Parts.Mutation.EditMemory(memoryId, newContent, notes);
        public void PinMemory(string memoryId, bool pinned) => Parts.Mutation.PinMemory(memoryId, pinned);
        public void PinRoundMemory(RoundMemory roundMemory, string memoryId) => Parts.Mutation.PinRoundMemory(roundMemory, memoryId);
        public void DeleteMemory(string memoryId) => Parts.Mutation.DeleteMemory(memoryId);
        public void ManualArchive() => Parts.Mutation.ManualArchive();
        public void AddActiveMemory(string content, MemoryType type, float importance = 1f, string relatedPawn = null) => Parts.Mutation.AddActiveMemory(content, type, importance, relatedPawn);
        internal bool IsDuplicateMemory(string content, string relatedPawn, MemoryType type) => Parts.Mutation.IsDuplicateMemory(content, relatedPawn, type);
        #endregion
}
    internal sealed class FourLayerMemoryCompParts
    {
        internal readonly FourLayerMemoryComp Owner;
        internal readonly FourLayerSummarization Summarization;
        internal readonly FourLayerDecay Decay;
        internal readonly FourLayerQuery Query;
        internal readonly FourLayerMutation Mutation;
        internal FourLayerMemoryCompParts(FourLayerMemoryComp owner)
        {
            Owner = owner;
            Summarization = new FourLayerSummarization(owner);
            Decay = new FourLayerDecay(owner);
            Query = new FourLayerQuery(owner);
            Mutation = new FourLayerMutation(owner);
        }
    }


}
