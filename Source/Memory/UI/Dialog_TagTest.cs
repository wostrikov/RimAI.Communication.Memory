using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace Ustas.RimAI.Communication.Memory.UI
{
    public class Dialog_TagTest : Window
    {
        private string testTag = "";
        private string testContext = "";
        private Pawn testPawn = null;
        private bool testResult = false;
        private string testMatchText = "";
        private bool testExecuted = false;
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(650f, 600f);

        public Dialog_TagTest()
        {
            this.doCloseX = true;
            this.doCloseButton = false;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
            this.forcePause = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;
            
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, inRect.width, 35f), 
                CommonKnowledgeTranslationKeys.TagTestTitle.Translate());
            Text.Font = GameFont.Small;
            y += 40f;
            
            
            Widgets.Label(new Rect(0f, y, inRect.width, 22f), 
                CommonKnowledgeTranslationKeys.TagTestInputTag.Translate());
            y += 22f;
            testTag = Widgets.TextField(new Rect(0f, y, inRect.width, 28f), testTag);
            y += 32f;
            
            Widgets.Label(new Rect(0f, y, inRect.width, 22f), 
                CommonKnowledgeTranslationKeys.TagTestInputContext.Translate());
            y += 22f;
            testContext = Widgets.TextArea(new Rect(0f, y, inRect.width, 50f), testContext);
            y += 54f;
            
            Widgets.Label(new Rect(0f, y, inRect.width, 22f), 
                CommonKnowledgeTranslationKeys.TagTestSelectPawn.Translate());
            y += 22f;
            
            string pawnLabel = testPawn == null 
                ? CommonKnowledgeTranslationKeys.TagTestNoPawn.Translate().ToString()
                : testPawn.LabelShort;
            
            if (Widgets.ButtonText(new Rect(0f, y, inRect.width, 28f), pawnLabel))
            {
                ShowPawnSelectionMenu();
            }
            y += 32f;
            
            float buttonWidth = (inRect.width - 10f) / 2f;
            if (Widgets.ButtonText(new Rect(0f, y, buttonWidth, 32f), "Перевірити"))
            {
                ExecuteTagTest();
            }
            
            if (Widgets.ButtonText(new Rect(buttonWidth + 10f, y, buttonWidth, 32f), 
                CommonKnowledgeTranslationKeys.TagTestClear.Translate()))
            {
                testTag = "";
                testContext = "";
                testPawn = null;
                testExecuted = false;
            }
            y += 40f;
            
            Widgets.DrawLineHorizontal(0f, y, inRect.width);
            y += 15f;
            
            float resultAreaHeight = inRect.height - y;
            Rect resultRect = new Rect(0f, y, inRect.width, resultAreaHeight);
            
            if (testExecuted)
            {
                DrawResultArea(resultRect);
            }
            else
            {
                DrawEmptyResultArea(resultRect);
            }
        }
        
        private void DrawEmptyResultArea(Rect rect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            Widgets.Label(rect, "Натисніть «Перевірити», щоб почати тест");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawResultArea(Rect rect)
        {
            float y = rect.y;
            
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, y, rect.width, 25f), 
                CommonKnowledgeTranslationKeys.TagTestResult.Translate());
            y += 25f;
            
            Text.Font = GameFont.Medium;
            if (testResult)
            {
                GUI.color = new Color(0.3f, 0.9f, 0.3f);
                Widgets.Label(new Rect(rect.x, y, rect.width, 30f), 
                    CommonKnowledgeTranslationKeys.TagTestMatched.Translate());
            }
            else
            {
                GUI.color = new Color(0.9f, 0.3f, 0.3f);
                Widgets.Label(new Rect(rect.x, y, rect.width, 30f), 
                    CommonKnowledgeTranslationKeys.TagTestNotMatched.Translate());
            }
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            y += 35f;
            
            Widgets.Label(new Rect(rect.x, y, rect.width, 25f), 
                CommonKnowledgeTranslationKeys.TagTestMatchText.Translate());
            y += 25f;
            
            float scrollAreaHeight = rect.yMax - y;
            Rect scrollOuterRect = new Rect(rect.x, y, rect.width, scrollAreaHeight);
            
            Text.Font = GameFont.Tiny;
            float contentHeight = Text.CalcHeight(testMatchText, rect.width - 20f);
            Text.Font = GameFont.Small;
            
            Rect scrollViewRect = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(contentHeight + 20f, scrollAreaHeight));
            
            Widgets.BeginScrollView(scrollOuterRect, ref scrollPosition, scrollViewRect);
            
            Rect textBoxRect = new Rect(5f, 5f, scrollViewRect.width - 10f, contentHeight + 10f);
            Widgets.DrawBoxSolid(textBoxRect, new Color(0.1f, 0.1f, 0.1f, 0.8f));
            
            Rect textRect = textBoxRect.ContractedBy(5f);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.9f, 0.9f, 0.9f);
            Widgets.Label(textRect, testMatchText);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            
            Widgets.EndScrollView();
        }

        private void ShowPawnSelectionMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            
            options.Add(new FloatMenuOption(
                CommonKnowledgeTranslationKeys.TagTestNoPawn.Translate(),
                delegate { testPawn = null; }
            ));
            
            if (Find.CurrentMap != null)
            {
                var allPawns = Find.CurrentMap.mapPawns.AllPawns
                    .Where(p => p.RaceProps.Humanlike)
                    .OrderBy(p => p.Faction != Faction.OfPlayer)
                    .ThenBy(p => p.LabelShort);
                
                foreach (var pawn in allPawns)
                {
                    string label = $"{pawn.LabelShort}";
                    if (pawn.Faction != null)
                    {
                        label += $" ({pawn.Faction.Name})";
                    }
                    
                    Pawn localPawn = pawn;
                    options.Add(new FloatMenuOption(label, delegate { testPawn = localPawn; }));
                }
            }
            
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ExecuteTagTest()
        {
            if (string.IsNullOrWhiteSpace(testTag))
            {
                Messages.Message("Введіть мітку", MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            var tempEntry = new CommonKnowledgeEntry(testTag, "Тестовий вміст")
            {
                matchMode = KeywordMatchMode.Any
            };
            
            System.Text.StringBuilder matchTextBuilder = new System.Text.StringBuilder();
            matchTextBuilder.Append(testContext);
            
            if (testPawn != null)
            {
                matchTextBuilder.Append(" ");
                matchTextBuilder.Append(BuildPawnInfoText(testPawn));
            }
            
            testMatchText = matchTextBuilder.ToString();
            
            testResult = TestTagMatch(testMatchText, tempEntry);
            testExecuted = true;
            
            Log.Message($"[Тест тегу] Тег: {testTag}");
            Log.Message($"[Тест тегу] Діалог: {testContext}");
            Log.Message($"[Тест тегу] Pawn: {(testPawn != null ? testPawn.LabelShort : "немає")}");
            Log.Message($"[Тест тегу] Фактичний текст зіставлення: {testMatchText}");
            Log.Message($"[Тест тегу] Результат: {(testResult ? "збіг" : "немає збігу")}");
        }

        private string BuildPawnInfoText(Pawn pawn)
        {
            if (pawn == null)
                return string.Empty;

            var sb = new System.Text.StringBuilder();

            try
            {
                if (!string.IsNullOrEmpty(pawn.Name?.ToStringShort))
                {
                    sb.Append(pawn.Name.ToStringShort);
                    sb.Append(" ");
                }

                if (pawn.RaceProps != null && pawn.RaceProps.Humanlike)
                {
                    float ageYears = pawn.ageTracker.AgeBiologicalYearsFloat;
                    
                    if (ageYears < 3f)
                    {
                        sb.Append("немовля малюк ");
                    }
                    else if (ageYears < 13f)
                    {
                        sb.Append("дитина дитя ");
                    }
                    else if (ageYears < 18f)
                    {
                        sb.Append("підліток ");
                    }
                    else
                    {
                        sb.Append("дорослий ");
                    }
                }

                sb.Append(pawn.gender.GetLabel());
                sb.Append(" ");

                if (pawn.def != null)
                {
                    sb.Append(pawn.def.label);
                    sb.Append(" ");
                    
                    try
                    {
                        if (pawn.genes != null && pawn.genes.Xenotype != null)
                        {
                            string xenotypeName = pawn.genes.Xenotype.label ?? pawn.genes.Xenotype.defName;
                            if (!string.IsNullOrEmpty(xenotypeName))
                            {
                                sb.Append(xenotypeName);
                                sb.Append(" ");
                            }
                        }
                    }
                    catch { }
                }

                if (pawn.IsColonist)
                {
                    sb.Append("колоніст ");
                }
                else if (pawn.IsPrisoner)
                {
                    sb.Append("бранець ");
                }
                else if (pawn.IsSlaveOfColony)
                {
                    sb.Append("раб ");
                }
                else if (pawn.HostFaction == Faction.OfPlayer)
                {
                    sb.Append("гість ");
                }
                else if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer)
                {
                    sb.Append(pawn.Faction.Name);
                    sb.Append(" ");
                }

                if (pawn.story?.traits != null)
                {
                    foreach (var trait in pawn.story.traits.allTraits)
                    {
                        if (trait?.def?.label != null)
                        {
                            sb.Append(trait.def.label);
                            sb.Append(" ");
                        }
                    }
                }

                if (pawn.skills != null)
                {
                    foreach (var skillRecord in pawn.skills.skills)
                    {
                        if (skillRecord.TotallyDisabled || skillRecord.def?.label == null)
                            continue;
                        
                        int level = skillRecord.Level;
                        
                        if (level >= 5)
                        {
                            sb.Append(skillRecord.def.label);
                            sb.Append(level);
                            sb.Append(" ");
                            
                            if (level >= 15)
                            {
                                sb.Append(skillRecord.def.label);
                                sb.Append("майстер ");
                            }
                            else if (level >= 10)
                            {
                                sb.Append(skillRecord.def.label);
                                sb.Append("вправний ");
                            }
                        }
                    }
                }

                if (pawn.health != null)
                {
                    if (pawn.health.hediffSet.GetInjuredParts().Any())
                    {
                        sb.Append("поранений ");
                    }
                    else if (!pawn.health.HasHediffsNeedingTend())
                    {
                        sb.Append("здоровий ");
                    }
                }

                if (pawn.relations != null)
                {
                    var relatedPawns = pawn.relations.RelatedPawns.Take(5);
                    foreach (var relatedPawn in relatedPawns)
                    {
                        if (!string.IsNullOrEmpty(relatedPawn.Name?.ToStringShort))
                        {
                            sb.Append(relatedPawn.Name.ToStringShort);
                            sb.Append(" ");
                        }
                    }
                }

                if (pawn.story?.Adulthood != null)
                {
                    string backstoryTitle = pawn.story.Adulthood.TitleFor(pawn.gender);
                    if (!string.IsNullOrEmpty(backstoryTitle))
                    {
                        sb.Append(backstoryTitle);
                        sb.Append(" ");
                    }
                }
                
                if (pawn.story?.Childhood != null)
                {
                    string childhoodTitle = pawn.story.Childhood.TitleFor(pawn.gender);
                    if (!string.IsNullOrEmpty(childhoodTitle))
                    {
                        sb.Append(childhoodTitle);
                        sb.Append(" ");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[Тест тегу] Помилка під час побудови даних Pawn: {ex.Message}");
            }

            return sb.ToString().Trim();
        }

        private bool TestTagMatch(string text, CommonKnowledgeEntry entry)
        {
            var tags = entry.GetTags();
            if (tags == null || tags.Count == 0) return false;

            switch (entry.matchMode)
            {
                case KeywordMatchMode.Any:
                    foreach (var tag in tags)
                    {
                        if (string.IsNullOrWhiteSpace(tag)) continue;
                        if (text.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }
                    return false;

                case KeywordMatchMode.All:
                    foreach (var tag in tags)
                    {
                        if (string.IsNullOrWhiteSpace(tag)) continue;
                        if (text.IndexOf(tag, StringComparison.OrdinalIgnoreCase) < 0)
                            return false;
                    }
                    return true;

                default:
                    return false;
            }
        }
    }
}
