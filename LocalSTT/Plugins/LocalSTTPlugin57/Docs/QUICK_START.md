# Быстрый старт

## Blueprint (5 минут)

1. Создайте Actor. Добавьте компонент **Local STT Audio Capture**.
2. В **BeginPlay**:

```
Create Model (node: Create STT Model) → Return Value
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
PrivateDependencyModuleNames.AddRange(new[] { "LocalSTTCore" });
```

**MyVoiceActor.h:**

```cpp
#include "LocalSTTModel.h"
#include "LocalSTTRecognizer.h"
#include "LocalSTTAudioCaptureComponent.h"
#include "MyVoiceActor.generated.h"

UCLASS()
class AMyVoiceActor : public AActor
{
    GENERATED_BODY()
    UPROPERTY(VisibleAnywhere)
    ULocalSTTAudioCaptureComponent* AudioCapture;

    UFUNCTION()
    void OnResult(const FString& Text, float Confidence);
};
```

**MyVoiceActor.cpp:**

```cpp
void AMyVoiceActor::BeginPlay()
{
    Super::BeginPlay();

    AudioCapture = NewObject<ULocalSTTAudioCaptureComponent>(this);
    AudioCapture->RegisterComponent();

    ULocalSTTModel* Model = ULocalSTTModel::CreateSTTModel(TEXT("stt-model-small-ru-0.22"));
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
