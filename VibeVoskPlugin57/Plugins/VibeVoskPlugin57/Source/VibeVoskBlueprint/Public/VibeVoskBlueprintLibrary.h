// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "VibeVoskModel.h"
#include "VibeVoskRecognizer.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "VibeVoskBlueprintLibrary.generated.h"

/**
 * Библиотека функций для работы с Vibe Vosk в Blueprint
 */
UCLASS()
class VIBEVOSKBLUEPRINT_API UVibeVoskBlueprintLibrary : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:
	/**
	 * Создать модель Vibe Vosk
	 *
	* @param ModelName Имя папки с моделью (например: vosk-model-small-ru-0.22) или полный путь. Если пусто, используется модель по умолчанию.
	 */
    UFUNCTION(BlueprintCallable, Category = "Vibe Vosk")
    static UVibeVoskModel* CreateVoskModel(const FString& ModelName = TEXT(""));

	/**
	 * Создать распознаватель
	 *
	 * @param Model Модель Vibe Vosk
	 * @param SampleRate Частота дискретизации (по умолчанию 16000)
	 */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk")
	static UVibeVoskRecognizer* CreateVoskRecognizer(UVibeVoskModel* Model, float SampleRate = 16000.0f);

	/**
	 * Создать распознаватель с ограниченной грамматикой
	 *
	 * @param Model Модель Vibe Vosk
	 * @param Words Список допустимых слов/фраз для распознавания
	 * @param SampleRate Частота дискретизации (по умолчанию 16000)
	 */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk")
	static UVibeVoskRecognizer* CreateVoskRecognizerWithGrammar(UVibeVoskModel* Model, const TArray<FString>& Words, float SampleRate = 16000.0f);

	/**
	 * Обработать аудиоданные и получить результат
	 *
	 * @param Recognizer Распознаватель
	 * @param AudioData Массив аудио сэмплов (float, -1.0 до 1.0)
	 * @return true если обработка успешна
	 */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk")
	static bool ProcessAudio(UVibeVoskRecognizer* Recognizer, const TArray<float>& AudioData);

	/**
	 * Получить финальный результат распознавания
	 */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk")
	static FString GetRecognitionResult(UVibeVoskRecognizer* Recognizer);

	/**
	 * Получить частичный результат (в процессе речи)
	 */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk")
	static FString GetPartialRecognitionResult(UVibeVoskRecognizer* Recognizer);

	/**
	 * Сбросить распознаватель для новой сессии
	 */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk")
	static void ResetRecognizer(UVibeVoskRecognizer* Recognizer);

	/**
	 * Освободить модель
	 */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk")
	static void DestroyModel(UVibeVoskModel* Model);

	/**
	 * Освободить распознаватель
	 */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk")
	static void DestroyRecognizer(UVibeVoskRecognizer* Recognizer);

	/**
	 * Проверить валидность модели
	 */
	UFUNCTION(BlueprintPure, Category = "Vibe Vosk")
	static bool IsModelValid(UVibeVoskModel* Model);

	/**
	 * Проверить валидность распознавателя
	 */
	UFUNCTION(BlueprintPure, Category = "Vibe Vosk")
	static bool IsRecognizerValid(UVibeVoskRecognizer* Recognizer);

	/**
	 * Получить путь к модели по умолчанию
	 */
	UFUNCTION(BlueprintPure, Category = "Vibe Vosk")
	static FString GetDefaultModelPath();

	/**
	 * Получить версию Vibe Vosk
	 */
	UFUNCTION(BlueprintPure, Category = "Vibe Vosk")
	static FString GetVoskVersion();
};
