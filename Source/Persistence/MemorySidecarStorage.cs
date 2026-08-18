using System.IO;
using Ustas.RimAI.Core.Storage;
using Verse;

namespace Ustas.RimAI.Communication.Memory.Persistence;

/// <summary>
/// Memory sidecar paths over <see cref="ILocalStorage"/>. Scribe save-game data stays elsewhere.
/// Filenames and directories are frozen for 7.5.9 compatibility.
/// </summary>
internal static class MemorySidecarStorage
{
    internal const string MemoryExportsFolderName = "MemoryExports";
    internal const string PerformanceReportPrefix = "RimTalk_Performance_";

    static ILocalStorage Storage => LocalStorage.Current;

    internal static string MemoryExportsDirectory =>
        Path.Combine(GenFilePaths.SaveDataFolderPath, MemoryExportsFolderName);

    internal static bool DirectoryExists(string path) => Storage.DirectoryExists(path);

    internal static void CreateDirectory(string path) => Storage.CreateDirectory(path);

    internal static string[] GetFiles(string path, string searchPattern) =>
        Storage.GetFiles(path, searchPattern);

    internal static System.DateTime GetLastWriteTime(string path) =>
        Storage.GetLastWriteTime(path);

    internal static void WriteAllText(string path, string contents) =>
        Storage.WriteAllText(path, contents);

    internal static string BuildPerformanceReportPath(string timestamp) =>
        Path.Combine(GenFilePaths.SaveDataFolderPath, PerformanceReportPrefix + timestamp + ".txt");
}
