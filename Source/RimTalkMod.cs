using Verse;
using UnityEngine;
using Ustas.RimAI.Core.Handshake;
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
                MemoryComposition.Current.Start);
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
            
            Memory.PromptNormalizer.UpdateRules(Settings.normalizationRules);
        }
    }
}
