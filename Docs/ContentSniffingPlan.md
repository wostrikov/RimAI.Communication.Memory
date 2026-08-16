# План реалізації аналізу вмісту

## Передумови
Наразі модифікація `RimTalk-ExpandMemory` автоматично вставляє типовий `PromptEntry`, що містить контекст пам’яті та знань, в активний пресет RimTalk. 
Однак коли користувачі перемикаються на сильно налаштовані або розширені пресети, які вже містять пам’ять/knowledge variables (e.g., `{{pawn.memory}}` або `{{knowledge}}`), автоматична вставка стає зайвою, спричиняючи марне витрачання токенів і потенційну плутанину з AI.

## Мета
Реалізувати механізм «евристичного аналізу вмісту». Перед вставкою типового запису модифікація просканує наявні записи в активному пресеті. Якщо буде виявлено, що пресет уже використовує змінні пам’яті або знань, процес автоматичної вставки буде безпечно перервано.

## Цільовий файл
`Source/API/RimTalkAPIIntegration.cs`

## Деталі реалізації

### 1. Визначення ключових слів для аналізу
Ми визначимо масив критичних підрядків, які вказують, що пресет уже обробляє пам’ять/knowledge.
```csharp
private static readonly string[] SniffingKeywords = new string[]
{
    "pawn.memory", "p.memory",
    "pawn.ABM", "p.ABM",
    "pawn.ELS", "p.ELS",
    "pawn.CLPA", "p.CLPA",
    "{{knowledge}}", "{{ knowledge }}",
    "knowledge_grouped", "knowledge_rules", 
    "knowledge_lore", "knowledge_status", 
    "knowledge_history"
};
```

### 2. Зміна логіки `RegisterPromptEntry`
У методі `RegisterPromptEntry` ми додамо крок для отримання всіх записів із `ActivePreset` і виконання сканування вмісту.

**Поточна логіка:**
1. Отримати ActivePreset.
2. Спробувати знайти наявний запис моду за детермінованим ID.
3. Якщо запис знайдено, оновити його вміст і повернути результат.
4. Якщо запис не знайдено, створити та вставити новий запис.

**Нова логіка:**
1. Отримати ActivePreset.
2. Спробувати знайти наявний запис моду за детермінованим ID.
3. Якщо запис знайдено, оновити його вміст і повернути результат.
4. **[NEW]** Якщо запис не знайдено, отримати список усіх наявних entries (`Entries` властивість /field) з ActivePreset.
5. **[NEW]** Перебрати `Content` кожного запису (ігноруючи нульові або порожні рядки).
6. **[NEW]** Перевірити, чи містить `Content` будь-яке з `SniffingKeywords`.
7. **[NEW]** Якщо ключове слово знайдено, вивести message (e.g., `"[MemoryPatch] Detected custom memory variables in active preset. Skipping auto-injection."`) і `return`, щоб перервати вставлення.
8. Якщо ключових слів не знайдено, продовжити створення та вставлення нового запису як зазвичай.

### 3. Реалізація рефлексії для сумісності
Оскільки типи RimTalk визначаються через reflection (`_promptAPIType`, `_promptPresetType` тощо), логіка розпізнавання також має використовувати рефлексію для доступу до списку `Entries` і `Content` кожного елемента.

```csharp
// Example conceptual code for sniffing via reflection:
var entriesField = preset.GetType().GetField("Entries");
if (entriesField != null)
{
    var entriesList = entriesField.GetValue(preset) as System.Collections.IEnumerable;
    if (entriesList != null)
    {
        foreach (var entry in entriesList)
        {
            var contentProp = entry.GetType().GetProperty("Content") ?? entry.GetType().GetField("Content");
            string content = contentProp?.GetValue(entry) as string;
            
            if (!string.IsNullOrEmpty(content))
            {
                foreach (var keyword in SniffingKeywords)
                {
                    if (content.Contains(keyword))
                    {
                        // Match found, skip injection
                        Log.Message($"[MemoryPatch] Detected custom memory variable '{keyword}' in preset. Skipping auto-injection.");
                        return;
                    }
                }
            }
        }
    }
}
```

## Переваги
- **Нульове налаштування:** Гравцям не потрібно вручну перемикати параметри під час переходу між базовими та розширеними пресетами.
- **Висока сумісність:** Працює лише на основі перевірки рядків, не залежачи від конкретної структури чи правил іменування користувацьких пресетів.
- **Без руйнівних змін:** Якщо користувач видалить користувацькі записи, що містять змінні, мод виявить їхню відсутність після наступного перезапуску й автоматично додасть резервний запис за замовчуванням.
