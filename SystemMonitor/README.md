# Системный Монитор

![SystemMonitor](SystemMonitor_Preview.png)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue?logo=windows)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)

WPF-приложение для мониторинга системных ресурсов в реальном времени с современным интерфейсом и темной темой.

## Возможности

### Режимы мониторинга

- **Processor** — мониторинг загрузки CPU с графиком истории
- **Memory** — отображение использования памяти
- **Network** — мониторинг сетевого трафика (входящий/исходящий)
- **GPU** — отображение загрузки видеокарты
- **GPU-Memory** — мониторинг памяти видеокарты

### Особенности интерфейса

- Перетаскиваемое окно (за любую область)
- Темная тема
- Адаптивные графики с историей 100 точек
- Обновление данных каждые 500 мс
- Статусная строка с текущим временем и показателями

## Сборка

```bash
dotnet restore
dotnet build
```

Для публикации:

```bash
dotnet publish --configuration Release --output ./publish
```

## Запуск

```bash
cd MyConsoleApp1\bin\Debug\net10.0-windows
.\MyConsoleApp1.exe
```

## Технологии

- C# 10.0
- .NET 10.0
- WPF (Windows Presentation Foundation)
- PerformanceCounter для мониторинга
- Canvas, Polyline для графики

## Лицензия

MIT License. Приложение сгенерировано нейросетями.
