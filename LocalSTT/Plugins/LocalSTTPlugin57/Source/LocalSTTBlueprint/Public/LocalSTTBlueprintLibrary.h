// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "LocalSTTModel.h"
#include "LocalSTTRecognizer.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "LocalSTTBlueprintLibrary.generated.h"

/**
 * Библиотека функций для работы с Local STT в Blueprint
 */
UCLASS()
class LOCALSTTBLUEPRINT_API ULocalSTTBlueprintLibrary : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:
	/**
	 * Создать модель Local STT
	 *
	* @param ModelName Имя папки с моделью (например: vosk-model-small-ru-0.22) или полный путь. Если пусто, используется модель по умолчанию.
	 */
    UFUNCTION(BlueprintCallable, Category = "Local STT")
    static ULocalSTTModel* CreateSTTModel(const FString& ModelName = TEXT(""));

	/**
	 * Создать распознаватель
	 *
	 * @param Model Модель Local STT
	 * @param SampleRate Частота дискретизации (по умолчанию 16000)
	 */
	UFUNCTION(BlueprintCallable, Category = "Local STT")
	static ULocalSTTRecognizer* CreateSTTRecognizer(ULocalSTTModel* Model, float SampleRate = 16000.0f);

	/**
	 * Создать распознаватель с ограниченной грамматикой
	 *
	 * @param Model Модель Local STT
	 * @param Words Список допустимых слов/фраз для распознавания
	 * @param SampleRate Частота дискретизации (по умолчанию 16000)
	 */
	UFUNCTION(BlueprintCallable, Category = "Local STT")
	static ULocalSTTRecognizer* CreateSTTRecognizerWithGrammar(ULocalSTTModel* Model, const TArray<FString>& Words, float SampleRate = 16000.0f);

	/**
	 * Обработать аудиоданные и получить результат
	 *
	 * @param Recognizer Распознаватель
	 * @param AudioData Массив аудио сэмплов (float, -1.0 до 1.0)
	 * @return true если обработка успешна
	 */
	UFUNCTION(BlueprintCallable, Category = "Local STT")
	static bool ProcessAudio(ULocalSTTRecognizer* Recognizer, const TArray<float>& AudioData);

	/**
	 * Получить финальный результат распознавания
	 */
	UFUNCTION(BlueprintCallable, Category = "Local STT")
	static FString GetRecognitionResult(ULocalSTTRecognizer* Recognizer);

	/**
	 * Получить частичный результат (в процессе речи)
	 */
	UFUNCTION(BlueprintCallable, Category = "Local STT")
	static FString GetPartialRecognitionResult(ULocalSTTRecognizer* Recognizer);

	/**
	 * Сбросить распознаватель для новой сессии
	 */
	UFUNCTION(BlueprintCallable, Category = "Local STT")
	static void ResetRecognizer(ULocalSTTRecognizer* Recognizer);

	/**
	 * Освободить модель
	 */
	UFUNCTION(BlueprintCallable, Category = "Local STT")
	static void DestroyModel(ULocalSTTModel* Model);

	/**
	 * Освободить распознаватель
	 */
	UFUNCTION(BlueprintCallable, Category = "Local STT")
	static void DestroyRecognizer(ULocalSTTRecognizer* Recognizer);

	/**
	 * Проверить валидность модели
	 */
	UFUNCTION(BlueprintPure, Category = "Local STT")
	static bool IsModelValid(ULocalSTTModel* Model);

	/**
	 * Проверить валидность распознавателя
	 */
	UFUNCTION(BlueprintPure, Category = "Local STT")
	static bool IsRecognizerValid(ULocalSTTRecognizer* Recognizer);

	/**
	 * Получить путь к модели по умолчанию
	 */
	UFUNCTION(BlueprintPure, Category = "Local STT")
	static FString GetDefaultModelPath();

	/**
	 * Получить версию Local STT
	 */
	UFUNCTION(BlueprintPure, Category = "Local STT")
	static FString GetSTTVersion();
};
