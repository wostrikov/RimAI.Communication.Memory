using HarmonyLib;
using Ustas.RimAI.Communication.Memory.API;
using Ustas.RimAI.Communication.Memory.Integration;
using Ustas.RimAI.Communication.Memory.VectorDB;
using Ustas.RimAI.Core.Composition;
using Ustas.RimAI.Core.Diagnostics;
using Ustas.RimAI.Core.Handshake;
using Ustas.RimAI.Core.Memory;
using Ustas.RimAI.Core.Modules;

namespace Ustas.RimAI.Communication.Memory;

/// <summary>
/// Module composition root for RimAI.Communication.Memory.
/// Owns Harmony (process lifetime), typed memory contracts, and VectorService.
/// </summary>
public sealed class MemoryComposition : IRimAiModuleComposition
{
    // RimAI.composition: ROOT_OWNED_SINGLETON — module composition root for Memory.
    public static MemoryComposition Current { get; } = new();

    public string ModuleId => RimAiModuleIds.Memory;

    public bool IsStarted { get; private set; }

    /// <summary>Typed Memory/Knowledge provider owned by this root.</summary>
    public MemoryContextProvider ContextProvider { get; private set; }

    /// <summary>Process-lifetime vector index; embeddings remain disabled at EmbeddingService.</summary>
    public VectorService VectorService { get; private set; }

    Harmony _harmony;

    public void Start()
    {
        if (IsStarted)
            return;

        Memory.BackCompatibilityFix.ForceInitialize();
        Memory.PromptNormalizer.UpdateRules(RimTalkMemoryPatchMod.Settings.normalizationRules);

        _harmony = new Harmony("ustas.rimai.communication.memory");
        _harmony.PatchAll();
        TalkLifecycleBridge.Register();
        RimAIModuleRegistry.Current.Register(new RimAIModuleDescriptor(
            "memory",
            "RimAI.Communication.Memory",
            "RimAI.Communication.Memory",
            "Communication",
            "RimAI.Communication"));

        VectorService ??= VectorService.BindRootOwned(new VectorService());
        ContextProvider = new MemoryContextProvider();
        MemoryContextAccess.Register(ContextProvider);
        MemoryContextAccess.RegisterKnowledge(ContextProvider);
        MemoryPipelineProbe.Register();
        RimAiLog.Info(RimAiLogCategory.Memory, "[RimAI.Memory] Loaded successfully");
        IsStarted = true;
    }

    public void Stop()
    {
        if (!IsStarted)
            return;

        // Do not UnpatchAll — Harmony is process-lifetime (matches Communication siblings).
        // Do not null VectorService — Scribe/ExposeData may still touch Instance after Stop.
        TalkLifecycleBridge.Unregister();
        MemoryContextAccess.Clear();
        ContextProvider = null;
        IsStarted = false;
    }
}
