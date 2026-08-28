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
    internal sealed class MemoryManagerSummarization : MemoryManagerCollaborator
    {
        internal MemoryManagerSummarization(MemoryManager owner) : base(owner) { }

internal void SummarizeAllMemories()
        {
            if (Current.Game == null) return;

            int queuedCount = 0;
            
            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn.IsColonist || MemoryManager.IsColonyAnimalWithVocalLink(pawn))
                    {
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
                ModuleLog.Message($"[RimAI.Memory] 📋 Queued {queuedCount} colonists for summarization (15s delay between each)");
                nextSummarizationTick = Find.TickManager.TicksGame;
            }
            else
            {
                ModuleLog.Message($"[RimAI.Memory] ✅ No colonists need summarization");
            }
        }

internal void ProcessSummarizationQueue()
        {
            if (summarizationQueue.Count == 0)
                return;

            int currentTick = Find.TickManager.TicksGame;
            
            if (currentTick < nextSummarizationTick)
                return;

            Pawn pawn = summarizationQueue.Dequeue();
            
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                if (summarizationQueue.Count > 0)
                {
                    nextSummarizationTick = currentTick;
                }
                return;
            }

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
                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    ModuleLog.Message($"[RimAI.Memory] Summarized memories for {pawn.LabelShort} ({summarizationQueue.Count} remaining)");
                }
            }

            if (summarizationQueue.Count > 0)
            {
                nextSummarizationTick = currentTick + SUMMARIZATION_DELAY_TICKS;
                
            }
            else
            {
                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    ModuleLog.Message($"[RimAI.Memory] All colonists summarized!");
                }
            }
        }

internal void ProcessManualSummarizationQueue()
        {
            if (manualSummarizationQueue.Count == 0)
                return;

            int currentTick = Find.TickManager.TicksGame;
            
            if (currentTick < nextManualSummarizationTick)
                return;

            Pawn pawn = manualSummarizationQueue.Dequeue();
            
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                if (manualSummarizationQueue.Count > 0)
                {
                    nextManualSummarizationTick = currentTick;
                }
                return;
            }

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
                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    ModuleLog.Message($"[RimAI.Memory] Manual summarized for {pawn.LabelShort} ({scmCount} SCM -> ELS, {manualSummarizationQueue.Count} remaining)");
                }
                
                Messages.Message(
                    $"{pawn.LabelShort}: підсумовано {scmCount} короткочасних спогадів",
                    MessageTypeDefOf.TaskCompletion,
                    false
                );
            }

            if (manualSummarizationQueue.Count > 0)
            {
                nextManualSummarizationTick = currentTick + MANUAL_SUMMARIZATION_DELAY_TICKS;
            }
            else
            {
                if (Prefs.DevMode && UnityEngine.Random.value < 0.1f)
                {
                    ModuleLog.Message($"[RimAI.Memory] All manual summarizations complete!");
                }
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
                ModuleLog.Message($"[RimAI.Memory] 📋 Queued {queuedCount} colonists for manual summarization (1s delay between each)");
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
                    if (pawn.IsColonist || MemoryManager.IsColonyAnimalWithVocalLink(pawn))
                    {
                        var fourLayerComp = pawn.TryGetComp<FourLayerMemoryComp>();
                        if (fourLayerComp != null)
                        {
                            fourLayerComp.DecayActivity();
                        }
                        else
                        {
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
