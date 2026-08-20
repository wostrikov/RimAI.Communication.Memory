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
            const float ACTIVITY_THRESHOLD = 0.01f;

            int removedSCM = 0;
            int removedELS = 0;
            int removedCLPA = 0;

            int beforeSCM = situationalMemories.Count;
            situationalMemories.RemoveAll(m =>
                m.Activity < ACTIVITY_THRESHOLD &&
                !m.IsPinned
            );
            removedSCM = beforeSCM - situationalMemories.Count;

            int beforeELS = eventLogMemories.Count;
            eventLogMemories.RemoveAll(m =>
                m.Activity < ACTIVITY_THRESHOLD &&
                !m.IsPinned
            );
            removedELS = beforeELS - eventLogMemories.Count;

            int beforeCLPA = archiveMemories.Count;
            archiveMemories.RemoveAll(m =>
                m.Activity < ACTIVITY_THRESHOLD &&
                !m.IsPinned
            );
            removedCLPA = beforeCLPA - archiveMemories.Count;

            if (Prefs.DevMode && (removedSCM > 0 || removedELS > 0 || removedCLPA > 0))
            {
                var pawn = parent as Pawn;
                Log.Message($"[Memory] {pawn?.LabelShort ?? "Unknown"} cleaned up " +
                           $"{removedSCM} SCM + {removedELS} ELS + {removedCLPA} CLPA memories (activity < {ACTIVITY_THRESHOLD})");
            }
        }

internal void EnforceMemoryLimits()
        {
            int removedSCM = 0;
            int removedELS = 0;

            int scmNonPinnedCount = situationalMemories.Count(m => !m.IsPinned);
            int elsNonPinnedCount = eventLogMemories.Count(m => !m.IsPinned);

            if (scmNonPinnedCount > MaxSCM)
            {
                int toRemoveCount = scmNonPinnedCount - MaxSCM;
                var toRemove = situationalMemories
                    .Where(m => !m.IsPinned)
                    .OrderBy(m => m.Activity)
                    .ThenBy(m => m.GameTick)
                    .Take(toRemoveCount)
                    .ToList();

                foreach (var memory in toRemove)
                {
                    situationalMemories.Remove(memory);
                    removedSCM++;
                }
            }

            if (elsNonPinnedCount > MaxELS)
            {
                int toRemoveCount = elsNonPinnedCount - MaxELS;
                var toRemove = eventLogMemories
                    .Where(m => !m.IsPinned)
                    .OrderBy(m => m.Activity)
                    .ThenBy(m => m.GameTick)
                    .Take(toRemoveCount)
                    .ToList();

                foreach (var memory in toRemove)
                {
                    eventLogMemories.Remove(memory);
                    removedELS++;
                }
            }

            if (Prefs.DevMode && (removedSCM > 0 || removedELS > 0))
            {
                var pawn = parent as Pawn;
                int scmPinnedCount = situationalMemories.Count(m => m.IsPinned);
                int elsPinnedCount = eventLogMemories.Count(m => m.IsPinned);

                Log.Message($"[Memory] {pawn?.LabelShort ?? "Unknown"} enforced limits: " +
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
