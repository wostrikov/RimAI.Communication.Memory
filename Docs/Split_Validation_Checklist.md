# MainTabWindow_Memory Розділення — контрольний список перевірки

## ? Виконані роботи

### 1. Створення файлів часткових класів
- [x] MainTabWindow_Memory_TopBar.cs (145 рядків)
- [x] MainTabWindow_Memory_Controls.cs (376 рядків)
- [x] MainTabWindow_Memory_Timeline.cs (440 рядків)
- [x] MainTabWindow_Memory_Actions.cs (176 рядків)
- [x] MainTabWindow_Memory_ImportExport.cs (230 рядків)
- [x] MainTabWindow_Memory_Utilities.cs (210 рядків)
- [x] MainTabWindow_Memory_Helpers.cs (280 рядків — уже існує)

### 2. Створення документа
- [x] Звіт про розділення (Docs/Split_MainTabWindow_Memory.md)
- [x] Контрольний список перевірки (Docs/Split_Validation_Checklist.md)

---

## ? Незавершена робота

### 3. Очищення головного файлу
- [ ] Відкрити `Source/Memory/UI/MainTabWindow_Memory.cs`
- [ ] Видалити методи, розділені в інші файли:
  - [ ] Методи, пов’язані з DrawTopBar (у файлі TopBar)
  - [ ] Методи, пов’язані з DrawControlPanel (у файлі Controls)
  - [ ] Пов’язані методи DrawTimeline (у файлі Timeline)
  - [ ] Методи операцій SummarizeMemories тощо (у файлі Actions)
  - [ ] Методи ExportMemories тощо (у файлі ImportExport)
  - [ ] Методи GetFilteredMemories тощо (у файлі Utilities)
- [ ] Зберегти основний вміст:
  - [ ] Визначення полів
  - [ ] Конструктор
  - [ ] DoWindowContents (точка входу)
  - [ ] Визначення класу та простору імен
- [ ] Переконатися, що основний файл містить близько 100 рядків

### 4. Перевірка компіляції
- [ ] Виконати команду компіляції: `dotnet build RimTalk-ExpandMemory.csproj`
- [ ] Перевірити наявність помилок компіляції
- [ ] Виправити всі помилки missing reference

### 5. Функціональне тестування
- [ ] Запустити RimWorld
- [ ] Завантажити тестове збереження
- [ ] Відкрити вікно Mind Stream
- [ ] Перевірити функції:
  - [ ] Селектор Pawn працює належним чином
  - [ ] Фільтр працює належним чином
  - [ ] Картки пам’яті відображаються належним чином
  - [ ] Функція підсумовування працює належним чином
  - [ ] Функція архівації працює належним чином
  - [ ] Функція видалення працює належним чином
  - [ ] Функція експорту працює належним чином
  - [ ] Функція імпорту працює належним чином

### 6. Git-коміт
- [ ] Переглянути зміни: `git status`
- [ ] Додати файли: `git add Source/Memory/UI/MainTabWindow_Memory*.cs Docs/拆分*.md`
- [ ] Надіслати: `git commit -m "refactor: 拆分 MainTabWindow_Memory 为 7 个 partial class 文件"`
- [ ] Опублікувати: `git push origin <branch-name>`

---

## ?? Довідка з очищення основного файлу

### Вміст, який потрібно зберегти

```csharp
using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using RimTalk.Memory;
using System;

namespace RimTalk.Memory.UI
{
    public partial class MainTabWindow_Memory : MainTabWindow
    {
        // ==================== Data & State ====================
        private Pawn selectedPawn = null;
        private FourLayerMemoryComp currentMemoryComp = null;
        
        private bool showAllHumanlikes = false;
        
        private HashSet<MemoryEntry> selectedMemories = new HashSet<MemoryEntry>();
        private MemoryEntry lastSelectedMemory = null;
        
        private bool isDragging = false;
        private Vector2 dragStartPos = Vector2.zero;
        private Vector2 dragCurrentPos = Vector2.zero;
        
        private Vector2 timelineScrollPosition = Vector2.zero;
        private MemoryType? filterType = null;
        
        private bool showABM = true;
        private bool showSCM = true;
        private bool showELS = true;
        private bool showCLPA = true;
        
        private List<MemoryEntry> cachedFilteredMemories;
        private bool filtersDirty = true;
        
        private const float TOP_BAR_HEIGHT = 50f;
        private const float CONTROL_PANEL_WIDTH = 220f;
        private const float SPACING = 10f;
        private const float CARD_WIDTH_FULL = 600f;
        private const float CARD_SPACING = 8f;
        
        private List<MemoryEntry> cachedMemories = new List<MemoryEntry>();
        private List<float> cachedCardHeights = new List<float>();
        private List<float> cachedCardYPositions = new List<float>();
        private float cachedTotalHeight = 0f;
        
        private int lastMemoryCount = -1;
        private bool lastShowABM;
        private bool lastShowSCM;
        private bool lastShowELS;
        private bool lastShowCLPA;
        private MemoryType? lastFilterType;
        private Pawn lastSelectedPawn;
        private int lastRefreshTick = -1;
        
        public override Vector2 RequestedTabSize => new Vector2(1200f, 700f);

        // ==================== Main Layout ====================
        
        public override void DoWindowContents(Rect inRect)
        {
            // Top Bar
            Rect topBarRect = new Rect(0f, 0f, inRect.width, TOP_BAR_HEIGHT);
            DrawTopBar(topBarRect);
            
            // Content area
            float contentY = TOP_BAR_HEIGHT + SPACING;
            float contentHeight = inRect.height - contentY;
            
            if (selectedPawn == null)
            {
                DrawNoPawnSelected(new Rect(0f, contentY, inRect.width, contentHeight));
                return;
            }
            
            var memoryComp = selectedPawn.TryGetComp<FourLayerMemoryComp>();
            if (memoryComp == null)
            {
                DrawNoMemoryComponent(new Rect(0f, contentY, inRect.width, contentHeight));
                return;
            }
            
            currentMemoryComp = memoryComp;
            
            CheckAndRefreshCache();
            
            // Left Control Panel
            Rect controlPanelRect = new Rect(0f, contentY, CONTROL_PANEL_WIDTH, contentHeight);
            DrawControlPanel(controlPanelRect);
            
            // Right Timeline
            float timelineX = CONTROL_PANEL_WIDTH + SPACING;
            float timelineWidth = inRect.width - timelineX;
            Rect timelineRect = new Rect(timelineX, contentY, timelineWidth, contentHeight);
            DrawTimeline(timelineRect);
            
            // Handle drag end
            if (Event.current.type == EventType.MouseUp && isDragging)
            {
                isDragging = false;
                Event.current.Use();
            }
        }
    }
}
```

### Вміст, який потрібно видалити
- Усі методи, пов’язані з `DrawTopBar`, → уже у файлі TopBar
- Усі методи, пов’язані з `DrawControlPanel`, → уже у файлі Controls
- Усі методи, пов’язані з `DrawTimeline`, → уже у файлі Timeline
- Усі методи, пов’язані з `SummarizeMemories` тощо, → уже у файлі Actions
- Усі методи, пов’язані з `ExportMemories` тощо, → уже у файлі ImportExport
- Усі методи, пов’язані з `GetFilteredMemories` тощо, → уже у файлі Utilities

---

## ?? Примітки

1. **Не видаляйте оголошення полів** — усі поля private потрібно зберегти в основному файлі
2. **Не видаляйте константи** — TOP_BAR_HEIGHT, CONTROL_PANEL_WIDTH тощо потрібно зберегти
3. **Збережіть DoWindowContents** — це точка входу, її потрібно залишити в основному файлі
4. **Резервна копія перед компіляцією** — рекомендується спочатку зафіксувати поточний стан у Git

---

## ?? Поширені запитання

### Помилка компіляції: метод не знайдено
**Причина**: метод переміщено до іншого файлу, але головний файл усе ще його викликає  
**Рішення**: перевірте, чи міститься цей метод у файлі partial; partial class об’єднується автоматично

### Помилка компіляції: поле не визначено
**Причина**: визначення поля було помилково видалено  
**Рішення**: відновіть визначення поля в головному файлі

### Помилка під час виконання: NullReferenceException
**Причина**: поле не ініціалізовано  
**Рішення**: перевірте конструктор і код ініціалізації полів

---

**Останнє оновлення**: 2025-01-XX  
**Стан**: ? Усі файли частин створено, очікується очищення головного файлу
