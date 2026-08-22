using System;

internal static class Program
{
    public static int Main()
    {
        int n = 0;
        n += MemoryLayerMaintenanceTests.Run();
        n += MemoryConsolidationTests.Run();
        n += MemoryScopeRoundtripTests.Run();
        n += KnowledgeMatchPolicyTests.Run();
        n += MemoryHostWiringGuardTests.Run();
        Console.WriteLine("MEMORY_FOCUSED_TESTS_OK passed=" + n);
        Console.WriteLine("TESTS total=" + n + " failed=0");
        return 0;
    }
}
