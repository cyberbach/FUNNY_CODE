# Быстрый старт

## Blueprint (5 минут)

1. Создайте Actor. Добавьте компонент **Vibe Vosk Audio Capture**.
2. В **BeginPlay**:

```
Create Model (node: Create Vosk Model) → Return Value
└── Initialize (на компоненте, вход: модель)
    └── Start Recording (на компоненте)
```

3. Свяжите события компонента:
   - **On Final Result** → Print String (финальный текст)
   - **On Partial Result** → Text Block (текст в реальном времени)

4. Запустите проект. Говорите в микрофон.

## C++ (5 минут)

**YourProject.Build.cs:**

```cpp
PrivateDependencyModuleNames.AddRange(new[] { "VibeVoskCore" });
```

**MyVoiceActor.h:**

```cpp
#include "VibeVoskModel.h"
#include "VibeVoskRecognizer.h"
#include "VibeVoskAudioCaptureComponent.h"
#include "MyVoiceActor.generated.h"

UCLASS()
class AMyVoiceActor : public AActor
{
    GENERATED_BODY()
    UPROPERTY(VisibleAnywhere)
    UVibeVoskAudioCaptureComponent* AudioCapture;

    UFUNCTION()
    void OnResult(const FString& Text, float Confidence);
};
```

**MyVoiceActor.cpp:**

```cpp
void AMyVoiceActor::BeginPlay()
{
    Super::BeginPlay();

    AudioCapture = NewObject<UVibeVoskAudioCaptureComponent>(this);
    AudioCapture->RegisterComponent();

    UVibeVoskModel* Model = UVibeVoskModel::CreateVoskModel(TEXT("vosk-model-small-ru-0.22"));
    if (AudioCapture->Initialize(Model))
    {
        AudioCapture->OnFinalResult.AddDynamic(this, &AMyVoiceActor::OnResult);
        AudioCapture->StartRecording();
    }
}

void AMyVoiceActor::OnResult(const FString& Text, float Confidence)
{
    UE_LOG(LogTemp, Log, TEXT("Recognized: %s (%.2f)"), *Text, Confidence);
}
```

## Что дальше

- [Blueprint Guide](BLUEPRINT_GUIDE.md) — голосовые команды, режим грамматики
- [C++ Guide](CPP_GUIDE.md) — детали C++ API, делегаты, отладка
