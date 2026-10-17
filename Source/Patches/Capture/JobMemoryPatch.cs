using HarmonyLib;
using Ustas.RimAI.Communication.Memory.Capture;
using Verse;
using Verse.AI;

namespace Ustas.RimAI.Communication.Memory.Patches.Capture
{

    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class Pawn_JobTracker_StartJob_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Job ___curJob, Pawn ___pawn)
        {
            JobMemoryCapturer.ExtractJobInfoEnter(___curJob, ___pawn);
        }
    }

    // CleanupCurrentJob(JobCondition condition, bool releaseReservations,
    // bool cancelBusyStancesSoft, bool canReturnToPool, bool? carryThingAfterJobOverride)
    // has no overload; the condition says whether the job was done or abandoned.
    [HarmonyPatch(typeof(Pawn_JobTracker), "CleanupCurrentJob")]
    public static class Pawn_JobTracker_CleanupCurrentJob_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(JobCondition condition, Job ___curJob, Pawn ___pawn)
        {
            JobMemoryCapturer.BuildJobMemoryEnter(condition, ___curJob, ___pawn);
        }
    }

}