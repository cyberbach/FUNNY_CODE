// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "VibeVoskPluginSettings.generated.h"

/**
 * Настройки плагина VoskPlugin57
 */
UCLASS(Config = Game, DefaultConfig, meta = (DisplayName = "VOSK Plugin"))
class VIBEVOSKCORE_API UVibeVoskPluginSettings : public UDeveloperSettings
{
	GENERATED_BODY()

public:
	UVibeVoskPluginSettings();

	/** Путь к папке с моделями (по умолчанию используется PluginDir/Models) */
	UPROPERTY(Config, EditAnywhere, Category = "Models", meta = (DisplayName = "Models Directory"))
	FString ModelsDirectory;

	/** Модель по умолчанию для распознавания */
	UPROPERTY(Config, EditAnywhere, Category = "Models", meta = (DisplayName = "Default Model"))
	FString DefaultModelName;

	/** Включить логирование VOSK */
	UPROPERTY(Config, EditAnywhere, Category = "Debug", meta = (DisplayName = "Enable VOSK Logging"))
	bool bEnableVibeVoskLogging;

	/** Уровень логирования VOSK (0-3) */
	UPROPERTY(Config, EditAnywhere, Category = "Debug", meta = (DisplayName = "VOSK Log Level", ClampMin = "0", ClampMax = "3"))
	int32 VoskLogLevel;

	/** Получить синглтон настроек */
	static UVibeVoskPluginSettings* Get();

	/** Получить путь к моделям */
	FString GetVibeVoskModelsPath() const;

	/** Получить полный путь к модели по умолчанию */
	FString GetDefaultModelPath() const;

	/** Проверить наличие VOSK SDK */
	bool IsVoskSdkInstalled() const;

	/** Проверить наличие модели */
	bool IsModelInstalled(const FString& ModelName) const;

	/** Получить путь к Binaries/Win64 папке плагина */
	FString GetPluginBinPath() const;
};
