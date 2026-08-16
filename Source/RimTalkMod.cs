using Verse;
using UnityEngine;
using HarmonyLib;
using RimTalk.Memory.API;
using Ustas.RimAI.Core.Memory;
using Ustas.RimAI.Core.Modules;

namespace RimTalk.MemoryPatch
{
    public class RimTalkMemoryPatchMod : Mod
    {
        public static RimTalkMemoryPatchSettings Settings;

        public RimTalkMemoryPatchMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimTalkMemoryPatchSettings>();
            
            // ⭐ v3.3.2.5: 强制预注册关键类型，确保旧存档兼容性
            Memory.BackCompatibilityFix.ForceInitialize();
            
            // ⭐ 初始化提示词规范化器
            Memory.PromptNormalizer.UpdateRules(Settings.normalizationRules);
            
            var harmony = new Harmony("cj.rimtalk.expandmemory");
            harmony.PatchAll();
            RimAIModuleRegistry.Current.Register(new RimAIModuleDescriptor(
                "memory",
                "RimAI.Communication.Memory",
                "RimAI.Communication.Memory",
                "Communication",
                "RimAI.Communication"));
            var memoryContext = new MemoryContextProvider();
            MemoryContextAccess.Register(memoryContext);
            MemoryContextAccess.RegisterKnowledge(memoryContext);
            Log.Message("[RimTalk-Expand Memory] Loaded successfully");
            
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
