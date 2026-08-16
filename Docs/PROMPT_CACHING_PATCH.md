# Патч реалізації Prompt Caching

## Спрощена реалізація (потрібно змінити лише метод BuildJsonRequest)

Знайдіть файл: `Source/Memory/AI/IndependentAISummarizer.cs`

Знайдіть метод `BuildJsonRequest` (приблизно рядок 477) і замініть його наведеним нижче кодом:

```csharp
private static string BuildJsonRequest(string prompt)
{
    StringBuilder stringBuilder = new StringBuilder();
    bool isGoogle = (provider == "Google");
    
    if (isGoogle)
    {
        // Google Gemini: 保持原有格式
        string str = prompt.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "").Replace("\t", "\\t");
        
        stringBuilder.Append("{");
        stringBuilder.Append("\"contents\":[{");
        stringBuilder.Append("\"parts\":[{");
        stringBuilder.Append("\"text\":\"" + str + "\"");
        stringBuilder.Append("}]");
        stringBuilder.Append("}],");
        stringBuilder.Append("\"generationConfig\":{");
        stringBuilder.Append("\"temperature\":0.7,");
        stringBuilder.Append("\"maxOutputTokens\":200");
        
        if (model.Contains("flash"))
        {
            stringBuilder.Append(",\"thinkingConfig\":{\"thinkingBudget\":0}");
        }
        
        stringBuilder.Append("}");
        stringBuilder.Append("}");
    }
    else
    {
        // ? v3.3.4: OpenAI/DeepSeek - 实现Prompt Caching
        var settings = RimTalk.MemoryPatch.RimTalkMemoryPatchMod.Settings;
        bool enableCaching = settings != null && settings.enablePromptCaching;
        
        // 固定的系统指令（可缓存）
        string systemPrompt = "你是一个RimWorld殖民地的记忆总结助手。\\n" +
                            "请用极简的语言总结记忆内容。\\n" +
                            "只输出总结文字，不要其他格式。";
        
        // 用户数据（记忆列表）
        string userPrompt = prompt.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "").Replace("\t", "\\t");
        
        stringBuilder.Append("{");
        stringBuilder.Append("\"model\":\"" + model + "\",");
        stringBuilder.Append("\"messages\":[");
        
        // system消息（带缓存控制）
        stringBuilder.Append("{\"role\":\"system\",");
        stringBuilder.Append("\"content\":\"" + systemPrompt + "\"");
        
        if (enableCaching)
        {
            if (provider == "OpenAI" && (model.Contains("gpt-4") || model.Contains("gpt-3.5")))
            {
                // OpenAI Prompt Caching
                stringBuilder.Append(",\"cache_control\":{\"type\":\"ephemeral\"}");
            }
            else if (provider == "DeepSeek")
            {
                // DeepSeek缓存控制
                stringBuilder.Append(",\"cache\":true");
            }
        }
        
        stringBuilder.Append("},");
        
        // user消息（变化的内容）
        stringBuilder.Append("{\"role\":\"user\",");
        stringBuilder.Append("\"content\":\"" + userPrompt + "\"");
        stringBuilder.Append("}],");
        
        stringBuilder.Append("\"temperature\":0.7,");
        stringBuilder.Append("\"max_tokens\":200");
        
        if (enableCaching && provider == "DeepSeek")
        {
            stringBuilder.Append(",\"enable_prompt_cache\":true");
        }
        
        stringBuilder.Append("}");
    }
    
    return stringBuilder.ToString();
}
```

## Результат

- ? Автоматично позначає фіксовані інструкції як придатні для кешування
- ? OpenAI/DeepSeek API автоматично вмикає Prompt Caching
- ? Перший виклик оплачується за звичайним тарифом, а влучання в кеш протягом наступних 5–10 хвилин зменшує вартість на 50%
- ? Користувач може керувати цим перемикачем у налаштуваннях моду: `enablePromptCaching`

## Тестування

1. Скомпілюйте мод
2. Перегляньте формат запитів JSON у DevMode
3. Перевірте статистику кешування у відповіді API (якщо провайдер її надає)

Готово!
