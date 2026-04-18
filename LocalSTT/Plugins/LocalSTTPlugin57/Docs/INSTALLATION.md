# Установка

## 1. Плагин

Скопируйте `LocalSTTPlugin57` в `YourProject/Plugins/`. Включите в редакторе (Edit → Plugins).

## 2. STT SDK

Скачайте [stt-api release](https://github.com/alphacep/stt-api/releases) для Windows.

Разместите файлы в `Plugins/LocalSTTPlugin57/ThirdParty/stt/`:

```
ThirdParty/stt/
├── include/
│   └── stt_api.h
├── bin/
│   ├── libstt.dll
│   ├── libgcc_s_seh-1.dll
│   ├── libstdc++-6.dll
│   └── libwinpthread-1.dll
└── lib/
    └── stt.lib
```

## 3. Языковые модели

Положите модели в `Plugins/LocalSTTPlugin57/Binaries/Win64/Models/`:

```
Models/
├── stt-model-small-ru-0.22/
│   ├── conf/
│   │   └── mfcc.conf
│   ├── ivector/
│   └── ...
└── stt-model-small-en-us-0.15/
```

Популярные модели:

| Модель | Язык | Размер | Ссылка |
|--------|------|--------|--------|
| `stt-model-small-ru-0.22` | Русский | ~40 MB | [GitHub](https://alphacephei.com/stt/models) |
| `stt-model-small-en-us-0.15` | Английский | ~40 MB | [GitHub](https://alphacephei.com/stt/models) |
| `stt-model-ru-0.42` | Русский (большая) | ~1.6 GB | [GitHub](https://alphacephei.com/stt/models) |

## 4. Проверка

Запустите проект. В Output Log должно быть:

```
LogLocalSTT: SttCore: STT library loaded successfully.
LogLocalSTT: SttCore: Found model: stt-model-small-ru-0.22
```

## Настройки

**Project Settings → STT Plugin:**

| Параметр | Описание | По умолчанию |
|----------|----------|-------------|
| Models Directory | Путь к моделям (пусто = авто) | `Binaries/Win64/Models/` |
| Default Model | Имя модели по умолчанию | `stt-model-small-ru-0.22` |
| Enable STT Logging | Включить логи | false |
| STT Log Level | Уровень (0–3) | 0 |
