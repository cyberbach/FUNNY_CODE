// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#include "LocalSTTBlueprintLibrary.h"
#include "LocalSTTCoreModule.h"
#include "LocalSTTPluginSettings.h"

ULocalSTTModel* ULocalSTTBlueprintLibrary::CreateSTTModel(const FString& ModelName)
{
	FString Name = ModelName;
	if (Name.IsEmpty())
	{
		Name = ULocalSTTPluginSettings::Get()->DefaultModelName;
	}

	// ULocalSTTModel::CreateModel теперь принимает как полный путь, так и имя модели
	return ULocalSTTModel::CreateSTTModel(Name);
}

ULocalSTTRecognizer* ULocalSTTBlueprintLibrary::CreateSTTRecognizer(ULocalSTTModel* Model, float SampleRate)
{
	return ULocalSTTRecognizer::CreateRecognizer(Model, SampleRate);
}

ULocalSTTRecognizer* ULocalSTTBlueprintLibrary::CreateSTTRecognizerWithGrammar(ULocalSTTModel* Model, const TArray<FString>& Words, float SampleRate)
{
	return ULocalSTTRecognizer::CreateRecognizerWithGrammar(Model, Words, SampleRate);
}

bool ULocalSTTBlueprintLibrary::ProcessAudio(ULocalSTTRecognizer* Recognizer, const TArray<float>& AudioData)
{
	if (Recognizer == nullptr || !Recognizer->IsValid())
	{
		return false;
	}

	return Recognizer->ProcessAudio(AudioData);
}

FString ULocalSTTBlueprintLibrary::GetRecognitionResult(ULocalSTTRecognizer* Recognizer)
{
	if (Recognizer == nullptr || !Recognizer->IsValid())
	{
		return FString();
	}

	return Recognizer->GetResult();
}

FString ULocalSTTBlueprintLibrary::GetPartialRecognitionResult(ULocalSTTRecognizer* Recognizer)
{
	if (Recognizer == nullptr || !Recognizer->IsValid())
	{
		return FString();
	}

	return Recognizer->GetPartialResult();
}

void ULocalSTTBlueprintLibrary::ResetRecognizer(ULocalSTTRecognizer* Recognizer)
{
	if (Recognizer != nullptr && Recognizer->IsValid())
	{
		Recognizer->Reset();
	}
}

void ULocalSTTBlueprintLibrary::DestroyModel(ULocalSTTModel* Model)
{
	if (Model != nullptr)
	{
		Model->DestroyModel();
	}
}

void ULocalSTTBlueprintLibrary::DestroyRecognizer(ULocalSTTRecognizer* Recognizer)
{
	if (Recognizer != nullptr)
	{
		Recognizer->DestroyRecognizer();
	}
}

bool ULocalSTTBlueprintLibrary::IsModelValid(ULocalSTTModel* Model)
{
	return Model != nullptr && Model->IsValid();
}

bool ULocalSTTBlueprintLibrary::IsRecognizerValid(ULocalSTTRecognizer* Recognizer)
{
	return Recognizer != nullptr && Recognizer->IsValid();
}

FString ULocalSTTBlueprintLibrary::GetDefaultModelPath()
{
	return ULocalSTTPluginSettings::Get()->GetDefaultModelPath();
}

FString ULocalSTTBlueprintLibrary::GetSTTVersion()
{
	return TEXT("0.3.45");
}
