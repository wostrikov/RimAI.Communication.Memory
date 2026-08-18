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

            // 步骤1：衰减所有记忆的activity
            foreach (var memory in situationalMemories)
                memory.Decay(scmRate);

            foreach (var memory in eventLogMemories)
                memory.Decay(elsRate);

            foreach (var memory in archiveMemories)
                memory.Decay(clpaRate);

            // ⭐ 步骤2：清理极低activity的"死亡"记忆（方案1）
            CleanupLowActivityMemories();

            // ⭐ 步骤3：强制执行容量限制（方案3）
            EnforceMemoryLimits();
        }

internal void CleanupLowActivityMemories()
        {
            const float ACTIVITY_THRESHOLD = 0.01f; // activity < 0.01视为"死亡"

            int removedSCM = 0;
            int removedELS = 0;
            int removedCLPA = 0;

            // 清理SCM中的低activity记忆（移除 isUserEdited 检查）
            int beforeSCM = situationalMemories.Count;
            situationalMemories.RemoveAll(m =>
                m.Activity < ACTIVITY_THRESHOLD &&
                !m.IsPinned
            );
            removedSCM = beforeSCM - situationalMemories.Count;

            // 清理ELS中的低activity记忆（移除 isUserEdited 检查）
            int beforeELS = eventLogMemories.Count;
            eventLogMemories.RemoveAll(m =>
                m.Activity < ACTIVITY_THRESHOLD &&
                !m.IsPinned
            );
            removedELS = beforeELS - eventLogMemories.Count;

            // ⭐ 清理CLPA中的低activity记忆（移除 isUserEdited 检查）
            int beforeCLPA = archiveMemories.Count;
            archiveMemories.RemoveAll(m =>
                m.Activity < ACTIVITY_THRESHOLD &&
                !m.IsPinned
            );
            removedCLPA = beforeCLPA - archiveMemories.Count;

            // 开发模式日志
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

            // ⭐ 修复：只计算非固定的记忆数量（移除 isUserEdited 检查）
            int scmNonPinnedCount = situationalMemories.Count(m => !m.IsPinned);
            int elsNonPinnedCount = eventLogMemories.Count(m => !m.IsPinned);

            // ⭐ 处理SCM容量限制（移除 isUserEdited 检查）
            if (scmNonPinnedCount > MaxSCM)
            {
                int toRemoveCount = scmNonPinnedCount - MaxSCM;
                // 按activity升序排序，删除最低的
                var toRemove = situationalMemories
                    .Where(m => !m.IsPinned)
                    .OrderBy(m => m.Activity)
                    .ThenBy(m => m.GameTick) // 相同activity时，删除更旧的
                    .Take(toRemoveCount)
                    .ToList();

                foreach (var memory in toRemove)
                {
                    situationalMemories.Remove(memory);
                    removedSCM++;
                }
            }

            // ⭐ 处理ELS容量限制（移除 isUserEdited 检查）
            if (elsNonPinnedCount > MaxELS)
            {
                int toRemoveCount = elsNonPinnedCount - MaxELS;
                // 按activity升序排序，删除最低的
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

            // 开发模式日志
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
