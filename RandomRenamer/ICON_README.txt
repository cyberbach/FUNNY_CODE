Иконка в проекте: `RandomRenamer.ico` (в корне рядом с `MP3RandomRenamer.csproj`).

Как создать/подготовить иконку:
- Используйте PNG 256x256 и конвертируйте в ICO (онлайн-сервисы или векторный редактор).
- Имя файла должно быть `RandomRenamer.ico`.

После добавления/замены иконки перестройте проект в Visual Studio или выполните:

```powershell
dotnet build
```

При публикации в один файл иконка будет встроена в exe при использовании `PublishSingleFile` и `SelfContained`.
