// Copyright 2026 Andrey (cb) Mikheev. All Rights Reserved.
// Обёртка для динамической загрузки функций VOSK API

#pragma once

#include "CoreMinimal.h"
#include "HAL/PlatformProcess.h"
#include "Misc/Paths.h"

// Forward declaration VOSK types
typedef struct VoskModel VoskModel;
typedef struct VoskRecognizer VoskRecognizer;

/**
 * Динамическая загрузка функций VOSK API
 */
class FVibeVoskApiFunctions
{
public:
	static FVibeVoskApiFunctions& Get()
	{
		static FVibeVoskApiFunctions Instance;
		return Instance;
	}

	void* LoadLibrary(const FString& DllPath)
	{
		if (DllHandle != nullptr)
		{
			FreeLibrary();
		}

		FString DllDir = FPaths::GetPath(DllPath);
		FPlatformProcess::PushDllDirectory(*DllDir);
		DllHandle = FPlatformProcess::GetDllHandle(*DllPath);
		FPlatformProcess::PopDllDirectory(*DllDir);

		if (DllHandle == nullptr)
		{
			return nullptr;
		}

		// Загружаем все функции
		vosk_model_new = (VoskModel*(*)(const char*))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_model_new"));
		vosk_model_free = (void(*)(VoskModel*))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_model_free"));
		vosk_model_find_word = (int(*)(VoskModel*, const char*))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_model_find_word"));

		vosk_recognizer_new = (VoskRecognizer*(*)(VoskModel*, float))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_new"));
		vosk_recognizer_new_spk = (VoskRecognizer*(*)(VoskModel*, float, void*))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_new_spk"));
		vosk_recognizer_new_grm = (VoskRecognizer*(*)(VoskModel*, float, const char*))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_new_grm"));
		vosk_recognizer_free = (void(*)(VoskRecognizer*))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_free"));
		vosk_recognizer_set_spk_model = (void(*)(VoskRecognizer*, void*))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_set_spk_model"));
		vosk_recognizer_set_grm = (void(*)(VoskRecognizer*, const char*))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_set_grm"));
		vosk_recognizer_set_max_alternatives = (void(*)(VoskRecognizer*, int))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_set_max_alternatives"));
		vosk_recognizer_set_words = (void(*)(VoskRecognizer*, int))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_set_words"));
		vosk_recognizer_set_partial_words = (void(*)(VoskRecognizer*, int))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_set_partial_words"));
		vosk_recognizer_set_nlsml = (void(*)(VoskRecognizer*, int))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_set_nlsml"));

		vosk_recognizer_accept_waveform = (int(*)(VoskRecognizer*, const char*, int))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_accept_waveform"));
		vosk_recognizer_accept_waveform_s = (int(*)(VoskRecognizer*, const short*, int))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_accept_waveform_s"));
		vosk_recognizer_accept_waveform_f = (int(*)(VoskRecognizer*, const float*, int))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_accept_waveform_f"));

		vosk_recognizer_result = (const char*(*)(VoskRecognizer*))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_result"));
		vosk_recognizer_partial_result = (const char*(*)(VoskRecognizer*))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_partial_result"));
		vosk_recognizer_final_result = (const char*(*)(VoskRecognizer*))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_final_result"));
		vosk_recognizer_reset = (void(*)(VoskRecognizer*))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_recognizer_reset"));

		vosk_set_log_level = (void(*)(int))FPlatformProcess::GetDllExport(DllHandle, TEXT("vosk_set_log_level"));

		return DllHandle;
	}

	void FreeLibrary()
	{
		if (DllHandle != nullptr)
		{
			FPlatformProcess::FreeDllHandle(DllHandle);
			DllHandle = nullptr;
		}
	}

	bool IsLoaded() const { return DllHandle != nullptr; }

	// Функции VOSK API
	VoskModel* (*vosk_model_new)(const char* model_path);
	void (*vosk_model_free)(VoskModel* model);
	int (*vosk_model_find_word)(VoskModel* model, const char* word);

	VoskRecognizer* (*vosk_recognizer_new)(VoskModel* model, float sample_rate);
	VoskRecognizer* (*vosk_recognizer_new_spk)(VoskModel* model, float sample_rate, void* spk_model);
	VoskRecognizer* (*vosk_recognizer_new_grm)(VoskModel* model, float sample_rate, const char* grammar);
	void (*vosk_recognizer_free)(VoskRecognizer* recognizer);
	void (*vosk_recognizer_set_spk_model)(VoskRecognizer* recognizer, void* spk_model);
	void (*vosk_recognizer_set_grm)(VoskRecognizer* recognizer, const char* grammar);
	void (*vosk_recognizer_set_max_alternatives)(VoskRecognizer* recognizer, int max_alternatives);
	void (*vosk_recognizer_set_words)(VoskRecognizer* recognizer, int words);
	void (*vosk_recognizer_set_partial_words)(VoskRecognizer* recognizer, int partial_words);
	void (*vosk_recognizer_set_nlsml)(VoskRecognizer* recognizer, int nlsml);

	int (*vosk_recognizer_accept_waveform)(VoskRecognizer* recognizer, const char* data, int length);
	int (*vosk_recognizer_accept_waveform_s)(VoskRecognizer* recognizer, const short* data, int length);
	int (*vosk_recognizer_accept_waveform_f)(VoskRecognizer* recognizer, const float* data, int length);

	const char* (*vosk_recognizer_result)(VoskRecognizer* recognizer);
	const char* (*vosk_recognizer_partial_result)(VoskRecognizer* recognizer);
	const char* (*vosk_recognizer_final_result)(VoskRecognizer* recognizer);
	void (*vosk_recognizer_reset)(VoskRecognizer* recognizer);

	void (*vosk_set_log_level)(int log_level);

private:
	FVibeVoskApiFunctions() : DllHandle(nullptr) {}
	~FVibeVoskApiFunctions() { FreeLibrary(); }

	void* DllHandle;
};

// Макросы для удобного доступа к функциям
#define VIBEVOSK_API(Function) FVibeVoskApiFunctions::Get().Function
