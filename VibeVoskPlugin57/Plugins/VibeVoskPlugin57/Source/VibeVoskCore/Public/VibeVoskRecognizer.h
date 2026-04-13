// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "VibeVoskModel.h"
#include "UObject/NoExportTypes.h"
#include "VibeVoskRecognizer.generated.h"

/** Установить показ отладочных сообщений для всех распознавателей */
VIBEVOSKCORE_API void SetVibeVoskRecognizerDebugMessages(bool bShow);

/**
 * Распознаватель VOSK
 * Обрабатывает аудиопоток и возвращает результаты распознавания
 */
UCLASS(BlueprintType, Category = "VOSK")
class VIBEVOSKCORE_API UVibeVoskRecognizer : public UObject
{
	GENERATED_BODY()

public:
	/** Создать распознаватель с моделью и частотой дискретизации */
	UFUNCTION(BlueprintCallable, Category = "VOSK|Recognizer")
	static UVibeVoskRecognizer* CreateRecognizer(UVibeVoskModel* Model, float SampleRate = 16000.0f);

	/** Освободить распознаватель */
	UFUNCTION(BlueprintCallable, Category = "VOSK|Recognizer")
	void DestroyRecognizer();

	/** Обработать аудиоданные (float массив) */
	UFUNCTION(BlueprintCallable, Category = "VOSK|Recognizer")
	bool ProcessAudio(const TArray<float>& AudioData);

	/** Обработать аудиоданные (int16 массив) - только C++ */
	bool ProcessAudioInt16(const TArray<int16>& AudioData);

	/** Получить финальный результат */
	UFUNCTION(BlueprintCallable, Category = "VOSK|Recognizer")
	FString GetResult();

	/** Получить финальный результат после окончания потока */
	UFUNCTION(BlueprintCallable, Category = "VOSK|Recognizer")
	FString GetFinalResult();

	/** Получить частичный результат (в процессе речи) */
	UFUNCTION(BlueprintCallable, Category = "VOSK|Recognizer")
	FString GetPartialResult();

	/** Сбросить распознаватель */
	UFUNCTION(BlueprintCallable, Category = "VOSK|Recognizer")
	void Reset();

	/** Проверка активности распознавателя */
	UFUNCTION(BlueprintPure, Category = "VOSK|Recognizer")
	bool IsValid() const { return RecognizerHandle != nullptr; }

	/** Получить нативный handle распознавателя VOSK */
	void* GetNativeHandle() const { return RecognizerHandle; }

	/** Автоматическое освобождение нативных ресурсов при сборке мусора */
	virtual void BeginDestroy() override;

	/** Создать распознаватель с ограниченной грамматикой (списком слов) */
	UFUNCTION(BlueprintCallable, Category = "VOSK|Recognizer")
	static UVibeVoskRecognizer* CreateRecognizerWithGrammar(UVibeVoskModel* Model, const TArray<FString>& Words, float SampleRate = 16000.0f);

	/** Получить последнюю уверенность распознавания (0.0 - 1.0) */
	UFUNCTION(BlueprintPure, Category = "VOSK|Recognizer")
	float GetLastConfidence() const { return LastConfidence; }

	/** Событие: получен финальный результат */
	UPROPERTY(BlueprintAssignable, Category = "VOSK|Events")
	FVibeVoskResultDelegate OnRecognitionResult;

	/** Событие: получен частичный результат */
	UPROPERTY(BlueprintAssignable, Category = "VOSK|Events")
	FVibeVoskResultDelegate OnPartialRecognitionResult;

private:
	/** Внутренний handle распознавателя */
	void* RecognizerHandle = nullptr;

	/** Ссылка на модель */
	UPROPERTY()
	UVibeVoskModel* LinkedModel = nullptr;

	/** Частота дискретизации */
	UPROPERTY()
	float SampleRate = 16000.0f;

	/** Последняя уверенность распознавания */
	float LastConfidence = 0.0f;

	/** Внутренний метод: получить результат с уверенностью */
	FString GetResultWithConfidence(float& OutConfidence);
};
