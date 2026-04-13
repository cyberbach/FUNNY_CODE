// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#include "VibeVoskPluginSettings.h"
#include "VibeVoskCoreModule.h"
#include "Misc/ConfigCacheIni.h"
#include "Misc/Paths.h"
#include "Misc/App.h"
#include "HAL/PlatformProperties.h"
#include "HAL/PlatformProcess.h"
#include "HAL/FileManager.h"
#include "Interfaces/IPluginManager.h"

UVibeVoskPluginSettings::UVibeVoskPluginSettings()
	: ModelsDirectory(TEXT(""))
	, DefaultModelName(TEXT("vosk-model-small-ru-0.22"))
	, bEnableVibeVoskLogging(false)
	, VoskLogLevel(0)
{
}

UVibeVoskPluginSettings* UVibeVoskPluginSettings::Get()
{
	return StaticClass()->GetDefaultObject<UVibeVoskPluginSettings>();
}

FString UVibeVoskPluginSettings::GetVibeVoskModelsPath() const
{
	if (!ModelsDirectory.IsEmpty())
	{
		return ModelsDirectory;
	}

	// Предпочитаем папку внутри плагина: Plugins/VibeVoskPlugin57/Binaries/Win64/Models
	FString BaseDir;
	TSharedPtr<IPlugin> Plugin = IPluginManager::Get().FindPlugin(TEXT("VibeVoskPlugin57"));
	if (Plugin.IsValid())
	{
		FString PluginDir = Plugin->GetBaseDir();
		FString PluginBinModels = FPaths::Combine(PluginDir, TEXT("Binaries"), TEXT("Win64"), TEXT("Models"));
		if (FPaths::DirectoryExists(PluginBinModels))
		{
			BaseDir = PluginBinModels;
		}
		else
		{
			// Попытка использовать плагинскую папку даже если её ещё нет (создадим в редакторе ниже)
			BaseDir = PluginBinModels;
		}
	}
	else
	{
		// Если плагин не найден (редкий случай в упакованной игре), пробуем папку проекта
		FString ProjectPluginBin = FPaths::Combine(FPaths::ProjectDir(), TEXT("Plugins"), TEXT("VibeVoskPlugin57"), TEXT("Binaries"), TEXT("Win64"), TEXT("Models"));
		if (FPaths::DirectoryExists(ProjectPluginBin))
		{
			BaseDir = ProjectPluginBin;
		}
		else
		{
			// Фоллбэк: папка рядом с исполняемым файлом
			BaseDir = FPaths::GetPath(FPlatformProcess::ExecutablePath());
			BaseDir = FPaths::Combine(BaseDir, TEXT("VoskModels"));
		}
	}

	// Создаём папку, если её нет (только в редакторе, в игре – только чтение)
	if (!FPaths::DirectoryExists(BaseDir) && !FApp::IsGame())
	{
		IFileManager::Get().MakeDirectory(*BaseDir, true);
	}

	return BaseDir;
}

FString UVibeVoskPluginSettings::GetPluginBinPath() const
{
	TSharedPtr<IPlugin> Plugin = IPluginManager::Get().FindPlugin(TEXT("VibeVoskPlugin57"));
	if (Plugin.IsValid())
	{
		return FPaths::Combine(Plugin->GetBaseDir(), TEXT("Binaries"), TEXT("Win64"));
	}
	return FString();
}

FString UVibeVoskPluginSettings::GetDefaultModelPath() const
{
	return FPaths::Combine(GetVibeVoskModelsPath(), DefaultModelName);
}

bool UVibeVoskPluginSettings::IsVoskSdkInstalled() const
{
	TSharedPtr<IPlugin> Plugin = IPluginManager::Get().FindPlugin(TEXT("VibeVoskPlugin57"));
	if (Plugin.IsValid())
	{
		FString VoskDllPath = FPaths::Combine(Plugin->GetBaseDir(), TEXT("Binaries/Win64/libvosk.dll"));
		return FPaths::FileExists(VoskDllPath);
	}

	FString ProjectDllPath = FPaths::Combine(FPaths::ProjectDir(), TEXT("Binaries/Win64/libvosk.dll"));
	return FPaths::FileExists(ProjectDllPath);
}

bool UVibeVoskPluginSettings::IsModelInstalled(const FString& ModelName) const
{
	FString ModelsDir = GetVibeVoskModelsPath();
	FString ModelPath = FPaths::Combine(ModelsDir, ModelName);
	return FPaths::DirectoryExists(ModelPath);
}
