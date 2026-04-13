// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "AudioCaptureCore.h"
#include "VibeVoskModel.h"
#include "VibeVoskRecognizer.h"
#include "HAL/CriticalSection.h"
#include "VibeVoskAudioCaptureComponent.generated.h"

/**
 * Делегат для событий распознавания
 */
DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FVibeVoskFinalResultDelegate, const FString&, Text, float, Confidence);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FVibeVoskPartialResultDelegate, const FString&, Text);
DECLARE_DYNAMIC_MULTICAST_DELEGATE(FVibeVoskRecordingStartedDelegate);
DECLARE_DYNAMIC_MULTICAST_DELEGATE(FVibeVoskRecordingStoppedDelegate);

/**
 * Компонент распознавания речи Vibe Vosk
 * Захватывает аудио с микрофона и отправляет на распознавание
 */
UCLASS(ClassGroup = ("Vibe Vosk"), meta = (BlueprintSpawnableComponent))
class VIBEVOSKCORE_API UVibeVoskAudioCaptureComponent : public UActorComponent
{
	GENERATED_BODY()

public:
	UVibeVoskAudioCaptureComponent(const FObjectInitializer& ObjectInitializer);

	/** Инициализировать компонент с моделью */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk")
	bool Initialize(UVibeVoskModel* InModel);

	/** Получить список доступных устройств захвата (микрофонов) */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk | Recording")
	static TArray<FString> GetAvailableCaptureDevices();

	/** Получить информацию о выбранном устройстве */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk | Recording")
	static FString GetCaptureDeviceInfo(int32 DeviceIndex);

	/** Начать захват с микрофона и запись */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk | Recording")
	bool StartRecording();

	/** Остановить захват с микрофона */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk | Recording")
	void StopRecording();

	/** Начать распознарение записанного голоса */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk | Recognition")
	bool StartRecognition();

	/** Остановить распознавание */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk | Recognition")
	void StopRecognition();

	/** Сбросить распознаватель */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk")
	void ResetRecognizer();

	/** Обработать аудио данные (float массив) */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk")
	bool ProcessAudio(const TArray<float>& AudioData);

	/** Добавить аудио данные в буфер записи вручную */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk | Recording")
	void AppendAudioData(const TArray<float>& AudioData);

	/** Проверка активности захвата */
	UFUNCTION(BlueprintPure, Category = "Vibe Vosk")
	bool IsCapturing() const { return bIsCapturing; }

	/** Проверка активности распознавания */
	UFUNCTION(BlueprintPure, Category = "Vibe Vosk")
	bool IsRecognizing() const { return bIsRecognizing; }

	/** Проверка инициализации */
	UFUNCTION(BlueprintPure, Category = "Vibe Vosk")
	bool IsInitialized() const { return Recognizer != nullptr; }

	/** Получить частоту дискретизации */
	UFUNCTION(BlueprintPure, Category = "Vibe Vosk")
	float GetSampleRate() const { return SampleRate; }

	/** Получить записанный аудио буфер */
	UFUNCTION(BlueprintPure, Category = "Vibe Vosk | Recording")
	const TArray<float>& GetRecordedAudioBuffer() const { return RecordedAudioBuffer; }

	/** Очистить аудио буфер */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk | Recording")
	void ClearAudioBuffer();

	/** Событие: получен финальный результат распознавания */
	UPROPERTY(BlueprintAssignable, Category = "Vibe Vosk|Events")
	FVibeVoskFinalResultDelegate OnFinalResult;

	/** Событие: получен частичный результат распознавания */
	UPROPERTY(BlueprintAssignable, Category = "Vibe Vosk|Events")
	FVibeVoskPartialResultDelegate OnPartialResult;

	/** Событие: захват начался */
	UPROPERTY(BlueprintAssignable, Category = "Vibe Vosk|Events")
	FVibeVoskRecordingStartedDelegate OnVoiceRecordingStarted;

	/** Событие: захват остановлен */
	UPROPERTY(BlueprintAssignable, Category = "Vibe Vosk|Events")
	FVibeVoskRecordingStoppedDelegate OnVoiceRecordingStopped;

	/** Доступные модели (имена папок). Заполняется при инициализации модуля плагина */
	UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Vibe Vosk")
	TArray<FString> AvailableModels;

protected:
	virtual void BeginPlay() override;
	virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

	virtual void TickComponent(float DeltaTime, enum ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

	/** Callback получения аудио с микрофона */
	void OnAudioCaptured(const float* AudioData, int32 NumFrames, int32 NumChannels, int32 DeviceSampleRate, double StreamTime, bool bOverflow);

	/** Ресэмплинг аудио к целевой частоте */
	TArray<float> ResampleAudio(const float* InputData, int32 NumSamples, int32 SourceRate, int32 TargetRate);

private:
	/** Обработчик финального результата */
	UFUNCTION()
	void HandleRecognitionResult(const FString& Result);

	/** Обработчик частичного результата */
	UFUNCTION()
	void HandlePartialResult(const FString& Partial);

	/** Объект захвата аудио */
	Audio::FAudioCapture AudioCapture;

	/** Модель Vibe Vosk */
	UPROPERTY()
	UVibeVoskModel* Model = nullptr;

	/** Распознаватель Vibe Vosk */
	UPROPERTY()
	UVibeVoskRecognizer* Recognizer = nullptr;

	/** Частота дискретизации (Vibe Vosk требует 16kHz) */
	UPROPERTY(EditAnywhere, Category = "Vibe Vosk")
	float SampleRate = 16000.0f;

	/** Коэффициент усиления аудиосигнала (умножение амплитуды) */
	UPROPERTY(EditAnywhere, Category = "Vibe Vosk", meta = (ClampMin = "1.0", ClampMax = "100.0"))
	float AudioGain = 10.0f;

	/** Показывать отладочные сообщения в логе */
	UPROPERTY(EditAnywhere, Category = "Vibe Vosk | Debug")
	bool bShowDebugMessages = false;

	/** Индекс устройства захвата (INDEX_NONE = устройство по умолчанию) */
	UPROPERTY(EditAnywhere, Category = "Vibe Vosk | Recording", meta = (ClampMin = "-1"))
	int32 CaptureDeviceIndex = INDEX_NONE;

	/** Имя устройства захвата (если указано, используется вместо индекса) */
	UPROPERTY(EditAnywhere, Category = "Vibe Vosk | Recording")
	FString CaptureDeviceName;

	/** Флаг инициализации */
	bool bIsInitialized = false;

	/** Флаг активности захвата */
	bool bIsCapturing = false;

	/** Аудио буфер для записи голоса */
	TArray<float> RecordedAudioBuffer;

	/** Флаг активности распознавания */
	bool bIsRecognizing = false;

	/** Позиция в буфере для распознавания */
	int32 CurrentRecognitionPosition = 0;

	/** Минимальный размер порции для отправки в Vibe Vosk */
	const int32 MIN_BUFFER_SIZE = 1600; // 100 мс = 1600 сэмплов

	/** Мьютекс для потокобезопасного доступа к аудио буферу */
	FCriticalSection AudioBufferCriticalSection;

	/** Промежуточный буфер для данных из аудио-потока (защищён мьютексом) */
	TArray<float> PendingAudioData;

	/** Предварительно выделенный буфер для ресэмплинга (избегаем аллокаций в аудио-потоке) */
	TArray<float> ResampleWorkBuffer;
};
