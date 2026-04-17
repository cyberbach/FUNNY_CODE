# C++ Guide

## Настройка зависимостей

**YourProject/Source/YourTarget/YourTarget.Build.cs:**

```cpp
PrivateDependencyModuleNames.AddRange(new string[] { "LocalSTTCore" });
```

## Модель

```cpp
#include "LocalSTTModel.h"

// По имени (разрешается через настройки)
ULocalSTTModel* Model = ULocalSTTModel::CreateSTTModel(TEXT("stt-model-small-ru-0.22"));

// По полному пути
ULocalSTTModel* Model = ULocalSTTModel::CreateSTTModel(TEXT("D:/Models/stt-model-small-ru-0.22"));

// Проверка
if (!Model->IsValid()) { /* ошибка */ }

// Освобождение
Model->DestroyModel();
```

## Распознаватель

### Обычный

```cpp
#include "LocalSTTRecognizer.h"

ULocalSTTRecognizer* Rec = ULocalSTTRecognizer::CreateSTTRecognizer(Model, 16000.0f);
```

### С грамматикой

```cpp
TArray<FString> Words = { TEXT("начать"), TEXT("стоп"), TEXT("сохранить") };
ULocalSTTRecognizer* Rec = ULocalSTTRecognizer::CreateSTTRecognizerWithGrammar(Model, Words, 16000.0f);
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

## ULocalSTTAudioCaptureComponent

```cpp
#include "LocalSTTAudioCaptureComponent.h"

// Создание
ULocalSTTAudioCaptureComponent* Capture = NewObject<ULocalSTTAudioCaptureComponent>(Actor);
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
TArray<FString> Devices = ULocalSTTAudioCaptureComponent::GetAvailableCaptureDevices();
for (const FString& Dev : Devices) { UE_LOG(LogTemp, Log, TEXT("%s"), *Dev); }
```

## Настройки

```cpp
#include "LocalSTTPluginSettings.h"

const ULocalSTTPluginSettings* Settings = ULocalSTTPluginSettings::Get();
FString ModelsPath = Settings->GetLocalSTTModelsPath();
FString DefaultModel = Settings->GetDefaultModelPath();
bool bInstalled = Settings->IsSTTSdkInstalled();
```

## Модуль

```cpp
#include "LocalSTTCoreModule.h"

if (FModuleManager::Get().IsModuleLoaded(TEXT("LocalSTTCore")))
{
    const TArray<FString>& Models = FLocalSTTCoreModule::Get().GetAvailableModels();
    bool bLoaded = FLocalSTTCoreModule::Get().IsSTTLoaded();
}
```

## Логирование и отладка

```cpp
// Категория логов
UE_LOG(LogLocalSTT, Log, TEXT("message"));

// Консольная переменная
// localstt.RecognizerDebug 0/1 — отладка распознавателя
```
