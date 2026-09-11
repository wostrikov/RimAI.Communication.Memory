using System;
using System.IO;
using Ustas.RimAI.Communication.Memory.Policy;

internal static class MemorySimilarityPolicyTests
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

        const string roster = "[Учасники: Леся, Борсук]\n";
        string line = "Леся: Борсук, цей сталевий актив має негайно опинитися в Зона складу 1.";

        T(MemorySimilarityPolicy.IsNearDuplicate(line, line), "identical-is-a-twin");
        T(MemorySimilarityPolicy.IsNearDuplicate(roster + line, line), "roster-line-ignored");
        T(MemorySimilarityPolicy.IsNearDuplicate(line, line + " Швидко."), "copy-with-a-word-more-is-a-twin");
        T(!MemorySimilarityPolicy.IsNearDuplicate(roster + "Борсук: Сьогодні ввечері полюватимемо на оленів біля річки.",
            roster + "Леся: Треба полагодити дах над кухнею до дощу."), "same-pawns-different-talk-is-not");
        T(!MemorySimilarityPolicy.IsNearDuplicate("", line), "empty-never-matches");
        T(!MemorySimilarityPolicy.IsNearDuplicate(roster, roster), "roster-only-never-matches");
        T(MemorySimilarityPolicy.Comparable(roster + line) == line, "comparable-drops-roster");

        string mutation = Read("FourLayerMutation.cs.src");
        string collector = Read("ABMCollector.cs.src");
        T(mutation.Contains("MemorySimilarityPolicy.IsNearDuplicate"), "stored-twins-merge");
        T(collector.Contains("MemorySimilarityPolicy.IsNearDuplicate"), "injected-twins-skipped");
        return n;
    }

    static string Read(string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, name);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
