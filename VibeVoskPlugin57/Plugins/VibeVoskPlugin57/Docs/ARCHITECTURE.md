# Архитектура

## Структура модулей

```
VibeVoskPlugin57
├── VibeVoskCore        — ядро: DLL, модель, распознаватель, компонент, настройки
└── VibeVoskBlueprint   — тонкая обёртка статических функций для Blueprint
```

## Слои

```
Пользователь (Blueprint / C++)
        ↓
UVibeVoskBlueprintLibrary   UVibeVoskAudioCaptureComponent
        ↓                           ↓
UVibeVoskModel ────────→ UVibeVoskRecognizer
        ↓
FVibeVoskApiFunctions (динамическая загрузка libvosk.dll)
        ↓
libvosk.dll (VOSK C API)
```

## Ключевые решения

### Динамическая загрузка DLL

`FVibeVoskApiFunctions` — синглтон, загружает `libvosk.dll` при старте модуля `VibeVoskCore`. Плагин не упадёт если DLL отсутствует (`WITH_VOSK=0` на non-Windows).

### Разделение на два модуля

- **VibeVoskCore** — вся логика, зависит от AudioCapture.
- **VibeVoskBlueprint** — тонкий proxy, зависит только от VibeVoskCore. Не трогает VOSK API напрямую.

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
  →_pending буфер (mutex)
  → TickComponent забирает
  → UVibeVoskRecognizer::ProcessAudio()
  → VOSK API (int16)
  → JSON-парсинг → делегат
```

### Confidence

`vosk_recognizer_set_words(1)` включён. Уверенность считается как среднее `conf` из массива `result` в JSON.

### Сканирование моделей

При старте `VibeVoskCore` сканирует `Binaries/Win64/Models/` и ищет папки с `conf/mfcc.conf`. Результаты доступны через `FVibeVoskCoreModule::GetAvailableModels()` и свойство `AvailableModels` компонента.

## Зависимости

| Модуль | Public | Private |
|--------|--------|---------|
| VibeVoskCore | Core, CoreUObject, Engine, AudioCapture, AudioCaptureCore, AudioMixer, SignalProcessing, Json, JsonUtilities, DeveloperSettings, Projects | Slate, SlateCore |
| VibeVoskBlueprint | Core, CoreUObject, Engine, VibeVoskCore | Slate, SlateCore |
