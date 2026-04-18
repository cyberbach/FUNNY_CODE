#include "LocalSTTEditorModule.h"
#include "Modules/ModuleManager.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/Paths.h"
#include "Misc/ConfigCacheIni.h"
#include "Logging/LogMacros.h"
#include "LocalSTTPluginSettings.h"

DEFINE_LOG_CATEGORY_STATIC(LogLocalSTTEditor, Log, All);

void FLocalSTTEditorModule::StartupModule()
{
#if WITH_EDITOR
    ULocalSTTPluginSettings* Settings = ULocalSTTPluginSettings::Get();
    if (!Settings)
    {
        UE_LOG(LogLocalSTTEditor, Warning, TEXT("LocalSTTPluginSettings not available."));
        return;
    }

    if (!Settings->bAutoAddModelsToPackaging)
    {
        UE_LOG(LogLocalSTTEditor, Verbose, TEXT("Auto-add Models to packaging is disabled in plugin settings."));
        return;
    }

    FString ModelsDir = Settings->GetLocalSTTModelsPath();

    if (ModelsDir.IsEmpty())
    {
        UE_LOG(LogLocalSTTEditor, Warning, TEXT("Models directory path is empty."));
        return;
    }

    // Write to project packaging settings INI section directly to avoid module include dependency
    const TCHAR* Section = TEXT("/Script/UnrealEd.ProjectPackagingSettings");
    const TCHAR* Key = TEXT("DirectoriesToAlwaysStageAsNonUFS");

    // Explicitly write to Config/DefaultGame.ini in the project directory so the Project Settings UI picks it up.
    FString GameIniPath = FPaths::Combine(FPaths::ProjectConfigDir(), TEXT("DefaultGame.ini"));

    TArray<FString> Existing;
    if (GConfig)
    {
        GConfig->GetArray(Section, Key, Existing, *GameIniPath);

        bool bAlready = false;
        for (const FString& Entry : Existing)
        {
            if (Entry.Contains(ModelsDir))
            {
                bAlready = true;
                break;
            }
        }

        if (!bAlready)
        {
            FString NewEntry = FString::Printf(TEXT("(Path=\"%s\")"), *ModelsDir);
            Existing.Add(NewEntry);
            GConfig->SetArray(Section, Key, Existing, *GameIniPath);
            GConfig->Flush(false, *GameIniPath);
            UE_LOG(LogLocalSTTEditor, Log, TEXT("Added Models directory to %s -> %s"), Section, *GameIniPath);
        }
    }
#endif
}

void FLocalSTTEditorModule::ShutdownModule()
{
}

IMPLEMENT_MODULE(FLocalSTTEditorModule, LocalSTTEditor)
