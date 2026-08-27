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
    internal class PromptNormalizationWindow : Window
    {
        internal readonly RimTalkMemoryPatchSettings settings;
        internal Vector2 scrollPos;

        public override Vector2 InitialSize => new Vector2(750f, 520f);

        public PromptNormalizationWindow(RimTalkMemoryPatchSettings settings)
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
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, 420f);
            Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
            // Verse wraps a Listing into a second column, off the visible view, as soon as
            // content passes the rect height, and CurHeight then reports that new column.
            // A scrolling settings page never wants that; see validate_scrollable_listings.
            listing.maxOneColumn = true;
            listing.Begin(viewRect);

            Text.Font = GameFont.Medium;
            listing.Label("RimTalk_Settings_PromptReplacementTitle".Translate());
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            listing.Label("RimTalk_Settings_PromptReplacementDesc".Translate());
            GUI.color = Color.white;
            listing.Gap(6f);

            settings.DrawPromptNormalizationSettings(listing);

            listing.End();
            Widgets.EndScrollView();

            PromptNormalizer.UpdateRules(settings.normalizationRules);
        }
    }
}
