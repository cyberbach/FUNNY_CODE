// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#include "LocalSTTBlueprintModule.h"

#define LOCTEXT_NAMESPACE "FLocalSTTBlueprintModule"

void FLocalSTTBlueprintModule::StartupModule()
{
	UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTBlueprint: Module started"));
}

void FLocalSTTBlueprintModule::ShutdownModule()
{
	UE_LOG(LogLocalSTT, Log, TEXT("LocalSTTBlueprint: Module shutdown"));
}

#undef LOCTEXT_NAMESPACE

IMPLEMENT_MODULE(FLocalSTTBlueprintModule, LocalSTTBlueprint)
