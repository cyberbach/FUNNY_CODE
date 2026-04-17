// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#include "LocalSTTCoreModule.h"
#include "LocalSTTApiWrapper.h"
#include "LocalSTTPluginSettings.h"
#include "Interfaces/IPluginManager.h"
#include "HAL/PlatformProcess.h"
#include "HAL/FileManager.h"
#include "Misc/Paths.h"

#define LOCTEXT_NAMESPACE "FLocalSTTCoreModule"

DEFINE_LOG_CATEGORY(LogLocalSTT);

void FLocalSTTCoreModule::StartupModule()
{
	if (!LoadLocalSTTDll())
	{
		UE_LOG(LogLocalSTT, Error, TEXT("LocalSTTCore: Failed to load VOSK library. Make sure libvosk.dll and MinGW dependencies are in Binaries/Win64/"));
	}
	else
	{
		UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTCore: VOSK library loaded successfully."));
	}

	// Сканируем директорию моделей для получения списка доступных моделей
	ScanAvailableModels();
}

void FLocalSTTCoreModule::ShutdownModule()
{
	FreeLocalSTTLibrary();
	UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTCore: Module shutdown"));
}

void FLocalSTTCoreModule::ScanAvailableModels()
{
	AvailableModels.Empty();

	FString ModelsDir = ULocalSTTPluginSettings::Get()->GetLocalSTTModelsPath();
	if (!FPaths::DirectoryExists(ModelsDir))
	{
		UE_LOG(LogLocalSTT, Warning, TEXT("LocalSTTCore: Models directory not found: %s"), *ModelsDir);
		return;
	}

	TArray<FString> Subdirectories;
	IFileManager::Get().FindFiles(Subdirectories, *(ModelsDir / TEXT("*")), false, true);

	for (const FString& DirName : Subdirectories)
	{
		// Проверяем, что это похоже на модель VOSK (содержит файлы конфигурации)
		FString FullModelPath = FPaths::Combine(ModelsDir, DirName);
		FString ConfPath = FPaths::Combine(FullModelPath, TEXT("conf"));
		FString MfccConfPath = FPaths::Combine(FullModelPath, TEXT("conf"), TEXT("mfcc.conf"));

		if (FPaths::DirectoryExists(ConfPath) || FPaths::FileExists(MfccConfPath))
		{
			AvailableModels.Add(DirName);
			UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTCore: Found model: %s"), *DirName);
		}
	}

	if (AvailableModels.Num() == 0)
	{
		UE_LOG(LogLocalSTT, Warning, TEXT("LocalSTTCore: No VOSK models found in: %s"), *ModelsDir);
	}
	else
	{
		UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTCore: Found %d model(s)"), AvailableModels.Num());
	}
}

bool FLocalSTTCoreModule::LoadLocalSTTDll()
{
	FString DllPath = GetLocalSTTDllPath();

	if (!FPaths::FileExists(DllPath))
	{
		UE_LOG(LogLocalSTT, Error, TEXT("LocalSTTCore: VOSK DLL not found at: %s"), *DllPath);
		return false;
	}

	// Загружаем через FLocalSTTApiFunctions — это заполнит все указатели на функции
	void* Handle = FLocalSTTApiFunctions::Get().LoadLibrary(DllPath);

	if (Handle == nullptr)
	{
		UE_LOG(LogLocalSTT, Error, TEXT("LocalSTTCore: Failed to load VOSK DLL: %s. Check that libgcc_s_seh-1.dll, libstdc++-6.dll, libwinpthread-1.dll are present."), *DllPath);
		return false;
	}

	// Применяем настройки логирования VOSK из плагин-настроек
	if (FLocalSTTApiFunctions::Get().vosk_set_log_level)
	{
		const ULocalSTTPluginSettings* Settings = ULocalSTTPluginSettings::Get();
		int32 VoskSdkLogLevel = Settings->bEnableLocalSTTLogging ? Settings->VoskLogLevel : 0;
		FLocalSTTApiFunctions::Get().vosk_set_log_level(VoskSdkLogLevel);
	}

	VoskHandle = Handle;
	return true;
}

void FLocalSTTCoreModule::FreeLocalSTTLibrary()
{
	FLocalSTTApiFunctions::Get().FreeLibrary();
	VoskHandle = nullptr;
}

FString FLocalSTTCoreModule::GetLocalSTTDllPath() const
{
	TSharedPtr<IPlugin> Plugin = IPluginManager::Get().FindPlugin(TEXT("LocalSTTPlugin57"));

	UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTCore: ProjectDir: %s"), *FPaths::ProjectDir());

	if (Plugin.IsValid())
	{
		UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTCore: Plugin BaseDir: %s"), *Plugin->GetBaseDir());
	}

	TArray<FString> SearchPaths;

	// 1. Binaries плагина (через IPluginManager)
	if (Plugin.IsValid())
	{
		SearchPaths.Add(FPaths::Combine(Plugin->GetBaseDir(), TEXT("Binaries/Win64/libvosk.dll")));
	}

	// 2. Binaries проекта
	SearchPaths.Add(FPaths::Combine(FPaths::ProjectDir(), TEXT("Binaries/Win64/libvosk.dll")));

	// 3. Plugins папка
	SearchPaths.Add(FPaths::Combine(FPaths::ProjectDir(), TEXT("Plugins/LocalSTTPlugin57/Binaries/Win64/libvosk.dll")));

	// 4. ThirdParty плагина
	SearchPaths.Add(FPaths::Combine(FPaths::ProjectDir(), TEXT("Plugins/LocalSTTPlugin57/ThirdParty/vosk/bin/libvosk.dll")));

	// 5. Рядом с исполняемым файлом
	FString ExeDir = FPaths::GetPath(FPlatformProcess::ExecutablePath());
	SearchPaths.Add(FPaths::Combine(ExeDir, TEXT("libvosk.dll")));

	for (const FString& Path : SearchPaths)
	{
		UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTCore: Checking: %s"), *Path);
		if (FPaths::FileExists(Path))
		{
			UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTCore: Found DLL at: %s"), *Path);
			return Path;
		}
	}

	FString FallbackPath = FPaths::Combine(FPaths::ProjectDir(), TEXT("Plugins/LocalSTTPlugin57/Binaries/Win64/libvosk.dll"));
	UE_LOG(LogLocalSTT, Error, TEXT("LocalSTTCore: DLL not found! Searched: Binaries/Win64, Plugins/LocalSTTPlugin57/Binaries/Win64, ThirdParty/vosk/bin. Using fallback: %s"), *FallbackPath);
	return FallbackPath;
}

IMPLEMENT_MODULE(FLocalSTTCoreModule, LocalSTTCore)
