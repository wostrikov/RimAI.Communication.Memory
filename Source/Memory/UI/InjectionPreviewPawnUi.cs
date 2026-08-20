
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;
using Ustas.RimAI.Communication.Memory.API;
using Ustas.RimAI.Communication.Prompt;

namespace Ustas.RimAI.Communication.Memory.Debug
{
    internal sealed class InjectionPreviewPawnUi : Dialog_InjectionPreviewCollaborator
    {
        internal InjectionPreviewPawnUi(Dialog_InjectionPreview owner) : base(owner) { }

public void DoWindowContents(Rect inRect)
        {
            float yPos = 0f;

            Text.Font = GameFont.Medium;
            GUI.color = new Color(1f, 0.9f, 0.7f);
            Widgets.Label(new Rect(0f, yPos, 600f, 35f), "RimTalk_Preview_Title".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            yPos += 40f;

            DrawPawnSelectors(new Rect(0f, yPos, inRect.width, 80f));
            yPos += 85f;

            if (selectedPawn == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(0f, inRect.height / 2 - 20f, inRect.width, 40f), 
                    "RimTalk_Preview_NoColonist".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            Rect helpButtonRect = new Rect(inRect.width - 240f, yPos, 110f, 30f);
            if (Widgets.ButtonText(helpButtonRect, "RimTalk_Preview_Help".Translate()))
            {
                Owner.ShowHelpDialog();
            }
            
            Rect refreshButtonRect = new Rect(inRect.width - 120f, yPos, 110f, 30f);
            if (Widgets.ButtonText(refreshButtonRect, "RimTalk_Preview_Refresh".Translate()))
            {
                Owner.RefreshPreview();
            }
            yPos += 35f;

            float contentHeight = inRect.height - yPos - 50f;
            float halfWidth = (inRect.width - 15f) / 2f;
            
            Rect leftRect = new Rect(0f, yPos, halfWidth, contentHeight);
            Owner.DrawKnowledgePanel(leftRect);
            
            Rect rightRect = new Rect(halfWidth + 15f, yPos, halfWidth, contentHeight);
            Owner.DrawMemoryPanel(rightRect);
        }

internal void DrawPawnSelectors(Rect rect)
        {
            GUI.color = new Color(0.8f, 0.9f, 1f);
            Widgets.Label(new Rect(rect.x, rect.y, 120f, 30f), "RimTalk_Preview_CurrentPawn".Translate());
            GUI.color = Color.white;

            Rect buttonRect = new Rect(rect.x + 130f, rect.y, 200f, 30f);
            string label = selectedPawn != null ? selectedPawn.LabelShort : (string)"RimTalk_Preview_None".Translate();
            if (Widgets.ButtonText(buttonRect, label))
            {
                ShowPawnSelectionMenu(isPrimary: true);
            }

            if (selectedPawn != null)
            {
                GUI.color = Color.gray;
                string info = $"{selectedPawn.def.label} | {selectedPawn.gender.GetLabel()}";
                Widgets.Label(new Rect(rect.x + 340f, rect.y + 5f, 300f, 30f), info);
                GUI.color = Color.white;
            }

            float secondRowY = rect.y + 35f;
            GUI.color = new Color(1f, 0.9f, 0.8f);
            Widgets.Label(new Rect(rect.x, secondRowY, 120f, 30f), "RimTalk_Preview_TargetPawn".Translate());
            GUI.color = Color.white;

            Rect targetButtonRect = new Rect(rect.x + 130f, secondRowY, 200f, 30f);
            string targetLabel = targetPawn != null ? targetPawn.LabelShort : (string)"RimTalk_Preview_NoneClickToSelect".Translate();
            if (Widgets.ButtonText(targetButtonRect, targetLabel))
            {
                ShowPawnSelectionMenu(isPrimary: false);
            }

            if (targetPawn != null)
            {
                GUI.color = Color.gray;
                string targetInfo = $"{targetPawn.def.label} | {targetPawn.gender.GetLabel()}";
                Widgets.Label(new Rect(rect.x + 340f, secondRowY + 5f, 250f, 30f), targetInfo);
                GUI.color = Color.white;
                
                Rect clearButtonRect = new Rect(rect.x + 600f, secondRowY, 80f, 30f);
                if (Widgets.ButtonText(clearButtonRect, "RimTalk_Preview_Clear".Translate()))
                {
                    targetPawn = null;
                    Owner.RefreshPreview();
                }
            }
        }

internal void ShowPawnSelectionMenu(bool isPrimary)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            
            if (Find.CurrentMap != null)
            {
                var allHumanlikes = Find.CurrentMap.mapPawns.AllPawnsSpawned
                    .Where(p => p.RaceProps.Humanlike)
                    .OrderBy(p =>
                    {
                        if (p.IsColonist) return 1;
                        if (p.IsPrisoner) return 2;
                        if (p.IsSlaveOfColony) return 3;
                        if (p.HostFaction == Faction.OfPlayer) return 4;
                        return 5;
                    })
                    .ThenBy(p => p.LabelShort);
                
                foreach (var pawn in allHumanlikes)
                {
                    Pawn localPawn = pawn;
                    
                    string optionLabel = pawn.LabelShort;
                    
                    if (pawn.IsColonist)
                        optionLabel += " " + "RimTalk_Preview_Colonist".Translate();
                    else if (pawn.IsPrisoner)
                        optionLabel += " " + "RimTalk_Preview_Prisoner".Translate();
                    else if (pawn.IsSlaveOfColony)
                        optionLabel += " " + "RimTalk_Preview_Slave".Translate();
                    else if (pawn.HostFaction == Faction.OfPlayer)
                        optionLabel += " " + "RimTalk_Preview_Guest".Translate();
                    else if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer)
                        optionLabel += $" ({pawn.Faction.Name})";
                    
                    if (!isPrimary && selectedPawn != null && pawn == selectedPawn)
                        optionLabel += " " + "RimTalk_Preview_SameAsCurrent".Translate();
                    
                    options.Add(new FloatMenuOption(optionLabel, delegate
                    {
                        if (isPrimary)
                        {
                            selectedPawn = localPawn;
                            if (targetPawn == localPawn)
                                targetPawn = null;
                        }
                        else
                        {
                            targetPawn = localPawn;
                        }
                        Owner.RefreshPreview();
                    }));
                }
            }

            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
            else
            {
                Messages.Message("RimTalk_Preview_NoHumanlikes".Translate(), MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
