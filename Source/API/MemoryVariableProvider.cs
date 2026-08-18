using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.Injection;
using Ustas.RimAI.Communication.Prompt;
using Ustas.RimAI.Core.Diagnostics;
using Ustas.RimAI.Core.Memory;

namespace Ustas.RimAI.Communication.Memory.API
{
    /// <summary>
    /// Provides {{pawn.memory}} for Scriban. On the normal Talk path, returns the Projection
    /// already prepared by PromptManager.AttachTypedMemoryContext (OPTION A).
    /// Fallback retrieval runs only when no precomputed Projection is present.
    /// </summary>
    public static class MemoryVariableProvider
    {
        /// <summary>
        /// Gets Memory text for a pawn. Prefer precomputed TypedMemoryProjections from LastContext.
        /// </summary>
        public static string GetPawnMemory(Pawn pawn)
        {
            if (pawn == null)
            {
                return "";
            }

            // OPTION A — normal Talk path: present Attach's query-aware Projection (no second GetContext).
            var promptContext = PromptManager.LastContext;
            if (promptContext != null
                && promptContext.UsedTypedMemoryContext
                && promptContext.TryGetTypedMemoryProjection(pawn.ThingID, out var precomputed))
            {
                return precomputed ?? string.Empty;
            }

            var typed = MemoryContextAccess.Current;
            if (typed != null)
            {
                // Fallback: template/preview without Attach. Explicit PawnId-only request.
                var result = typed.GetContext(new MemoryContextRequest { PawnId = pawn.ThingID });
                return result?.Projection ?? string.Empty;
            }

            try
            {
                var settings = RimTalkMemoryPatchMod.Settings;

                // 优先使用四层记忆系统
                var fourLayerComp = pawn.TryGetComp<FourLayerMemoryComp>();
                if (fourLayerComp != null)
                {
                    // ⭐ v5.1: 规范化在 GetFourLayerMemories 内部已处理（缓存后规范化）
                    return GetFourLayerMemories(pawn, fourLayerComp, settings);
                }

                // 回退到旧的记忆组件
                var memoryComp = pawn.TryGetComp<PawnMemoryComp>();
                if (memoryComp != null)
                {
                    // ⭐ v5.1: 旧版路径补规范化
                    return PromptNormalizer.Normalize(GetLegacyMemories(memoryComp, settings));
                }

                return "(Компонент пам'яті відсутній)";
            }
            catch (Exception ex)
            {
                // RimAI.exception: TEMPORARY_EXPLICIT_EXCEPTION — Scriban fire-and-forget must not fail talk render.
                RimAiLog.Warning(RimAiLogCategory.Memory, $"[MemoryPatch] Error getting pawn memory for {pawn?.LabelShort}.", exception: ex);
                return "";
            }
        }

        /// <summary>
        /// ⭐ v4.2: 缓存每个 Pawn 的记忆结果，避免重复计算
        /// Key: Pawn.ThingID, Value: 记忆文本
        /// </summary>
        [ThreadStatic]
        private static Dictionary<string, string> _pawnMemoryCache;

        /// <summary>
        /// ⭐ v4.2: 上次缓存的时间戳
        /// </summary>
        [ThreadStatic]
        private static int _memoryCacheTick;

        /// <summary>
        /// ⭐ v4.2: 缓存有效期（2秒 = 120 ticks）
        /// </summary>
        private const int MEMORY_CACHE_EXPIRE_TICKS = 120;

        /// <summary>
        /// ⭐ v5.0: 获取四层记忆系统的记忆（重构版）
        ///
        /// 使用 UnifiedMemoryInjector 统一调度：
        /// 1. 对话记忆优先
        /// 2. ABM 占用总配额
        /// 3. 序号连续
        /// </summary>
        private static string GetFourLayerMemories(Pawn pawn, FourLayerMemoryComp comp, RimTalkMemoryPatchSettings settings)
        {
            string pawnId = pawn.ThingID;
            int currentTick = Find.TickManager?.TicksGame ?? 0;

            // ⭐ v4.2 / v5.0.1: 检查缓存是否有效
            // 修复：加载新存档时，currentTick 可能小于 _memoryCacheTick，导致差值为负数
            // 使用 Math.Abs 或者检查 currentTick < _memoryCacheTick 来处理这种情况
            if (_pawnMemoryCache == null ||
                currentTick < _memoryCacheTick ||  // 新存档加载，tick 重置
                currentTick - _memoryCacheTick > MEMORY_CACHE_EXPIRE_TICKS)
            {
                _pawnMemoryCache = new Dictionary<string, string>();
                _memoryCacheTick = currentTick;
            }

            // ⭐ v4.2: 如果缓存中有这个 Pawn 的结果，直接返回
            if (_pawnMemoryCache.TryGetValue(pawnId, out string cachedResult))
            {
                if (Prefs.DevMode)
                {
                    RimAiLog.Debug(RimAiLogCategory.Memory, $"[Memory] Using cached result for {pawn.LabelShort}");
                }
                return cachedResult;
            }

            // ⭐ v5.0: 使用 UnifiedMemoryInjector 统一注入
            string dialogueContext = GetCurrentDialogueContext();
            string result = UnifiedMemoryInjector.Inject(pawn, dialogueContext);

            // 如果结果为空，回退到最近记忆
            if (string.IsNullOrEmpty(result))
            {
                result = GetRecentMemories(comp, settings.maxInjectedMemories);
            }

            // ⭐ v5.1: 应用提示词规范化规则（迁移自 SmartInjectionManager）
            // 注意：必须先规范化，再缓存，否则缓存命中时会绕过规范化
            result = PromptNormalizer.Normalize(result);

            // ⭐ v4.2: 缓存结果（存规范化后的内容）
            _pawnMemoryCache[pawnId] = result;

            return result;
        }

        /// <summary>
        /// 获取最近的记忆（无匹配时的回退）
        /// </summary>
        private static string GetRecentMemories(FourLayerMemoryComp comp, int maxCount)
        {
            var recentMemories = new List<MemoryEntry>();

            // 从各层收集最近的记忆
            recentMemories.AddRange(comp.SituationalMemories.Take(maxCount / 2));
            recentMemories.AddRange(comp.EventLogMemories.Take(maxCount / 2));

            if (recentMemories.Count == 0)
            {
                return "(Спогадів поки немає)";
            }

            // 按时间排序
            var sortedMemories = recentMemories
                .OrderByDescending(m => m.GameTick)
                .Take(maxCount)
                .ToList();

            // ⭐ v5.0: 使用 MemoryFormatter 格式化
            return MemoryFormatter.Format(sortedMemories);
        }

        /// <summary>
        /// 获取旧版记忆组件的记忆
        /// </summary>
        private static string GetLegacyMemories(PawnMemoryComp comp, RimTalkMemoryPatchSettings settings)
        {
            var memories = comp.GetRelevantMemories(settings.maxInjectedMemories);

            if (memories == null || memories.Count == 0)
            {
                return "(Спогадів поки немає)";
            }

            var sb = new StringBuilder();
            int index = 1;

            foreach (var memory in memories)
            {
                sb.AppendLine($"{index}. {memory.Content} ({memory.AgeString})");
                index++;
            }

            return sb.ToString().TrimEnd();
        }

        // ⭐ v5.0: FormatMemories 和 GetMemoryTypeTag 已移动到 MemoryFormatter
        // 保留此注释以便追踪代码变更历史

        /// <summary>
        /// 获取当前对话上下文（用于关键词匹配）
        /// 从 RimTalkMemoryAPI 获取缓存的上下文
        /// </summary>
        private static string GetCurrentDialogueContext()
        {
            // From RimTalkMemoryAPI cache when the untyped fallback path runs.
            var context = Patches.RimTalkMemoryAPI.GetLastRimTalkContext(out _, out int tick);

            // Cache expires after 60 ticks.
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (currentTick - tick > 60)
            {
                return "";
            }

            return context ?? "";
        }

        // 注意：固定记忆(isPinned)不需要单独处理
        // DynamicMemoryInjection 已经给 isPinned 的记忆加了 0.5 的评分加成
        // 它们会自然地排在 {{memory}} 输出的前面

        /// <summary>
        /// 获取 Pawn 的 ABM 层记忆（超短期记忆）
        /// 由 RimTalk Mustache Parser 在解析 {{pawnN.ABM}} 时调用
        /// ⭐ v5.0: 使用 UnifiedMemoryInjector.InjectABMOnly
        /// </summary>
        public static string GetPawnABM(Pawn pawn)
        {
            if (pawn == null) return "";

            try
            {
                // ⭐ v5.1: 应用提示词规范化规则
                return PromptNormalizer.Normalize(UnifiedMemoryInjector.InjectABMOnly(pawn));
            }
            catch (Exception ex)
            {
                RimAiLog.Warning(RimAiLogCategory.Memory, $"[MemoryPatch] Error getting ABM for {pawn?.LabelShort}.", exception: ex);
                return "";
            }
        }

        /// <summary>
        /// 获取 Pawn 的 ELS 层记忆（中期记忆 - Event Log Summary）
        /// 由 RimTalk Mustache Parser 在解析 {{pawnN.ELS}} 时调用
        /// </summary>
        public static string GetPawnELS(Pawn pawn)
        {
            if (pawn == null) return "";

            try
            {
                var comp = pawn.TryGetComp<FourLayerMemoryComp>();
                if (comp == null || comp.EventLogMemories == null || comp.EventLogMemories.Count == 0)
                {
                    return "(Спогадів ELS немає)";
                }

                // ⭐ v5.1: 应用提示词规范化规则
                return PromptNormalizer.Normalize(FormatMemoryList(comp.EventLogMemories, MemoryLayer.EventLog));
            }
            catch (Exception ex)
            {
                RimAiLog.Warning(RimAiLogCategory.Memory, $"[MemoryPatch] Error getting ELS for {pawn?.LabelShort}.", exception: ex);
                return "";
            }
        }

        /// <summary>
        /// 获取 Pawn 的 CLPA 层记忆（长期记忆 - Colony Lore & Persona Archive）
        /// 由 RimTalk Mustache Parser 在解析 {{pawnN.CLPA}} 时调用
        /// </summary>
        public static string GetPawnCLPA(Pawn pawn)
        {
            if (pawn == null) return "";

            try
            {
                var comp = pawn.TryGetComp<FourLayerMemoryComp>();
                if (comp == null || comp.ArchiveMemories == null || comp.ArchiveMemories.Count == 0)
                {
                    return "(Спогадів CLPA немає)";
                }

                // ⭐ v5.1: 应用提示词规范化规则
                return PromptNormalizer.Normalize(FormatMemoryList(comp.ArchiveMemories, MemoryLayer.Archive));
            }
            catch (Exception ex)
            {
                RimAiLog.Warning(RimAiLogCategory.Memory, $"[MemoryPatch] Error getting CLPA for {pawn?.LabelShort}.", exception: ex);
                return "";
            }
        }

        /// <summary>
        /// 格式化记忆列表（用于 ELS/CLPA 输出）
        /// ⭐ v5.0: 使用 MemoryFormatter 格式化
        /// </summary>
        private static string FormatMemoryList(List<MemoryEntry> memories, MemoryLayer layer)
        {
            if (memories == null || memories.Count == 0)
            {
                return "";
            }

            var settings = RimTalkMemoryPatchMod.Settings;
            int maxCount = settings?.maxInjectedMemories ?? 10;

            // 按时间降序排序（最新的在前）
            var sortedMemories = memories
                .OrderByDescending(m => m.GameTick)
                .Take(maxCount)
                .ToList();

            // ⭐ v5.0: 使用 MemoryFormatter 格式化
            return MemoryFormatter.Format(sortedMemories);
        }

        /// <summary>
        /// 获取 Pawn 的 ELS 层匹配记忆（经过上下文匹配）
        /// 由 RimTalk Mustache Parser 在解析 {{pawnN.matchELS}} 时调用
        /// 使用 DynamicMemoryInjection 的匹配逻辑，只返回 ELS 层
        /// </summary>
        public static string GetPawnMatchELS(Pawn pawn)
        {
            if (pawn == null) return "";

            try
            {
                var comp = pawn.TryGetComp<FourLayerMemoryComp>();
                if (comp == null || comp.EventLogMemories == null || comp.EventLogMemories.Count == 0)
                {
                    return "(Спогадів ELS немає)";
                }

                var settings = RimTalkMemoryPatchMod.Settings;
                string dialogueContext = GetCurrentDialogueContext();

                // 使用匹配逻辑获取 ELS 记忆
                string result = GetMatchedMemoriesForLayer(
                    pawn,
                    comp,
                    comp.EventLogMemories,
                    MemoryLayer.EventLog,
                    dialogueContext,
                    settings?.maxInjectedMemories ?? 10
                );

                if (string.IsNullOrEmpty(result))
                {
                    return "(Відповідних спогадів ELS немає)";
                }

                // ⭐ v5.1: 应用提示词规范化规则
                return PromptNormalizer.Normalize(result);
            }
            catch (Exception ex)
            {
                RimAiLog.Warning(RimAiLogCategory.Memory, $"[MemoryPatch] Error getting matchELS for {pawn?.LabelShort}.", exception: ex);
                return "";
            }
        }

        /// <summary>
        /// 获取 Pawn 的 CLPA 层匹配记忆（经过上下文匹配）
        /// 由 RimTalk Mustache Parser 在解析 {{pawnN.matchCLPA}} 时调用
        /// 使用 DynamicMemoryInjection 的匹配逻辑，只返回 CLPA 层
        /// </summary>
        public static string GetPawnMatchCLPA(Pawn pawn)
        {
            if (pawn == null) return "";

            try
            {
                var comp = pawn.TryGetComp<FourLayerMemoryComp>();
                if (comp == null || comp.ArchiveMemories == null || comp.ArchiveMemories.Count == 0)
                {
                    return "(Спогадів CLPA немає)";
                }

                var settings = RimTalkMemoryPatchMod.Settings;
                string dialogueContext = GetCurrentDialogueContext();

                // 使用匹配逻辑获取 CLPA 记忆
                string result = GetMatchedMemoriesForLayer(
                    pawn,
                    comp,
                    comp.ArchiveMemories,
                    MemoryLayer.Archive,
                    dialogueContext,
                    settings?.maxInjectedMemories ?? 10
                );

                if (string.IsNullOrEmpty(result))
                {
                    return "(Відповідних спогадів CLPA немає)";
                }

                // ⭐ v5.1: 应用提示词规范化规则
                return PromptNormalizer.Normalize(result);
            }
            catch (Exception ex)
            {
                RimAiLog.Warning(RimAiLogCategory.Memory, $"[MemoryPatch] Error getting matchCLPA for {pawn?.LabelShort}.", exception: ex);
                return "";
            }
        }

        /// <summary>
        /// 获取特定层级的匹配记忆
        /// 复用 DynamicMemoryInjection 的评分逻辑，但只针对指定层级
        /// ⭐ v5.0: 使用 MemoryFormatter 格式化
        /// </summary>
        private static string GetMatchedMemoriesForLayer(
            Pawn pawn,
            FourLayerMemoryComp comp,
            List<MemoryEntry> memories,
            MemoryLayer layer,
            string context,
            int maxCount)
        {
            if (memories == null || memories.Count == 0)
            {
                return null;
            }

            // 使用 DynamicMemoryInjection 的匹配逻辑
            DynamicMemoryInjection.InjectMemoriesWithDetails(
                comp,
                context,
                maxCount,
                out var scores,
                layer
            );

            // 从结果中过滤只保留指定层级的记忆
            if (scores == null || scores.Count == 0)
            {
                return null;
            }

            var layerMemories = scores // 剔除了多余的层级过滤逻辑，直接在 InjectMemoriesWithDetails 内部处理
                .OrderByDescending(s => s.TotalScore)
                .Select(s => s.Memory)
                .ToList();

            if (layerMemories.Count == 0)
            {
                return null;
            }

            // ⭐ v5.0: 使用 MemoryFormatter 格式化
            return MemoryFormatter.Format(layerMemories);
        }
    }
}

