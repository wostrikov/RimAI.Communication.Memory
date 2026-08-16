# Конвеєр потокового захоплення RoundMemory

## Огляд

Окрім наявного пакетного конвеєра RoundMemory (`TalkHistory_AddMessageHistory_Patch` → Postfix `AddResponsesToHistory`), **додано** окремий конвеєр потокового захоплення. Основна ідея: щойно pawn **вимовляє** репліку, її негайно додають до RoundMemory поточної сесії, а не пакетно формують після завершення всього раунду діалогу.

## Архітектура

```
                     PromptContext_FromTalkRequest_Patch (Prefix)
                         │ request.Participants = pawns  ← {{pawns}} 集合桥接
                         ▼
GenerateTalk → pawns列表 → LLM流式 → responses入队
                                         │
                           DisplayTalk(0.5s)
                               │
                      CreateInteraction(pawn, talk)  ← 唯一 Hook
                               │
               CreateInteraction_StreamingRoundMemory_Patch (Postfix)
                               │
                     ApiHistory.GetApiLog(talk.Id).TalkRequest
                               │
                     RoundMemoryManager.StreamingBuildRoundMemory(...)
```

## Точки підключення

**Основний Hook**: `TalkService.CreateInteraction(Pawn pawn, TalkResponse talk)` — спрацьовує, коли бульбашка діалогу **фактично відображається**. Жодна відповідь, проігнорована/пропущена/така, що не пройшла Display gate, не викликає його.

**Допоміжний Hook**: `PromptContext.FromTalkRequest(TalkRequest, List<Pawn>)` — на етапі формування prompt записує початковий набір pawn з `{{pawns}}` у `talkRequest.Participants`, щоб потоковий конвеєр міг отримати список учасників, яких фактично сприймає LLM.

### Чому саме CreateInteraction

```
DisplayTalk (每0.5s)
  ├─ GATE 1: 父消息被忽略? 或 pawn 无法展示? → IgnoreTalkResponse (不入 CreateInteraction)
  ├─ GATE 2: pawn 处于危险? → IgnoreAllTalkResponses (不入 CreateInteraction)
  ├─ GATE 3: 回复间隔未到? → continue (稍后重试)
  └─ 全部通过 → CreateInteraction ★ 只有这里才"说出"
```

## Ізоляція сесій

`ConditionalWeakTable<object, RoundMemory>` використовує посилання `TalkRequest` як ключ слабкого посилання. Ключ оголошено як узагальнений `<T> where T : class`, щоб `RoundMemoryManager` не залежав безпосередньо від типу RimTalk.

Усі відповіді одного раунду `GenerateTalk` через `ApiHistory.GetApiLog(talk.Id)?.TalkRequest` розпізнаються як такі, що посилаються на один об’єкт, природно ізолюючи різні раунди/паралельні сеанси.

**Перевага слабких посилань**: коли `TalkRequest` буде зібрано збирачем сміття через GC (після споживання всіх response не залишиться інших сильних посилань), відповідний запис `RoundMemory` буде автоматично видалено, тому ручне очищення не потрібне.

## Машина станів

```
CreateInteraction → patch Postfix:
  │
  ├─ talk.Id → ApiHistory.GetApiLog → .TalkRequest → talkRequest
  │
  ├─ talkRequest.Recipient.IsPlayer() → isUserInitiate
  │
  ├─ RoundMemoryManager.StreamingBuildRoundMemory<T>(talkRequest, content, participants, isUserInitiate)
  │   │
  │   ├─ _dictToRoundMemory.TryGetValue(talkRequest, out roundMemory)?
  │   │   │
  │   │   ├─ NO → 新 session:
  │   │   │   ├─ new RoundMemory(participants, userContent)
  │   │   │   │     Content = "[对话参与者: Arrow, Bob]" (或含用户台词)
  │   │   │   ├─ _roundMemories.Add(rm)          // 加入全局缓冲
  │   │   │   ├─ 各pawn.ActiveMemories.Add(rm)    // 分发到个人
  │   │   │   └─ _dictToRoundMemory.AddOrUpdate(tr, rm)
  │   │   │
  │   │   └─ YES → 已有 session:
  │   │         └─ (跳过创建)
  │   │
  │   └─ roundMemory.AppendLine(content)  // 追加 "$name: $text"
```

## Ін’єкція реплік гравця

Власноруч введена гравцем репліка не проходить через `CreateInteraction` (`CustomDialogueService.ExecuteDialogue` безпосередньо надсилає overlay), тому через Postfix `CapturePlayerDialogue` перехоплюється `_playerDialogue` і під час створення нового RoundMemory у потоковому конвеєрі ін’єктується як initial content конструктора.

`isUserInitiate` визначає це через `talkRequest.Recipient.IsPlayer()`, точно розрізняючи сценарії «ініційовано самим гравцем» і «користувач наказав colonist сказати репліку», щоб уникнути дублювання повідомлень у другому випадку.

## Вивільнення об’єктів

| Об’єкт | Стратегія вивільнення |
|------|----------|
| `TalkRequest` (ключ dict) | Слабке посилання `ConditionalWeakTable`: GC видаляється автоматично, ручне очищення не потрібне |
| `RoundMemory` | Не вивільняється, спільно утримується кільцевим буфером `_roundMemories` і ABM pawn |
| `ApiLog` | Лише тимчасовий запит, посилання не утримує |

Резервний варіант: під час завантаження збереження `FinalizeInit()` може відновити `_dictToRoundMemory`.

## Потокобезпечність

`CreateInteraction` спрацьовує в ланцюжку `DisplayTalk()` → ігровий tick головного потоку. Читання й запис `_dictToRoundMemory` виконуються в головному потоці без блокувань. `ConditionalWeakTable` внутрішньо забезпечує потокобезпечність.

## Розв’язання залежності від RimTalk

`RoundMemoryManager` не має жодних посилань на тип RimTalk. Усі виклики RimTalk API зосереджені у двох patch:

| Рівень | Файл | Залежність від RimTalk |
|----|------|-------------|
| Ядро | `RoundMemoryManager.cs` | **Нуль** |
| Міст | `CreateInteraction_StreamingRoundMemory_Patch.cs` | `ApiHistory`, `TalkResponse`, `RimTalkMemoryPatchMod` |
| Міст | `PromptContext_FromTalkRequest_Patch.cs` | `PromptContext`, `TalkRequest` |

## Перелік файлів

| Файл | Призначення |
|------|------|
| `Source/Memory/RoundMemory/RoundMemory.cs` | Додано `AppendLine()`, конструктор підтримує null content; вилучено обрізання `MaxContentLength`; `GetParticipants()` перейменовано на `GetParticipantsRoster()` |
| `Source/Memory/RoundMemory/RoundMemoryManager.cs` | Додано `_dictToRoundMemory` (`ConditionalWeakTable`), `StreamingBuildRoundMemory<T>()`, `AddRoundMemory()`, `GetPlayerDialogue()`; вилучено `_playerPawn`, `MaxContentLength`; спрощено сигнатуру `BuildRoundMemory` |
| `Source/Patches/CreateInteraction_StreamingRoundMemory_Patch.cs` | Harmony Postfix: очищення тексту → `talkRequest.Recipient.IsPlayer()` визначає, чи ініційовано користувачем → делегування `StreamingBuildRoundMemory` |
| `Source/Patches/PromptContext_FromTalkRequest_Patch.cs` | Harmony Prefix: заповнення `talkRequest.Participants` |
| `Source/Patches/TalkHistory_AddMessageHistory_Patch.cs` | Старий конвеєр пакетної обробки (закоментовано) |

## Посилання на ключові зовнішні API

| API | Розташування | Призначення |
|-----|------|------|
| `ApiHistory.GetApiLog(Guid)` → `ApiLog` | `RimTalk.Data` | TalkResponse.Id → TalkRequest |
| `ApiLog.TalkRequest` | `RimTalk.Data` | Ключ сесії |
| `TalkRequest.Participants` | `RimTalk.Data` | Початковий набір учасників (заповнюється Prefix) |
| `TalkRequest.Recipient.IsPlayer()` | RimWorld | Визначає, чи ініціатором є сам гравець |
| `TalkService.CreateInteraction` | `RimTalk.Service` | Основна ціль Hook |
| `PromptContext.FromTalkRequest` | `RimTalk.Prompt` | Допоміжна ціль Hook |
