using System;
using System.IO;
using Ustas.RimAI.Communication.Memory.Policy;

internal static class CombatSessionTests
{
    public static int Run()
    {
        int n = 0;
        void T(bool x, string s)
        {
            if (!x)
                throw new Exception("FAILED " + s);
            n++;
        }

        const string targets = "цілей: {0}";
        const string more = "та інші";
        const string shot = "Стрілянина з позиції";

        var session = new CombatSession();
        T(!session.Continues(0, false), "closed-session-never-continues");

        session.Start(1000, melee: false);
        T(session.IsNewTarget(7), "first-target-is-new");
        session.AddTarget(7, "рейдер");
        T(session.Describe(shot, targets, more) == "Стрілянина з позиції: рейдер", "single-target-has-no-count");

        // Shooting the same raider again changes nothing but the clock.
        T(session.Continues(1100, false), "next-shot-continues");
        session.Touch(1100);
        T(!session.IsNewTarget(7), "same-thing-is-not-new");
        T(session.DistinctTargets == 1, "repeat-shots-are-one-target");

        // Two raiders with one name are two targets.
        session.AddTarget(8, "рейдер");
        session.AddTarget(9, "Іван");
        T(session.DistinctTargets == 3, "distinct-by-thing-id");
        T(session.Describe(shot, targets, more) == "Стрілянина з позиції (цілей: 3): рейдер ×2, Іван", "multi-target-groups-by-name");

        session.AddTarget(10, "варг");
        session.AddTarget(CombatSession.GroundTargetId, "ділянка");
        T(session.Describe(shot, targets, more) == "Стрілянина з позиції (цілей: 5): рейдер ×2, Іван, варг та інші",
            "three-names-then-the-rest-trail-off");

        // Half an hour of quiet ends the fight.
        int last = session.LastTick;
        T(session.Continues(last + CombatSession.GapTicks, false), "gap-at-limit-continues");
        T(!session.Continues(last + CombatSession.GapTicks + 1, false), "gap-past-limit-ends");

        // Switching between shooting and melee ends it too.
        T(!session.Continues(last + 1, true), "melee-after-shooting-is-new");

        // Two hours from the first shot is the most one memory covers,
        // however steady the fire.
        var steady = new CombatSession();
        steady.Start(0, false);
        int tick = 0;
        while (tick + 1000 <= CombatSession.MaxDurationTicks)
        {
            tick += 1000;
            T(steady.Continues(tick, false), "steady-fire-continues-" + tick);
            steady.Touch(tick);
        }
        T(!steady.Continues(CombatSession.MaxDurationTicks + 1, false), "two-hour-cap");

        steady.Start(9000, true);
        T(steady.DistinctTargets == 0 && steady.Melee && steady.StartTick == 9000, "start-resets");

        T(CombatSession.GapTicks == 1250 && CombatSession.MaxDurationTicks == 5000, "thirty-minutes-and-two-hours");

        string capturer = Read("CombatMemoryCapturer.cs.src");
        string patch = Read("CombatMemoryPatch.cs.src");
        T(capturer.Contains("pawn.CurJobDef != JobDefOf.Wait_Combat"), "only-holding-position");
        T(capturer.Contains("enableCombatMemory"), "setting-gates-capture");
        T(capturer.Contains("violent: true"), "only-violent-verbs");
        T(patch.Contains("new[] { typeof(Pawn), typeof(Verb) }"), "patch-names-argument-types");
        return n;
    }

    static string Read(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, name);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
