using Ustas.RimAI.Communication.Memory.Capture;
using Ustas.RimAI.Communication.Memory.Policy;
using Ustas.RimAI.Communication.Memory.UI;
using Ustas.RimAI.Communication.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Ustas.RimAI.Communication.Memory.Diagnostics;

namespace Ustas.RimAI.Communication.Memory
{
    internal sealed class FourLayerDecay : FourLayerMemoryCompCollaborator
    {
        internal FourLayerDecay(FourLayerMemoryComp owner) : base(owner) { }

public void DecayActivity()
        {
            float scmRate = RimTalkMemoryPatchMod.Settings.scmDecayRate;
            float elsRate = RimTalkMemoryPatchMod.Settings.elsDecayRate;
            float clpaRate = RimTalkMemoryPatchMod.Settings.clpaDecayRate;

            foreach (var memory in situationalMemories)
                memory.Decay(scmRate);

            foreach (var memory in eventLogMemories)
                memory.Decay(elsRate);

            foreach (var memory in archiveMemories)
                memory.Decay(clpaRate);

            CleanupLowActivityMemories();

            EnforceMemoryLimits();
        }

internal void CleanupLowActivityMemories()
        {
            const float ACTIVITY_THRESHOLD = MemoryLayerMaintenance.ActivityCleanupThreshold;

            int removedSCM = MemoryLayerMaintenance.RemoveLowActivity(
                situationalMemories, m => m.Activity, m => m.IsPinned, ACTIVITY_THRESHOLD);
            int removedELS = MemoryLayerMaintenance.RemoveLowActivity(
                eventLogMemories, m => m.Activity, m => m.IsPinned, ACTIVITY_THRESHOLD);
            int removedCLPA = MemoryLayerMaintenance.RemoveLowActivity(
                archiveMemories, m => m.Activity, m => m.IsPinned, ACTIVITY_THRESHOLD);

            if (Prefs.DevMode && (removedSCM > 0 || removedELS > 0 || removedCLPA > 0))
            {
                var pawn = parent as Pawn;
                ModuleLog.Message($"[Memory] {pawn?.LabelShort ?? "Unknown"} cleaned up " +
                           $"{removedSCM} SCM + {removedELS} ELS + {removedCLPA} CLPA memories (activity < {ACTIVITY_THRESHOLD})");
            }
        }

internal void EnforceMemoryLimits()
        {
            int removedSCM = 0;
            int removedELS = 0;

            int scmNonPinnedCount = situationalMemories.Count(m => !m.IsPinned);
            int elsNonPinnedCount = eventLogMemories.Count(m => !m.IsPinned);

            removedSCM = MemoryLayerMaintenance.EnforceLimit(
                situationalMemories, MaxSCM, m => m.Activity, m => m.GameTick, m => m.IsPinned);
            removedELS = MemoryLayerMaintenance.EnforceLimit(
                eventLogMemories, MaxELS, m => m.Activity, m => m.GameTick, m => m.IsPinned);

            if (Prefs.DevMode && (removedSCM > 0 || removedELS > 0))
            {
                var pawn = parent as Pawn;
                int scmPinnedCount = situationalMemories.Count(m => m.IsPinned);
                int elsPinnedCount = eventLogMemories.Count(m => m.IsPinned);

                ModuleLog.Message($"[Memory] {pawn?.LabelShort ?? "Unknown"} enforced limits: " +
                           $"removed {removedSCM} SCM (non-pinned: {scmNonPinnedCount - removedSCM}, pinned: {scmPinnedCount}, max: {MaxSCM}) + " +
                           $"{removedELS} ELS (non-pinned: {elsNonPinnedCount - removedELS}, pinned: {elsPinnedCount}, max: {MaxELS})");
            }
        }

internal void PromoteToSituational(MemoryEntry memory)
        {
            memory.Layer = MemoryLayer.Situational;
            situationalMemories.Insert(0, memory);
            bool flag = situationalMemories.Count > MaxSCM * 1.5f;
            if (flag)
            {
                Log.Warning(string.Format("[Memory] {0} SCM overflow ({1}), needs summarization", parent.LabelShort, situationalMemories.Count));
            }
        }
    }
}
