# C++ Guide

## Настройка зависимостей

**YourProject/Source/YourTarget/YourTarget.Build.cs:**

```cpp
PrivateDependencyModuleNames.AddRange(new string[] { "VibeVoskCore" });
```

## Модель

```cpp
#include "VibeVoskModel.h"

// По имени (разрешается через настройки)
UVibeVoskModel* Model = UVibeVoskModel::CreateVoskModel(TEXT("vosk-model-small-ru-0.22"));

// По полному пути
UVibeVoskModel* Model = UVibeVoskModel::CreateVoskModel(TEXT("D:/Models/vosk-model-small-ru-0.22"));

// Проверка
if (!Model->IsValid()) { /* ошибка */ }

// Освобождение
Model->DestroyModel();
```

## Распознаватель

### Обычный

```cpp
#include "VibeVoskRecognizer.h"

UVibeVoskRecognizer* Rec = UVibeVoskRecognizer::CreateRecognizer(Model, 16000.0f);
```

### С грамматикой

```cpp
TArray<FString> Words = { TEXT("начать"), TEXT("стоп"), TEXT("сохранить") };
UVibeVoskRecognizer* Rec = UVibeVoskRecognizer::CreateRecognizerWithGrammar(Model, Words, 16000.0f);
```

### Обработка аудио

```cpp
// float массив, -1.0..1.0
Rec->ProcessAudio(AudioData);

// int16 массив (только C++)
TArray<int16> Int16Data;
Rec->ProcessAudioInt16(Int16Data);
```

### Результаты

```cpp
// Синхронно
FString Text = Rec->GetResult();
FString Final = Rec->GetFinalResult();
FString Partial = Rec->GetPartialResult();
float Confidence = Rec->GetLastConfidence();

// Сброс
Rec->Reset();

// Освобождение
Rec->DestroyRecognizer();
```

### Делегаты

```cpp
Rec->OnRecognitionResult.AddLambda([](const FString& Result) {
    UE_LOG(LogTemp, Log, TEXT("Final: %s"), *Result);
});

Rec->OnPartialRecognitionResult.AddLambda([](const FString& Partial) {
    UE_LOG(LogTemp, Log, TEXT("Partial: %s"), *Partial);
});
```

## UVibeVoskAudioCaptureComponent

```cpp
#include "VibeVoskAudioCaptureComponent.h"

// Создание
UVibeVoskAudioCaptureComponent* Capture = NewObject<UVibeVoskAudioCaptureComponent>(Actor);
Capture->RegisterComponent();

// Инициализация
if (!Capture->Initialize(Model)) { /* ошибка */ }

// Настройки
Capture->AudioGain = 15.0f;
Capture->CaptureDeviceName = TEXT("Microphone (Realtek)");

// Биндинг событий
Capture->OnFinalResult.AddDynamic(this, &AMyActor::OnResult);
Capture->OnPartialResult.AddDynamic(this, &AMyActor::OnPartial);

// Запуск
Capture->StartRecording();

// Остановка
Capture->StopRecording();
Capture->StopRecognition();

// Список микрофонов
TArray<FString> Devices = UVibeVoskAudioCaptureComponent::GetAvailableCaptureDevices();
for (const FString& Dev : Devices) { UE_LOG(LogTemp, Log, TEXT("%s"), *Dev); }
```

## Настройки

```cpp
#include "VibeVoskPluginSettings.h"

const UVibeVoskPluginSettings* Settings = UVibeVoskPluginSettings::Get();
FString ModelsPath = Settings->GetVibeVoskModelsPath();
FString DefaultModel = Settings->GetDefaultModelPath();
bool bInstalled = Settings->IsVoskSdkInstalled();
```

## Модуль

```cpp
#include "VibeVoskCoreModule.h"

if (FModuleManager::Get().IsModuleLoaded(TEXT("VibeVoskCore")))
{
    const TArray<FString>& Models = FVibeVoskCoreModule::Get().GetAvailableModels();
    bool bLoaded = FVibeVoskCoreModule::Get().IsVoskLoaded();
}
```

## Логирование и отладка

```cpp
// Категория логов
UE_LOG(LogVibeVosk, Log, TEXT("message"));

// Консольная переменная
// vosk.RecognizerDebug 0/1 — отладка распознавателя
```
