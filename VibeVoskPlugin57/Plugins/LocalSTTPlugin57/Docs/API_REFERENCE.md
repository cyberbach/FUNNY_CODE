# API Reference

## Модули

| Модуль | Описание |
|--------|----------|
| `LocalSTTCore` | Ядро: DLL-загрузка, модель, распознаватель, компонент, настройки |
| `LocalSTTBlueprint` | Тонкая обёртка статических функций для Blueprint |

## Классы

### FLocalSTTApiFunctions

Синглтон динамической загрузки `libstt.dll`.

| Метод | Описание |
|-------|----------|
| `static FLocalSTTApiFunctions& Get()` | Получить инстанс |
| `void* LoadLibrary(const FString& DllPath)` | Загрузить DLL, вернуть handle или nullptr |
| `void FreeLibrary()` | Освободить DLL |
| `bool IsLoaded() const` | Проверка загрузки |

Макрос `LOCALSTT_API(Function)` — удобный доступ к функциям.

---

### ULocalSTTModel

UObject-обёртка над STT моделью.

| Метод | Описание |
|-------|----------|
| `static ULocalSTTModel* CreateSTTModel(const FString& ModelPath)` | Создать модель. Путь — полный или имя (разрешается через настройки) |
| `void DestroyModel()` | Освободить модель |
| `bool IsValid() const` | Проверка валидности |
| `FString GetModelPath() const` | Путь к модели |
| `void* GetNativeHandle() const` | Нативный handle (SttModel*) |

---

### ULocalSTTRecognizer

UObject-обёртка над STT распознавателем.

| Метод | Описание |
|-------|----------|
| `static ULocalSTTRecognizer* CreateSTTRecognizer(ULocalSTTModel*, float SampleRate)` | Создать распознаватель |
| `static ULocalSTTRecognizer* CreateSTTRecognizerWithGrammar(ULocalSTTModel*, const TArray<FString>& Words, float SampleRate)` | С ограниченной грамматикой |
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

Глобальная функция: `SetLocalSTTRecognizerDebugMessages(bool bShow)`.

---

### ULocalSTTAudioCaptureComponent

Компонент захвата микрофона + распознавание. Blueprint-spawnable.

| Метод | Описание |
|-------|----------|
| `bool Initialize(ULocalSTTModel*)` | Инициализировать с моделью |
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
| `OnDebugMessage` | `FString Message` | Отладочное сообщение (работает и в Shipping, управляется `bShowDebugMessages`) |

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

### ULocalSTTPluginSettings

Настройки плагина (Project Settings → STT Plugin).

| Метод | Описание |
|-------|----------|
| `static ULocalSTTPluginSettings* Get()` | Получить синглтон |
| `FString GetLocalSTTModelsPath() const` | Путь к моделям |
| `FString GetDefaultModelPath() const` | Полный путь к модели по умолчанию |
| `bool IsSTTSdkInstalled() const` | Проверка наличия STT DLL |
| `bool IsModelInstalled(const FString& ModelName) const` | Проверка наличия модели |
| `FString GetPluginBinPath() const` | Путь к Binaries/Win64 |

**Свойства:**

| Свойство | Тип | Описание |
|----------|-----|----------|
| `ModelsDirectory` | FString | Путь к моделям (пусто = авто) |
| `DefaultModelName` | FString | Модель по умолчанию |
| `bEnableLocalSTTLogging` | bool | Включить логирование STT |
| `SttLogLevel` | int32 | Уровень логов (0–3) |

---

### FLocalSTTCoreModule

Модуль ядра.

| Метод | Описание |
|-------|----------|
| `static FLocalSTTCoreModule& Get()` | Получить инстанс |
| `bool IsSTTLoaded() const` | Проверка загрузки DLL |
| `const TArray<FString>& GetAvailableModels() const` | Список обнаруженных моделей |

**Консольная переменная:**

| CVar | Тип | Описание |
|------|-----|----------|
| `localstt.RecognizerDebug` | bool | Включить отладочные сообщения распознавателя |

**Лог-категория:** `LogLocalSTT`

---

### ULocalSTTBlueprintLibrary

Статические Blueprint-функции (тонкая обёртка над Core API).

| Метод | Описание |
|-------|----------|
| `CreateSTTModel(FString ModelPath)` | Создать модель |
| `CreateSTTModel()` | Создать модель по умолчанию |
| `CreateSTTRecognizer(ULocalSTTModel*, float SampleRate)` | Создать распознаватель |
| `CreateSTTRecognizerWithGrammar(ULocalSTTModel*, TArray<FString> Words, float SampleRate)` | С грамматикой |
| `ProcessAudio(ULocalSTTRecognizer*, TArray<float>)` | Обработать аудио |
| `GetRecognitionResult(ULocalSTTRecognizer*)` | Получить результат |
| `GetPartialRecognitionResult(ULocalSTTRecognizer*)` | Частичный результат |
| `ResetRecognizer(ULocalSTTRecognizer*)` | Сброс |
| `DestroyModel(ULocalSTTModel*)` | Освободить модель |
| `DestroyRecognizer(ULocalSTTRecognizer*)` | Освободить распознаватель |
| `IsModelValid(ULocalSTTModel*)` | Проверка модели |
| `IsRecognizerValid(ULocalSTTRecognizer*)` | Проверка распознавателя |
| `GetDefaultModelPath()` | Путь к модели |
| `GetSTTVersion()` | Версия STT |
