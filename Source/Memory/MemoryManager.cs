using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Patches;

namespace Ustas.RimAI.Communication.Memory
{
    /// <summary>
    /// WorldComponent to manage global memory decay and daily summarization
    /// 支持四层记忆系统 (FMS)
    /// ⭐ v3.3.2.3: 添加向后兼容性支持
    /// </summary>
    public class MemoryManager : WorldComponent
    {
        internal MemoryManagerParts Parts;

        // ⭐ 静态构造函数确保类型正确注册
        static MemoryManager()
        {
            // RimWorld会自动发现和注册WorldComponent子类
            // 这个静态构造函数确保类型在使用前被初始化
        }
        
        internal int lastDecayTick = 0;
        internal const int DecayInterval = 2500; // Every in-game hour
        
        internal int lastSummarizationDay = -1; // 上次ELS总结的日期
        internal int lastArchiveDay = -1;        // 上次CLPA归档的日期
        
        // ⭐ 冷启动缓冲：本次会话开始时间（不保存）
        internal int sessionStartTick = -1;
        internal const int COLD_START_DELAY = 200; // 启动后延迟200 ticks (约3秒) 再开始运作

        // ⭐ 总结队列（延迟处理）
        internal Queue<Pawn> summarizationQueue = new Queue<Pawn>();
        internal int nextSummarizationTick = 0;
        internal const int SUMMARIZATION_DELAY_TICKS = 900; // 15秒 = 15 * 60 ticks
        
        // ⭐ 手动总结队列（延迟1秒）
        internal Queue<Pawn> manualSummarizationQueue = new Queue<Pawn>();
        internal int nextManualSummarizationTick = 0;
        internal const int MANUAL_SUMMARIZATION_DELAY_TICKS = 60; // 1秒 = 60 ticks

        // 全局常识库
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
        
        // 对话缓存
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
        
        // ⭐ 提示词缓存（新增）
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

        /// <summary>
        /// 静态方法获取常识库
        /// </summary>
        public static CommonKnowledgeLibrary GetCommonKnowledge()
        {
            if (Current.Game == null) return new CommonKnowledgeLibrary();
            
            var manager = Find.World.GetComponent<MemoryManager>();
            return manager?.CommonKnowledge ?? new CommonKnowledgeLibrary();
        }
        
        /// <summary>
        /// 静态方法获取对话缓存
        /// </summary>
        public static ConversationCache GetConversationCache()
        {
            if (Current.Game == null) return new ConversationCache();
            
            var manager = Find.World.GetComponent<MemoryManager>();
            return manager?.ConversationCache ?? new ConversationCache();
        }
        
        /// <summary>
        /// ⭐ 静态方法获取提示词缓存（新增）
        /// </summary>
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

        
        
        /// <summary>
        /// ⭐ 修复2：更新所有事件常识的时间前缀
        /// </summary>
        
        
        /// <summary>
        /// 检查并触发每日总结（游戏时间 0 点）
        /// </summary>
        

        /// <summary>
        /// 为所有殖民者触发每日总结
        /// </summary>
        

        /// <summary>
        /// ⭐ 处理总结队列（每个殖民者之间延迟15秒）
        /// </summary>
        

        /// <summary>
        /// ⭐ 处理手动总结队列（每个殖民者之间延迟1秒）
        /// </summary>
        
        
        /// <summary>
        /// ⭐ v4.0: 处理对话记忆队列
        /// 从异步线程的队列中取出完整对话，为所有参与者添加ABM记忆
        /// </summary>
        
        
        
        
        /// <summary>
        /// ⭐ v4.0: 格式化对话文本
        /// 格式: [对话参与者：张三、李四、王五]
        ///       张三: "你好"
        ///       李四: "你好啊"
        /// </summary>
        
        
        /// <summary>
        /// ⭐ v4.0: 通过 ThingID 列表查找 Pawn
        /// </summary>
        

        /// <summary>
        /// ⭐ 手动触发总结（批量）
        /// </summary>
        

        /// <summary>
        /// 为所有殖民者触发记忆衰减
        /// </summary>
        

        /// <summary>
        /// 检查并触发CLPA归档（按天数间隔）
        /// ⭐ v3.3.2.33: 重构 - 实现真正的 ELS → CLPA 自动归档（前25%）
        /// </summary>
        /// <param name="currentDay">当前游戏中的天数</param>
        
        
        /// <summary>
        /// 创建归档摘要（简单版本）
        /// </summary>
        
        
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
                // ⭐ 确保所有组件都已初始化
                if (commonKnowledge == null)
                {
                    commonKnowledge = new CommonKnowledgeLibrary();
                    Log.Warning("[RimAI.Memory] commonKnowledge was null, initialized new instance");
                }
                if (conversationCache == null)
                {
                    conversationCache = new ConversationCache();
                    Log.Warning("[RimAI.Memory] conversationCache was null, initialized new instance");
                }
                if (promptCache == null)
                {
                    promptCache = new PromptCache();
                    Log.Warning("[RimAI.Memory] promptCache was null, initialized new instance");
                }
                
                // ⭐ 重新初始化队列（不保存到存档）
                if (summarizationQueue == null)
                    summarizationQueue = new Queue<Pawn>();
                if (manualSummarizationQueue == null)
                    manualSummarizationQueue = new Queue<Pawn>();
                
                // ⭐ 兼容性处理：旧存档初始化
                // 如果是旧存档（没有记录过日期），将日期初始化为当前日期，防止立即触发归档/总结
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
                
                Log.Message($"[RimAI.Memory] MemoryManager loaded successfully.");
            }
        }
        
        #region 辅助方法
        /// <summary>
        /// ⭐ v3.5.2: 检测是否为配置了链接催化剂的殖民地动物或机械体
        /// </summary>
        
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
            // ⭐ 冷启动缓冲：进入游戏后延迟运作，避免加载时的性能冲击
            if (sessionStartTick == -1) sessionStartTick = Find.TickManager.TicksGame;
            if (Find.TickManager.TicksGame - sessionStartTick < COLD_START_DELAY) return;

            // 每小时衰减记忆活跃度
            if (Find.TickManager.TicksGame - lastDecayTick >= DecayInterval)
            {
                Owner.DecayAllMemories();
                lastDecayTick = Find.TickManager.TicksGame;
                
                // 检查工作会话超时
                // 已迁移至 JobMemoryCapturer 的 CompTick 中自动处理
                
                // ⭐ 每小时更新Pawn状态常识（24小时间隔检查）
                if (RimTalkMemoryPatchMod.Settings.enablePawnStatusKnowledge)
                {
                    PawnStatusKnowledgeGenerator.UpdateAllColonistStatus();
                }
                
                // ⭐ v3.4.0: 移除常识库自动生成事件历史功能
                // 原有的 EventRecordKnowledgeGenerator.ScanRecentPlayLog() 调用已移除
                
                // 定期清理
                PawnStatusKnowledgeGenerator.CleanupUpdateRecords();
            }
            
            // ⭐ 处理总结队列（每tick检查）
            Owner.ProcessSummarizationQueue();
            
            // ⭐ 处理手动总结队列
            Owner.ProcessManualSummarizationQueue();
            
            // ⭐ v4.0: 处理对话记忆队列（新的完整对话记忆系统）
            //ProcessConversationQueue();
            
            // 每天 0 点触发总结
            CheckDailySummarization();
        }

internal void UpdateEventKnowledgeTimePrefixes()
        {
            if (commonKnowledge == null || commonKnowledge.Entries == null)
                return;
            
            int currentTick = Find.TickManager.TicksGame;
            int updatedCount = 0;
            
            // 只更新带时间戳的事件常识
            foreach (var entry in commonKnowledge.Entries)
            {
                if (entry.creationTick >= 0 && !string.IsNullOrEmpty(entry.originalEventText))
                {
                    // 保存原始内容用于比较
                    string oldContent = entry.content;
                    
                    // 更新时间前缀
                    entry.UpdateEventTimePrefix(currentTick);
                    
                    // 如果内容发生变化，计数
                    if (entry.content != oldContent)
                    {
                        updatedCount++;
                    }
                }
            }
            
            // 开发模式日志（每10次更新才输出一次）
            if (Prefs.DevMode && updatedCount > 0 && UnityEngine.Random.value < 0.1f)
            {
                Log.Message($"[RimAI.Memory] Updated {updatedCount} event knowledge time prefixes");
            }
        }

internal void CheckDailySummarization()
        {
            if (Current.Game == null || Find.CurrentMap == null) return;
            
            // 检查设置是否启用
            if (!RimTalkMemoryPatchMod.Settings.enableDailySummarization)
                return;
            
            int currentDay = GenDate.DaysPassed;
            int currentHour = GenLocalDate.HourOfDay(Find.CurrentMap);
            int targetHour = RimTalkMemoryPatchMod.Settings.summarizationHour;
            
            // 当天第一次检查，且时间在目标小时（ELS总结：每天一次）
            if (currentDay != lastSummarizationDay && currentHour == targetHour)
            {
                Log.Message($"[RimAI.Memory] 🌙 Day {currentDay}, Hour {currentHour}: Triggering daily ELS summarization");
                
                foreach (var map in Find.Maps)
                {
                    foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                    {
                        // ⭐ v3.5.2: 扩展到殖民者 + 配置了链接催化剂的殖民地动物/机械体
                        if (pawn.IsColonist || IsColonyAnimalWithVocalLink(pawn))
                        {
                            // 将总结任务加入队列
                            summarizationQueue.Enqueue(pawn);
                        }
                    }
                }
                
                lastSummarizationDay = currentDay;
            }
            
            // CLPA归档：按天数间隔触发
            Owner.CheckArchiveInterval(currentDay);
        }

internal static bool IsColonyAnimalWithVocalLink(Pawn pawn)
        {
            if (pawn == null || pawn.Faction != Faction.OfPlayer) return false;
            if (pawn.RaceProps?.Humanlike == true) return false; // 人类已经被 IsColonist 覆盖
            
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
