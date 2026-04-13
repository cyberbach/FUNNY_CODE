// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#include "VibeVoskBlueprintLibrary.h"
#include "VibeVoskCoreModule.h"
#include "VibeVoskPluginSettings.h"

UVibeVoskModel* UVibeVoskBlueprintLibrary::CreateVoskModel(const FString& ModelName)
{
	FString Name = ModelName;
	if (Name.IsEmpty())
	{
		Name = UVibeVoskPluginSettings::Get()->DefaultModelName;
	}

	// UVibeVoskModel::CreateModel теперь принимает как полный путь, так и имя модели
	return UVibeVoskModel::CreateVoskModel(Name);
}

UVibeVoskRecognizer* UVibeVoskBlueprintLibrary::CreateVoskRecognizer(UVibeVoskModel* Model, float SampleRate)
{
	return UVibeVoskRecognizer::CreateRecognizer(Model, SampleRate);
}

UVibeVoskRecognizer* UVibeVoskBlueprintLibrary::CreateVoskRecognizerWithGrammar(UVibeVoskModel* Model, const TArray<FString>& Words, float SampleRate)
{
	return UVibeVoskRecognizer::CreateRecognizerWithGrammar(Model, Words, SampleRate);
}

bool UVibeVoskBlueprintLibrary::ProcessAudio(UVibeVoskRecognizer* Recognizer, const TArray<float>& AudioData)
{
	if (Recognizer == nullptr || !Recognizer->IsValid())
	{
		return false;
	}

	return Recognizer->ProcessAudio(AudioData);
}

FString UVibeVoskBlueprintLibrary::GetRecognitionResult(UVibeVoskRecognizer* Recognizer)
{
	if (Recognizer == nullptr || !Recognizer->IsValid())
	{
		return FString();
	}

	return Recognizer->GetResult();
}

FString UVibeVoskBlueprintLibrary::GetPartialRecognitionResult(UVibeVoskRecognizer* Recognizer)
{
	if (Recognizer == nullptr || !Recognizer->IsValid())
	{
		return FString();
	}

	return Recognizer->GetPartialResult();
}

void UVibeVoskBlueprintLibrary::ResetRecognizer(UVibeVoskRecognizer* Recognizer)
{
	if (Recognizer != nullptr && Recognizer->IsValid())
	{
		Recognizer->Reset();
	}
}

void UVibeVoskBlueprintLibrary::DestroyModel(UVibeVoskModel* Model)
{
	if (Model != nullptr)
	{
		Model->DestroyModel();
	}
}

void UVibeVoskBlueprintLibrary::DestroyRecognizer(UVibeVoskRecognizer* Recognizer)
{
	if (Recognizer != nullptr)
	{
		Recognizer->DestroyRecognizer();
	}
}

bool UVibeVoskBlueprintLibrary::IsModelValid(UVibeVoskModel* Model)
{
	return Model != nullptr && Model->IsValid();
}

bool UVibeVoskBlueprintLibrary::IsRecognizerValid(UVibeVoskRecognizer* Recognizer)
{
	return Recognizer != nullptr && Recognizer->IsValid();
}

FString UVibeVoskBlueprintLibrary::GetDefaultModelPath()
{
	return UVibeVoskPluginSettings::Get()->GetDefaultModelPath();
}

FString UVibeVoskBlueprintLibrary::GetVoskVersion()
{
	return TEXT("0.3.45");
}
