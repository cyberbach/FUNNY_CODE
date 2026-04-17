// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.

#include "LocalSTTRecognizer.h"
#include "LocalSTTModel.h"
#include "LocalSTTApiWrapper.h"
#include "LocalSTTCoreModule.h"
#include "Json.h"
#include "JsonUtilities.h"

// Консольная переменная для включения/выключения отладочных сообщений распознавателя
static TAutoConsoleVariable<bool> CVarLocalSTTRecognizerDebug(
	TEXT("vosk.RecognizerDebug"),
	false,
	TEXT("Enable LocalSTT recognizer debug logging"));

void SetLocalSTTRecognizerDebugMessages(bool bShow)
{
	CVarLocalSTTRecognizerDebug->Set(bShow);
}

#define LOCALSTT_REC_DEBUG_LOG(Format, ...) \
	do { if (CVarLocalSTTRecognizerDebug.GetValueOnAnyThread()) { UE_LOG(LogLocalSTT, Log, TEXT(Format), ##__VA_ARGS__); } } while(0)

// Парсит JSON от VOSK и извлекает текст, опционально — уверенность
static FString ParseLocalSTTJson(const char* JsonStr, float* OutConfidence = nullptr)
{
	if (JsonStr == nullptr)
	{
		return FString();
	}

	FString ResultJson = FString(UTF8_TO_TCHAR(JsonStr));

	TSharedPtr<FJsonObject> JsonObject;
	TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(ResultJson);

	if (FJsonSerializer::Deserialize(Reader, JsonObject) && JsonObject.IsValid())
	{
		FString Text;
		if (JsonObject->TryGetStringField(TEXT("text"), Text))
		{
			// Парсим confidence из массива "result" (если vosk_recognizer_set_words был включён)
			if (OutConfidence)
			{
				*OutConfidence = 0.0f;
				const TArray<TSharedPtr<FJsonValue>>* ResultArray;
				if (JsonObject->TryGetArrayField(TEXT("result"), ResultArray) && ResultArray->Num() > 0)
				{
					float TotalConf = 0.0f;
					int32 Count = 0;
					for (const auto& Item : *ResultArray)
					{
						const TSharedPtr<FJsonObject>& WordObj = Item->AsObject();
						if (WordObj.IsValid())
						{
							double Conf = 0.0;
							if (WordObj->TryGetNumberField(TEXT("conf"), Conf))
							{
								TotalConf += static_cast<float>(Conf);
								Count++;
							}
						}
					}
					if (Count > 0)
					{
						*OutConfidence = TotalConf / Count;
					}
				}
			}
			return Text;
		}
		// Для partial_result
		if (JsonObject->TryGetStringField(TEXT("partial"), Text))
		{
			return Text;
		}
	}

	return ResultJson;
}

ULocalSTTRecognizer* ULocalSTTRecognizer::CreateRecognizer(ULocalSTTModel* Model, float InSampleRate)
{
	if (!FLocalSTTApiFunctions::Get().IsLoaded())
	{
		UE_LOG(LogLocalSTT, Error, TEXT("VoskRecognizer: VOSK API not loaded"));
		return nullptr;
	}

	if (Model == nullptr || !Model->IsValid())
	{
		UE_LOG(LogLocalSTT, Error, TEXT("VoskRecognizer: Invalid model provided"));
		return nullptr;
	}

	// Создание объекта
	ULocalSTTRecognizer* Recognizer = NewObject<ULocalSTTRecognizer>();
	Recognizer->AddToRoot(); // Защита от GC до явного вызова DestroyRecognizer
	Recognizer->LinkedModel = Model;
	Recognizer->SampleRate = InSampleRate;

	// Создание распознавателя через VOSK API
	Recognizer->RecognizerHandle = (void*)LOCALSTT_API(vosk_recognizer_new)(
		static_cast<VoskModel*>(Model->GetNativeHandle()), InSampleRate);

	if (Recognizer->RecognizerHandle == nullptr)
	{
		UE_LOG(LogLocalSTT, Error, TEXT("VoskRecognizer: Failed to create recognizer"));
		Recognizer->RemoveFromRoot();
		Recognizer->ConditionalBeginDestroy();
		return nullptr;
	}

	// Включаем вывод слов с confidence в финальных результатах
	LOCALSTT_API(vosk_recognizer_set_words)(
		static_cast<VoskRecognizer*>(Recognizer->RecognizerHandle), 1);

	// Включаем частичные результаты с информацией о словах
	LOCALSTT_API(vosk_recognizer_set_partial_words)(
		static_cast<VoskRecognizer*>(Recognizer->RecognizerHandle), 1);

	UE_LOG(LogLocalSTT, Log, TEXT("VoskRecognizer: Created with sample rate: %f"), InSampleRate);
	return Recognizer;
}

ULocalSTTRecognizer* ULocalSTTRecognizer::CreateRecognizerWithGrammar(ULocalSTTModel* Model, const TArray<FString>& Words, float InSampleRate)
{
	if (!FLocalSTTApiFunctions::Get().IsLoaded())
	{
		UE_LOG(LogLocalSTT, Error, TEXT("VoskRecognizer: VOSK API not loaded"));
		return nullptr;
	}

	if (Model == nullptr || !Model->IsValid())
	{
		UE_LOG(LogLocalSTT, Error, TEXT("VoskRecognizer: Invalid model provided"));
		return nullptr;
	}

	if (Words.Num() == 0)
	{
		UE_LOG(LogLocalSTT, Warning, TEXT("VoskRecognizer: Empty grammar, creating standard recognizer"));
		return CreateRecognizer(Model, InSampleRate);
	}

	// Формируем JSON-грамматику: ["слово1", "слово2", "[unk]"]
	FString GrammarJson = TEXT("[");
	for (int32 i = 0; i < Words.Num(); i++)
	{
		if (i > 0) GrammarJson += TEXT(", ");
		GrammarJson += FString::Printf(TEXT("\"%s\""), *Words[i]);
	}
	GrammarJson += TEXT(", \"[unk]\"]");

	// Создание объекта
	ULocalSTTRecognizer* Recognizer = NewObject<ULocalSTTRecognizer>();
	Recognizer->AddToRoot();
	Recognizer->LinkedModel = Model;
	Recognizer->SampleRate = InSampleRate;

	// Создание распознавателя с грамматикой через VOSK API
	FTCHARToUTF8 Utf8Grammar(*GrammarJson);
	Recognizer->RecognizerHandle = (void*)LOCALSTT_API(vosk_recognizer_new_grm)(
		static_cast<VoskModel*>(Model->GetNativeHandle()), InSampleRate, Utf8Grammar.Get());

	if (Recognizer->RecognizerHandle == nullptr)
	{
		UE_LOG(LogLocalSTT, Error, TEXT("VoskRecognizer: Failed to create recognizer with grammar"));
		Recognizer->RemoveFromRoot();
		Recognizer->ConditionalBeginDestroy();
		return nullptr;
	}

	UE_LOG(LogLocalSTT, Log, TEXT("VoskRecognizer: Created with grammar (%d words), sample rate: %f"), Words.Num(), InSampleRate);
	return Recognizer;
}

void ULocalSTTRecognizer::DestroyRecognizer()
{
	if (RecognizerHandle != nullptr && FLocalSTTApiFunctions::Get().IsLoaded())
	{
		LOCALSTT_API(vosk_recognizer_free)(static_cast<VoskRecognizer*>(RecognizerHandle));
		RecognizerHandle = nullptr;
		LinkedModel = nullptr;
		UE_LOG(LogLocalSTT, Log, TEXT("VoskRecognizer: Recognizer destroyed"));
	}
	RemoveFromRoot();
}

void ULocalSTTRecognizer::BeginDestroy()
{
	// Автоматическое освобождение нативных ресурсов при сборке мусора
	if (RecognizerHandle != nullptr && FLocalSTTApiFunctions::Get().IsLoaded())
	{
		UE_LOG(LogLocalSTT, Log, TEXT("VoskRecognizer: Auto-releasing recognizer in BeginDestroy"));
		LOCALSTT_API(vosk_recognizer_free)(static_cast<VoskRecognizer*>(RecognizerHandle));
		RecognizerHandle = nullptr;
	}
	Super::BeginDestroy();
}

bool ULocalSTTRecognizer::ProcessAudio(const TArray<float>& AudioData)
{
	if (!FLocalSTTApiFunctions::Get().IsLoaded() || RecognizerHandle == nullptr || AudioData.Num() == 0)
	{
		return false;
	}

	// Логируем амплитуду для диагностики
	float MaxAmp = 0.0f;
	float SumAmp = 0.0f;
	for (float S : AudioData)
	{
		float A = FMath::Abs(S);
		MaxAmp = FMath::Max(MaxAmp, A);
		SumAmp += A;
	}
	float AvgAmp = SumAmp / AudioData.Num();
	LOCALSTT_REC_DEBUG_LOG("VoskRecognizer: Processing %d samples, MaxAmp=%.4f, AvgAmp=%.4f",
		AudioData.Num(), MaxAmp, AvgAmp);

	// Конвертируем float в int16 для VOSK (более надёжный формат)
	TArray<int16> Int16Data;
	Int16Data.SetNum(AudioData.Num());
	for (int32 i = 0; i < AudioData.Num(); i++)
	{
		Int16Data[i] = static_cast<int16>(FMath::Clamp(AudioData[i], -1.0f, 1.0f) * 32767.0f);
	}

	// Обработка через VOSK API (int16 версия)
	int Result = LOCALSTT_API(vosk_recognizer_accept_waveform_s)(
		static_cast<VoskRecognizer*>(RecognizerHandle),
		Int16Data.GetData(),
		Int16Data.Num()
	);

	LOCALSTT_REC_DEBUG_LOG("VoskRecognizer: Return code: %d", Result);

	// Получаем частичный результат ВСЕГДА
	FString Partial = GetPartialResult();
	if (!Partial.IsEmpty())
	{
		LOCALSTT_REC_DEBUG_LOG("VoskRecognizer: Partial result: %s", *Partial);
		OnPartialRecognitionResult.Broadcast(Partial);
	}

	// Если вернуло 1 — VOSK обнаружил конец фразы, получаем финальный результат
	if (Result == 1)
	{
		float Confidence = 0.0f;
		FString FinalResult = GetResultWithConfidence(Confidence);
		LastConfidence = Confidence;
		if (!FinalResult.IsEmpty())
		{
			LOCALSTT_REC_DEBUG_LOG("VoskRecognizer: Final result: %s (confidence: %.2f)", *FinalResult, Confidence);
			OnRecognitionResult.Broadcast(FinalResult);
		}
	}

	return Result != -1; // -1 = ошибка
}

bool ULocalSTTRecognizer::ProcessAudioInt16(const TArray<int16>& AudioData)
{
	if (!FLocalSTTApiFunctions::Get().IsLoaded() || RecognizerHandle == nullptr || AudioData.Num() == 0)
	{
		return false;
	}

	// Обработка через VOSK API
	int Result = LOCALSTT_API(vosk_recognizer_accept_waveform_s)(
		static_cast<VoskRecognizer*>(RecognizerHandle),
		AudioData.GetData(),
		AudioData.Num()
	);

	// Если вернуло 1 — VOSK обнаружил конец фразы, получаем финальный результат
	if (Result == 1)
	{
		float Confidence = 0.0f;
		FString FinalResult = GetResultWithConfidence(Confidence);
		LastConfidence = Confidence;
		if (!FinalResult.IsEmpty())
		{
			OnRecognitionResult.Broadcast(FinalResult);
		}
	}

	// Получаем частичный результат
	FString Partial = GetPartialResult();
	if (!Partial.IsEmpty())
	{
		OnPartialRecognitionResult.Broadcast(Partial);
	}

	return Result != -1;
}

FString ULocalSTTRecognizer::GetResult()
{
	if (!FLocalSTTApiFunctions::Get().IsLoaded() || RecognizerHandle == nullptr)
	{
		return FString();
	}

	const char* ResultCStr = LOCALSTT_API(vosk_recognizer_result)(static_cast<VoskRecognizer*>(RecognizerHandle));
	if (ResultCStr == nullptr)
	{
		return FString();
	}

	return ParseLocalSTTJson(ResultCStr);
}

FString ULocalSTTRecognizer::GetResultWithConfidence(float& OutConfidence)
{
	OutConfidence = 0.0f;
	if (!FLocalSTTApiFunctions::Get().IsLoaded() || RecognizerHandle == nullptr)
	{
		return FString();
	}

	const char* ResultCStr = LOCALSTT_API(vosk_recognizer_result)(static_cast<VoskRecognizer*>(RecognizerHandle));
	if (ResultCStr == nullptr)
	{
		return FString();
	}

	return ParseLocalSTTJson(ResultCStr, &OutConfidence);
}

FString ULocalSTTRecognizer::GetFinalResult()
{
	if (!FLocalSTTApiFunctions::Get().IsLoaded() || RecognizerHandle == nullptr)
	{
		return FString();
	}

	const char* ResultCStr = LOCALSTT_API(vosk_recognizer_final_result)(static_cast<VoskRecognizer*>(RecognizerHandle));
	if (ResultCStr == nullptr)
	{
		return FString();
	}

	float Confidence = 0.0f;
	FString Text = ParseLocalSTTJson(ResultCStr, &Confidence);
	LastConfidence = Confidence;
	return Text;
}

FString ULocalSTTRecognizer::GetPartialResult()
{
	if (!FLocalSTTApiFunctions::Get().IsLoaded() || RecognizerHandle == nullptr)
	{
		return FString();
	}

	const char* ResultCStr = LOCALSTT_API(vosk_recognizer_partial_result)(static_cast<VoskRecognizer*>(RecognizerHandle));
	if (ResultCStr == nullptr)
	{
		return FString();
	}

	return ParseLocalSTTJson(ResultCStr);
}

void ULocalSTTRecognizer::Reset()
{
	if (RecognizerHandle != nullptr && FLocalSTTApiFunctions::Get().IsLoaded())
	{
		LOCALSTT_API(vosk_recognizer_reset)(static_cast<VoskRecognizer*>(RecognizerHandle));
	}
}
