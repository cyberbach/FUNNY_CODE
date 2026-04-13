# Blueprint Guide

## Создание актера с голосовым управлением

### 1. Настройка

Создайте Actor → добавьте компонент **Vibe Vosk Audio Capture**.

### 2. BeginPlay

```
[Create Vosk Model] ──→ Model (без входа = модель по умолчанию)
         ↓
[Initialize] (на компоненте)
         ↓
[Start Recording] (на компоненте)
```

### 3. Обработка результатов

**On Final Result** (Text, Confidence):
- Полная фраза после окончания речи
- Используйте для команд, поиска, логики

**On Partial Result** (Text):
- Текст в реальном времени, пока речь не закончена
- Используйте для HUD-отображения

### 4. Голосовые команды

```
On Final Result
    → Switch on String (Text)
        → "начать"   → StartGame
        → "стоп"     → StopGame
        → "сохранить" → SaveGame
```

## Режим грамматики

Ограничивает распознавание конкретными словами — повышает точность для команд.

```
[Create Vosk Recognizer With Grammar]
    Words: ["начать", "стоп", "сохранить", "загрузить"]
```

## Два режима работы

### Реал-тайм (потоковый)

```
Start Recording → On Partial Result (часто) → On Final Result (по окончании фразы)
```

### Записать → распознать

```
Start Recording → Stop Recording → Start Recognition → On Final Result (всё сразу)
```

Полезно для голосового чата: игрок зажал кнопку → сказал → отпустил → получил текст.

## Справочник нод

### Компонент Vibe Vosk Audio Capture

| Нода | Описание |
|------|----------|
| Initialize(Model) | Инициализировать с моделью |
| Start Recording | Начать захват микрофона |
| Stop Recording | Остановить захват |
| Start Recognition | Начать распознавание записанного |
| Stop Recognition | Остановить распознавание |
| Reset Recognizer | Сбросить распознаватель |
| Process Audio(Array) | Обработать аудио вручную |
| Append Audio Data(Array) | Добавить аудио в буфер |
| Clear Audio Buffer | Очистить буфер |
| Is Capturing | Проверка захвата |
| Is Recognizing | Проверка распознавания |
| Is Initialized | Проверка инициализации |
| Get Sample Rate | Частота дискретизации |
| Get Recorded Audio Buffer | Буфер записи |
| Get Available Capture Devices | Список микрофонов (static) |
| Get Capture Device Info(Index) | Информация об устройстве (static) |

### События компонента

| Событие | Выходы | Когда |
|---------|--------|-------|
| On Final Result | Text, Confidence | Фраза завершена |
| On Partial Result | Text | Речь в процессе |
| On Voice Recording Started | — | Захват начался |
| On Voice Recording Stopped | — | Захват остановлен |

### Свойства компонента

| Свойство | Описание | По умолчанию |
|----------|----------|-------------|
| Sample Rate | Частота дискретизации | 16000 |
| Audio Gain | Усиление сигнала | 10.0 |
| b Show Debug Messages | Отладочные логи | false |
| Capture Device Index | Индекс микрофона (-1 = default) | -1 |
| Capture Device Name | Имя микрофона | (пусто) |
| Available Models | Найденные модели | (авто) |

### Статические функции (Vibe Vosk Blueprint Library)

| Нода | Описание |
|------|----------|
| Create Vosk Model | Модель по умолчанию |
| Create Vosk Model(Path) | Модель по пути/имени |
| Create Vosk Recognizer(Model, Rate) | Распознаватель |
| Create Vosk Recognizer With Grammar(Model, Words, Rate) | С грамматикой |
| Process Audio(Recognizer, Array) | Обработать аудио |
| Get Recognition Result(Recognizer) | Получить результат |
| Get Partial Recognition Result(Recognizer) | Частичный результат |
| Reset Recognizer(Recognizer) | Сброс |
| Destroy Model(Model) | Освободить модель |
| Destroy Recognizer(Recognizer) | Освободить распознаватель |
| Is Model Valid(Model) | Проверка модели |
| Is Recognizer Valid(Recognizer) | Проверка распознавателя |
| Get Default Model Path | Путь к модели по умолчанию |
| Get Vosk Version | Версия VOSK |
