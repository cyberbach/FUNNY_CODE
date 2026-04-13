// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "VibeVoskModel.generated.h"

/**
 * Делегат для событий распознавания
 */
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FVibeVoskResultDelegate, const FString&, Result);

/**
 * Модель Vibe Vosk для распознавания речи
 * Обёртка над VoskModel из Vibe Vosk API
 */
UCLASS(BlueprintType, Category = "Vibe Vosk")
class VIBEVOSKCORE_API UVibeVoskModel : public UObject
{
	GENERATED_BODY()

public:
	/** Создать модель из пути к папке с моделью */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk|Model")
	static UVibeVoskModel* CreateVoskModel(const FString& ModelPath);

	/** Освободить модель */
	UFUNCTION(BlueprintCallable, Category = "Vibe Vosk|Model")
	void DestroyModel();

	/** Проверка валидности модели */
	UFUNCTION(BlueprintPure, Category = "Vibe Vosk|Model")
	bool IsValid() const { return ModelHandle != nullptr; }

	/** Получить путь к модели */
	UFUNCTION(BlueprintPure, Category = "Vibe Vosk|Model")
	FString GetModelPath() const { return ModelPath; }

	/** Получить нативный handle модели Vibe Vosk */
	void* GetNativeHandle() const { return ModelHandle; }

	/** Автоматическое освобождение нативных ресурсов при сборке мусора */
	virtual void BeginDestroy() override;

protected:
	/** Путь к папке с моделью */
	UPROPERTY()
	FString ModelPath;

private:
	/** Внутренний handle модели Vibe Vosk (не модифицировать напрямую!) */
	void* ModelHandle = nullptr;
};
