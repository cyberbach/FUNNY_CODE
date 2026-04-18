// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "AudioCaptureCore.h"
#include "LocalSTTModel.h"
#include "LocalSTTRecognizer.h"
#include "HAL/CriticalSection.h"
#include "LocalSTTAudioCaptureComponent.generated.h"

/**
 * Делегаты для событий
 */
DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FLocalSTTFinalResultDelegate, const FString&, Text, float, Confidence);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FLocalSTTPartialResultDelegate, const FString&, Text);
DECLARE_DYNAMIC_MULTICAST_DELEGATE(FLocalSTTRecordingStartedDelegate);
DECLARE_DYNAMIC_MULTICAST_DELEGATE(FLocalSTTRecordingStoppedDelegate);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FLocalSTTDebugMessageDelegate, const FString&, Message);

/**
 * Компонент распознавания речи LocalSTT
 * Захватывает аудио с микрофона и отправляет на распознавание
 */
UCLASS(ClassGroup = ("LocalSTT"), meta = (BlueprintSpawnableComponent))
class LOCALSTTCORE_API ULocalSTTAudioCaptureComponent : public UActorComponent
{
	GENERATED_BODY()

public:
	ULocalSTTAudioCaptureComponent(const FObjectInitializer& ObjectInitializer);

	/** Инициализировать компонент с моделью */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT")
	bool Initialize(ULocalSTTModel* InModel);

	/** Получить список доступных устройств захвата (микрофонов) */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT | Recording")
	static TArray<FString> GetAvailableCaptureDevices();

	/** Получить информацию о выбранном устройстве */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT | Recording")
	static FString GetCaptureDeviceInfo(int32 DeviceIndex);

	/** Начать захват с микрофона и запись */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT | Recording")
	bool StartRecording();

	/** Остановить захват с микрофона */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT | Recording")
	void StopRecording();

	/** Начать распознарение записанного голоса */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT | Recognition")
	bool StartRecognition();

	/** Остановить распознавание */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT | Recognition")
	void StopRecognition();

	/** Сбросить распознаватель */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT")
	void ResetRecognizer();

	/** Обработать аудио данные (float массив) */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT")
	bool ProcessAudio(const TArray<float>& AudioData);

	/** Добавить аудио данные в буфер записи вручную */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT | Recording")
	void AppendAudioData(const TArray<float>& AudioData);

	/** Проверка активности захвата */
	UFUNCTION(BlueprintPure, Category = "LocalSTT")
	bool IsCapturing() const { return bIsCapturing; }

	/** Проверка активности распознавания */
	UFUNCTION(BlueprintPure, Category = "LocalSTT")
	bool IsRecognizing() const { return bIsRecognizing; }

	/** Проверка инициализации */
	UFUNCTION(BlueprintPure, Category = "LocalSTT")
	bool IsInitialized() const { return Recognizer != nullptr; }

	/** Получить частоту дискретизации */
	UFUNCTION(BlueprintPure, Category = "LocalSTT")
	float GetSampleRate() const { return SampleRate; }

	/** Получить записанный аудио буфер */
	UFUNCTION(BlueprintPure, Category = "LocalSTT | Recording")
	const TArray<float>& GetRecordedAudioBuffer() const { return RecordedAudioBuffer; }

	/** Очистить аудио буфер */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT | Recording")
	void ClearAudioBuffer();

	/** Событие: получен финальный результат распознавания */
	UPROPERTY(BlueprintAssignable, Category = "LocalSTT|Events")
	FLocalSTTFinalResultDelegate OnFinalResult;

	/** Событие: получен частичный результат распознавания */
	UPROPERTY(BlueprintAssignable, Category = "LocalSTT|Events")
	FLocalSTTPartialResultDelegate OnPartialResult;

	/** Событие: захват начался */
	UPROPERTY(BlueprintAssignable, Category = "LocalSTT|Events")
	FLocalSTTRecordingStartedDelegate OnVoiceRecordingStarted;

	/** Событие: захват остановлен */
	UPROPERTY(BlueprintAssignable, Category = "LocalSTT|Events")
	FLocalSTTRecordingStoppedDelegate OnVoiceRecordingStopped;

	/** Событие: отладочное сообщение (работает и в Shipping-билде, управляется флагом bShowDebugMessages) */
	UPROPERTY(BlueprintAssignable, Category = "LocalSTT|Events")
	FLocalSTTDebugMessageDelegate OnDebugMessage;

	/** Доступные модели (имена папок). Заполняется при инициализации модуля плагина */
	UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "LocalSTT")
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

	/** Модель LocalSTT */
	UPROPERTY()
	ULocalSTTModel* Model = nullptr;

	/** Распознаватель LocalSTT */
	UPROPERTY()
	ULocalSTTRecognizer* Recognizer = nullptr;

	/** Частота дискретизации (LocalSTT требует 16kHz) */
	UPROPERTY(EditAnywhere, Category = "LocalSTT")
	float SampleRate = 16000.0f;

	/** Коэффициент усиления аудиосигнала (умножение амплитуды) */
	UPROPERTY(EditAnywhere, Category = "LocalSTT", meta = (ClampMin = "1.0", ClampMax = "100.0"))
	float AudioGain = 10.0f;

	/** Показывать отладочные сообщения в логе */
	UPROPERTY(EditAnywhere, Category = "LocalSTT | Debug")
	bool bShowDebugMessages = false;

	/** Индекс устройства захвата (INDEX_NONE = устройство по умолчанию) */
	UPROPERTY(EditAnywhere, Category = "LocalSTT | Recording", meta = (ClampMin = "-1"))
	int32 CaptureDeviceIndex = INDEX_NONE;

	/** Имя устройства захвата (если указано, используется вместо индекса) */
	UPROPERTY(EditAnywhere, Category = "LocalSTT | Recording")
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

	/** Минимальный размер порции для отправки в LocalSTT */
	const int32 MIN_BUFFER_SIZE = 1600; // 100 мс = 1600 сэмплов

	/** Мьютекс для потокобезопасного доступа к аудио буферу */
	FCriticalSection AudioBufferCriticalSection;

	/** Промежуточный буфер для данных из аудио-потока (защищён мьютексом) */
	TArray<float> PendingAudioData;

	/** Предварительно выделенный буфер для ресэмплинга (избегаем аллокаций в аудио-потоке) */
	TArray<float> ResampleWorkBuffer;
};
