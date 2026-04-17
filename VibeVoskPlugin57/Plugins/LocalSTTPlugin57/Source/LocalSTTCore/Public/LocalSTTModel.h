// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "LocalSTTModel.generated.h"

/**
 * Делегат для событий распознавания
 */
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FLocalSTTResultDelegate, const FString&, Result);

/**
 * Модель LocalSTT для распознавания речи
 * Обёртка над VoskModel из LocalSTT API
 */
UCLASS(BlueprintType, Category = "LocalSTT")
class LOCALSTTCORE_API ULocalSTTModel : public UObject
{
	GENERATED_BODY()

public:
	/** Создать модель из пути к папке с моделью */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT|Model")
	static ULocalSTTModel* CreateVoskModel(const FString& ModelPath);

	/** Освободить модель */
	UFUNCTION(BlueprintCallable, Category = "LocalSTT|Model")
	void DestroyModel();

	/** Проверка валидности модели */
	UFUNCTION(BlueprintPure, Category = "LocalSTT|Model")
	bool IsValid() const { return ModelHandle != nullptr; }

	/** Получить путь к модели */
	UFUNCTION(BlueprintPure, Category = "LocalSTT|Model")
	FString GetModelPath() const { return ModelPath; }

	/** Получить нативный handle модели LocalSTT */
	void* GetNativeHandle() const { return ModelHandle; }

	/** Автоматическое освобождение нативных ресурсов при сборке мусора */
	virtual void BeginDestroy() override;

protected:
	/** Путь к папке с моделью */
	UPROPERTY()
	FString ModelPath;

private:
	/** Внутренний handle модели LocalSTT (не модифицировать напрямую!) */
	void* ModelHandle = nullptr;
};
