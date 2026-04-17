// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#include "LocalSTTModel.h"
#include "LocalSTTCoreModule.h"
#include "LocalSTTApiWrapper.h"
#include "LocalSTTPluginSettings.h"
#include "Misc/Paths.h"

ULocalSTTModel* ULocalSTTModel::CreateVoskModel(const FString& ModelPath)
{
	if (!FLocalSTTApiFunctions::Get().IsLoaded())
	{
		UE_LOG(LogLocalSTT, Error, TEXT("LocalSTTModel: VOSK API not loaded"));
		return nullptr;
	}

	// Если передали имя модели (а не полный путь), соберём полный путь из настроек
	FString FullPath = ModelPath;
	if (!FPaths::DirectoryExists(FullPath))
	{
		// Если строка не содержит разделителей путей, считаем это именем модели
		if (!ModelPath.Contains(TEXT("/")) && !ModelPath.Contains(TEXT("\\")))
		{
			FString ModelsDir = ULocalSTTPluginSettings::Get()->GetLocalSTTModelsPath();
			FullPath = FPaths::Combine(ModelsDir, ModelPath);
		}

		if (!FPaths::DirectoryExists(FullPath))
		{
			UE_LOG(LogLocalSTT, Error, TEXT("LocalSTTModel: Model path does not exist: %s"), *FullPath);
			return nullptr;
		}
	}

	// Создание объекта
	ULocalSTTModel* Model = NewObject<ULocalSTTModel>();
	Model->AddToRoot(); // Защита от GC до явного вызова DestroyModel
	Model->ModelPath = FullPath;

	// Загрузка модели через VOSK API
	FTCHARToUTF8 Utf8Path(*FullPath);
	Model->ModelHandle = (void*)LOCALSTT_API(vosk_model_new)(Utf8Path.Get());

	if (Model->ModelHandle == nullptr)
	{
		UE_LOG(LogLocalSTT, Error, TEXT("LocalSTTModel: Failed to load model: %s"), *FullPath);
		Model->RemoveFromRoot();
		Model->ConditionalBeginDestroy();
		return nullptr;
	}

	UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTModel: Model loaded successfully: %s"), *FullPath);
	return Model;
}

void ULocalSTTModel::DestroyModel()
{
	if (ModelHandle != nullptr && FLocalSTTApiFunctions::Get().IsLoaded())
	{
		LOCALSTT_API(vosk_model_free)(static_cast<VoskModel*>(ModelHandle));
		ModelHandle = nullptr;
		ModelPath.Empty();
		UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTModel: Model destroyed"));
	}
	RemoveFromRoot();
}

void ULocalSTTModel::BeginDestroy()
{
	// Автоматическое освобождение нативных ресурсов при сборке мусора
	if (ModelHandle != nullptr && FLocalSTTApiFunctions::Get().IsLoaded())
	{
		UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTModel: Auto-releasing model in BeginDestroy"));
		LOCALSTT_API(vosk_model_free)(static_cast<VoskModel*>(ModelHandle));
		ModelHandle = nullptr;
	}
	Super::BeginDestroy();
}
