using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YTVtoText.VoskWrapper;

namespace YTVtoText;

public partial class MainWindow : Window
{
    private readonly string _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
    private readonly Dictionary<string, string> _voskModels = new()
    {
        { "vosk-model-small-ru-0.22", "https://alphacephei.com/vosk/models/vosk-model-small-ru-0.22.zip" },
        { "vosk-model-small-en-us-0.15", "https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip" },
        { "vosk-model-ru-0.42", "https://alphacephei.com/vosk/models/vosk-model-ru-0.42.zip" }
    };
    private string _voskModelUrl = "";
    private readonly string _voskDllUrl = "https://github.com/alphacep/vosk-api/releases/download/v0.3.45/vosk-win64-0.3.45.zip";
    private readonly string _ffmpegUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
    private readonly string _ytDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";

    private readonly Dictionary<int, LogEntry> _logEntries = new();
    private int _logIdCounter;
    private System.Threading.Timer? _activityTimer;
    private int _dotCounter;

    static MainWindow()
    {
        // Регистрируем провайдер кодировок для Windows-1251
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private class LogEntry
    {
        public string BaseMessage { get; set; } = "";
        public int LineIndex { get; set; }
        public bool ShowActivity { get; set; } = false;
    }

    public MainWindow()
    {
        InitializeComponent();
        
        var selectedItem = VoskModelComboBox.SelectedItem as ComboBoxItem;
        string modelFolder = selectedItem?.Tag?.ToString() ?? "vosk-model-small-ru-0.22";
        _voskModelUrl = _voskModels.GetValueOrDefault(modelFolder, _voskModels["vosk-model-small-ru-0.22"]);
        
        VoskModelComboBox.SelectionChanged += (s, e) =>
        {
            var item = VoskModelComboBox.SelectedItem as ComboBoxItem;
            string modelName = item?.Tag?.ToString() ?? "vosk-model-small-ru-0.22";
            _voskModelUrl = _voskModels.GetValueOrDefault(modelName, _voskModels["vosk-model-small-ru-0.22"]);
        };
        
        _activityTimer = new System.Threading.Timer(UpdateActivityIndicator, null, 1000, 1000);
    }

    protected override void OnClosed(EventArgs e)
    {
        _activityTimer?.Dispose();
        base.OnClosed(e);
    }

    private void UpdateActivityIndicator(object? state)
    {
        Dispatcher.Invoke(() =>
        {
            var activeEntries = _logEntries.Values.Where(e => e.ShowActivity).ToList();
            if (activeEntries.Count == 0)
                return;

            _dotCounter = (_dotCounter + 1) % 4;
            var dots = new string('.', _dotCounter);

            foreach (var entry in activeEntries)
            {
                UpdateActivityLine(entry, dots);
            }
        });
    }

    private void UpdateActivityLine(LogEntry entry, string dots)
    {
        var textLines = LogTextBox.Text.Split('\n');
        if (entry.LineIndex < 0 || entry.LineIndex >= textLines.Length)
            return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var newMessage = $"[{timestamp}] {entry.BaseMessage} {dots}";

        var lineStart = LogTextBox.GetCharacterIndexFromLineIndex(entry.LineIndex);
        var lineEnd = lineStart + textLines[entry.LineIndex].Length;
        LogTextBox.Select(lineStart, lineEnd - lineStart);
        LogTextBox.Text = LogTextBox.Text.Remove(lineStart, lineEnd - lineStart).Insert(lineStart, newMessage);
        LogTextBox.ScrollToEnd();
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        string url = UrlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            Log("Ошибка: Введите ссылку на YouTube видео");
            return;
        }

        DownloadButton.IsEnabled = false;
        UrlTextBox.IsEnabled = false;

        try
        {
            await ProcessVideoAsync(url);
        }
        catch (Exception ex)
        {
            Log($"Ошибка: {ex.Message}");
        }
        finally
        {
            DownloadButton.IsEnabled = true;
            UrlTextBox.IsEnabled = true;
        }
    }

    private async Task ProcessVideoAsync(string url)
    {
        Log("Проверка зависимостей...");

        var selectedItem = VoskModelComboBox.SelectedItem as ComboBoxItem;
        string modelFolder = selectedItem?.Tag?.ToString() ?? "vosk-model-small-ru-0.22";
        
        // Проверка и загрузка модели Vosk (только если нужно распознавание)
        string modelPath = Path.Combine(_appDirectory, modelFolder);
        if (RecognizeTextCheckBox.IsChecked == true && !Directory.Exists(modelPath))
        {
            var logId = Log($"Загрузка модели {modelFolder}...", showProgress: true);
            await DownloadAndExtractVoskModelAsync(modelPath, logId);
            UpdateLogStatus(logId, true);
        }
        else if (RecognizeTextCheckBox.IsChecked == true)
        {
            Log("Модель Vosk найдена... ОК");
        }

        // Проверка DLL Vosk
        string voskDllPath = Path.Combine(_appDirectory, "vosk.dll");
        if (RecognizeTextCheckBox.IsChecked == true && !File.Exists(voskDllPath))
        {
            var logId = Log("Загрузка библиотеки Vosk...", showProgress: true);
            await DownloadVoskDllAsync(voskDllPath, logId);
            UpdateLogStatus(logId, true);
        }
        else if (RecognizeTextCheckBox.IsChecked == true)
        {
            Log("Библиотека Vosk найдена... ОК");
        }

        // Проверка ffmpeg
        string ffmpegPath = Path.Combine(_appDirectory, "ffmpeg.exe");
        if ((RecognizeTextCheckBox.IsChecked == true) && !File.Exists(ffmpegPath))
        {
            var logId = Log("Загрузка ffmpeg...", showProgress: true);
            await DownloadFfmpegAsync(ffmpegPath, logId);
            UpdateLogStatus(logId, true);
        }
        else if ((RecognizeTextCheckBox.IsChecked == true) && File.Exists(ffmpegPath))
        {
            Log("FFmpeg найден... ОК");
        }

        // Проверка yt-dlp
        string ytDlpPath = Path.Combine(_appDirectory, "yt-dlp.exe");
        if (!File.Exists(ytDlpPath))
        {
            var logId = Log("Загрузка yt-dlp...", showProgress: true);
            await DownloadYtDlpAsync(ytDlpPath, logId);
            UpdateLogStatus(logId, true);
        }
        else
        {
            Log("yt-dlp найден... ОК");
        }

        // Получаем название видео
        Log("Получение информации о видео...", skipDuplicate: true);
        string videoTitle = await GetVideoTitleAsync(url);
        string safeTitle = SanitizeFileName(videoTitle);
        Log($"Название: {safeTitle}", skipDuplicate: true);

        // Скачивание видео
        var downloadLogId = Log("Скачивание видео с YouTube...", showProgress: true);
        string videoPath = await DownloadVideoAsync(url, downloadLogId, safeTitle);
        UpdateLogStatus(downloadLogId, true);

        // Скачивание субтитров
        if (DownloadSubtitlesCheckBox.IsChecked == true)
        {
            Log("Скачивание субтитров...", skipDuplicate: true);
            string subtitlePath = await DownloadSubtitlesAsync(url, safeTitle);
            if (!string.IsNullOrEmpty(subtitlePath))
            {
                Log($"Субтитры сохранены: {subtitlePath}", skipDuplicate: true);
            }
            else
            {
                Log("Субтитры не найдены", skipDuplicate: true);
            }
        }

        // Распознавание текста
        if (RecognizeTextCheckBox.IsChecked == true)
        {
            var convertLogId = Log("Декодирование аудио из видео...", showProgress: true);
            var chunkFiles = await ConvertToWavChunksAsync(videoPath, convertLogId, safeTitle);
            UpdateLogStatus(convertLogId, true);

            var recognizeLogId = Log("Распознавание текста с помощью Vosk...", showActivity: true);
            string recognizedText = await RecognizeSpeechFromChunksAsync(chunkFiles, modelPath, recognizeLogId);
            UpdateLogStatus(recognizeLogId, true);

            // Сохранение текста
            Log("Сохранение текстового файла...", skipDuplicate: true);
            string textFilePath = SaveTextFile(recognizedText, safeTitle);

            Log($"Готово! Текст сохранён в: {textFilePath}");
        }
        else
        {
            // Если не нужно распознавание, просто сохраняем видео
            if (SaveVideoCheckBox.IsChecked != true)
            {
                // Удаляем видео
                try
                {
                    var videoFiles = Directory.GetFiles(_appDirectory, $"{safeTitle}.*");
                    foreach (var file in videoFiles)
                    {
                        if (file.EndsWith("_subtitles.txt")) continue;
                        File.Delete(file);
                    }
                }
                catch { }
            }
            Log("Готово!");
        }

        Log("Завершение работы");
    }

    private async Task<string> DownloadSubtitlesAsync(string url, string safeTitle)
    {
        string ytDlpPath = Path.Combine(_appDirectory, "yt-dlp.exe");
        string outputPath = Path.Combine(_appDirectory, $"{safeTitle}_subtitles");
        
        var startInfo = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = $"--write-sub --write-auto-sub --sub-lang ru --skip-download -o \"{outputPath}\" \"{url}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await Task.Run(() => process.WaitForExit());

        // Ищем файл с субтитрами (может быть .vtt или .srv3)
        var subtitleFiles = Directory.GetFiles(_appDirectory, $"{safeTitle}_subtitles.*");
        
        // Переименовываем в .txt
        if (subtitleFiles.Length > 0)
        {
            string txtPath = Path.Combine(_appDirectory, $"{safeTitle}_subtitles.txt");
            File.Copy(subtitleFiles[0], txtPath, true);
            File.Delete(subtitleFiles[0]);
            return txtPath;
        }
        
        return null;
    }

    private async Task<string> GetVideoTitleAsync(string url)
    {
        string ytDlpPath = Path.Combine(_appDirectory, "yt-dlp.exe");
        
        // Используем JSON output для надёжного получения названия
        var startInfo = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = $"--dump-json --no-download \"{url}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await Task.Run(() => process.WaitForExit());

        if (process.ExitCode != 0)
        {
            throw new Exception($"yt-dlp ошибка: {errorBuilder.ToString()}");
        }

        var json = outputBuilder.ToString().Trim();
        
        try
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonElement>(json);
            if (jsonDoc.TryGetProperty("title", out var titleProperty))
            {
                return titleProperty.GetString() ?? "video";
            }
        }
        catch
        {
            // Fallback к простому методу
        }
        
        return "video";
    }

    private string SanitizeFileName(string fileName)
    {
        // Удаляем недопустимые символы для имён файлов Windows
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
        
        // Заменяем опасные символы
        fileName = fileName.Replace('/', '_').Replace('\\', '_')
            .Replace(':', '_').Replace('*', '_').Replace('?', '_')
            .Replace('"', '_').Replace('<', '_').Replace('>', '_')
            .Replace('|', '_').Replace('\n', ' ').Replace('\r', ' ');
        
        // Обрезаем до разумной длины
        if (fileName.Length > 100)
        {
            fileName = fileName.Substring(0, 100);
        }
        
        return fileName.Trim();
    }

    private async Task DownloadFileWithProgressAsync(HttpClient httpClient, string url, string destinationPath, int logId, int startPercent, int endPercent)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        var bytes = new byte[81920];
        var totalRead = 0L;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        while (true)
        {
            var read = await stream.ReadAsync(bytes.AsMemory(0, bytes.Length));
            if (read == 0)
                break;

            await fileStream.WriteAsync(bytes.AsMemory(0, read));
            totalRead += read;

            if (totalBytes.HasValue)
            {
                var percent = (int)(totalRead * 100 / totalBytes.Value);
                var mappedPercent = startPercent + (percent * (endPercent - startPercent) / 100);
                UpdateLogProgress(logId, mappedPercent);
            }
        }
    }

    private async Task DownloadAndExtractVoskModelAsync(string modelPath, int logId)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(10);

        string zipPath = Path.Combine(_appDirectory, "vosk-model.zip");

        // Загрузка с прогрессом
        await DownloadFileWithProgressAsync(httpClient, _voskModelUrl, zipPath, logId, 0, 50);

        Log("Распаковка модели Vosk...");
        ZipFile.ExtractToDirectory(zipPath, _appDirectory, true);

        File.Delete(zipPath);
        UpdateLogProgress(logId, 100);
    }

    private async Task DownloadVoskDllAsync(string voskDllPath, int logId)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(10);

        string zipPath = Path.Combine(_appDirectory, "vosk-lib.zip");
        
        // Загрузка с прогрессом
        await DownloadFileWithProgressAsync(httpClient, _voskDllUrl, zipPath, logId, 0, 50);

        Log("Распаковка библиотеки Vosk...");
        string tempExtractPath = Path.Combine(_appDirectory, "vosk_temp");
        if (Directory.Exists(tempExtractPath))
            Directory.Delete(tempExtractPath, true);

        ZipFile.ExtractToDirectory(zipPath, tempExtractPath, true);

        // Копировать все файлы из распакованной папки в папку приложения
        var allFiles = Directory.GetFiles(tempExtractPath, "*.*", SearchOption.AllDirectories);
        int copiedCount = 0;
        foreach (var file in allFiles)
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(_appDirectory, fileName);
            try
            {
                File.Copy(file, destFile, true);
                Log($"  Скопировано: {fileName}");
                
                // Переименовываем libvosk.dll в vosk.dll для совместимости
                if (fileName == "libvosk.dll")
                {
                    string renamedDest = Path.Combine(_appDirectory, "vosk.dll");
                    File.Copy(file, renamedDest, true);
                    Log($"  Скопировано как: vosk.dll");
                }
            }
            catch (Exception ex)
            {
                Log($"  Ошибка копирования {fileName}: {ex.Message}");
            }
            copiedCount++;
            UpdateLogProgress(logId, 50 + (copiedCount * 50 / allFiles.Length));
        }

        Directory.Delete(tempExtractPath, true);
        File.Delete(zipPath);
        UpdateLogProgress(logId, 100);
    }

    private async Task DownloadFfmpegAsync(string ffmpegPath, int logId)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(10);

        string zipPath = Path.Combine(_appDirectory, "ffmpeg.zip");
        
        // Загрузка с прогрессом
        await DownloadFileWithProgressAsync(httpClient, _ffmpegUrl, zipPath, logId, 0, 60);

        Log("Распаковка ffmpeg...");
        string tempExtractPath = Path.Combine(_appDirectory, "ffmpeg_temp");
        if (Directory.Exists(tempExtractPath))
            Directory.Delete(tempExtractPath, true);

        ZipFile.ExtractToDirectory(zipPath, tempExtractPath, true);

        // Найти ffmpeg.exe в распакованной папке
        var ffmpegExe = Directory.GetFiles(tempExtractPath, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (ffmpegExe != null)
        {
            File.Copy(ffmpegExe, ffmpegPath, true);
        }

        Directory.Delete(tempExtractPath, true);
        File.Delete(zipPath);
        UpdateLogProgress(logId, 100);
    }

    private async Task DownloadYtDlpAsync(string ytDlpPath, int logId)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(10);

        await DownloadFileWithProgressAsync(httpClient, _ytDlpUrl, ytDlpPath, logId, 0, 100);
    }

    private async Task<string> DownloadVideoAsync(string url, int logId, string safeTitle)
    {
        string outputPath = Path.Combine(_appDirectory, $"{safeTitle}.%(ext)s");
        string ytDlpPath = Path.Combine(_appDirectory, "yt-dlp.exe");

        var startInfo = new ProcessStartInfo
        {
            FileName = ytDlpPath,
            Arguments = $"-f worst --progress --newline --merge-output-format mp4 -o \"{outputPath}\" \"{url}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (s, e) => 
        { 
            if (e.Data != null) 
            {
                outputBuilder.AppendLine(e.Data);
                // Парсим прогресс из вывода yt-dlp (формат: [download]   4.5% of ...
                if (e.Data.Contains("[download]") && e.Data.Contains("%"))
                {
                    var parts = e.Data.Split('%');
                    if (parts.Length > 0)
                    {
                        var percentPart = parts[parts.Length - 2];
                        if (int.TryParse(percentPart.Trim(), out var percent))
                        {
                            UpdateLogProgress(logId, percent);
                        }
                    }
                }
            } 
        };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await Task.Run(() => process.WaitForExit());

        if (process.ExitCode != 0)
        {
            throw new Exception($"yt-dlp ошибка: {errorBuilder.ToString()}");
        }

        // Найти скачанный файл
        var videoFiles = Directory.GetFiles(_appDirectory, $"{safeTitle}.*");
        if (videoFiles.Length == 0)
        {
            throw new Exception("Видео не было скачано");
        }

        UpdateLogProgress(logId, 100);
        return videoFiles[0];
    }

    private async Task<string> ConvertToWavAsync(string videoPath, int logId)
    {
        string wavPath = Path.ChangeExtension(videoPath, ".wav");

        // Сначала получаем длительность видео
        TimeSpan? totalDuration = null;
        var durationStartInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(_appDirectory, "ffmpeg.exe"),
            Arguments = $"-i \"{videoPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardErrorEncoding = Encoding.UTF8
        };

        using (var durationProcess = new Process { StartInfo = durationStartInfo })
        {
            durationProcess.Start();
            var errorOutput = new StringBuilder();
            durationProcess.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    errorOutput.AppendLine(e.Data);
                    // Ищем длительность: Duration: 00:00:XX.XX
                    if (e.Data.Contains("Duration:"))
                    {
                        var durationIndex = e.Data.IndexOf("Duration:");
                        var durationStr = e.Data.Substring(durationIndex + 10, 11);
                        if (TimeSpan.TryParse(durationStr, out var duration))
                        {
                            totalDuration = duration;
                        }
                    }
                }
            };
            durationProcess.BeginErrorReadLine();
            durationProcess.WaitForExit();
        }

        // Теперь конвертируем
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(_appDirectory, "ffmpeg.exe"),
            Arguments = $"-i \"{videoPath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 -y \"{wavPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };
        var errorBuilder = new StringBuilder();

        process.ErrorDataReceived += (s, e) => 
        { 
            if (e.Data != null) 
            {
                errorBuilder.AppendLine(e.Data);
                // Парсим прогресс из ffmpeg (формат: time=00:00:XX.XX)
                if (totalDuration.HasValue && e.Data.Contains("time="))
                {
                    var timeIndex = e.Data.IndexOf("time=");
                    if (timeIndex >= 0 && timeIndex + 5 + 8 <= e.Data.Length)
                    {
                        var timeStr = e.Data.Substring(timeIndex + 5, 8);
                        if (TimeSpan.TryParse(timeStr, out var currentTime))
                        {
                            var progress = (int)(currentTime.TotalMilliseconds / totalDuration.Value.TotalMilliseconds * 100);
                            progress = Math.Min(100, Math.Max(0, progress));
                            UpdateLogProgress(logId, progress);
                        }
                    }
                }
            } 
        };

        process.Start();
        process.BeginErrorReadLine();

        await Task.Run(() => process.WaitForExit());

        if (process.ExitCode != 0)
        {
            throw new Exception($"ffmpeg ошибка: {errorBuilder.ToString()}");
        }

        UpdateLogProgress(logId, 100);
        return wavPath;
    }

    private async Task<List<string>> ConvertToWavChunksAsync(string videoPath, int logId, string safeTitle)
    {
        var chunkFiles = new List<string>();
        string tempDir = Path.Combine(_appDirectory, "temp_chunks");

        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);

        Dispatcher.Invoke(() => Log("Получение длительности видео...", skipDuplicate: true));

        // Получаем длительность видео
        TimeSpan? totalDuration = null;
        var durationStartInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(_appDirectory, "ffmpeg.exe"),
            Arguments = $"-i \"{videoPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardErrorEncoding = Encoding.UTF8
        };

        using (var durationProcess = new Process { StartInfo = durationStartInfo })
        {
            durationProcess.Start();
            var errorOutput = new StringBuilder();
            durationProcess.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null && e.Data.Contains("Duration:"))
                {
                    var durationIndex = e.Data.IndexOf("Duration:");
                    var durationStr = e.Data.Substring(durationIndex + 10, 11);
                    if (TimeSpan.TryParse(durationStr, out var duration))
                    {
                        totalDuration = duration;
                    }
                }
            };
            durationProcess.BeginErrorReadLine();
            durationProcess.WaitForExit();
        }

        if (!totalDuration.HasValue)
        {
            throw new Exception("Не удалось получить длительность видео");
        }

        Dispatcher.Invoke(() => Log($"Длительность: {totalDuration.Value:hh\\:mm\\:ss}", skipDuplicate: true));

        // Разбиваем на чанки по 29 секунд
        int chunkDuration = 29; // секунд
        int totalChunks = (int)Math.Ceiling(totalDuration.Value.TotalSeconds / chunkDuration);
        int processedChunks = 0;

        Dispatcher.Invoke(() => Log($"Всего чанков: {totalChunks}", skipDuplicate: true));

        for (int i = 0; i < totalChunks; i++)
        {
            string chunkPath = Path.Combine(tempDir, $"{safeTitle}_chunk_{i:D4}.wav");
            int startTime = i * chunkDuration;
            
            Dispatcher.Invoke(() => Log($"Создание чанка {i + 1}/{totalChunks}...", skipDuplicate: true));

            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(_appDirectory, "ffmpeg.exe"),
                Arguments = $"-i \"{videoPath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 -ss {startTime} -t {chunkDuration} -y \"{chunkPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            var ffmpegError = new StringBuilder();
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) ffmpegError.AppendLine(e.Data); };
            
            process.Start();
            process.BeginErrorReadLine();
            
            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode == 0 && File.Exists(chunkPath))
            {
                chunkFiles.Add(chunkPath);
                Dispatcher.Invoke(() => Log($"Чанк {i + 1} создан", skipDuplicate: true));
            }
            else
            {
                Dispatcher.Invoke(() => Log($"Ошибка создания чанка {i + 1}: {ffmpegError}", skipDuplicate: true));
            }

            processedChunks++;
            int progress = (processedChunks * 100) / totalChunks;
            UpdateLogProgress(logId, progress);
        }

        UpdateLogProgress(logId, 100);
        Dispatcher.Invoke(() => Log($"Создано чанков: {chunkFiles.Count}", skipDuplicate: true));
        return chunkFiles;
    }

    private async Task<string> RecognizeSpeechFromChunksAsync(List<string> chunkFiles, string modelPath, int logId)
    {
        return await Task.Run(() =>
        {
            try
            {
                Dispatcher.Invoke(() => Log($"Загрузка модели: {modelPath}", skipDuplicate: true));
                
                if (!Directory.Exists(modelPath))
                {
                    throw new Exception($"Модель не найдена: {modelPath}");
                }

                using var model = new VoskModel(modelPath);
                Dispatcher.Invoke(() => Log("Модель загружена", skipDuplicate: true));

                var result = new StringBuilder();
                int totalChunks = chunkFiles.Count;

                for (int i = 0; i < totalChunks; i++)
                {
                    string chunkPath = chunkFiles[i];
                    Dispatcher.Invoke(() => Log($"Чанк {i + 1}/{totalChunks}", skipDuplicate: true));

                    var fileInfo = new FileInfo(chunkPath);
                    if (!fileInfo.Exists)
                    {
                        Dispatcher.Invoke(() => Log($"Чанк не найден", skipDuplicate: true));
                        continue;
                    }

                    Dispatcher.Invoke(() => Log($"Размер: {fileInfo.Length / 1024} КБ", skipDuplicate: true));

                    // Создаём новый распознаватель для каждого чанка
                    using (var recognizer = new VoskRecognizer(model, 16000f))
                    {
                        var chunkResult = RecognizeChunk(chunkPath, recognizer, logId, i, totalChunks);
                        if (!string.IsNullOrEmpty(chunkResult))
                        {
                            result.AppendLine(chunkResult);
                        }
                    }

                    // Обновляем прогресс
                    int progress = ((i + 1) * 100) / totalChunks;
                    UpdateLogProgress(logId, progress);
                }

                // Очищаем временные файлы
                Dispatcher.Invoke(() => Log("Очистка временных файлов...", skipDuplicate: true));
                string tempDir = Path.Combine(_appDirectory, "temp_chunks");
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }

                UpdateLogProgress(logId, 100);
                return result.ToString().Trim();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"Ошибка: {ex.Message}", skipDuplicate: true));
                throw;
            }
        });
    }

    private string RecognizeChunk(string chunkPath, VoskRecognizer recognizer, int logId, int chunkIndex, int totalChunks)
    {
        var result = new StringBuilder();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // Читаем весь файл сразу
            byte[] allBytes = File.ReadAllBytes(chunkPath);
            Dispatcher.Invoke(() => Log($"Загружено байт: {allBytes.Length}", skipDuplicate: true));
            
            // Отправляем всё сразу
            bool accepted = recognizer.AcceptWaveform(allBytes, allBytes.Length);
            
            sw.Stop();
            Dispatcher.Invoke(() => Log($"Обработка: {sw.ElapsedMilliseconds} мс", skipDuplicate: true));
            
            if (accepted)
            {
                var text = recognizer.Result();
                // Vosk возвращает UTF-8 JSON
                var jsonText = JsonSerializer.Deserialize<JsonElement>(text);
                if (jsonText.TryGetProperty("text", out var textProperty))
                {
                    var recognized = textProperty.GetString();
                    if (!string.IsNullOrEmpty(recognized))
                    {
                        Dispatcher.Invoke(() => Log($"→ {recognized}", skipDuplicate: true));
                        result.Append(recognized);
                    }
                }
            }

            // Финальный результат
            var finalResult = recognizer.FinalResult();
            var finalJson = JsonSerializer.Deserialize<JsonElement>(finalResult);
            if (finalJson.TryGetProperty("text", out var finalTextProperty))
            {
                var finalText = finalTextProperty.GetString();
                if (!string.IsNullOrEmpty(finalText))
                {
                    result.Append(" ").Append(finalText);
                }
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => Log($"Ошибка чанка: {ex.Message}", skipDuplicate: true));
        }
        
        return result.ToString().Trim();
    }

    private async Task<string> RecognizeSpeechAsync(string wavPath, string modelPath, int logId)
    {
        return await Task.Run(() =>
        {
            try
            {
                Dispatcher.Invoke(() => Log($"Загрузка модели из: {modelPath}"));
                
                if (!Directory.Exists(modelPath))
                {
                    throw new Exception($"Модель не найдена: {modelPath}");
                }

                using var model = new VoskModel(modelPath);
                Dispatcher.Invoke(() => Log("Модель загружена"));
                
                using var recognizer = new VoskRecognizer(model, 16000f);
                Dispatcher.Invoke(() => Log("Распознаватель создан"));

                var fileInfo = new FileInfo(wavPath);
                if (!fileInfo.Exists)
                {
                    throw new Exception($"WAV файл не найден: {wavPath}");
                }
                Dispatcher.Invoke(() => Log($"Размер WAV файла: {fileInfo.Length} байт"));

                using var waveStream = new VoskWaveStream(wavPath);
                Dispatcher.Invoke(() => Log($"Длина аудио: {waveStream.Length} байт"));
                
                var buffer = new byte[4096];
                var result = new StringBuilder();

                int bytesRead;
                int totalBytesRead = 0;
                int totalBytes = (int)waveStream.Length;
                int chunkCount = 0;

                while ((bytesRead = waveStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    totalBytesRead += bytesRead;
                    chunkCount++;

                    // Обновляем прогресс
                    int progress = (int)((totalBytesRead * 100.0) / totalBytes);
                    UpdateLogProgress(logId, progress);

                    if (recognizer.AcceptWaveform(buffer, bytesRead))
                    {
                        var text = recognizer.Result();
                        Dispatcher.Invoke(() => Log($"Распознано: {text}"));
                        if (!string.IsNullOrEmpty(text))
                        {
                            // Парсим JSON результат Vosk
                            var jsonText = JsonSerializer.Deserialize<JsonElement>(text);
                            if (jsonText.TryGetProperty("text", out var textProperty))
                            {
                                var recognized = textProperty.GetString();
                                if (!string.IsNullOrEmpty(recognized))
                                {
                                    result.AppendLine(recognized);
                                }
                            }
                        }
                    }
                }

                Dispatcher.Invoke(() => Log($"Всего обработано чанков: {chunkCount}"));

                // Получить финальный результат
                var finalResult = recognizer.FinalResult();
                Dispatcher.Invoke(() => Log($"Финальный результат: {finalResult}"));
                
                var finalJson = JsonSerializer.Deserialize<JsonElement>(finalResult);
                if (finalJson.TryGetProperty("text", out var finalTextProperty))
                {
                    var finalText = finalTextProperty.GetString();
                    if (!string.IsNullOrEmpty(finalText))
                    {
                        result.AppendLine(finalText);
                    }
                }

                UpdateLogProgress(logId, 100);
                return result.ToString().Trim();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"Ошибка распознавания: {ex.Message}"));
                Dispatcher.Invoke(() => Log($"Stack trace: {ex.StackTrace}"));
                throw;
            }
        });
    }

    private string SaveTextFile(string text, string safeTitle)
    {
        string textFilePath = Path.Combine(_appDirectory, $"{safeTitle}_text.txt");

        // Инструкция для нейронки
        string instruction = """
            Этот текст получен при декодировании видео с ютуба через распознавание речи Vosk.
            ---
            """;

        // Сохраняем в Windows-1251 для корректного отображения кириллицы
        var windows1251 = Encoding.GetEncoding(1251);
        File.WriteAllText(textFilePath, instruction + text, windows1251);

        // Очистка временных файлов
        try
        {
            // Удаляем видео и чанки
            var videoFiles = Directory.GetFiles(_appDirectory, $"{safeTitle}.*");
            foreach (var file in videoFiles)
            {
                if (file.EndsWith("_text.txt")) continue;
                if (file.EndsWith("_subtitles.txt")) continue; // Сохраняем субтитры
                File.Delete(file);
            }
            
            string tempDir = Path.Combine(_appDirectory, "temp_chunks");
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
        catch { }

        return textFilePath;
    }

    private int Log(string message, bool showProgress = false, bool showActivity = false, bool skipDuplicate = false)
    {
        var result = Dispatcher.Invoke(() =>
        {
            if (skipDuplicate)
            {
                // Проверяем, есть ли уже такая строка в конце лога
                var text = LogTextBox.Text;
                var lines = text.Split('\n');
                // Проверяем последние 10 строк
                for (int i = Math.Max(0, lines.Length - 11); i < lines.Length - 1; i++)
                {
                    // Сравниваем без временной метки
                    var lineWithoutTime = lines[i].Length > 12 ? lines[i].Substring(12) : lines[i];
                    var messageWithoutTime = message;
                    if (lineWithoutTime.Trim() == messageWithoutTime.Trim())
                    {
                        // Удаляем дубликат
                        var lineStart = LogTextBox.GetCharacterIndexFromLineIndex(i);
                        var lineLength = lines[i].Length + 1; // +1 для \n
                        LogTextBox.Select(lineStart, lineLength);
                        LogTextBox.SelectedText = "";
                        break;
                    }
                }
            }

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var fullMessage = $"[{timestamp}] {message}";
            LogTextBox.AppendText(fullMessage + Environment.NewLine);
            LogTextBox.ScrollToEnd();
            return LogTextBox.LineCount - 1;
        });

        if (showProgress || showActivity)
        {
            var logId = _logIdCounter++;
            _logEntries[logId] = new LogEntry { BaseMessage = message, LineIndex = result, ShowActivity = showActivity };
            return logId;
        }

        return -1;
    }

    private void UpdateLogProgress(int logId, int progress)
    {
        Dispatcher.Invoke(() =>
        {
            if (!_logEntries.TryGetValue(logId, out var entry))
                return;

            var textLines = LogTextBox.Text.Split('\n');
            if (entry.LineIndex < 0 || entry.LineIndex >= textLines.Length)
                return;

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var newMessage = $"[{timestamp}] {entry.BaseMessage} {progress}%";

            // Сохраняем позицию курсора
            var caretIndex = LogTextBox.CaretIndex;
            var selectionStart = LogTextBox.SelectionStart;
            var selectionLength = LogTextBox.SelectionLength;

            // Заменяем строку
            var lineStart = LogTextBox.GetCharacterIndexFromLineIndex(entry.LineIndex);
            var lineEnd = lineStart + textLines[entry.LineIndex].Length;
            LogTextBox.Select(lineStart, lineEnd - lineStart);
            LogTextBox.Text = LogTextBox.Text.Remove(lineStart, lineEnd - lineStart).Insert(lineStart, newMessage);

            // Восстанавливаем позицию курсора
            LogTextBox.Select(selectionStart, selectionLength);
            LogTextBox.CaretIndex = caretIndex;
            LogTextBox.ScrollToEnd();
        });
    }

    private void UpdateLogStatus(int logId, bool success)
    {
        Dispatcher.Invoke(() =>
        {
            if (!_logEntries.TryGetValue(logId, out var entry))
                return;

            // Отключаем анимацию активности
            entry.ShowActivity = false;

            var textLines = LogTextBox.Text.Split('\n');
            if (entry.LineIndex < 0 || entry.LineIndex >= textLines.Length)
                return;

            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var status = success ? "ОК" : "Ошибка";
            var newMessage = $"[{timestamp}] {entry.BaseMessage} {status}";

            // Сохраняем позицию курсора
            var caretIndex = LogTextBox.CaretIndex;
            var selectionStart = LogTextBox.SelectionStart;
            var selectionLength = LogTextBox.SelectionLength;

            // Заменяем строку и добавляем перенос
            var lineStart = LogTextBox.GetCharacterIndexFromLineIndex(entry.LineIndex);
            var lineEnd = lineStart + textLines[entry.LineIndex].Length;
            LogTextBox.Select(lineStart, lineEnd - lineStart);
            LogTextBox.Text = LogTextBox.Text.Remove(lineStart, lineEnd - lineStart).Insert(lineStart, newMessage + "\n");

            // Восстанавливаем позицию курсора
            LogTextBox.Select(selectionStart, selectionLength);
            LogTextBox.CaretIndex = caretIndex;
            LogTextBox.ScrollToEnd();

            _logEntries.Remove(logId);
        });
    }
}

// Простой класс для чтения WAV файлов
public class VoskWaveStream : Stream
{
    private readonly FileStream _fileStream;
    private readonly long _dataStart;
    private readonly int _dataSize;

    public VoskWaveStream(string wavPath)
    {
        _fileStream = new FileStream(wavPath, FileMode.Open, FileAccess.Read);

        // Чтение WAV заголовка
        var buffer = new byte[44];
        _fileStream.Read(buffer, 0, 44);

        // Найти data чанк
        while (true)
        {
            var chunkHeader = new byte[4];
            var chunkSizeBytes = new byte[4];
            _fileStream.Read(chunkHeader, 0, 4);
            _fileStream.Read(chunkSizeBytes, 0, 4);

            uint chunkSize = BitConverter.ToUInt32(chunkSizeBytes, 0);

            if (Encoding.ASCII.GetString(chunkHeader) == "data")
            {
                _dataStart = _fileStream.Position;
                _dataSize = (int)chunkSize;
                break;
            }

            _fileStream.Seek(chunkSize, SeekOrigin.Current);
        }

        _fileStream.Position = _dataStart;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int remaining = _dataSize - (int)(_fileStream.Position - _dataStart);
        int toRead = Math.Min(count, remaining);

        if (toRead <= 0)
            return 0;

        return _fileStream.Read(buffer, offset, toRead);
    }

    public override void Flush() => _fileStream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => _fileStream.Seek(offset, origin);
    public override void SetLength(long value) => _fileStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _fileStream.Write(buffer, offset, count);
    public override bool CanRead => _fileStream.CanRead;
    public override bool CanSeek => _fileStream.CanSeek;
    public override bool CanWrite => _fileStream.CanWrite;
    public override long Length => _dataSize;
    public override long Position
    {
        get => _fileStream.Position - _dataStart;
        set => _fileStream.Position = _dataStart + value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _fileStream?.Dispose();
        base.Dispose(disposing);
    }
}
