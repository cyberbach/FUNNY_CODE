// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

/** Категория логирования VOSK */
VIBEVOSKCORE_API DECLARE_LOG_CATEGORY_EXTERN(LogVibeVosk, Log, All);

class FVibeVoskCoreModule : public IModuleInterface
{
public:
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

	/** Проверить, загружена ли VOSK DLL */
	bool IsVoskLoaded() const { return VoskHandle != nullptr; }

private:
	/** Загрузить VOSK DLL */
	bool LoadVibeVoskDll();

	/** Освободить VOSK DLL */
	void FreeVibeVoskLibrary();

	/** Handle к VOSK DLL */
	void* VoskHandle = nullptr;

	/** Получить путь к VOSK DLL */
	FString GetVibeVoskDllPath() const;

public:
	/** Список доступных моделей, заполняется при инициализации модуля */
	const TArray<FString>& GetAvailableModels() const { return AvailableModels; }

	/** Получить инстанс модуля */
	static FVibeVoskCoreModule& Get()
	{
		return FModuleManager::LoadModuleChecked<FVibeVoskCoreModule>("VibeVoskCore");
	}

private:
	/** Массив имён моделей */
	TArray<FString> AvailableModels;

	/** Сканировать директорию моделей */
	void ScanAvailableModels();
};
