// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#include "VibeVoskCoreModule.h"
#include "VibeVoskApiWrapper.h"
#include "VibeVoskPluginSettings.h"
#include "Interfaces/IPluginManager.h"
#include "HAL/PlatformProcess.h"
#include "HAL/FileManager.h"
#include "Misc/Paths.h"

#define LOCTEXT_NAMESPACE "FVibeVoskCoreModule"

DEFINE_LOG_CATEGORY(LogVibeVosk);

void FVibeVoskCoreModule::StartupModule()
{
	if (!LoadVibeVoskDll())
	{
		UE_LOG(LogVibeVosk, Error, TEXT("VoskCore: Failed to load VOSK library. Make sure libvosk.dll and MinGW dependencies are in Binaries/Win64/"));
	}
	else
	{
		UE_LOG(LogVibeVosk, Log, TEXT("VoskCore: VOSK library loaded successfully."));
	}

	// Сканируем директорию моделей для получения списка доступных моделей
	ScanAvailableModels();
}

void FVibeVoskCoreModule::ShutdownModule()
{
	FreeVibeVoskLibrary();
	UE_LOG(LogVibeVosk, Log, TEXT("VoskCore: Module shutdown"));
}

void FVibeVoskCoreModule::ScanAvailableModels()
{
	AvailableModels.Empty();

	FString ModelsDir = UVibeVoskPluginSettings::Get()->GetVibeVoskModelsPath();
	if (!FPaths::DirectoryExists(ModelsDir))
	{
		UE_LOG(LogVibeVosk, Warning, TEXT("VoskCore: Models directory not found: %s"), *ModelsDir);
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
			UE_LOG(LogVibeVosk, Log, TEXT("VoskCore: Found model: %s"), *DirName);
		}
	}

	if (AvailableModels.Num() == 0)
	{
		UE_LOG(LogVibeVosk, Warning, TEXT("VoskCore: No VOSK models found in: %s"), *ModelsDir);
	}
	else
	{
		UE_LOG(LogVibeVosk, Log, TEXT("VoskCore: Found %d model(s)"), AvailableModels.Num());
	}
}

bool FVibeVoskCoreModule::LoadVibeVoskDll()
{
	FString DllPath = GetVibeVoskDllPath();

	if (!FPaths::FileExists(DllPath))
	{
		UE_LOG(LogVibeVosk, Error, TEXT("VoskCore: VOSK DLL not found at: %s"), *DllPath);
		return false;
	}

	// Загружаем через FVibeVoskApiFunctions — это заполнит все указатели на функции
	void* Handle = FVibeVoskApiFunctions::Get().LoadLibrary(DllPath);

	if (Handle == nullptr)
	{
		UE_LOG(LogVibeVosk, Error, TEXT("VoskCore: Failed to load VOSK DLL: %s. Check that libgcc_s_seh-1.dll, libstdc++-6.dll, libwinpthread-1.dll are present."), *DllPath);
		return false;
	}

	// Применяем настройки логирования VOSK из плагин-настроек
	if (FVibeVoskApiFunctions::Get().vosk_set_log_level)
	{
		const UVibeVoskPluginSettings* Settings = UVibeVoskPluginSettings::Get();
		int32 VoskSdkLogLevel = Settings->bEnableVibeVoskLogging ? Settings->VoskLogLevel : 0;
		FVibeVoskApiFunctions::Get().vosk_set_log_level(VoskSdkLogLevel);
	}

	VoskHandle = Handle;
	return true;
}

void FVibeVoskCoreModule::FreeVibeVoskLibrary()
{
	FVibeVoskApiFunctions::Get().FreeLibrary();
	VoskHandle = nullptr;
}

FString FVibeVoskCoreModule::GetVibeVoskDllPath() const
{
	TSharedPtr<IPlugin> Plugin = IPluginManager::Get().FindPlugin(TEXT("VibeVoskPlugin57"));

	UE_LOG(LogVibeVosk, Log, TEXT("VoskCore: ProjectDir: %s"), *FPaths::ProjectDir());

	if (Plugin.IsValid())
	{
		UE_LOG(LogVibeVosk, Log, TEXT("VoskCore: Plugin BaseDir: %s"), *Plugin->GetBaseDir());
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
	SearchPaths.Add(FPaths::Combine(FPaths::ProjectDir(), TEXT("Plugins/VibeVoskPlugin57/Binaries/Win64/libvosk.dll")));

	// 4. ThirdParty плагина
	SearchPaths.Add(FPaths::Combine(FPaths::ProjectDir(), TEXT("Plugins/VibeVoskPlugin57/ThirdParty/vosk/bin/libvosk.dll")));

	// 5. Рядом с исполняемым файлом
	FString ExeDir = FPaths::GetPath(FPlatformProcess::ExecutablePath());
	SearchPaths.Add(FPaths::Combine(ExeDir, TEXT("libvosk.dll")));

	for (const FString& Path : SearchPaths)
	{
		UE_LOG(LogVibeVosk, Log, TEXT("VoskCore: Checking: %s"), *Path);
		if (FPaths::FileExists(Path))
		{
			UE_LOG(LogVibeVosk, Log, TEXT("VoskCore: Found DLL at: %s"), *Path);
			return Path;
		}
	}

	FString FallbackPath = FPaths::Combine(FPaths::ProjectDir(), TEXT("Plugins/VibeVoskPlugin57/Binaries/Win64/libvosk.dll"));
	UE_LOG(LogVibeVosk, Error, TEXT("VoskCore: DLL not found! Searched: Binaries/Win64, Plugins/VibeVoskPlugin57/Binaries/Win64, ThirdParty/vosk/bin. Using fallback: %s"), *FallbackPath);
	return FallbackPath;
}

#undef LOCTEXT_NAMESPACE

IMPLEMENT_MODULE(FVibeVoskCoreModule, VibeVoskCore)
