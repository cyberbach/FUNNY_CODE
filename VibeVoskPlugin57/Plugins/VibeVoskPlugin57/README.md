# VibeVoskPlugin57

Локальное распознавание речи для Unreal Engine 5.7 на базе VOSK API. Оффлайн, без облачных API.

## Возможности

- Потоковое распознавание в реальном времени с частичными результатами
- Режим грамматики (ограничение по списку слов для голосовых команд)
- Мультиязычность (русский, английский, 20+ языков)
- Blueprint-интеграция через `UVibeVoskAudioCaptureComponent`
- C++ API с классами `UVibeVoskModel`, `UVibeVoskRecognizer`
- Настройки через Project Settings UI

## Быстрый старт

1. Скопируйте плагин в `Plugins/VibeVoskPlugin57/`
2. Поместите VOSK SDK в `Plugins/VibeVoskPlugin57/ThirdParty/vosk/`
3. Положите модель в `Plugins/VibeVoskPlugin57/Binaries/Win64/Models/`
4. В Blueprint: добавьте `Vibe Vosk Audio Capture` на актёр → `Initialize` → `StartRecording` → слушайте `OnFinalResult`

## Документация

| Файл | Описание |
|------|----------|
| `Docs/INSTALLATION.md` | Установка VOSK SDK и моделей |
| `Docs/QUICK_START.md` | Примеры за 5 минут (BP + C++) |
| `Docs/BLUEPRINT_GUIDE.md` | Полное руководство по Blueprint |
| `Docs/CPP_GUIDE.md` | Использование из C++ |
| `Docs/API_REFERENCE.md` | Справочник API |
| `Docs/TROUBLESHOOTING.md` | Решение проблем |
| `Docs/PACKAGING.md` | Упаковка проекта |

## Лицензии

| Компонент | Лицензия | Правообладатель |
|-----------|----------|-----------------|
| **VibeVoskPlugin57** (плагин) | MIT | Andrey (cb) Mikheev |
| **VOSK API** (распознавание) | Apache 2.0 | Alpha Cephei Inc |

Модели VOSK распространяются под отдельными лицензиями — см. страницу каждой модели на [alphacephei.com/vosk/models](https://alphacephei.com/vosk/models). Большинство small-моделей доступны под Apache 2.0.
