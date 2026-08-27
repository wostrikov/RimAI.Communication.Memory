using UnityEngine;
using Verse;
using RimWorld;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.UI
{
    public class Dialog_PromptEditor : Window
    {
        private RimTalkMemoryPatchSettings settings;
        
        private string editDailySummary;
        private string editDeepArchive;
        private int editMaxTokens;
        
        private const string DEFAULT_DAILY_SUMMARY = 
            "Підсумок спогадів колоніста {0}\n\n" +
            "Список спогадів\n" +
            "{1}\n\n" +
            "Виділи місця, персонажів і події\n" +
            "Об'єднай подібні події та зазнач їхню частоту\n" +
            "Виклади вкрай стисло, не більше 80 слів\n" +
            "Виведи лише текст підсумку без додаткового форматування";
        
        private const string DEFAULT_DEEP_ARCHIVE = 
            "Архів спогадів колоніста {0}\n\n" +
            "Список спогадів\n" +
            "{1}\n\n" +
            "Виділи основні риси та ключові події\n" +
            "Об'єднай подібний досвід і підкресли довгострокові тенденції\n" +
            "Виклади вкрай стисло, не більше 60 слів\n" +
            "Виведи лише текст підсумку без додаткового форматування";
        
        private Vector2 scrollPosition = Vector2.zero;
        
        public override Vector2 InitialSize => new Vector2(800f, 650f);
        
        public Dialog_PromptEditor()
        {
            this.settings = RimTalkMemoryPatchMod.Settings;
            
            doCloseX = true;
            doCloseButton = false;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            
            editDailySummary = string.IsNullOrEmpty(settings.dailySummaryPrompt) 
                ? DEFAULT_DAILY_SUMMARY 
                : settings.dailySummaryPrompt;
                
            editDeepArchive = string.IsNullOrEmpty(settings.deepArchivePrompt) 
                ? DEFAULT_DEEP_ARCHIVE 
                : settings.deepArchivePrompt;
                
            editMaxTokens = settings.summaryMaxTokens;
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0f, 0f, inRect.width, 35f);
            Widgets.Label(titleRect, "RimTalk_PromptEditor_Title".Translate());
            
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            Rect descRect = new Rect(0f, 35f, inRect.width, 20f);
            Widgets.Label(descRect, "RimTalk_PromptEditor_Desc".Translate());
            GUI.color = Color.white;
            
            float contentY = 60f;
            float contentHeight = inRect.height - contentY - 50f;
            Rect contentRect = new Rect(0f, contentY, inRect.width, contentHeight);
            
            DrawContent(contentRect);
            
            float buttonY = inRect.height - 40f;
            float buttonWidth = 120f;
            float spacing = 10f;
            
            Rect resetRect = new Rect(0f, buttonY, buttonWidth, 35f);
            if (Widgets.ButtonText(resetRect, "RimTalk_PromptEditor_ResetDefault".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "RimTalk_PromptEditor_ResetConfirm".Translate(),
                    delegate
                    {
                        editDailySummary = DEFAULT_DAILY_SUMMARY;
                        editDeepArchive = DEFAULT_DEEP_ARCHIVE;
                        editMaxTokens = 200;
                    }
                ));
            }
            
            float rightX = inRect.width - buttonWidth;
            Rect saveRect = new Rect(rightX, buttonY, buttonWidth, 35f);
            if (Widgets.ButtonText(saveRect, "RimTalk_Save".Translate()))
            {
                SaveAndClose();
            }
            
            rightX -= buttonWidth + spacing;
            Rect cancelRect = new Rect(rightX, buttonY, buttonWidth, 35f);
            if (Widgets.ButtonText(cancelRect, "RimTalk_Cancel".Translate()))
            {
                Close();
            }
        }
        
        private void DrawContent(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            Rect viewRect = new Rect(0f, 0f, rect.width - 20f, 900f);
            
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            // Verse wraps a Listing into a second column, off the visible view, as soon as
            // content passes the rect height, and CurHeight then reports that new column.
            // A scrolling settings page never wants that; see validate_scrollable_listings.
            listing.maxOneColumn = true;
            listing.Begin(viewRect);
            
            Text.Font = GameFont.Small;
            GUI.color = new Color(0.8f, 0.9f, 1f);
            listing.Label("RimTalk_PromptEditor_DailySummary".Translate());
            GUI.color = Color.white;
            
            GUI.color = Color.gray;
            listing.Label("RimTalk_PromptEditor_DailySummaryDesc".Translate());
            listing.Label("RimTalk_PromptEditor_Placeholders".Translate());
            GUI.color = Color.white;
            listing.Gap(4f);
            
            Rect dailyRect = listing.GetRect(180f);
            editDailySummary = Widgets.TextArea(dailyRect, editDailySummary);
            
            listing.Gap(15f);
            listing.GapLine();
            listing.Gap(10f);
            
            GUI.color = new Color(0.8f, 0.9f, 1f);
            listing.Label("RimTalk_PromptEditor_DeepArchive".Translate());
            GUI.color = Color.white;
            
            GUI.color = Color.gray;
            listing.Label("RimTalk_PromptEditor_DeepArchiveDesc".Translate());
            listing.Label("RimTalk_PromptEditor_Placeholders".Translate());
            GUI.color = Color.white;
            listing.Gap(4f);
            
            Rect archiveRect = listing.GetRect(180f);
            editDeepArchive = Widgets.TextArea(archiveRect, editDeepArchive);
            
            listing.Gap(15f);
            listing.GapLine();
            listing.Gap(10f);
            
            GUI.color = new Color(0.8f, 0.9f, 1f);
            listing.Label("RimTalk_PromptEditor_MaxTokens".Translate());
            GUI.color = Color.white;
            
            GUI.color = Color.gray;
            listing.Label("RimTalk_PromptEditor_MaxTokensDesc".Translate());
            GUI.color = Color.white;
            listing.Gap(4f);
            
            listing.Label("RimTalk_PromptEditor_MaxTokensLabel".Translate(editMaxTokens));
            editMaxTokens = (int)listing.Slider(editMaxTokens, 100, 8000);
            
            listing.Gap(10f);
            GUI.color = new Color(1f, 0.9f, 0.6f);
            listing.Label("RimTalk_PromptEditor_Tips".Translate());
            GUI.color = Color.gray;
            listing.Label("RimTalk_PromptEditor_Tip1".Translate());
            listing.Label("RimTalk_PromptEditor_Tip2".Translate());
            listing.Label("RimTalk_PromptEditor_Tip3".Translate());
            GUI.color = Color.white;
            
            listing.End();
            Widgets.EndScrollView();
        }
        
        private void SaveAndClose()
        {
            settings.dailySummaryPrompt = (editDailySummary == DEFAULT_DAILY_SUMMARY) 
                ? "" 
                : editDailySummary;
                
            settings.deepArchivePrompt = (editDeepArchive == DEFAULT_DEEP_ARCHIVE) 
                ? "" 
                : editDeepArchive;
                
            settings.summaryMaxTokens = editMaxTokens;
            
            settings.Write();
            
            Messages.Message("RimTalk_PromptEditor_Saved".Translate(), MessageTypeDefOf.PositiveEvent, false);
            
            Close();
        }
    }
}
