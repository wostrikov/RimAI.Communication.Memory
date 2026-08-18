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
    /// <summary>
    /// 四层记忆系统核心组件
    /// ABM -> SCM -> ELS -> CLPA
    /// </summary>
    public class FourLayerMemoryComp : ThingComp
    {
        internal FourLayerMemoryCompParts Parts;

        // 核心记忆存储
        internal List<MemoryEntry> activeMemories = new();      // ABM: 完整对话记录，无容量限制，总结后转 ELS
        internal List<MemoryEntry> situationalMemories = new(); // SCM: 固定后的轮次记忆ABM，或是非轮次记忆的过渡层
        internal List<MemoryEntry> eventLogMemories = new();    // ELS: 总结后的记忆，~50条
        internal List<MemoryEntry> archiveMemories = new();     // CLPA: 归档后的记忆，无容量限制

        // 直接持有工作记忆捕获模块
        internal readonly JobMemoryCapturer _jobCapturer;

        // 属性访问
        /// <summary>
        /// ABM: 完整对话记录，无容量限制，总结后转 ELS
        /// </summary>
        public List<MemoryEntry> ActiveMemories => activeMemories;

        /// <summary>
        /// SCM: 固定后的轮次记忆ABM，或是非轮次记忆的过渡层
        /// </summary>
        public List<MemoryEntry> SituationalMemories => situationalMemories;

        /// <summary>
        /// ELS: 总结后的记忆
        /// </summary>
        public List<MemoryEntry> EventLogMemories => eventLogMemories;

        /// <summary>
        /// CLPA: 归档后的记忆
        /// </summary>
        public List<MemoryEntry> ArchiveMemories => archiveMemories;

        /// <summary>
        /// 工作记忆捕获模块，负责捕获工作相关的记忆并存入 ABM
        /// </summary>
        public JobMemoryCapturer JobCapturer => _jobCapturer;

        // 配置项（从设置中读取）
        public static bool IsRoundMemoryEnabled => RimTalkMemoryPatchMod.Settings?.IsRoundMemoryActive ?? false;
        internal int MaxABM => RimTalkMemoryPatchMod.Settings.maxActiveMemories;
        internal int MaxSCM => RimTalkMemoryPatchMod.Settings.maxSituationalMemories;
        internal int MaxELS => RimTalkMemoryPatchMod.Settings.maxEventLogMemories;

        // 构造函数，初始化捕获模块
        public FourLayerMemoryComp()
        {
            Parts = new FourLayerMemoryCompParts(this);
            _jobCapturer = new JobMemoryCapturer(this);
        }


        // 存档读写
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref activeMemories, "activeMemories", LookMode.Deep); // label建议使用大写开头。但此处屎山已成
            Scribe_Collections.Look(ref situationalMemories, "situationalMemories", LookMode.Deep);
            Scribe_Collections.Look(ref eventLogMemories, "eventLogMemories", LookMode.Deep);
            Scribe_Collections.Look(ref archiveMemories, "archiveMemories", LookMode.Deep);

            // 集合空保护
            activeMemories ??= new();
            situationalMemories ??= new();
            eventLogMemories ??= new();
            archiveMemories ??= new();
        }

        

        // 经过艰辛的排查，终于确定此方法用于【一键总结所有殖民者】
        

        /// <summary>
        /// ⭐ 新方法：根据时间戳将记忆插入到正确的位置（保持列表按时间降序排序）
        /// </summary>
        

        

        

        

        /// <summary>
        /// 记忆衰减和自动清理
        /// ⭐ v3.3.14: 添加activity阈值清理 + 容量限制
        /// </summary>
        

        /// <summary>
        /// ⭐ v3.3.14: 清理极低activity的记忆（方案1）
        /// 当activity < 0.01时，认为记忆已"死亡"，可以安全删除
        /// ⭐ 移除 isUserEdited 检查，只保留固定记忆保护
        /// </summary>
        

        /// <summary>
        /// ⭐ v3.3.14: 强制执行容量限制（方案3）
        /// 当层级超过容量时，删除最低activity的记忆
        /// ⭐ 移除 isUserEdited 检查，只保留固定记忆保护
        /// </summary>
        

        /// <summary>
        /// ⭐ v4.0: 更新检索逻辑
        /// - ABM: 按 conversationId 去重后返回所有
        /// - SCM: 仅兼容旧存档，返回已有的
        /// - ELS/CLPA: 保持原有逻辑
        /// </summary>
        

        

        

        
        // RoundMemory入口
        
        // 获取 Memory 窗口实例
        internal static MainTabWindow_Memory GetMemoryWindowInstance()
        {
            return Find.WindowStack.Windows
                .OfType<MainTabWindow_Memory>()
                .FirstOrDefault();
        }

        

        

        

        // 此方法未正确处理固定的记忆
        


        // 注入层相关，待后续分离解耦
        // 兼容旧API：GetMemoryContext
        

        // 兼容旧API：GetRelevantMemories
        


        // 以下成员为非轮次记忆管线，已不再维护和更新
        /// <summary>
        /// 添加记忆到超短期记忆（ABM）
        /// 非轮次记忆管线，已不再维护和更新
        /// </summary>
        

        

        
    
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
