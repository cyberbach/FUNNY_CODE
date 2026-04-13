# API Reference

## Модули

| Модуль | Описание |
|--------|----------|
| `VibeVoskCore` | Ядро: DLL-загрузка, модель, распознаватель, компонент, настройки |
| `VibeVoskBlueprint` | Тонкая обёртка статических функций для Blueprint |

## Классы

### FVibeVoskApiFunctions

Синглтон динамической загрузки `libvosk.dll`.

| Метод | Описание |
|-------|----------|
| `static FVibeVoskApiFunctions& Get()` | Получить инстанс |
| `void* LoadLibrary(const FString& DllPath)` | Загрузить DLL, вернуть handle или nullptr |
| `void FreeLibrary()` | Освободить DLL |
| `bool IsLoaded() const` | Проверка загрузки |

Макрос `VIBEVOSK_API(Function)` — удобный доступ к функциям.

---

### UVibeVoskModel

UObject-обёртка над VOSK моделью.

| Метод | Описание |
|-------|----------|
| `static UVibeVoskModel* CreateVoskModel(const FString& ModelPath)` | Создать модель. Путь — полный или имя (разрешается через настройки) |
| `void DestroyModel()` | Освободить модель |
| `bool IsValid() const` | Проверка валидности |
| `FString GetModelPath() const` | Путь к модели |
| `void* GetNativeHandle() const` | Нативный handle (VoskModel*) |

---

### UVibeVoskRecognizer

UObject-обёртка над VOSK распознавателем.

| Метод | Описание |
|-------|----------|
| `static UVibeVoskRecognizer* CreateRecognizer(UVibeVoskModel*, float SampleRate)` | Создать распознаватель |
| `static UVibeVoskRecognizer* CreateRecognizerWithGrammar(UVibeVoskModel*, const TArray<FString>& Words, float SampleRate)` | С ограниченной грамматикой |
| `void DestroyRecognizer()` | Освободить распознаватель |
| `bool ProcessAudio(const TArray<float>&)` | Обработка аудио (Blueprint) |
| `bool ProcessAudioInt16(const TArray<int16>&)` | Обработка аудио (C++ only) |
| `FString GetResult()` | Текущий результат |
| `FString GetFinalResult()` | Финальный результат после конца потока |
| `FString GetPartialResult()` | Частичный результат (в процессе речи) |
| `void Reset()` | Сброс распознавателя |
| `bool IsValid() const` | Проверка валидности |
| `float GetLastConfidence() const` | Последняя уверенность (0.0–1.0) |

**События:**

| Делегат | Параметры | Описание |
|---------|-----------|----------|
| `OnRecognitionResult` | `FString Result` | Финальный результат |
| `OnPartialRecognitionResult` | `FString Partial` | Частичный результат |

Глобальная функция: `SetVibeVoskRecognizerDebugMessages(bool bShow)`.

---

### UVibeVoskAudioCaptureComponent

Компонент захвата микрофона + распознавание. Blueprint-spawnable.

| Метод | Описание |
|-------|----------|
| `bool Initialize(UVibeVoskModel*)` | Инициализировать с моделью |
| `bool StartRecording()` | Начать захват микрофона |
| `void StopRecording()` | Остановить захват |
| `bool StartRecognition()` | Начать распознавание записанного |
| `void StopRecognition()` | Остановить распознавание |
| `void ResetRecognizer()` | Сбросить распознаватель |
| `bool ProcessAudio(const TArray<float>&)` | Обработать аудио |
| `void AppendAudioData(const TArray<float>&)` | Добавить аудио вручную |
| `void ClearAudioBuffer()` | Очистить буфер записи |
| `bool IsCapturing() const` | Проверка захвата |
| `bool IsRecognizing() const` | Проверка распознавания |
| `bool IsInitialized() const` | Проверка инициализации |
| `float GetSampleRate() const` | Частота дискретизации |
| `const TArray<float>& GetRecordedAudioBuffer() const` | Буфер записи |
| `static TArray<FString> GetAvailableCaptureDevices()` | Список микрофонов |
| `static FString GetCaptureDeviceInfo(int32 DeviceIndex)` | Информация об устройстве |

**События:**

| Делегат | Параметры | Описание |
|---------|-----------|----------|
| `OnFinalResult` | `FString Text, float Confidence` | Финальный результат |
| `OnPartialResult` | `FString Text` | Частичный результат |
| `OnVoiceRecordingStarted` | — | Захват начался |
| `OnVoiceRecordingStopped` | — | Захват остановлен |

**Свойства:**

| Свойство | Тип | Описание |
|----------|-----|----------|
| `SampleRate` | float | Частота (по умолчанию 16000) |
| `AudioGain` | float | Усиление сигнала (1.0–100.0) |
| `bShowDebugMessages` | bool | Отладочные сообщения |
| `CaptureDeviceIndex` | int32 | Индекс микрофона (-1 = default) |
| `CaptureDeviceName` | FString | Имя микрофона (приоритетнее индекса) |
| `AvailableModels` | TArray<FString> | Список обнаруженных моделей |

---

### UVibeVoskPluginSettings

Настройки плагина (Project Settings → VOSK Plugin).

| Метод | Описание |
|-------|----------|
| `static UVibeVoskPluginSettings* Get()` | Получить синглтон |
| `FString GetVibeVoskModelsPath() const` | Путь к моделям |
| `FString GetDefaultModelPath() const` | Полный путь к модели по умолчанию |
| `bool IsVoskSdkInstalled() const` | Проверка наличия VOSK DLL |
| `bool IsModelInstalled(const FString& ModelName) const` | Проверка наличия модели |
| `FString GetPluginBinPath() const` | Путь к Binaries/Win64 |

**Свойства:**

| Свойство | Тип | Описание |
|----------|-----|----------|
| `ModelsDirectory` | FString | Путь к моделям (пусто = авто) |
| `DefaultModelName` | FString | Модель по умолчанию |
| `bEnableVibeVoskLogging` | bool | Включить логирование VOSK |
| `VoskLogLevel` | int32 | Уровень логов (0–3) |

---

### FVibeVoskCoreModule

Модуль ядра.

| Метод | Описание |
|-------|----------|
| `static FVibeVoskCoreModule& Get()` | Получить инстанс |
| `bool IsVoskLoaded() const` | Проверка загрузки DLL |
| `const TArray<FString>& GetAvailableModels() const` | Список обнаруженных моделей |

**Консольная переменная:**

| CVar | Тип | Описание |
|------|-----|----------|
| `vosk.RecognizerDebug` | bool | Включить отладочные сообщения распознавателя |

**Лог-категория:** `LogVibeVosk`

---

### UVibeVoskBlueprintLibrary

Статические Blueprint-функции (тонкая обёртка над Core API).

| Метод | Описание |
|-------|----------|
| `CreateVoskModel(FString ModelPath)` | Создать модель |
| `CreateVoskModel()` | Создать модель по умолчанию |
| `CreateVoskRecognizer(UVibeVoskModel*, float SampleRate)` | Создать распознаватель |
| `CreateVoskRecognizerWithGrammar(UVibeVoskModel*, TArray<FString> Words, float SampleRate)` | С грамматикой |
| `ProcessAudio(UVibeVoskRecognizer*, TArray<float>)` | Обработать аудио |
| `GetRecognitionResult(UVibeVoskRecognizer*)` | Получить результат |
| `GetPartialRecognitionResult(UVibeVoskRecognizer*)` | Частичный результат |
| `ResetRecognizer(UVibeVoskRecognizer*)` | Сброс |
| `DestroyModel(UVibeVoskModel*)` | Освободить модель |
| `DestroyRecognizer(UVibeVoskRecognizer*)` | Освободить распознаватель |
| `IsModelValid(UVibeVoskModel*)` | Проверка модели |
| `IsRecognizerValid(UVibeVoskRecognizer*)` | Проверка распознавателя |
| `GetDefaultModelPath()` | Путь к модели |
| `GetVoskVersion()` | Версия VOSK |
