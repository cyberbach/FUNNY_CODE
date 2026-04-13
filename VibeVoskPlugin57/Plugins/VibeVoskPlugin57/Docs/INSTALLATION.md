# Установка

## 1. Плагин

Скопируйте `VibeVoskPlugin57` в `YourProject/Plugins/`. Включите в редакторе (Edit → Plugins).

## 2. VOSK SDK

Скачайте [vosk-api release](https://github.com/alphacep/vosk-api/releases) для Windows.

Разместите файлы в `Plugins/VibeVoskPlugin57/ThirdParty/vosk/`:

```
ThirdParty/vosk/
├── include/
│   └── vosk_api.h
├── bin/
│   ├── libvosk.dll
│   ├── libgcc_s_seh-1.dll
│   ├── libstdc++-6.dll
│   └── libwinpthread-1.dll
└── lib/
    └── vosk.lib
```

## 3. Языковые модели

Положите модели в `Plugins/VibeVoskPlugin57/Binaries/Win64/Models/`:

```
Models/
├── vosk-model-small-ru-0.22/
│   ├── conf/
│   │   └── mfcc.conf
│   ├── ivector/
│   └── ...
└── vosk-model-small-en-us-0.15/
```

Популярные модели:

| Модель | Язык | Размер | Ссылка |
|--------|------|--------|--------|
| `vosk-model-small-ru-0.22` | Русский | ~40 MB | [GitHub](https://alphacephei.com/vosk/models) |
| `vosk-model-small-en-us-0.15` | Английский | ~40 MB | [GitHub](https://alphacephei.com/vosk/models) |
| `vosk-model-ru-0.42` | Русский (большая) | ~1.6 GB | [GitHub](https://alphacephei.com/vosk/models) |

## 4. Проверка

Запустите проект. В Output Log должно быть:

```
LogVibeVosk: VoskCore: VOSK library loaded successfully.
LogVibeVosk: VoskCore: Found model: vosk-model-small-ru-0.22
```

## Настройки

**Project Settings → VOSK Plugin:**

| Параметр | Описание | По умолчанию |
|----------|----------|-------------|
| Models Directory | Путь к моделям (пусто = авто) | `Binaries/Win64/Models/` |
| Default Model | Имя модели по умолчанию | `vosk-model-small-ru-0.22` |
| Enable VOSK Logging | Включить логи | false |
| VOSK Log Level | Уровень (0–3) | 0 |
