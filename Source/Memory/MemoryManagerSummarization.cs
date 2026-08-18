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
    internal sealed class MemoryManagerSummarization : MemoryManagerCollaborator
    {
        internal MemoryManagerSummarization(MemoryManager owner) : base(owner) { }

internal void SummarizeAllMemories()
        {
            if (Current.Game == null) return;

            // ⭐ 收集所有需要总结的殖民者，加入队列
            int queuedCount = 0;
            
            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                {
                    // ⭐ v3.5.2: 扩展到殖民者 + 配置了链接催化剂的殖民地动物/机械体
                    if (pawn.IsColonist || MemoryManager.IsColonyAnimalWithVocalLink(pawn))
                    {
                        // 检查是否有需要总结的记忆
                        var fourLayerComp = pawn.TryGetComp<FourLayerMemoryComp>();
                        if (fourLayerComp != null && fourLayerComp.SituationalMemories.Count > 0)
                        {
                            summarizationQueue.Enqueue(pawn);
                            queuedCount++;
                        }
                        else
                        {
                            var memoryComp = pawn.TryGetComp<PawnMemoryComp>();
                            if (memoryComp != null && memoryComp.SituationalMemories.Count > 0)
                            {
                                summarizationQueue.Enqueue(pawn);
                                queuedCount++;
                            }
                        }
                    }
                }
            }

            if (queuedCount > 0)
            {
                Log.Message($"[RimAI.Memory] 📋 Queued {queuedCount} colonists for summarization (15s delay between each)");
                // 立即处理第一个
                nextSummarizationTick = Find.TickManager.TicksGame;
            }
            else
            {
                Log.Message($"[RimAI.Memory] ✅ No colonists need summarization");
            }
        }

internal void ProcessSummarizationQueue()
        {
            if (summarizationQueue.Count == 0)
                return;

            int currentTick = Find.TickManager.TicksGame;
            
            // 检查是否到达下一个总结时间
            if (currentTick < nextSummarizationTick)
                return;

            // 从队列中取出一个殖民者
            Pawn pawn = summarizationQueue.Dequeue();
            
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                // 殖民者已死亡或销毁，跳过
                if (summarizationQueue.Count > 0)
                {
                    nextSummarizationTick = currentTick; // 立即处理下一个
                }
                return;
            }

            // 执行总结
            bool summarized = false;
            var fourLayerComp = pawn.TryGetComp<FourLayerMemoryComp>();
            if (fourLayerComp != null)
            {
                fourLayerComp.DailySummarization();
                summarized = true;
            }
            else
            {
                var memoryComp = pawn.TryGetComp<PawnMemoryComp>();
                if (memoryComp != null)
                {
                    memoryComp.DailySummarization();
                    summarized = true;
                }
            }

            if (summarized)
            {
                // ⭐ v3.3.2: 降低日志输出 - 仅DevMode且10%概率
                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    Log.Message($"[RimAI.Memory] Summarized memories for {pawn.LabelShort} ({summarizationQueue.Count} remaining)");
                }
            }

            // 如果还有更多殖民者，设置下一个总结时间（15秒后）
            if (summarizationQueue.Count > 0)
            {
                nextSummarizationTick = currentTick + SUMMARIZATION_DELAY_TICKS;
                
                // ⭐ v3.3.2: 移除下一次总结时间的日志
                // Log.Message($"[RimAI.Memory] Next colonist will be summarized in 15 seconds...");
            }
            else
            {
                // ⭐ v3.3.2: 降低日志输出
                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    Log.Message($"[RimAI.Memory] All colonists summarized!");
                }
            }
        }

internal void ProcessManualSummarizationQueue()
        {
            if (manualSummarizationQueue.Count == 0)
                return;

            int currentTick = Find.TickManager.TicksGame;
            
            // 检查是否到达下一个总结时间
            if (currentTick < nextManualSummarizationTick)
                return;

            // 从队列中取出一个殖民者
            Pawn pawn = manualSummarizationQueue.Dequeue();
            
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                // 殖民者已死亡或销毁，跳过
                if (manualSummarizationQueue.Count > 0)
                {
                    nextManualSummarizationTick = currentTick; // 立即处理下一个
                }
                return;
            }

            // 执行手动总结
            bool summarized = false;
            int scmCount = 0;
            var fourLayerComp = pawn.TryGetComp<FourLayerMemoryComp>();
            if (fourLayerComp != null)
            {
                scmCount = fourLayerComp.SituationalMemories.Count;
                if (scmCount > 0)
                {
                    fourLayerComp.ManualSummarization();
                    summarized = true;
                }
            }

            if (summarized)
            {
                // ⭐ v3.3.2: 降低日志输出
                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    Log.Message($"[RimAI.Memory] Manual summarized for {pawn.LabelShort} ({scmCount} SCM -> ELS, {manualSummarizationQueue.Count} remaining)");
                }
                
                // ⭐ 给用户反馈消息（保留）
                Messages.Message(
                    $"{pawn.LabelShort}: підсумовано {scmCount} короткочасних спогадів",
                    MessageTypeDefOf.TaskCompletion,
                    false
                );
            }

            // 如果还有更多殖民者，设置下一个总结时间（1秒后）
            if (manualSummarizationQueue.Count > 0)
            {
                nextManualSummarizationTick = currentTick + MANUAL_SUMMARIZATION_DELAY_TICKS;
            }
            else
            {
                // ⭐ v3.3.2: 降低日志输出
                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    Log.Message($"[RimAI.Memory] All manual summarizations complete!");
                }
                // ⭐ 所有总结完成后的消息（保留）
                Messages.Message("Ручне підсумування завершено для всіх колоністів", MessageTypeDefOf.PositiveEvent, false);
            }
        }

public void QueueManualSummarization(List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0) return;

            int queuedCount = 0;
            foreach (var pawn in pawns)
            {
                if (pawn != null && !pawn.Dead && !pawn.Destroyed)
                {
                    var fourLayerComp = pawn.TryGetComp<FourLayerMemoryComp>();
                    if (fourLayerComp != null && fourLayerComp.SituationalMemories.Count > 0)
                    {
                        manualSummarizationQueue.Enqueue(pawn);
                        queuedCount++;
                    }
                }
            }

            if (queuedCount > 0)
            {
                Log.Message($"[RimAI.Memory] 📋 Queued {queuedCount} colonists for manual summarization (1s delay between each)");
                // 立即处理第一个
                nextManualSummarizationTick = Find.TickManager.TicksGame;
            }
            else
            {
                Messages.Message("Немає колоністів, спогади яких потребують ручного підсумування", MessageTypeDefOf.RejectInput, false);
            }
        }

internal void DecayAllMemories()
        {
            if (Current.Game == null) return;

            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                {
                    // ⭐ v3.5.2: 扩展到殖民者 + 配置了链接催化剂的殖民地动物/机械体
                    if (pawn.IsColonist || MemoryManager.IsColonyAnimalWithVocalLink(pawn))
                    {
                        // 尝试新的四层记忆组件
                        var fourLayerComp = pawn.TryGetComp<FourLayerMemoryComp>();
                        if (fourLayerComp != null)
                        {
                            fourLayerComp.DecayActivity();
                        }
                        else
                        {
                            // 兼容旧的记忆组件
                            var memoryComp = pawn.TryGetComp<PawnMemoryComp>();
                            if (memoryComp != null)
                            {
                                memoryComp.DecayActivity();
                            }
                        }
                    }
                }
            }
        }
    }
}
