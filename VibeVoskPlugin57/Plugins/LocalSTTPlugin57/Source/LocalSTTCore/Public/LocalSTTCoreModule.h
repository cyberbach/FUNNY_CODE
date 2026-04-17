// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

/** Категория логирования LocalSTT */
LOCALSTTCORE_API DECLARE_LOG_CATEGORY_EXTERN(LogLocalSTT, Log, All);

class FLocalSTTCoreModule : public IModuleInterface
{
public:
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

	/** Проверить, загружена ли LocalSTT DLL */
	bool IsVoskLoaded() const { return VoskHandle != nullptr; }

private:
	/** Загрузить LocalSTT DLL */
	bool LoadLocalSTTDll();

	/** Освободить LocalSTT DLL */
	void FreeLocalSTTLibrary();

	/** Handle к LocalSTT DLL */
	void* VoskHandle = nullptr;

	/** Получить путь к LocalSTT DLL */
	FString GetLocalSTTDllPath() const;

public:
	/** Список доступных моделей, заполняется при инициализации модуля */
	const TArray<FString>& GetAvailableModels() const { return AvailableModels; }

	/** Получить инстанс модуля */
	static FLocalSTTCoreModule& Get()
	{
		return FModuleManager::LoadModuleChecked<FLocalSTTCoreModule>("LocalSTTCore");
	}

private:
	/** Массив имён моделей */
	TArray<FString> AvailableModels;

	/** Сканировать директорию моделей */
	void ScanAvailableModels();
};
