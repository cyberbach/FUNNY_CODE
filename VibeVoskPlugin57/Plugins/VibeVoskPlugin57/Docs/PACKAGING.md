# Packaging

## Что включать в пакет

| Файл | Путь | Источник |
|------|------|----------|
| DLL | `Binaries/Win64/libvosk.dll` | ThirdParty/vosk/bin/ |
| DLL | `Binaries/Win64/libgcc_s_seh-1.dll` | ThirdParty/vosk/bin/ |
| DLL | `Binaries/Win64/libstdc++-6.dll` | ThirdParty/vosk/bin/ |
| DLL | `Binaries/Win64/libwinpthread-1.dll` | ThirdParty/vosk/bin/ |
| Модели | `Binaries/Win64/Models/<model>/` | Вручную |
| Бинарники плагина | `Binaries/Win64/UnrealEditor-VibeVoskCore.dll` | Собираются автоматически |

## Автоматический staging DLL

`VibeVoskCore.Build.cs` уже настроен — DLL копируются в `Binaries/Win64/` через `RuntimeDependencies` при сборке.

## Модели

### Вариант 1: В папку Models (рекомендуется)

Положите модели в `Plugins/VibeVoskPlugin57/Binaries/Win64/Models/`. Они копируются в пакет через `RuntimeDependencies` в `.Build.cs`.

### Вариант 2: Отдельная загрузка

Не включайте модели в пакет. Пусть пользователь скачивает их отдельно и кладёт в `Binaries/Win64/Models/`.

### Вариант 3: Non-UFS контент

В `.Build.cs`:

```cpp
RuntimeDependencies.Add(DestPath, SourcePath, StagedFileType.NonUFS);
```

Модели будут в пакете, но не в .pak файле (быстрее загрузка).

## FilterPlugin.ini

```ini
[FilterPlugin]
/Plugins/VibeVoskPlugin57/...
```

## Проверка

После упаковки проверьте:

```
MyGame/Plugins/VibeVoskPlugin57/Binaries/Win64/libvosk.dll         — есть
MyGame/Plugins/VibeVoskPlugin57/Binaries/Win64/Models/<model>/     — есть
```

## Размер пакета

| Модель | Размер |
|--------|--------|
| `vosk-model-small-ru-0.22` | ~40 MB |
| `vosk-model-ru-0.42` | ~1.6 GB |

Для минимального пакета используйте small-модели.

## Известные проблемы

- **Не-ASCII символы в путях** — VOSK может не загрузить модель. Используйте латиницу
- **Длина пути > 260 символов** — Windows ограничение. Установите `LongPathsEnabled` в реестре
