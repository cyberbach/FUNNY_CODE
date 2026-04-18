// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "LocalSTTPluginSettings.generated.h"

/**
 * Настройки плагина LocalSTT
 */
UCLASS(Config = Game, DefaultConfig, meta = (DisplayName = "Local STT Plugin"))
class LOCALSTTCORE_API ULocalSTTPluginSettings : public UDeveloperSettings
{
	GENERATED_BODY()

public:
	ULocalSTTPluginSettings();

	/** Путь к папке с моделями (по умолчанию используется PluginDir/Models) */
	UPROPERTY(Config, EditAnywhere, Category = "Models", meta = (DisplayName = "Models Directory"))
	FString ModelsDirectory;

	/** Модель по умолчанию для распознавания */
	UPROPERTY(Config, EditAnywhere, Category = "Models", meta = (DisplayName = "Default Model"))
	FString DefaultModelName;

	/** Включить логирование LocalSTT */
	UPROPERTY(Config, EditAnywhere, Category = "Debug", meta = (DisplayName = "Enable LocalSTT Logging"))
	bool bEnableLocalSTTLogging;

	/** Уровень логирования LocalSTT (0-3) */
	UPROPERTY(Config, EditAnywhere, Category = "Debug", meta = (DisplayName = "LocalSTT Log Level", ClampMin = "0", ClampMax = "3"))
	int32 VoskLogLevel;

	/** Получить синглтон настроек */
	static ULocalSTTPluginSettings* Get();

	/** Получить путь к моделям */
	FString GetLocalSTTModelsPath() const;

	/** Получить полный путь к модели по умолчанию */
	FString GetDefaultModelPath() const;

	/** Проверить наличие VOSK SDK */
	bool IsVoskSdkInstalled() const;

	/** Проверить наличие модели */
	bool IsModelInstalled(const FString& ModelName) const;

	/** Получить путь к Binaries/Win64 папке плагина */
	FString GetPluginBinPath() const;

	/** Автоматически добавлять папку Models в DirectoriesToAlwaysStageAsNonUFS при старте редактора */
	UPROPERTY(Config, EditAnywhere, Category = "Packaging", meta = (DisplayName = "Auto-add Models to Packaging"))
	bool bAutoAddModelsToPackaging;
};
