// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#include "VibeVoskBlueprintModule.h"

#define LOCTEXT_NAMESPACE "FVibeVoskBlueprintModule"

void FVibeVoskBlueprintModule::StartupModule()
{
	UE_LOG(LogTemp, Log, TEXT("VibeVoskBlueprint: Module started"));
}

void FVibeVoskBlueprintModule::ShutdownModule()
{
	UE_LOG(LogTemp, Log, TEXT("VibeVoskBlueprint: Module shutdown"));
}

#undef LOCTEXT_NAMESPACE

IMPLEMENT_MODULE(FVibeVoskBlueprintModule, VibeVoskBlueprint)
