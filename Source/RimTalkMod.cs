using Verse;
using UnityEngine;
using HarmonyLib;
using Ustas.RimAI.Communication.Memory.API;
using Ustas.RimAI.Communication.Memory.Integration;
using Ustas.RimAI.Core.Handshake;
using Ustas.RimAI.Core.Memory;
using Ustas.RimAI.Core.Modules;

namespace Ustas.RimAI.Communication.Memory
{
    public class RimTalkMemoryPatchMod : Mod
    {
        public const string HandshakeModuleVersion = "3.4.0";
        public static RimTalkMemoryPatchSettings Settings;

        public RimTalkMemoryPatchMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimTalkMemoryPatchSettings>();
            RimAiHandshake.TryActivate(
                RimAiHandshakeDescriptor.Current(RimAiModuleIds.Memory, HandshakeModuleVersion, isOptional: true),
                Activate);
        }

        static void Activate()
        {
            // v3.3.2.5: force-register key types so old saves remain readable
            Memory.BackCompatibilityFix.ForceInitialize();
            Memory.PromptNormalizer.UpdateRules(Settings.normalizationRules);

            var harmony = new Harmony("ustas.rimai.communication.memory");
            harmony.PatchAll();
            TalkLifecycleBridge.Register();
            RimAIModuleRegistry.Current.Register(new RimAIModuleDescriptor(
                "memory",
                "RimAI.Communication.Memory",
                "RimAI.Communication.Memory",
                "Communication",
                "RimAI.Communication"));
            var memoryContext = new MemoryContextProvider();
            MemoryContextAccess.Register(memoryContext);
            MemoryContextAccess.RegisterKnowledge(memoryContext);
            Log.Message("[RimAI.Memory] Loaded successfully");

            if (Prefs.DevMode)
            {
                Log.Message($"[PromptNormalizer] Initialized with {Memory.PromptNormalizer.GetActiveRuleCount()} active rules");
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            RimAISettingsNavigation.Open("communication", "memory");
            Settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return Content?.Name ?? "RimAI.Communication.Memory";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            
            // 重新加载提示词规范化规则
            Memory.PromptNormalizer.UpdateRules(Settings.normalizationRules);
        }
    }
}
