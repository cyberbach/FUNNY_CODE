// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#include "VibeVoskAudioCaptureComponent.h"
#include "VibeVoskRecognizer.h"
#include "Misc/Paths.h"
#include "VibeVoskCoreModule.h"
#include "Modules/ModuleManager.h"

#ifndef UE_BUILD_SHIPPING
#define VIBEVOSK_DEBUG_LOG(Format, ...) \
	do { if (bShowDebugMessages) { UE_LOG(LogVibeVosk, Log, TEXT(Format), ##__VA_ARGS__); } } while(0)

#define VIBEVOSK_DEBUG_LOG_NOARGS(Format) \
	do { if (bShowDebugMessages) { UE_LOG(LogVibeVosk, Log, TEXT(Format)); } } while(0)
#else
#define VIBEVOSK_DEBUG_LOG(Format, ...)
#define VIBEVOSK_DEBUG_LOG_NOARGS(Format)
#endif

UVibeVoskAudioCaptureComponent::UVibeVoskAudioCaptureComponent(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
	PrimaryComponentTick.bCanEverTick = true;

	// Попробуем получить список моделей из модуля VoskCore
	if (FModuleManager::Get().IsModuleLoaded("VibeVoskCore"))
	{
		AvailableModels = FVibeVoskCoreModule::Get().GetAvailableModels();
	}
}

void UVibeVoskAudioCaptureComponent::BeginPlay()
{
	Super::BeginPlay();
}

void UVibeVoskAudioCaptureComponent::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
	Super::EndPlay(EndPlayReason);
	StopRecording();
	StopRecognition();
}

bool UVibeVoskAudioCaptureComponent::Initialize(UVibeVoskModel* InModel)
{
	if (InModel == nullptr || !InModel->IsValid())
	{
		UE_LOG(LogVibeVosk, Error, TEXT("VoskAudioCapture: Invalid model provided"));
		return false;
	}

	Model = InModel;

	if (!FPaths::DirectoryExists(Model->GetModelPath()))
	{
		UE_LOG(LogVibeVosk, Error, TEXT("Model directory not found: %s"), *Model->GetModelPath());
		return false;
	}

	Recognizer = UVibeVoskRecognizer::CreateRecognizer(Model, SampleRate);

	if (Recognizer == nullptr)
	{
		UE_LOG(LogVibeVosk, Error, TEXT("VoskAudioCapture: Failed to create recognizer"));
		return false;
	}

	// Подписка на события распознавателя
	Recognizer->OnRecognitionResult.AddDynamic(this, &UVibeVoskAudioCaptureComponent::HandleRecognitionResult);
	Recognizer->OnPartialRecognitionResult.AddDynamic(this, &UVibeVoskAudioCaptureComponent::HandlePartialResult);

	// Применяем настройку отладочных сообщений
	SetVibeVoskRecognizerDebugMessages(bShowDebugMessages);

	bIsInitialized = true;
	VIBEVOSK_DEBUG_LOG("VoskAudioCapture: Initialized successfully with sample rate: %f", SampleRate);
	return true;
}

TArray<FString> UVibeVoskAudioCaptureComponent::GetAvailableCaptureDevices()
{
	TArray<FString> DeviceList;
	Audio::FAudioCapture Capture;
	TArray<Audio::FCaptureDeviceInfo> Devices;
	int32 Count = Capture.GetCaptureDevicesAvailable(Devices);

	for (int32 i = 0; i < Devices.Num(); i++)
	{
		const auto& Dev = Devices[i];
		DeviceList.Add(FString::Printf(TEXT("[%d] %s (%d Hz, %d ch)"),
			i, *Dev.DeviceName, Dev.PreferredSampleRate, Dev.InputChannels));
	}

	return DeviceList;
}

FString UVibeVoskAudioCaptureComponent::GetCaptureDeviceInfo(int32 DeviceIndex)
{
	Audio::FAudioCapture Capture;
	Audio::FCaptureDeviceInfo Info;
	if (Capture.GetCaptureDeviceInfo(Info, DeviceIndex))
	{
		return FString::Printf(TEXT("Name: %s\nID: %s\nSample Rate: %d Hz\nChannels: %d\nAEC: %s"),
			*Info.DeviceName, *Info.DeviceId, Info.PreferredSampleRate,
			Info.InputChannels, Info.bSupportsHardwareAEC ? TEXT("Yes") : TEXT("No"));
	}
	return TEXT("Device not found");
}

bool UVibeVoskAudioCaptureComponent::StartRecording()
{
	if (!bIsInitialized)
	{
		UE_LOG(LogVibeVosk, Error, TEXT("VoskAudioCapture: Not initialized. Call Initialize() first."));
		return false;
	}

	if (bIsCapturing)
	{
		UE_LOG(LogVibeVosk, Warning, TEXT("VoskAudioCapture: Already capturing"));
		return true;
	}

	// Очищаем предыдущую запись
	RecordedAudioBuffer.Empty();
	{
		FScopeLock Lock(&AudioBufferCriticalSection);
		PendingAudioData.Empty();
	}

	// Определяем устройство захвата
	int32 SelectedDeviceIndex = Audio::DefaultDeviceIndex;

	// Если указано имя устройства — ищем по имени
	if (!CaptureDeviceName.IsEmpty())
	{
		TArray<Audio::FCaptureDeviceInfo> Devices;
		AudioCapture.GetCaptureDevicesAvailable(Devices);

		for (int32 i = 0; i < Devices.Num(); i++)
		{
			if (Devices[i].DeviceName.Contains(CaptureDeviceName) ||
				Devices[i].DeviceId.Contains(CaptureDeviceName))
			{
				SelectedDeviceIndex = i;
				VIBEVOSK_DEBUG_LOG("VoskAudioCapture: Found device by name '%s' at index %d",
					*CaptureDeviceName, SelectedDeviceIndex);
				break;
			}
		}

		if (SelectedDeviceIndex == Audio::DefaultDeviceIndex)
		{
			UE_LOG(LogVibeVosk, Warning, TEXT("VoskAudioCapture: Device '%s' not found, using default"),
				*CaptureDeviceName);
		}
	}
	else if (CaptureDeviceIndex != INDEX_NONE)
	{
		// Используем указанный индекс
		SelectedDeviceIndex = CaptureDeviceIndex;
	}

	// Настраиваем параметры захвата — используем родную частоту устройства
	Audio::FAudioCaptureDeviceParams Params;
	Params.SampleRate = Audio::InvalidDeviceSampleRate;  // Родная частота устройства
	Params.NumInputChannels = 1;
	Params.DeviceIndex = SelectedDeviceIndex;

	// Открываем поток с устройства ввода (микрофон)
	if (!AudioCapture.OpenAudioCaptureStream(Params,
		[this](const void* AudioData, int32 NumFrames, int32 NumChannels, int32 SampleRateValue, double StreamTime, bool bOverflow)
		{
			this->OnAudioCaptured((const float*)AudioData, NumFrames, NumChannels, SampleRateValue, StreamTime, bOverflow);
		},
		1024))
	{
		UE_LOG(LogVibeVosk, Error, TEXT("VoskAudioCapture: Failed to open microphone capture stream"));
		return false;
	}

	// Запускаем захват
	if (!AudioCapture.StartStream())
	{
		UE_LOG(LogVibeVosk, Error, TEXT("VoskAudioCapture: Failed to start microphone stream"));
		AudioCapture.CloseStream();
		return false;
	}

	bIsCapturing = true;
	VIBEVOSK_DEBUG_LOG("VoskAudioCapture: Microphone recording started (%d Hz, 1 ch)",
		static_cast<int32>(SampleRate));

	OnVoiceRecordingStarted.Broadcast();
	return true;
}

void UVibeVoskAudioCaptureComponent::StopRecording()
{
	if (!bIsCapturing)
	{
		return;
	}

	// Останавливаем захват
	AudioCapture.StopStream();
	AudioCapture.CloseStream();

	// Забираем оставшиеся данные из промежуточного буфера
	{
		FScopeLock Lock(&AudioBufferCriticalSection);
		if (PendingAudioData.Num() > 0)
		{
			RecordedAudioBuffer.Append(PendingAudioData);
			PendingAudioData.Empty();
		}
	}

	bIsCapturing = false;
	VIBEVOSK_DEBUG_LOG("VoskAudioCapture: Microphone recording stopped, recorded %d samples",
		RecordedAudioBuffer.Num());

	OnVoiceRecordingStopped.Broadcast();
}

void UVibeVoskAudioCaptureComponent::AppendAudioData(const TArray<float>& AudioData)
{
	RecordedAudioBuffer.Append(AudioData);
}

bool UVibeVoskAudioCaptureComponent::StartRecognition()
{
	if (!bIsInitialized)
	{
		UE_LOG(LogVibeVosk, Error, TEXT("VoskAudioCapture: Not initialized. Call Initialize() first."));
		return false;
	}

	if (bIsRecognizing)
	{
		UE_LOG(LogVibeVosk, Warning, TEXT("VoskAudioCapture: Already recognizing"));
		return true;
	}

	if (RecordedAudioBuffer.Num() == 0 && !bIsCapturing)
	{
		UE_LOG(LogVibeVosk, Warning, TEXT("VoskAudioCapture: No recorded audio to recognize"));
		return false;
	}

	// Сбрасываем распознаватель перед новым распознаванием
	Recognizer->Reset();

	bIsRecognizing = true;
	CurrentRecognitionPosition = 0;
	VIBEVOSK_DEBUG_LOG("VoskAudioCapture: Starting recognition of %d recorded samples",
		RecordedAudioBuffer.Num());

	return true;
}

void UVibeVoskAudioCaptureComponent::StopRecognition()
{
	if (!bIsRecognizing)
	{
		return;
	}

	bIsRecognizing = false;
	CurrentRecognitionPosition = 0;
	VIBEVOSK_DEBUG_LOG_NOARGS("VoskAudioCapture: Recognition stopped");
}

void UVibeVoskAudioCaptureComponent::ResetRecognizer()
{
	if (Recognizer != nullptr)
	{
		Recognizer->Reset();
		VIBEVOSK_DEBUG_LOG_NOARGS("VoskAudioCapture: Recognizer reset");
	}
}

void UVibeVoskAudioCaptureComponent::ClearAudioBuffer()
{
	RecordedAudioBuffer.Empty();
	CurrentRecognitionPosition = 0;
	{
		FScopeLock Lock(&AudioBufferCriticalSection);
		PendingAudioData.Empty();
	}
}

bool UVibeVoskAudioCaptureComponent::ProcessAudio(const TArray<float>& AudioData)
{
	if (Recognizer == nullptr || !Recognizer->IsValid() || AudioData.Num() == 0)
	{
		return false;
	}

	return Recognizer->ProcessAudio(AudioData);
}

void UVibeVoskAudioCaptureComponent::OnAudioCaptured(const float* AudioData, int32 NumFrames, int32 NumChannels, int32 DeviceSampleRate, double StreamTime, bool bOverflow)
{
	// ВНИМАНИЕ: Этот метод вызывается из АУДИО-ПОТОКА, не из Game Thread!
	// Все операции здесь должны быть потокобезопасными.

	if (!bIsCapturing)
	{
		return;
	}

	// Даунмикс многоканального аудио в моно
	int32 TotalSamples = NumFrames * NumChannels;
	const float* MonoData = AudioData;
	TArray<float> MonoBuffer;

	if (NumChannels > 1)
	{
		MonoBuffer.SetNumUninitialized(NumFrames);
		for (int32 i = 0; i < NumFrames; i++)
		{
			float Sum = 0.0f;
			for (int32 ch = 0; ch < NumChannels; ch++)
			{
				Sum += AudioData[i * NumChannels + ch];
			}
			MonoBuffer[i] = Sum / NumChannels;
		}
		MonoData = MonoBuffer.GetData();
		TotalSamples = NumFrames;
	}

	// Ресэмплим к 16kHz для VOSK
	int32 TargetRate = static_cast<int32>(SampleRate);
	TArray<float> ResampledData = ResampleAudio(MonoData, TotalSamples, DeviceSampleRate, TargetRate);

	// Применяем усиление
	for (float& Sample : ResampledData)
	{
		Sample *= AudioGain;
		// Клиппинг чтобы не выйти за диапазон
		Sample = FMath::Clamp(Sample, -1.0f, 1.0f);
	}

	// Потокобезопасно добавляем данные в промежуточный буфер
	{
		FScopeLock Lock(&AudioBufferCriticalSection);
		PendingAudioData.Append(ResampledData);
	}
}

TArray<float> UVibeVoskAudioCaptureComponent::ResampleAudio(const float* InputData, int32 NumSamples, int32 SourceRate, int32 TargetRate)
{
	if (SourceRate == TargetRate || SourceRate <= 0)
	{
		return TArray<float>(InputData, NumSamples);
	}

	float Ratio = static_cast<float>(SourceRate) / static_cast<float>(TargetRate);
	int32 TargetNumSamples = FMath::Max(1, FMath::RoundToInt(NumSamples / Ratio));

	TArray<float> OutputData;
	OutputData.SetNumUninitialized(TargetNumSamples);

	// Линейная интерполяция
	for (int32 i = 0; i < TargetNumSamples; i++)
	{
		float SrcIndex = i * Ratio;
		int32 Index1 = FMath::Clamp(static_cast<int32>(SrcIndex), 0, NumSamples - 1);
		int32 Index2 = FMath::Clamp(Index1 + 1, 0, NumSamples - 1);
		float Fraction = SrcIndex - Index1;

		OutputData[i] = InputData[Index1] * (1.0f - Fraction) + InputData[Index2] * Fraction;
	}

	return OutputData;
}

void UVibeVoskAudioCaptureComponent::TickComponent(float DeltaTime, enum ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

	// Потокобезопасно забираем данные из промежуточного буфера
	TArray<float> NewAudioData;
	{
		FScopeLock Lock(&AudioBufferCriticalSection);
		if (PendingAudioData.Num() > 0)
		{
			NewAudioData = MoveTemp(PendingAudioData);
			PendingAudioData.Reset();
		}
	}

	// Добавляем новые данные в основной буфер записи (только из Game Thread)
	if (NewAudioData.Num() > 0)
	{
		RecordedAudioBuffer.Append(NewAudioData);

		VIBEVOSK_DEBUG_LOG("VoskAudioCapture: Received %d samples from audio thread, total: %d",
			NewAudioData.Num(), RecordedAudioBuffer.Num());

		// Если идёт одновременная запись и распознавание — отправляем новые данные в VOSK
		if (bIsRecognizing && bIsCapturing)
		{
			ProcessAudio(NewAudioData);
		}
	}

	// Обрабатываем записанный буфер порциями для распознавания (режим пост-обработки)
	if (bIsRecognizing && !bIsCapturing && CurrentRecognitionPosition < RecordedAudioBuffer.Num())
	{
		int32 EndPosition = FMath::Min(CurrentRecognitionPosition + MIN_BUFFER_SIZE, RecordedAudioBuffer.Num());

		TArray<float> Chunk;
		Chunk.Append(&RecordedAudioBuffer[CurrentRecognitionPosition], EndPosition - CurrentRecognitionPosition);

		ProcessAudio(Chunk);

		CurrentRecognitionPosition = EndPosition;

		// Если обработали все данные — получаем финальный результат
		if (CurrentRecognitionPosition >= RecordedAudioBuffer.Num())
		{
			FString FinalText = Recognizer->GetFinalResult();
			float Confidence = Recognizer->GetLastConfidence();
			if (!FinalText.IsEmpty())
			{
				VIBEVOSK_DEBUG_LOG("VoskAudioCapture: Final recognition result: %s (confidence: %.2f)", *FinalText, Confidence);
				OnFinalResult.Broadcast(FinalText, Confidence);
			}
			else
			{
				VIBEVOSK_DEBUG_LOG_NOARGS("VoskAudioCapture: No speech detected in recorded audio");
			}

			StopRecognition();
		}
	}
}

void UVibeVoskAudioCaptureComponent::HandleRecognitionResult(const FString& Result)
{
	float Confidence = Recognizer ? Recognizer->GetLastConfidence() : 0.0f;
	OnFinalResult.Broadcast(Result, Confidence);
}

void UVibeVoskAudioCaptureComponent::HandlePartialResult(const FString& Partial)
{
	OnPartialResult.Broadcast(Partial);
}
