using System;
using System.IO;
using Ustas.RimAI.Communication.Memory.Policy;

// What can be checked without the game: the subject line of a summary prompt,
// and that the job capture keeps the wiring that makes it safe.
internal static class JobCaptureGuardTests
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

        T(SummarySubject.Compose("Леся", "жінка", 34) == "Леся (жінка, 34)", "name-gender-age");
        T(SummarySubject.Compose("Борсук", null, 7) == "Борсук (7)", "no-gender");
        T(SummarySubject.Compose("Борсук", " ", null) == "Борсук", "nothing-known");
        T(SummarySubject.Compose("Леся", "жінка", null) == "Леся (жінка)", "no-age");
        T(SummarySubject.Compose("Немовля", "чоловік", 0) == "Немовля (чоловік, 0)", "newborn-age-zero-kept");

        string capturer = Read("JobMemoryCapturer.cs.src");
        string patch = Read("JobMemoryPatch.cs.src");
        string summarizer = Read("IndependentAISummarizer.cs.src");

        T(patch.Contains("Prefix(JobCondition condition, Job ___curJob, Pawn ___pawn)"), "prefix-takes-condition");
        T(capturer.Contains("condition != JobCondition.Succeeded && !_jobsKeptWhenInterrupted.Contains(jobDef)"), "only-done-jobs-or-allow-list");
        foreach (string def in new[] { "AttackMelee", "AttackStatic", "CastAbilityOnThing", "CastAbilityOnWorldTile", "Lovin" })
            T(capturer.Contains("JobDefOf." + def), "allow-list-has-" + def);
        T(Count(capturer, "ReadFromGame(job, ") >= 2 && capturer.Contains("RimAI.catch-boundary: ALLOWED_TOP_LEVEL_BOUNDARY"), "report-and-label-guarded");
        T(!capturer.Contains("等") && !capturer.Contains("、") && !capturer.Contains("。"), "no-chinese-punctuation-in-memories");

        T(summarizer.Contains("\\\"model\\\":"), "chat-request-has-model-field");
        T(summarizer.Contains("SummarySubject.Compose("), "prompt-names-gender-and-age");
        return n;
    }

    static int Count(string text, string needle)
    {
        int count = 0;
        for (int i = text.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = text.IndexOf(needle, i + 1, StringComparison.Ordinal))
            count++;
        return count;
    }

    static string Read(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, name);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
