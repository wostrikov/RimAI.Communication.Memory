using System;
using RimWorld;
using Ustas.RimAI.Communication.Memory.Policy;
using Ustas.RimAI.Core.Diagnostics;
using Verse;

namespace Ustas.RimAI.Communication.Memory.Capture
{
    /// <summary>
    /// Fights a drafted pawn has while holding position. A pawn told to stay put
    /// sits in Wait_Combat and JobDriver_Wait fires its weapon directly, without
    /// an attack job, so the job capturer never sees those shots. The game
    /// announces every shot that actually left the barrel through
    /// Pawn.Notify_UsedVerb, after TryCastShot succeeded, so aiming that was cut
    /// short is not counted.
    ///
    /// An ordered attack runs as AttackStatic or AttackMelee, which the job
    /// capturer already remembers; the Wait_Combat test keeps the two apart.
    /// </summary>
    public class CombatMemoryCapturer
    {
        // Matches the job capturer's importance for attack jobs.
        private const float CombatImportance = 0.9f;

        private readonly FourLayerMemoryComp _memoryComp;

        // Not saved: the first shot after a load starts a new fight.
        private readonly CombatSession _session = new CombatSession();
        private MemoryEntry _lastCombatMemory;

        public CombatMemoryCapturer(FourLayerMemoryComp memoryComp)
        {
            _memoryComp = memoryComp;
        }

        /// <summary>
        /// Called for every verb any pawn uses, raiders and animals included, so
        /// the cheapest tests come first: any pawn not holding position is turned
        /// away by a settings read and a look at its current job.
        /// </summary>
        public static void CaptureAttackEnter(Pawn pawn, Verb verb)
        {
            if (RimTalkMemoryPatchMod.Settings?.enableCombatMemory != true
                || pawn is null
                || verb is null
                || pawn.CurJobDef != JobDefOf.Wait_Combat
                || !pawn.IsColonist
                || !IsViolentAttack(verb))
                return;

            try
            {
                pawn.GetComp<FourLayerMemoryComp>()?.CombatCapturer?.CaptureAttack(verb);
            }
            // RimAI.catch-boundary: ALLOWED_TOP_LEVEL_BOUNDARY — body of the Pawn.Notify_UsedVerb postfix; a target label that throws must not break the shot that was just fired.
            catch (Exception ex)
            {
                if (RimAiLog.Detailed)
                    RimAiLog.Warning(RimAiLogCategory.Memory,
                        "[RimAI.Memory] Skipped a combat memory for " + pawn.ThingID + ".", exception: ex);
            }
        }

        // Firefighting and setting fires use verbs too; only blows and shots
        // meant to hurt count.
        private static bool IsViolentAttack(Verb verb)
        {
            var props = verb.verbProps;
            return props is { violent: true } && (verb.IsMeleeAttack || props.Ranged);
        }

        private void CaptureAttack(Verb verb)
        {
            int tick = GenTicks.TicksGame;
            bool melee = verb.IsMeleeAttack;
            LocalTargetInfo target = verb.CurrentTarget;
            int targetId = target.Thing?.thingIDNumber ?? CombatSession.GroundTargetId;

            if (_lastCombatMemory is not null && _session.Continues(tick, melee))
            {
                _session.Touch(tick);
                if (!_session.IsNewTarget(targetId))
                    return;

                _session.AddTarget(targetId, TargetName(target));
                _lastCombatMemory.GameTick = tick;
                _lastCombatMemory.Content = Describe();
                return;
            }

            _session.Start(tick, melee);
            _session.AddTarget(targetId, TargetName(target));
            _lastCombatMemory = new MemoryEntry(Describe(), MemoryType.Action, MemoryLayer.Active, CombatImportance);
            _memoryComp.ActiveMemories.Add(_lastCombatMemory);
        }

        private static string TargetName(LocalTargetInfo target)
        {
            if (target.Thing is not { } thing)
                return "RimTalk_Memory_CombatArea".Translate().Resolve();
            return thing.LabelShort ?? thing.def?.label ?? "RimTalk_Memory_Unknown".Translate().Resolve();
        }

        private string Describe()
        {
            string action = (_session.Melee ? "RimTalk_Memory_CombatMelee" : "RimTalk_Memory_CombatRanged").Translate();
            return _session.Describe(action, "RimTalk_Memory_CombatTargets".Translate(), "RimTalk_Memory_CombatMore".Translate());
        }
    }
}
