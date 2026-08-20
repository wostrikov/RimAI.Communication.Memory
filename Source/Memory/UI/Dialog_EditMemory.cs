using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace Ustas.RimAI.Communication.Memory.UI
{
    public class Dialog_EditMemory : Window
    {
        private MemoryEntry memory;
        private FourLayerMemoryComp memoryComp;
        private string editedContent;
        private string editedNotes;
        private Vector2 scrollPosition;
        private Vector2 contentScrollPosition;
        private List<string> availableTags;
        private string newTagInput = "";

        public override Vector2 InitialSize => new Vector2(600f, 650f);

        public Dialog_EditMemory(MemoryEntry memory, FourLayerMemoryComp comp)
        {
            this.memory = memory;
            this.memoryComp = comp;
            this.editedContent = memory.Content;
            this.editedNotes = memory.Notes ?? "";
            
            availableTags = new List<string>
            {
                MemoryTags.开心, MemoryTags.悲伤, MemoryTags.愤怒, MemoryTags.焦虑, MemoryTags.平静,
                MemoryTags.战斗, MemoryTags.袭击, MemoryTags.受伤, MemoryTags.死亡, MemoryTags.完成任务,
                MemoryTags.闲聊, MemoryTags.深谈, MemoryTags.争吵, MemoryTags.友好, MemoryTags.敌对,
                MemoryTags.烹饪, MemoryTags.建造, MemoryTags.种植, MemoryTags.采矿, MemoryTags.研究, MemoryTags.医疗,
                MemoryTags.重要, MemoryTags.紧急
            };

            doCloseButton = false;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "Редагування спогаду");
            Text.Font = GameFont.Small;

            float curY = 45f;

            Rect infoRect = new Rect(0f, curY, inRect.width, 60f);
            GUI.color = Color.gray;
            Widgets.Label(new Rect(infoRect.x, infoRect.y, infoRect.width, 25f), 
                $"Тип: {memory.TypeName}  |  Рівень: {memory.LayerName}  |  Час: {memory.AgeString}");
            Widgets.Label(new Rect(infoRect.x, infoRect.y + 25f, infoRect.width, 25f), 
                $"Важливість: {memory.Importance:F2}  |  Активність: {memory.Activity:F2}");
            GUI.color = Color.white;
            curY += 65f;

            Widgets.Label(new Rect(0f, curY, inRect.width, 25f), "Вміст:");
            curY += 25f;
            
            Rect contentRect = new Rect(0f, curY, inRect.width, 150f);

            float textHeight = Mathf.Max(contentRect.height, Text.CalcHeight(editedContent, contentRect.width - 20f) + 10f);
            Rect viewRect = new Rect(0f, 0f, contentRect.width - 20f, textHeight);

            Widgets.BeginScrollView(contentRect, ref contentScrollPosition, viewRect);
            editedContent = Widgets.TextArea(viewRect, editedContent);
            Widgets.EndScrollView();

            curY += 155f;

            Widgets.Label(new Rect(0f, curY, inRect.width, 25f), "Примітки:");
            curY += 25f;
            
            Rect notesRect = new Rect(0f, curY, inRect.width, 60f);
            editedNotes = Widgets.TextArea(notesRect, editedNotes);
            curY += 65f;

            Widgets.Label(new Rect(0f, curY, inRect.width, 25f), "Мітки:");
            curY += 25f;

            DrawTagsSection(new Rect(0f, curY, inRect.width, 100f));
            curY += 105f;

            Rect pinnedRect = new Rect(0f, curY, inRect.width, 30f);
            bool wasPinned = memory.IsPinned;
            Widgets.CheckboxLabeled(pinnedRect, "Закріпити цей спогад (він не видалятиметься й не згасатиме)", ref memory.IsPinned);
            if (memory.IsPinned != wasPinned)
            {
                if (memory.IsPinned)
                {
                    memory.AddTag(MemoryTags.重要);
                }
            }
            curY += 35f;

            float buttonWidth = 120f;
            float buttonY = inRect.height - 40f;
            
            if (Widgets.ButtonText(new Rect(inRect.width - buttonWidth * 2 - 10f, buttonY, buttonWidth, 35f), "Зберегти"))
            {
                SaveChanges();
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.width - buttonWidth, buttonY, buttonWidth, 35f), "Скасувати"))
            {
                Close();
            }
        }

        private void DrawTagsSection(Rect rect)
        {
            Rect viewRect = new Rect(0f, 0f, rect.width - 20f, availableTags.Count * 25f + 40f);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float curY = 0f;

            if (memory.tags != null && memory.tags.Any())
            {
                foreach (var tag in memory.tags.ToList())
                {
                    Rect tagRect = new Rect(0f, curY, viewRect.width, 22f);
                    
                    Rect labelRect = new Rect(tagRect.x + 5f, tagRect.y, tagRect.width - 70f, tagRect.height);
                    Widgets.Label(labelRect, $"✓ {tag}");
                    
                    Rect removeRect = new Rect(tagRect.xMax - 60f, tagRect.y, 55f, 22f);
                    if (Widgets.ButtonText(removeRect, "Прибрати"))
                    {
                        memory.RemoveTag(tag);
                    }
                    
                    curY += 25f;
                }
                
                curY += 5f;
                Widgets.DrawLineHorizontal(0f, curY, viewRect.width);
                curY += 10f;
            }

            foreach (var tag in availableTags)
            {
                if (memory.tags != null && memory.tags.Contains(tag))
                    continue;

                Rect tagRect = new Rect(0f, curY, viewRect.width, 22f);
                
                Rect labelRect = new Rect(tagRect.x + 5f, tagRect.y, tagRect.width - 70f, tagRect.height);
                GUI.color = Color.gray;
                Widgets.Label(labelRect, tag);
                GUI.color = Color.white;
                
                Rect addRect = new Rect(tagRect.xMax - 60f, tagRect.y, 55f, 22f);
                if (Widgets.ButtonText(addRect, "Додати"))
                {
                    memory.AddTag(tag);
                }
                
                curY += 25f;
            }

            curY += 5f;
            Widgets.DrawLineHorizontal(0f, curY, viewRect.width);
            curY += 10f;
            
            Rect customLabelRect = new Rect(0f, curY, viewRect.width, 22f);
            Widgets.Label(customLabelRect, "Власна мітка:");
            curY += 25f;
            
            Rect inputRect = new Rect(0f, curY, viewRect.width - 70f, 22f);
            newTagInput = Widgets.TextField(inputRect, newTagInput);
            
            Rect addCustomRect = new Rect(viewRect.width - 60f, curY, 55f, 22f);
            if (Widgets.ButtonText(addCustomRect, "Додати") && !string.IsNullOrWhiteSpace(newTagInput))
            {
                memory.AddTag(newTagInput.Trim());
                newTagInput = "";
            }

            Widgets.EndScrollView();
        }

        private void SaveChanges()
        {
            memoryComp.EditMemory(memory.Id, editedContent, editedNotes);
            
            if (!memory.tags.Contains(MemoryTags.用户编辑))
            {
                memory.AddTag(MemoryTags.用户编辑);
            }

            Messages.Message("Спогад оновлено", MessageTypeDefOf.TaskCompletion);
        }
    }
}
