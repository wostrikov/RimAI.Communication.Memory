# Посібник із видалення функціоналу TTS

## ? Завершено

1. ? Видалити основні файли служби TTS
   - `Source/Memory/TTS/AzureTTSService.cs`
   - `Source/Memory/TTS/ITTSService.cs`
   - `Source/Memory/TTS/TTSAudioPlayer.cs`
   - `Source/Memory/TTS/TTSHelper.cs`
   - `Source/Memory/TTS/TTSManager.cs`

2. ? Видалити файли Patch TTS
   - `Source/Patches/TTSAutoPlayPatcher.cs`

3. ? Видалити документацію TTS
   - `Docs/TTS_*.md`
   - `Docs/KNOWLEDGE_TEXTAREA_FIX.md`

## ? Потрібно виконати вручну

### 1. Відредагувати `Source/RimTalkSettings.cs`

**Потрібно видалити:**

#### A. Визначення полів (приблизно рядки 51–58)
```csharp
// TTS 语音服务配置
public bool enableTTS = false;
public string ttsProvider = "Azure";
public string azureSpeechKey = "";
public string azureSpeechRegion = "eastus";
public string ttsVoice = "zh-CN-XiaoxiaoNeural";
public float ttsRate = 1.0f;
public float ttsVolume = 1.0f;
public bool ttsAutoPlay = false;
```

#### B. Стан згортання UI (приблизно рядок 97)
```csharp
private static bool expandTTSSettings = false;
```

#### C. Серіалізація ExposeData (приблизно рядки 130–138)
```csharp
Scribe_Values.Look(ref enableTTS, "tts_enableTTS", false);
Scribe_Values.Look(ref ttsProvider, "tts_provider", "Azure");
Scribe_Values.Look(ref azureSpeechKey, "tts_azureSpeechKey", "");
Scribe_Values.Look(ref azureSpeechRegion, "tts_azureSpeechRegion", "eastus");
Scribe_Values.Look(ref ttsVoice, "tts_voice", "zh-CN-XiaoxiaoNeural");
Scribe_Values.Look(ref ttsRate, "tts_rate", 1.0f);
Scribe_Values.Look(ref ttsVolume, "tts_volume", 1.0f);
Scribe_Values.Look(ref ttsAutoPlay, "tts_autoPlay", false);
```

#### D. Метод DoSettingsWindowContents (приблизно рядок 236)
Видаліть цей рядок:
```csharp
DrawCollapsibleSection(listingStandard, "?? TTS 语音服务", ref expandTTSSettings, () => DrawTTSSettings(listingStandard));
```

#### E. Метод DrawTTSSettings (увесь метод, приблизно 60–100 рядків)
```csharp
private void DrawTTSSettings(Listing_Standard listing)
{
    // ... 删除整个方法 ...
}
```

#### F. Метод TestTTS (увесь метод, приблизно 20–30 рядків)
```csharp
private void TestTTS()
{
    // ... 删除整个方法 ...
}
```

### 2. Перевірити компіляцію

Після видалення наведеного вище коду виконайте:
```powershell
msbuild RimTalk-ExpandMemory.csproj /p:Configuration=Release
```

Компіляція має завершитися успішно.

### 3. Оновити About.xml (необов’язково)

Якщо в `About/About.xml` згадується функція TTS, відповідний опис також можна видалити.

---

## ?? Метод швидкого пошуку

У `Source/RimTalkSettings.cs` знайдіть такі ключові слова:
- `TTS`
- `enableTTS`
- `ttsProvider`
- `azureSpeech`
- `DrawTTSSettings`
- `TestTTS`

Видаліть усі блоки коду, що містять ці ключові слова.

---

## ? Контрольний список перевірки

- [ ] Видалити визначення поля TTS
- [ ] Видалити стан згортання TTS UI
- [ ] Видалити код серіалізації TTS
- [ ] Видалити виклик DrawTTSSettings
- [ ] Видалити метод DrawTTSSettings
- [ ] Видалити метод TestTTS
- [ ] Компіляція успішна

---

**Після завершення функцію TTS буде повністю вилучено з проєкту.**
