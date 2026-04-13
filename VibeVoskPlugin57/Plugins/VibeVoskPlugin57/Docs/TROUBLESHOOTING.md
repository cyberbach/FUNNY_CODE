# Troubleshooting

## DLL не найдена

**Симптом:** `VoskCore: VOSK DLL not found at: ...`

1. Проверьте `Plugins/VibeVoskPlugin57/Binaries/Win64/libvosk.dll`
2. Убедитесь что рядом лежат: `libgcc_s_seh-1.dll`, `libstdc++-6.dll`, `libwinpthread-1.dll`
3. Проверьте `Plugins/VibeVoskPlugin57/ThirdParty/vosk/bin/` — DLL копируются оттуда при сборке

## Модель не загружается

**Симптом:** `VoskModel: Model path does not exist`

1. Проверьте `Plugins/VibeVoskPlugin57/Binaries/Win64/Models/`
2. Внутри папки модели должен быть `conf/mfcc.conf`
3. В Output Log: `VoskCore: Found model: ...` — если пусто, модель не обнаружена

## Распознавание не работает

1. **Проверьте микрофон:** `Get Available Capture Devices` — видит ли устройство
2. **Проверьте уровень звука:** включите `bShowDebugMessages` на компоненте, смотрите `MaxAmp` в логах. Если < 0.01 — увеличьте `AudioGain`
3. **Проверьте sample rate:** VOSK требует 16kHz. Компонент ресэмплит автоматически
4. **Включите логи:** Project Settings → Enable VOSK Logging, или CVar `vosk.RecognizerDebug 1`

## Краш при завершении

- Не вызывайте `DestroyModel()` / `DestroyRecognizer()` дважды
- Если используете C++ — не удаляйте модель пока распознаватель ещё жив
- `BeginDestroy()` освобождает ресурсы автоматически — ручное освобождение опционально

## Низкое качество распознавания

1. Попробуйте большую модель (`vosk-model-ru-0.42` вместо `vosk-model-small-ru-0.22`)
2. Увеличьте `AudioGain` (10–30)
3. Используйте режим грамматики для команд
4. Убедитесь что микрофон достаточно близко к источнику звука

## Ошибки сборки

**`vosk_api.h not found`:** проверьте `ThirdParty/vosk/include/vosk_api.h`

**`vosk.lib not found`:** проверьте `ThirdParty/vosk/lib/vosk.lib`

**`unresolved external symbol`:** добавьте `"VibeVoskCore"` в `PrivateDependencyModuleNames` вашего `.Build.cs`

## Packaged build

1. Модели должны быть в `Plugins/VibeVoskPlugin57/Binaries/Win64/Models/` (копируются через `RuntimeDependencies`)
2. DLL должны быть в `Binaries/Win64/` (копируются через `RuntimeDependencies`)
3. Проверьте `FilterPlugin.ini` — плагин должен быть включён в упаковку
