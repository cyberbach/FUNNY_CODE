# GenQRLite

![GenQRLite](MinimalDotNetApp_Preview.png)

![Windows](https://img.shields.io/badge/Windows-10%2F11-blue?logo=windows)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)

Генератор QR-кодов с минимальным интерфейсом на .NET 8 с использованием Native AOT.

## Возможности

- Генерация QR-кодов из текста
- Сохранение QR-кодов в файл
- Изменение размера окна
- Перетаскивание за тело окна

## Сборка

```bash
dotnet publish -c Release -r win-x64
```

Или используйте batch-файл:

```batch
__Build_Shipping.bat
```

Результат: один исполняемый файл размером ~970 КБ.

## Лицензия

MIT License. Приложение сгенерировано нейросетями.
