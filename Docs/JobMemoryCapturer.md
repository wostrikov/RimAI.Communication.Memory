# JobMemoryCapturer Архітектурний підсумок: подієва архітектура зі знімками та двома Hook

## 1. Семантичне виправлення: пам’ять записує те, що «вже сталося», а не те, що «ось-ось станеться»

Суть цього рефакторингу — виправити саму семантику захоплення: **пам’ять має записувати те, що вже сталося, а не те, що ось-ось станеться**.

Для цього момент захоплення змінено з `StartJob` (початок роботи) на `CleanupCurrentJob` (завершення роботи) — записувати роботу в пам’ять лише після її фактичного завершення. `StartJob`(Postfix) перетворюється на суто момент **попереднього вилучення інформації**: на початку роботи, коли цільовий Thing гарантовано існує, витягнути report/targetName у кеш для використання в `CleanupCurrentJob`(Prefix), водночас усуваючи стару проблему зі зниженням `GetReport`, коли цільовий об’єкт Discarded.

У старій архітектурі пам’ять створювалася вже в `StartJob`, тобто семантично записувався «виданий намір», що спричиняло низку недоліків: після паузи та мікрокерування Thing /скасування в той самий момент породжувалися примарні спогади, негайно перервані роботи створювали миттєві записи, ціль згодом могла бути Discard, через що звіт деградував тощо. У старій архітектурі ці проблеми виправлялися окремо (наприклад, «спочатку записати, потім Remove привид»), але цей рефакторинг не спрямований на жодну з них — **після виправлення семантики наведені недоліки зникають природним чином**. Робота з однаковим часом початку й завершення не записується, оскільки «фактично не відбулася» (див. §7); це природний наслідок цієї семантики, а не спеціальний механізм очищення.

---

## 2. Основна архітектура

- **Два Hook**: `StartJob`(Postfix) попередньо вилучає інформацію → `CleanupCurrentJob`(Prefix) використовує кеш для створення спогаду.
- **Перетворення на POCO**: `JobMemoryCapturer` не успадковує `ThingComp`, а безпосередньо утримується `FourLayerMemoryComp`; старий `WorkSessionAggregator` (-500 рядків) видалено, що усуває накладні витрати на ін’єкцію компонентів.
- **Нульове heartbeat**: залежності від `CompTick` / `WorldComponentTick` повністю усунуто; переходи станів керуються подіями, без фонового опитування.
- **Керування даними**: три статичні набори `_jobsToIgnore` / `_dictAggregatedJobToDesc` / `_dictJobToImportance` одноразово ініціалізуються у статичному конструкторі через зіставлення з шаблоном `switch`; Regex і хаос if-else вилучено, розпізнавання роботи та вилучення цілі O(1).
- **Конвеєр із трьома гілками**: точне об’єднання / нечітка агрегація / створення нового; гілка агрегації перезаписує `_lastJobMemory` на місці, без виділення пам’яті.

---

## 3. Часова послідовність Hook і механізм знімків

### Робочий процес із двома Hook (коли нова робота замінює стару)

```
Pawn_JobTracker.StartJob(newJob)
  ├─ CleanupCurrentJob(oldJob)            ← 清理旧工作
  │   └─ [Prefix] BuildJobMemoryEnter(oldJob)  → 消费缓存生成记忆
  ├─ curJob = newJob                      ← 切换
  └─ [Postfix] ExtractJobInfoEnter(newJob)     → 预提取信息入缓存
```

Ключова гарантія часової послідовності: `BuildJobMemory` старої роботи виконується раніше за `ExtractJobInfo` нової роботи — оскільки `CleanupCurrentJob` викликається до `curJob = newJob`. Це гарантує, що `BuildJobMemory(old)` читає кеш, який усе ще містить результат вилучення для old, і його не буде перезаписано new.

> Коли робота завершується природно (не через заміну), `CleanupCurrentJob` так само викликається, і Prefix спрацьовує; цього разу нова робота не йде слідом, кеш після споживання `BuildJobMemory` зберігає старе значення до перезапису Postfix наступного `StartJob`.

### Режим кешу знімка

```
ExtractJobInfo(job)                     BuildJobMemory(job)
  ├─ CurJob = job (WeakRef)             ├─ GetJobReportCached(job)
  ├─ _curJobReport    = GetReport(job)  │   → CurJob==job ? _curJobReport    : 降级提取
  └─ _curJobTargetAName = GetName(job)  └─ GetTargetANameCached(job)
                                          → CurJob==job ? _curJobTargetAName : 降级提取
```

`CurJob` зберігається через `WeakReference<Job>`, а протягом робочого циклу чинність кешу перевіряється порівнянням посилань. Запасний шлях спрацьовує, коли об’єкт Job змінюється (або під час першої роботи після завантаження збереження), і повторно безпосередньо отримує дані через `job.GetReport(parentPawn)` — ціль у цей момент може бути Discarded, тому текст звіту деградує, але гра не аварійно завершується.

---

## 4. Опускання шлюзу

Рівень Patch зводиться до однорядкового делегування, а всі перевірки зосереджено в статичному методі Enter `JobMemoryCapturer`:

```csharp
// StartJob Postfix —— 信息预提取
[HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
public static class Pawn_JobTracker_StartJob_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Job ___curJob, Pawn ___pawn)
        => JobMemoryCapturer.ExtractJobInfoEnter(___curJob, ___pawn);
}

// CleanupCurrentJob Prefix —— 记忆生成
[HarmonyPatch(typeof(Pawn_JobTracker), "CleanupCurrentJob")]
public static class Pawn_JobTracker_CleanupCurrentJob_Patch
{
    [HarmonyPrefix]
    public static void Prefix(Job ___curJob, Pawn ___pawn)
        => JobMemoryCapturer.BuildJobMemoryEnter(___curJob, ___pawn);
}
```

Шлюз уніфіковано обробляє: чинність job, відповідність набору ігнорування, статус colonist, перемикач Settings; `BuildJobMemoryEnter` додатково виконує блокування в тому самому кадрі (див. §7). `ExtractJobInfoEnter` не виконує блокування в тому самому кадрі — саме отримання даних нешкідливе, кеш роботи з нульовою тривалістю природно перезаписується наступним `StartJob`, а `BuildJobMemoryEnter` має власний запасний пропуск того самого моменту.

---

## 5. Конвеєр розподілу на три гілки

```
BuildJobMemory(job)
  ├─ 【精确合并】  条件: TargetNames.Count ≤ 1 且 report 逐字相等 且 时间窗口内
  │   行为: 就地覆写 _lastJobMemory，不创建新对象
  │   例:  连续对同一块矿脉采矿 → "连续5次采矿"
  │
  ├─ 【模糊聚合】  条件: 白名单命中 且 聚合描述匹配 且 时间窗口内
  │   行为: 就地覆写 _lastJobMemory，追加新目标名到 TargetNames
  │   例:  搬运钢铁、大米、木头 → "两小时内搬运了3次钢铁、大米、木头等"
  │
  └─ 【新建】      条件: 前两条均不命中 (不同类工作/超时/首次记录)
      行为: 创建新 MemoryEntry，Add 到 ActiveMemories，重置会话
```

### Матриця визначення

| Умова | Точне об’єднання | Нечітке агрегування | Створення нового |
|------|:---:|:---:|:---:|
| `TargetNames.Count ≤ 1` | **Властиве** | — | — |
| Текст `report` однаковий | **Властиве** | — | — |
| У білому списку знайдено `ableToAggregate` | — | **Властиве** | — |
| `jobAggregateDesc == _lastJobAggregateDesc` | — | **Властиве** | — |
| `sharedCondition()` (часове вікно + наявна пам’ять) | Оцінюється наприкінці | Оцінюється наприкінці | — |

### Переваги: без повторних перевірок і без повторних важких операцій

Механізм розгалуження гарантує, що **протягом усього виклику `BuildJobMemory` будь-яка дорога операція виконується не більш як один раз, а будь-яке спільне рішення обчислюється не більш як один раз**:

- `sharedCondition()`: кешування `bool?` + одноразове обчислення `??=`, точний і нечіткий конвеєри спільно використовують той самий результат.
- `getReport()`: одноразове обчислення `string ??=`, `GetReport` (потенційно дорога операція) запускається не більш як один раз.
- У ланцюжку `&&` дешеві перевірки виконуються першими: легкі перевірки на кшталт `TargetNames.Count` / `ableToAggregate` / порівняння опису — спочатку, а дорогі операції на кшталт `getReport()` / `sharedCondition()` обчислюються лише після успішного проходження попередніх перевірок.
- Витягування цілей виконується лише за умови збігу з білим списком (так само і в `ExtractJobInfo`, і в `StartNewSession`), тому для неагрегувальної роботи витрати на витягування нульові.
- У новому конвеєрі `newReport = getReport()` обчислюється явно, після чого повторно використовується для створення `MemoryEntry` і `StartNewSession`, без повторного витягування.

---

## 6. Перезапис на місці та приріст ваги

Гілка агрегації не створює нового `MemoryEntry`, а безпосередньо змінює `_lastJobMemory`:

```csharp
void updateMemoryBase() {
    _lastJobMemory.GameTick = Find.TickManager.TicksGame;
    _lastJobMemory.Importance += ImportanceIncrement();
}
// 精确合并: _lastJobMemory.Content = BuildExactContent();
// 模糊聚合: _lastJobMemory.Content = BuildFuzzyContent();
```

Нуль запитів до компонентів, нуль серіалізацій, нуль виділень пам’яті. `MemoryEntry.Importance` у setter на фізичному рівні `Math.Clamp(0, 1)`, запобігаючи переповненню.

Приріст ваги: +0.02 щогодини, у перші 20 разів додатково по +0.01.

---

## 7. Скасування в тому самому кадрі (пропуск нульової тривалості)

У `BuildJobMemoryEnter`: якщо `currentTick == job.startTick`, це означає, що job почався й завершився в ту саму мить і насправді не виконувався; запис безпосередньо `return` пропускається.

Це природний висновок із семантики §1 — «те, чого не сталося, не записується», — а не спеціальний механізм очищення примар. У старій архітектурі запис виконувався вже в `StartJob`, тому робота нульової тривалості спочатку потрапляла до сховища, а потім видалялася через `Remove`; у новій архітектурі вона взагалі не потрапляє до сховища, тож логіка Remove у старій версії більше не потрібна.

---

## 8. Інтелектуальне витягування цілей

```csharp
if (targetThing == Parent) return "自己";                              // 自目标
if (targetThing is Blueprint or Frame)                                 // 蓝图/框架
    return targetThing.def?.entityDefToBuild?.label ?? string.Empty;
return targetThing.LabelShort ?? targetThing.def?.label ?? string.Empty;  // 普通
```

Викликається під час `StartJob`; цільовий Thing гарантовано живий, тому hack зі станом Discard не потрібен. Витягування виконується лише для білих списків агрегувальної роботи, а для неагрегувальної роботи пропускається задля економії ресурсів. `HashSet<string> TargetNames` автоматично усуває дублікати.

---

## 9. Діаграма архітектури конвеєра

```
======================== FourLayerMemoryComp → [JobMemoryCapturer (POCO)] ========================

  [Patch: StartJob Postfix]              [Patch: CleanupCurrentJob Prefix]
        │                                         │
        v                                         v
  ExtractJobInfoEnter(job, pawn)          BuildJobMemoryEnter(job, pawn)
        │ 闸门: job/def/ignore/                 │ 闸门: job/def/ignore/
        │       colonist/settings                │       startTick≠currentTick/
        │                                         │       colonist/settings
        v                                         v
  ExtractJobInfo(job)                     BuildJobMemory(job)
  (缓存: CurJob / report / targetAName)          │
                                         ┌───────┴───────┐
                                         v 精确合并       v 模糊聚合
                                   report== 且 单目标   白名单== 且 同desc
                                         │               │
                                         │ return        │ return
                                         └───────┬───────┘
                                                 v (都不命中)
                                            ┌──────────┐
                                            │ 新建     │
                                            │ new Mem  │
                                            │ Add+Sess │
                                            └──────────┘
```
