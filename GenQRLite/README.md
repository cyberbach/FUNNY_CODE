# MinimalDotNetApp

![Preview](MinimalDotNetApp_Preview.png)

Минимальное Windows-приложение на .NET 8 с использованием Native AOT.

## Сборка

```bash
dotnet publish -c Release -r win-x64
```

Или используйте batch-файл:

```batch
__Build_Shipping.bat
```

## Результат

Один исполняемый файл размером ~970 КБ.

## Возможности

- Изменение размера окна
- Кнопка закрытия
- Перетаскивание за тело окна
- Текст "минимальное приложение" по центру
