# Packaging

## Что включать в пакет

| Файл | Путь | Источник |
|------|------|----------|
| DLL | `Binaries/Win64/libstt.dll` | ThirdParty/stt/bin/ |
| DLL | `Binaries/Win64/libgcc_s_seh-1.dll` | ThirdParty/stt/bin/ |
| DLL | `Binaries/Win64/libstdc++-6.dll` | ThirdParty/stt/bin/ |
| DLL | `Binaries/Win64/libwinpthread-1.dll` | ThirdParty/stt/bin/ |
| Модели | `Binaries/Win64/Models/<model>/` | Вручную |
| Бинарники плагина | `Binaries/Win64/UnrealEditor-LocalSTTCore.dll` | Собираются автоматически |

## Автоматический staging DLL

`LocalSTTCore.Build.cs` уже настроен — DLL копируются в `Binaries/Win64/` через `RuntimeDependencies` при сборке.

## Модели

### Вариант 1: В папку Models (рекомендуется)

Положите модели в `Plugins/LocalSTTPlugin57/Binaries/Win64/Models/`. Они копируются в пакет через `RuntimeDependencies` в `.Build.cs`.

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
/Plugins/LocalSTTPlugin57/...
```

## Проверка

После упаковки проверьте:

```
MyGame/Plugins/LocalSTTPlugin57/Binaries/Win64/libstt.dll         — есть
MyGame/Plugins/LocalSTTPlugin57/Binaries/Win64/Models/<model>/     — есть
```

## Размер пакета

| Модель | Размер |
|--------|--------|
| `stt-model-small-ru-0.22` | ~40 MB |
| `stt-model-ru-0.42` | ~1.6 GB |

Для минимального пакета используйте small-модели.

## Известные проблемы

- **Не-ASCII символы в путях** — STT может не загрузить модель. Используйте латиницу
- **Длина пути > 260 символов** — Windows ограничение. Установите `LongPathsEnabled` в реестре
