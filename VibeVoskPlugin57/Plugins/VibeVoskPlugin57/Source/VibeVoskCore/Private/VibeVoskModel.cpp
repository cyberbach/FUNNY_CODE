// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#include "VibeVoskModel.h"
#include "VibeVoskCoreModule.h"
#include "VibeVoskApiWrapper.h"
#include "VibeVoskPluginSettings.h"
#include "Misc/Paths.h"

UVibeVoskModel* UVibeVoskModel::CreateVoskModel(const FString& ModelPath)
{
	if (!FVibeVoskApiFunctions::Get().IsLoaded())
	{
		UE_LOG(LogVibeVosk, Error, TEXT("VoskModel: VOSK API not loaded"));
		return nullptr;
	}

	// Если передали имя модели (а не полный путь), соберём полный путь из настроек
	FString FullPath = ModelPath;
	if (!FPaths::DirectoryExists(FullPath))
	{
		// Если строка не содержит разделителей путей, считаем это именем модели
		if (!ModelPath.Contains(TEXT("/")) && !ModelPath.Contains(TEXT("\\")))
		{
			FString ModelsDir = UVibeVoskPluginSettings::Get()->GetVibeVoskModelsPath();
			FullPath = FPaths::Combine(ModelsDir, ModelPath);
		}

		if (!FPaths::DirectoryExists(FullPath))
		{
			UE_LOG(LogVibeVosk, Error, TEXT("VoskModel: Model path does not exist: %s"), *FullPath);
			return nullptr;
		}
	}

	// Создание объекта
	UVibeVoskModel* Model = NewObject<UVibeVoskModel>();
	Model->AddToRoot(); // Защита от GC до явного вызова DestroyModel
	Model->ModelPath = FullPath;

	// Загрузка модели через VOSK API
	FTCHARToUTF8 Utf8Path(*FullPath);
	Model->ModelHandle = (void*)VIBEVOSK_API(vosk_model_new)(Utf8Path.Get());

	if (Model->ModelHandle == nullptr)
	{
		UE_LOG(LogVibeVosk, Error, TEXT("VoskModel: Failed to load model: %s"), *FullPath);
		Model->RemoveFromRoot();
		Model->ConditionalBeginDestroy();
		return nullptr;
	}

	UE_LOG(LogVibeVosk, Log, TEXT("VoskModel: Model loaded successfully: %s"), *FullPath);
	return Model;
}

void UVibeVoskModel::DestroyModel()
{
	if (ModelHandle != nullptr && FVibeVoskApiFunctions::Get().IsLoaded())
	{
		VIBEVOSK_API(vosk_model_free)(static_cast<VoskModel*>(ModelHandle));
		ModelHandle = nullptr;
		ModelPath.Empty();
		UE_LOG(LogVibeVosk, Log, TEXT("VoskModel: Model destroyed"));
	}
	RemoveFromRoot();
}

void UVibeVoskModel::BeginDestroy()
{
	// Автоматическое освобождение нативных ресурсов при сборке мусора
	if (ModelHandle != nullptr && FVibeVoskApiFunctions::Get().IsLoaded())
	{
		UE_LOG(LogVibeVosk, Log, TEXT("VoskModel: Auto-releasing model in BeginDestroy"));
		VIBEVOSK_API(vosk_model_free)(static_cast<VoskModel*>(ModelHandle));
		ModelHandle = nullptr;
	}
	Super::BeginDestroy();
}
