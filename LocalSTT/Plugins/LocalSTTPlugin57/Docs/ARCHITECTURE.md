# Архитектура

## Структура модулей

```
LocalSTTPlugin57
├── LocalSTTCore        — ядро: DLL, модель, распознаватель, компонент, настройки
└── LocalSTTBlueprint   — тонкая обёртка статических функций для Blueprint
```

## Слои

```
Пользователь (Blueprint / C++)
        ↓
ULocalSTTBlueprintLibrary   ULocalSTTAudioCaptureComponent
        ↓                           ↓
ULocalSTTModel ──────────→ ULocalSTTRecognizer
        ↓
FLocalSTTApiFunctions (динамическая загрузка libstt.dll)
        ↓
libstt.dll (STT C API)
```

## Ключевые решения

### Динамическая загрузка DLL

`FLocalSTTApiFunctions` — синглтон, загружает `libstt.dll` при старте модуля `LocalSTTCore`. Плагин не упадёт если DLL отсутствует (`WITH_STT=0` на non-Windows).

### Разделение на два модуля

- **LocalSTTCore** — вся логика, зависит от AudioCapture.
- **LocalSTTBlueprint** — тонкий proxy, зависит только от LocalSTTCore. Не трогает STT API напрямую.

### Потокобезопасность

Аудиопоток → Game Thread:

1. `OnAudioCaptured()` вызывается из audio thread
2. Данные пишутся в `PendingAudioData` под `FCriticalSection`
3. `TickComponent()` забирает данные через `MoveTemp` в game thread
4. Распознавание происходит в game thread

### Управление нативными ресурсами

- `AddToRoot()` при создании — защита от GC
- `RemoveFromRoot()` при явном `Destroy*()`
- `BeginDestroy()` — автоматическое освобождение при сборке мусора

### Аудио-пайплайн

```
Микрофон (48kHz, multi-ch)
  → Даунмикс в моно
  → Ресэмпл до 16kHz (линейная интерполяция)
  → AudioGain + клиппинг
  → pending буфер (mutex)
  → TickComponent забирает
  → ULocalSTTRecognizer::ProcessAudio()
  → STT API (int16)
  → JSON-парсинг → делегат
```

### Confidence

`stt_recognizer_set_words(1)` включён. Уверенность считается как среднее `conf` из массива `result` в JSON.

### Сканирование моделей

При старте `LocalSTTCore` сканирует `Binaries/Win64/Models/` и ищет папки с `conf/mfcc.conf`. Результаты доступны через `FLocalSTTCoreModule::GetAvailableModels()` и свойство `AvailableModels` компонента.

## Зависимости

| Модуль | Public | Private |
|--------|--------|---------|
| LocalSTTCore | Core, CoreUObject, Engine, AudioCapture, AudioCaptureCore, AudioMixer, SignalProcessing, Json, JsonUtilities, DeveloperSettings, Projects | Slate, SlateCore |
| LocalSTTBlueprint | Core, CoreUObject, Engine, LocalSTTCore | Slate, SlateCore |
