using HarmonyLib;
using Ustas.RimAI.Communication.Memory.Integration;
using Ustas.RimAI.Core.Composition;
using Ustas.RimAI.Core.Diagnostics;
using Ustas.RimAI.Core.Handshake;
using Ustas.RimAI.Core.Memory;
using Ustas.RimAI.Core.Modules;

namespace Ustas.RimAI.Communication.Memory;

/// <summary>
/// Module composition root for RimAI.Communication.Memory. Owns Harmony and Core memory contracts.
/// </summary>
public sealed class MemoryComposition : IRimAiModuleComposition
{
    public static MemoryComposition Current { get; } = new();

    public string ModuleId => RimAiModuleIds.Memory;

    public bool IsStarted { get; private set; }

    public void Start()
    {
        if (IsStarted)
            return;

        Memory.BackCompatibilityFix.ForceInitialize();
        Memory.PromptNormalizer.UpdateRules(RimTalkMemoryPatchMod.Settings.normalizationRules);

        var harmony = new Harmony("ustas.rimai.communication.memory");
        harmony.PatchAll();
        TalkLifecycleBridge.Register();
        RimAIModuleRegistry.Current.Register(new RimAIModuleDescriptor(
            "memory",
            "RimAI.Communication.Memory",
            "RimAI.Communication.Memory",
            "Communication",
            "RimAI.Communication"));
        var memoryContext = new API.MemoryContextProvider();
        MemoryContextAccess.Register(memoryContext);
        MemoryContextAccess.RegisterKnowledge(memoryContext);
        RimAiLog.Info(RimAiLogCategory.Memory, "[RimAI.Memory] Loaded successfully");
        IsStarted = true;
    }

    public void Stop()
    {
        IsStarted = false;
    }
}
