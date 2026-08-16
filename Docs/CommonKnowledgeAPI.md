# RimTalk Загальнодоступна документація API бази знань

## ?? Огляд

`CommonKnowledgeAPI` надає повний інтерфейс для роботи з базою здорового глузду, дозволяючи іншим модифікаціям легко додавати, оновлювати й керувати такими даними.

## ?? Швидкий початок роботи

### 1. Додайте залежність

Додайте посилання у файлі `.csproj` вашої модифікації:

```xml
<Reference Include="RimTalkMemoryPatch">
  <HintPath>..\..\RimTalk-ExpandMemory\1.6\Assemblies\RimTalkMemoryPatch.dll</HintPath>
  <Private>False</Private>
</Reference>
```

### 2. Підключіть простір імен

```csharp
using RimTalk.Memory;
```

## ?? Довідка API

### Додати загальні знання

#### Просте додавання

```csharp
// 添加一条简单的常识
string id = CommonKnowledgeAPI.AddKnowledge(
    tag: "世界观,边缘世界",
    content: "这是一个科技倒退的时代，人类文明散落在星系各处",
    importance: 0.7f  // 重要性 0-1，默认 0.5
);
```

#### Розширене додавання

```csharp
// 添加一条带完整参数的常识
string id = CommonKnowledgeAPI.AddKnowledgeEx(
    tag: "规则,对话",
    content: "你必须用中文回复，保持角色扮演",
    importance: 0.9f,
    matchMode: KeywordMatchMode.All,  // All=所有标签必须匹配，Any=任意一个即可
    targetPawnId: -1,  // -1=全局，其他=仅对特定Pawn有效
    canBeExtracted: true,  // 是否可以被提取（用于常识链）
    canBeMatched: true     // 是否可以被匹配（用于常识链）
);
```

#### Масове додавання

```csharp
var knowledgeList = new List<(string tag, string content)>
{
    ("世界观,科技", "光速引擎已经失传"),
    ("世界观,社会", "机械体和人类共存"),
    ("规则,语气", "使用幽默的语气")
};

int count = CommonKnowledgeAPI.AddKnowledgeBatch(knowledgeList, importance: 0.6f);
Log.Message($"成功添加 {count} 条常识");
```

### Оновити загальні знання

```csharp
// 更新内容
bool success = CommonKnowledgeAPI.UpdateKnowledge(id, "新的内容");

// 更新标签
bool success = CommonKnowledgeAPI.UpdateKnowledgeTag(id, "新标签");

// 更新重要性
bool success = CommonKnowledgeAPI.UpdateKnowledgeImportance(id, 0.8f);

// 启用/禁用
bool success = CommonKnowledgeAPI.SetKnowledgeEnabled(id, false);
```

### Запит загальних знань

```csharp
// 根据ID查找
CommonKnowledgeEntry entry = CommonKnowledgeAPI.FindKnowledgeById("ck-abc123");

// 根据标签查找（支持部分匹配）
List<CommonKnowledgeEntry> entries = CommonKnowledgeAPI.FindKnowledge("世界观");

// 根据内容查找
List<CommonKnowledgeEntry> entries = CommonKnowledgeAPI.FindKnowledgeByContent("光速");

// 获取所有常识
List<CommonKnowledgeEntry> allEntries = CommonKnowledgeAPI.GetAllKnowledge();

// 获取常识数量
int count = CommonKnowledgeAPI.GetKnowledgeCount();
```

### Видалити загальні знання

```csharp
// 根据ID删除
bool success = CommonKnowledgeAPI.RemoveKnowledge(id);

// 根据标签删除所有匹配的
int count = CommonKnowledgeAPI.RemoveKnowledgeByTag("旧标签");

// 清空所有常识（危险操作！）
bool success = CommonKnowledgeAPI.ClearAllKnowledge();
```

### Імпортувати / Експортувати

```csharp
// 导出为文本
string text = CommonKnowledgeAPI.ExportToText();

// 从文本导入
int count = CommonKnowledgeAPI.ImportFromText(text, clearExisting: false);
```

### Статистика

```csharp
KnowledgeStats stats = CommonKnowledgeAPI.GetStats();
Log.Message($"总数: {stats.TotalCount}");
Log.Message($"启用: {stats.EnabledCount}");
Log.Message($"禁用: {stats.DisabledCount}");
Log.Message($"用户编辑: {stats.UserEditedCount}");
Log.Message($"全局常识: {stats.GlobalCount}");
Log.Message($"Pawn专属: {stats.PawnSpecificCount}");
```

## ?? Варіанти використання

### Варіант 1: додавання правил для конкретного мода

```csharp
public class MyModInitializer : Mod
{
    public MyModInitializer(ModContentPack content) : base(content)
    {
        // 在 Mod 加载时添加规则
        LongEventHandler.QueueLongEvent(() =>
        {
            CommonKnowledgeAPI.AddKnowledge(
                tag: "规则,MyMod",
                content: "你是一个魔法世界的角色，可以使用魔法技能",
                importance: 0.9f
            );
        }, "InitializingMyMod", false, null);
    }
}
```

### Варіант 2: динамічне оновлення подій

```csharp
public class MyEventHandler
{
    private string knowledgeId;

    public void OnEventStart()
    {
        // 事件开始时添加常识
        knowledgeId = CommonKnowledgeAPI.AddKnowledge(
            tag: "事件,活跃,MyEvent",
            content: "当前正在进行魔法仪式，需要保持安静",
            importance: 0.8f
        );
    }

    public void OnEventEnd()
    {
        // 事件结束时删除常识
        if (!string.IsNullOrEmpty(knowledgeId))
        {
            CommonKnowledgeAPI.RemoveKnowledge(knowledgeId);
        }
    }
}
```

### Варіант 3: загальні знання для пішака

```csharp
public void AddPawnSpecificKnowledge(Pawn pawn)
{
    // 为特定 Pawn 添加专属常识
    CommonKnowledgeAPI.AddKnowledgeEx(
        tag: $"角色背景,{pawn.Name.ToStringShort}",
        content: $"{pawn.Name.ToStringShort} 曾经是一名传奇法师",
        importance: 0.7f,
        targetPawnId: pawn.thingIDNumber  // 只对这个 Pawn 有效
    );
}
```

### Варіант 4: масове керування

```csharp
public void InitializeQuestKnowledge()
{
    // 查找并删除旧的任务常识
    int removed = CommonKnowledgeAPI.RemoveKnowledgeByTag("任务,旧");
    
    // 添加新的任务常识
    var questKnowledge = new List<(string, string)>
    {
        ("任务,活跃", "需要收集10个魔法水晶"),
        ("任务,活跃", "避免在夜晚外出"),
        ("任务,活跃", "保护村庄免受怪物攻击")
    };
    
    int added = CommonKnowledgeAPI.AddKnowledgeBatch(questKnowledge, 0.8f);
    Log.Message($"任务常识已更新: 删除 {removed} 条，添加 {added} 条");
}
```

## ?? Примітки

### 1. Міркування щодо продуктивності

- Уникайте виклику операцій запиту на кожному кадрі
- Пакетні операції кращі за одиничні
- Кешуйте результати запитів

```csharp
// ? 不好的做法
public override void Tick()
{
    var entries = CommonKnowledgeAPI.FindKnowledge("世界观"); // 每帧查询
}

// ? 好的做法
private List<CommonKnowledgeEntry> cachedEntries;
private int lastUpdateTick = 0;

public override void Tick()
{
    if (Find.TickManager.TicksGame - lastUpdateTick > 2500) // 每小时更新一次
    {
        cachedEntries = CommonKnowledgeAPI.FindKnowledge("世界观");
        lastUpdateTick = Find.TickManager.TicksGame;
    }
}
```

### 2. Рекомендації щодо іменування тегів

**Рекомендований формат** (із розділенням комами):
- ? `"规则,对话"` - Чітко й стисло
- ? `"世界观,科技,光速"` - Кілька тегів
- ? `"MyMod,规则,魔法"` - Із простором імен

**Формати, яких слід уникати**:
- ? `"规则-世界观"` - Хоча підтримується, не рекомендується
- ? `"rule1"` - Беззмістовна назва тегу

**Принципи іменування тегів**:
1. **Використовуйте змістовні теги** - для полегшення розуміння та пошуку
2. **Використовуйте розділення комами** - стандартний роздільник, чітко й зрозуміло
3. **Додайте простір імен** — щоб уникнути конфліктів з іншими модами (наприклад, `"MyMod,规则"`) 
4. **Будьте лаконічними** — мітки не мають бути надто довгими

**Рекомендовані категорії міток**:
| Призначення | Рекомендований формат | Приклад |
|------|----------|------|
| **Правила** | `规则,子分类` | `规则,对话` `规则,行为` |
| **Світобудова** | `世界观,方面` | `世界观,科技` `世界观,历史` |
| **Персонажі** | `角色背景,名字` | `角色背景,张三` |
| **Події** | `事件,活跃` або `事件,历史` | `事件,活跃,袭击` |
| **Стани** | `状态,分类` | `状态,健康` `状态,情绪` |

### 3. Налаштування важливості

| Важливість | Опис | Приклад |
|--------|------|------|
| 0.9-1.0 | Ключові правила, яких необхідно дотримуватися | Правила гри, вимоги до рольової гри |
| 0.7-0.8 | Важлива інформація | Налаштування світу, активні події |
| 0.5-0.6 | Загальна інформація | Передісторія, довідкові матеріали |
| 0.3-0.4 | Необов’язкова інформація | Деталі опису, великодки |
| 0.1-0.2 | Низький пріоритет | Неважливі підказки |

### 4. Функція ланцюжка загальних знань

Якщо ви хочете, щоб загальні знання підтримували ланцюжкове зіставлення (одне загальне знання запускає інше):

```csharp
CommonKnowledgeAPI.AddKnowledgeEx(
    tag: "魔法,火系",
    content: "火系魔法威力强大但容易失控",
    importance: 0.7f,
    canBeExtracted: true,  // ? 允许提取：这条常识的内容可以用于触发其他常识
    canBeMatched: true     // ? 允许匹配：其他常识的内容可以触发这条常识
);
```

## ?? Усунення несправностей

### 1. Загальні знання не застосовуються

Перевірте:
- Чи ввімкнено загальні знання (`isEnabled = true`)?
- Чи правильно зіставлено теги
- Чи не надто низька важливість

```csharp
var entry = CommonKnowledgeAPI.FindKnowledgeById(id);
if (entry != null)
{
    Log.Message($"Enabled: {entry.isEnabled}");
    Log.Message($"Tag: {entry.tag}");
    Log.Message($"Importance: {entry.importance}");
}
```

### 2. Не вдається знайти загальні знання

```csharp
// 检查常识是否存在
bool exists = CommonKnowledgeAPI.ExistsKnowledge(id);
if (!exists)
{
    Log.Warning($"Knowledge not found: {id}");
}
```

### 3. API повертає null

```csharp
// 检查游戏状态
if (Current.Game == null)
{
    Log.Warning("Game not loaded yet!");
    return;
}

// 确保在主线程调用
LongEventHandler.QueueLongEvent(() =>
{
    var id = CommonKnowledgeAPI.AddKnowledge("test", "test content");
}, "AddingKnowledge", false, null);
```

## ?? Повний приклад

```csharp
using RimTalk.Memory;
using Verse;

namespace MyMod
{
    public class MyKnowledgeManager
    {
        private Dictionary<string, string> knowledgeIds = new Dictionary<string, string>();

        public void Initialize()
        {
            // 添加基础规则
            string ruleId = CommonKnowledgeAPI.AddKnowledgeEx(
                tag: "规则,MyMod",
                content: "你是一个魔法世界的角色",
                importance: 0.9f,
                canBeExtracted: false,
                canBeMatched: false
            );
            knowledgeIds["rule"] = ruleId;

            // 添加世界观
            var worldKnowledge = new List<(string, string)>
            {
                ("世界观,魔法", "魔法能量来自月光"),
                ("世界观,种族", "精灵族擅长治疗魔法"),
                ("世界观,历史", "古代文明已经消失")
            };
            CommonKnowledgeAPI.AddKnowledgeBatch(worldKnowledge, 0.7f);

            // 输出统计
            var stats = CommonKnowledgeAPI.GetStats();
            Log.Message($"[MyMod] Initialized {stats.TotalCount} knowledge entries");
        }

        public void OnEventStart(string eventName, string description)
        {
            // 添加事件常识
            string eventId = CommonKnowledgeAPI.AddKnowledge(
                tag: $"事件,活跃,{eventName}",
                content: description,
                importance: 0.8f
            );
            knowledgeIds[eventName] = eventId;
        }

        public void OnEventEnd(string eventName)
        {
            // 删除事件常识
            if (knowledgeIds.TryGetValue(eventName, out string eventId))
            {
                CommonKnowledgeAPI.RemoveKnowledge(eventId);
                knowledgeIds.Remove(eventName);
            }
        }

        public void Cleanup()
        {
            // 清理所有 MyMod 相关的常识
            int removed = CommonKnowledgeAPI.RemoveKnowledgeByTag("MyMod");
            Log.Message($"[MyMod] Removed {removed} knowledge entries");
        }
    }
}
```

## ?? Пов’язані посилання

- [RimTalk GitHub](https://github.com/sanguodxj-byte/RimTalk-ExpandMemory)
- [Опис категорій бази загальних знань](#常识库分类系统)
- [API журнал змін](../CHANGELOG.md)

## ?? Система категоризації бази загальних знань

### Правила автоматичної категоризації

База загальних знань автоматично розподіляє записи по різних вкладках відповідно до тегів: достатньо, щоб тег **містив** тег категорії:

| Категорія | Теги для збігу | Пріоритет |
|------|----------|--------|
| **Правила/Інструкції** | `规则` `instructions` `instruction` `rule` | 1 (найвищий)|
| **Стан колоністів** | `殖民者状态` `pawnstatus` `colonist` `状态` | 2 |
| **Історія** | `历史` `history` `past` `记录` | 3 |
| **Світогляд** | `世界观` `lore` `background` `背景` `设定` | 4 |
| **Інше** | Не містить жодного з наведених тегів | 5 (за замовчуванням)|

### Приклади категоризації

Усі наведені нижче теги буде правильно категоризовано:

```csharp
// 规则类（优先级最高）
"规则"              → 规则分类
"规则,对话"         → 规则分类
"常识规则"          → 规则分类（包含"规则"）
"Instructions"      → 规则分类

// 世界观类
"世界观"            → 世界观分类
"世界观,科技"       → 世界观分类
"背景设定"          → 世界观分类（包含"背景"）

// 状态类
"殖民者状态"        → 状态分类
"状态,健康"         → 状态分类
"PawnStatus"        → 状态分类

// 历史类
"历史"              → 历史分类
"历史记录"          → 历史分类
"History"           → 历史分类
```

### Пояснення пріоритетів

Якщо один тег одночасно містить кілька тегів категорій, категорію буде визначено за пріоритетом:

```csharp
"规则,世界观"       → 规则分类（规则优先级1，世界观优先级4）
"世界观,历史"       → 历史分类（历史优先级3，世界观优先级4）
"状态,历史"         → 状态分类（状态优先级2，历史优先级3）
```

**Рекомендація**: якщо потрібно віднести загальне знання до певної категорії, просто додайте до тегу відповідний тег категорії.

## ?? Ліцензія

Цей API дотримується умов ліцензії RimTalk.
