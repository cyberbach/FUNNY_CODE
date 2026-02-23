using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace TGHistoryRipper;

public partial class MainWindow : Window
{
    private string? _currentFilePath;
    private string _modifiedJson = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0 && files[0].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
                return;
            }
        }
        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0)
            {
                try
                {
                    _currentFilePath = files[0];
                    var content = File.ReadAllText(_currentFilePath);
                    
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        StatusText.Text = "Файл пустой!";
                        StatusText.Foreground = System.Windows.Media.Brushes.Red;
                        return;
                    }

                    JsonDocument.Parse(content).Dispose();
                    _modifiedJson = content;
                    FileNameText.Text = Path.GetFileName(_currentFilePath);
                    StatusText.Text = "Файл загружен. Введите ключи и нажмите Сохранить.";
                    StatusText.Foreground = System.Windows.Media.Brushes.Green;
                }
                catch (JsonException ex)
                {
                    StatusText.Text = $"Неверный JSON: {ex.Message}";
                    StatusText.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            StatusText.Text = "Сначала перетащите файл!";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            return;
        }

        var keysInput = KeysTextBox.Text.Trim();
        if (string.IsNullOrEmpty(keysInput))
        {
            StatusText.Text = "Введите ключи для удаления!";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
            return;
        }

        var keysToRemove = keysInput.Split(',')
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrEmpty(k))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        try
        {
            var doc = JsonDocument.Parse(_modifiedJson);
            var root = doc.RootElement;
            List<JsonElement> processedItems = new();
            List<JsonElement> originalItems = new();

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    originalItems.Add(item.Clone());
                    var modified = RemoveKeys(item.Clone(), keysToRemove);
                    processedItems.Add(modified);
                }

                _modifiedJson = JsonSerializer.Serialize(processedItems, new JsonSerializerOptions { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("messages", out var messages))
                {
                    foreach (var item in messages.EnumerateArray())
                    {
                        originalItems.Add(item.Clone());
                        var modified = RemoveKeys(item.Clone(), keysToRemove);
                        processedItems.Add(modified);
                    }
                }
                else
                {
                    originalItems.Add(root.Clone());
                    var modified = RemoveKeys(root.Clone(), keysToRemove);
                    processedItems.Add(modified);
                }

                _modifiedJson = JsonSerializer.Serialize(processedItems, new JsonSerializerOptions { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }

            var backupPath = _currentFilePath + ".old";
            File.Copy(_currentFilePath, backupPath, true);
            File.WriteAllText(_currentFilePath, _modifiedJson);

            var hashtagsInput = HashtagTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(hashtagsInput))
            {
                var hashtags = hashtagsInput.Split(',')
                    .Select(h => h.Trim())
                    .Where(h => !string.IsNullOrEmpty(h))
                    .ToList();

                int savedHashtagCount = 0;

                foreach (var hashtag in hashtags)
                {
                    var filtered = originalItems
                        .Where(item => ContainsHashtag(item, hashtag))
                        .Select(item => RemoveKeys(item, keysToRemove))
                        .ToList();

                    if (filtered.Count > 0)
                    {
                        var baseName = Path.GetFileNameWithoutExtension(_currentFilePath);
                        var markdownPath = Path.Combine(Path.GetDirectoryName(_currentFilePath)!, $"{baseName}_{hashtag}.md");

                        var markdown = GenerateMarkdown(filtered);
                        File.WriteAllText(markdownPath, markdown);
                        savedHashtagCount++;
                    }
                }

                if (savedHashtagCount > 0)
                    StatusText.Text = $"Сохранено! Основной + {savedHashtagCount} md-файлов.";
                else
                    StatusText.Text = "Ключи удалены. Хештеги не найдены.";
            }
            else
            {
                StatusText.Text = "Ключи удалены и файл сохранён!";
            }
            
            StatusText.Foreground = System.Windows.Media.Brushes.Green;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка: {ex.Message}";
            StatusText.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private bool ContainsHashtag(JsonElement element, string hashtag)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("text", out var textProp))
            {
                if (textProp.ValueKind == JsonValueKind.String)
                {
                    var text = textProp.GetString() ?? "";
                    if (text.Contains($"#{hashtag}", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                else if (textProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in textProp.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            if (item.GetString()?.Contains($"#{hashtag}", StringComparison.OrdinalIgnoreCase) == true)
                                return true;
                        }
                        else if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("text", out var innerText))
                        {
                            if (innerText.ValueKind == JsonValueKind.String)
                            {
                                if (innerText.GetString()?.Contains($"#{hashtag}", StringComparison.OrdinalIgnoreCase) == true)
                                    return true;
                            }
                        }
                    }
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    if (item.GetString()?.Contains($"#{hashtag}", StringComparison.OrdinalIgnoreCase) == true)
                        return true;
                }
                else if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("text", out var textProp))
                {
                    if (textProp.ValueKind == JsonValueKind.String)
                    {
                        if (textProp.GetString()?.Contains($"#{hashtag}", StringComparison.OrdinalIgnoreCase) == true)
                            return true;
                    }
                }
                else if (item.ValueKind == JsonValueKind.Object)
                {
                    if (ContainsHashtag(item, hashtag))
                        return true;
                }
            }
        }

        return false;
    }

    private JsonElement RemoveKeys(JsonElement element, HashSet<string> keysToRemove)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return element;

        var dict = new Dictionary<string, JsonElement>();
        foreach (var prop in element.EnumerateObject())
        {
            if (keysToRemove.Contains(prop.Name))
                continue;

            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                dict[prop.Name] = RemoveKeys(prop.Value, keysToRemove);
            }
            else if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                var newArray = new List<JsonElement>();
                foreach (var item in prop.Value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                        newArray.Add(RemoveKeys(item, keysToRemove));
                    else
                        newArray.Add(item.Clone());
                }

                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { 
                    Indented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                writer.WriteStartArray();
                foreach (var item in newArray)
                    item.WriteTo(writer);
                writer.WriteEndArray();
                writer.Flush();
                stream.Position = 0;
                var newDoc = JsonDocument.Parse(stream);
                dict[prop.Name] = newDoc.RootElement.Clone();
            }
            else
            {
                dict[prop.Name] = prop.Value.Clone();
            }
        }

        using var outputStream = new MemoryStream();
        using var outputWriter = new Utf8JsonWriter(outputStream, new JsonWriterOptions { 
            Indented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        outputWriter.WriteStartObject();
        foreach (var kvp in dict)
        {
            outputWriter.WritePropertyName(kvp.Key);
            kvp.Value.WriteTo(outputWriter);
        }
        outputWriter.WriteEndObject();
        outputWriter.Flush();
        outputStream.Position = 0;
        var resultDoc = JsonDocument.Parse(outputStream);
        return resultDoc.RootElement.Clone();
    }

    private string GenerateMarkdown(List<JsonElement> items)
    {
        var categories = new Dictionary<string, List<(string Title, string? Link)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var (title, link) = ExtractTitleAndLink(item);
            if (string.IsNullOrEmpty(title)) continue;

            var fullText = GetFullText(item);
            var category = Categorize(title, fullText);

            if (!categories.ContainsKey(category))
                categories[category] = new List<(string, string?)>();

            categories[category].Add((title, link));
        }

        var sb = new System.Text.StringBuilder();
        foreach (var cat in categories.OrderBy(c => c.Key))
        {
            sb.AppendLine($"## {cat.Key}");
            sb.AppendLine();
            foreach (var item in cat.Value)
            {
                if (!string.IsNullOrEmpty(item.Link))
                    sb.AppendLine($"- [{item.Title}]({item.Link})");
                else
                    sb.AppendLine($"- {item.Title}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private (string? Title, string? Link) ExtractTitleAndLink(JsonElement item)
    {
        if (!item.TryGetProperty("text", out var textProp)) return (null, null);

        string? title = null;
        string? link = null;

        if (textProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in textProp.EnumerateArray())
            {
                if (part.ValueKind == JsonValueKind.Object)
                {
                    if (part.TryGetProperty("text", out var partText) && 
                        part.TryGetProperty("href", out var partHref))
                    {
                        if (title == null)
                            title = partText.GetString();
                        link = partHref.GetString();
                    }
                    else if (part.TryGetProperty("text", out var partText2))
                    {
                        if (title == null)
                            title = partText2.GetString();
                    }
                }
                else if (part.ValueKind == JsonValueKind.String && title == null)
                {
                    var str = part.GetString();
                    if (!string.IsNullOrWhiteSpace(str))
                        title = str.Trim();
                }
            }
        }
        else if (textProp.ValueKind == JsonValueKind.String)
        {
            title = textProp.GetString();
        }

        return (title, link);
    }

    private string GetFullText(JsonElement item)
    {
        if (!item.TryGetProperty("text", out var textProp)) return "";

        var sb = new System.Text.StringBuilder();
        if (textProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in textProp.EnumerateArray())
            {
                if (part.ValueKind == JsonValueKind.String)
                    sb.Append(part.GetString());
                else if (part.TryGetProperty("text", out var t))
                    sb.Append(t.GetString());
            }
        }
        return sb.ToString();
    }

    private string Categorize(string title, string fullText)
    {
        var text = (title + " " + fullText).ToLower();

        if (text.Contains("курс") || text.Contains("course") || text.Contains("masterclass") || text.Contains("обучение"))
            return "Курсы";
        if (text.Contains("демо") || text.Contains("демонстрация") || text.Contains("demo") || text.Contains("технодемо"))
            return "Демо";
        if (text.Contains("окружение") || text.Contains("environment") || text.Contains("сцена") || text.Contains("пак") || text.Contains("pack") || text.Contains("биом") || text.Contains("лес") || text.Contains("город"))
            return "Окружения";
        if (text.Contains("модель") || text.Contains("пропс") || text.Contains("модуль") || text.Contains("props") || text.Contains("мебель") || text.Contains("здание"))
            return "Модели/Пропсы";
        if (text.Contains("персонаж") || text.Contains("character") || text.Contains("npc") || text.Contains("metahuman"))
            return "Персонажи";
        if (text.Contains("анимация") || text.Contains("animation") || text.Contains("mocap") || text.Contains("control rig"))
            return "Анимации";
        if (text.Contains("плагин") || text.Contains("инструмент") || text.Contains("tool") || text.Contains("система") || text.Contains("generator"))
            return "Плагины/Инструменты";
        if (text.Contains("материал") || text.Contains("шейдер") || text.Contains("texture") || text.Contains("material") || text.Contains("shader"))
            return "Материалы/Текстуры";
        if (text.Contains("книга") || text.Contains("book") || text.Contains("pdf"))
            return "Книги";
        if (text.Contains("набор") || text.Contains("коллекция") || text.Contains("bundle"))
            return "Наборы ассетов";

        return "Прочее";
    }
}
