using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ustas.RimAI.Communication.Memory
{
    

    internal class AdvancedSettingsWindow : Window
    {
        internal readonly RimTalkMemoryPatchSettings settings;
        internal Vector2 scrollPos;

        public override Vector2 InitialSize => new Vector2(900f, 760f);

        public AdvancedSettingsWindow(RimTalkMemoryPatchSettings settings)
        {
            this.settings = settings;
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, 2200f); // ⭐ 增加高度
            Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
            listing.Begin(viewRect);

            Text.Font = GameFont.Medium;
            listing.Label("RimTalk_Settings_AdvancedSettingsTitle".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            listing.Label("RimTalk_Settings_AdvancedSettingsDesc".Translate());
            GUI.color = Color.white;
            listing.GapLine();

            settings.DrawCollapsibleSection(listing, "RimTalk_Settings_DynamicInjectionSection".Translate(), ref RimTalkMemoryPatchSettings.expandDynamicInjection, delegate { settings.DrawDynamicInjectionSettings(listing); });
            settings.DrawCollapsibleSection(listing, "RimTalk_Settings_MemoryCapacitySection".Translate(), ref RimTalkMemoryPatchSettings.expandMemoryCapacity, delegate { settings.DrawMemoryCapacitySettings(listing); });
            settings.DrawCollapsibleSection(listing, "RimTalk_Settings_MemoryDecaySection".Translate(), ref RimTalkMemoryPatchSettings.expandDecayRates, delegate { settings.DrawDecaySettings(listing); });
            settings.DrawCollapsibleSection(listing, "RimTalk_Settings_SummarizationSection".Translate(), ref RimTalkMemoryPatchSettings.expandSummarization, delegate { settings.DrawSummarizationSettings(listing); });

            if (settings.useAISummarization)
            {
                settings.DrawCollapsibleSection(listing, "RimTalk_Settings_AIConfigSection".Translate(), ref RimTalkMemoryPatchSettings.expandAIConfig, delegate { settings.DrawAIConfigSettings(listing); });
            }

            settings.DrawCollapsibleSection(listing, "RimTalk_Settings_MemoryTypesSection".Translate(), ref RimTalkMemoryPatchSettings.expandMemoryTypes, delegate { settings.DrawMemoryTypesSettings(listing); });
            
            // ⭐ 添加向量增强设置
            settings.DrawCollapsibleSection(listing, "RimTalk_Settings_VectorEnhancementSection".Translate(), ref RimTalkMemoryPatchSettings.expandVectorEnhancement, delegate { settings.DrawVectorEnhancementSettings(listing); });
            
            settings.DrawCollapsibleSection(listing, "RimTalk_Settings_ExperimentalSection".Translate(), ref RimTalkMemoryPatchSettings.expandExperimentalFeatures, delegate { settings.DrawExperimentalFeaturesSettings(listing); });

            listing.End();
            Widgets.EndScrollView();
        }
    }
}
