using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace GenMotionDesign;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient;
    private string _currentHtml = "";
    private string _tempHtmlPath = "";

    public MainWindow()
    {
        InitializeComponent();
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        InitializeFFmpeg();
        InitializeWebView();
    }

    private async void InitializeFFmpeg()
    {
        try
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var ffmpegPath = Path.Combine(appDir, "ffmpeg.exe");

            if (!File.Exists(ffmpegPath))
            {
                txtStatus.Text = "Downloading FFmpeg...";
                await DownloadFFmpeg(ffmpegPath);
            }

            txtStatus.Text = "Ready";
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"FFmpeg error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"FFmpeg init error: {ex}");
        }
    }

    private async Task DownloadFFmpeg(string ffmpegPath)
    {
        try
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var zipPath = Path.Combine(appDir, "ffmpeg.zip");
            var url = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            
            txtStatus.Text = "Downloading FFmpeg...";
            
            await DownloadFileWithProgressAsync(client, url, zipPath);

            txtStatus.Text = "Extracting FFmpeg...";
            var tempExtractPath = Path.Combine(appDir, "ffmpeg_temp");
            if (Directory.Exists(tempExtractPath))
                Directory.Delete(tempExtractPath, true);

            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, tempExtractPath);

            var ffmpegExe = Directory.GetFiles(tempExtractPath, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (ffmpegExe != null)
            {
                File.Copy(ffmpegExe, ffmpegPath, true);
            }

            Directory.Delete(tempExtractPath, true);
            File.Delete(zipPath);
            
            txtStatus.Text = "FFmpeg ready";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Download error: {ex}");
            txtStatus.Text = $"Download error: {ex.Message}";
        }
    }

    private async Task DownloadFileWithProgressAsync(HttpClient httpClient, string url, string destinationPath)
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
                txtStatus.Text = $"Downloading FFmpeg... {percent}%";
            }
        }
    }

    private async void InitializeWebView()
    {
        try
        {
            await webPreview.EnsureCoreWebView2Async();
            webPreview.CoreWebView2.NavigateToString("<html><body><h1>Preview will appear here</h1></body></html>");
            txtStatus.Text = "WebView2 initialized";
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Error initializing WebView2: {ex.Message}";
        }
    }

    private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
        var apiKey = txtApiKey.Text.Trim();
        var url = txtUrl.Text.Trim();
        var model = txtModel.Text.Trim();
        var systemPrompt = txtSystemPrompt.Text.Trim();
        var userPrompt = txtUserPrompt.Text.Trim();

        if (string.IsNullOrEmpty(apiKey))
        {
            MessageBox.Show("Please enter API Key", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(userPrompt))
        {
            MessageBox.Show("Please enter a prompt", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        btnGenerate.IsEnabled = false;
        txtStatus.Text = "Generating...";

        try
        {
            var requestBody = new
            {
                model = model,
                stream = "false",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Headers.Add("HTTP-Referer", "https://genmotiondesign.app");
            request.Headers.Add("X-Title", "GenMotionDesign");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                txtStatus.Text = $"Error: {response.StatusCode}";
                MessageBox.Show($"API Error: {responseContent}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            string htmlContent = "";
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentProp))
                {
                    htmlContent = contentProp.GetString() ?? "";
                }
            }

            htmlContent = ExtractHtml(htmlContent);
            _currentHtml = WrapHtml(htmlContent);
            
            txtAiResponse.Text = htmlContent;
            webPreview.CoreWebView2.NavigateToString(_currentHtml);
            txtStatus.Text = "Generated successfully";

            SaveTempHtml(_currentHtml);
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnGenerate.IsEnabled = true;
        }
    }

    private string ExtractHtml(string content)
    {
        if (string.IsNullOrEmpty(content)) return content;

        if (content.Contains("<html", StringComparison.OrdinalIgnoreCase) || 
            content.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                content, 
                @"(<html[\s\S]*?</html>)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Value;
        }

        if (content.Contains("```html"))
        {
            var start = content.IndexOf("```html") + 7;
            var end = content.LastIndexOf("```");
            if (end > start)
                return content.Substring(start, end - start).Trim();
        }
        else if (content.Contains("```"))
        {
            var start = content.IndexOf("```") + 3;
            var end = content.LastIndexOf("```");
            if (end > start)
                return content.Substring(start, end - start).Trim();
        }

        if (content.Contains("<!DOCTYPE html>") || content.Contains("<html"))
        {
            return content;
        }

        return "<html><body style='background:black;color:white;font-family:sans-serif;font-size:48px;text-align:center;padding-top:40vh;'>" + content + "</body></html>";
    }

    private string WrapHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) 
            return "<html><body><h1>No content</h1></body></html>";

        if (!html.Contains("<html", StringComparison.OrdinalIgnoreCase))
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ 
            margin: 0; 
            padding: 20px; 
            display: flex; 
            justify-content: center; 
            align-items: center; 
            min-height: 100vh;
            background: #000;
        }}
    </style>
</head>
<body>
{html}
</body>
</html>";
        }

        return html;
    }

    private void SaveTempHtml(string html)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "genmotion_preview.html");
            File.WriteAllText(tempPath, html);
            _tempHtmlPath = tempPath;
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Error saving temp HTML: {ex.Message}";
        }
    }

    private void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        webPreview.ExecuteScriptAsync(@"
            var animations = document.getAnimations();
            animations.forEach(a => a.play());
        ");
        
        StartFrameCounter();
    }

    private void BtnPause_Click(object sender, RoutedEventArgs e)
    {
        webPreview.ExecuteScriptAsync(@"
            var animations = document.getAnimations();
            animations.forEach(a => a.pause());
        ");
        
        StopFrameCounter();
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        webPreview.ExecuteScriptAsync(@"
            var animations = document.getAnimations();
            animations.forEach(a => {
                a.cancel();
                a.currentTime = 0;
            });
        ");
        
        StopFrameCounter();
        txtCurrentFrame.Text = "0";
    }

    private System.Windows.Threading.DispatcherTimer? _frameTimer;
    private int _totalFrames = 0;
    private int _currentFrameNum = 0;
    private double _fps = 30;

    private void StartFrameCounter()
    {
        try { _fps = double.Parse(txtFPS.Text); } catch { _fps = 30; }
        var interval = 1000.0 / _fps;
        
        _frameTimer = new System.Windows.Threading.DispatcherTimer();
        _frameTimer.Interval = TimeSpan.FromMilliseconds(interval);
        _frameTimer.Tick += async (s, e) =>
        {
            try
            {
                var result = await webPreview.CoreWebView2.ExecuteScriptAsync(@"
                    (function() {
                        var anims = document.getAnimations();
                        if (anims.length === 0) return 0;
                        var maxDuration = 0;
                        var maxCurrentTime = 0;
                        anims.forEach(a => {
                            var timing = a.effect ? a.effect.getComputedTiming() : null;
                            var dur = timing ? timing.duration : 0;
                            if (!dur || dur === Infinity || dur === 0) dur = 5000;
                            maxDuration = Math.max(maxDuration, dur);
                            maxCurrentTime = Math.max(maxCurrentTime, a.currentTime);
                        });
                        return { duration: maxDuration, current: maxCurrentTime };
                    })()
                ");
                
                if (!string.IsNullOrEmpty(result) && result != "null")
                {
                    var data = System.Text.Json.JsonDocument.Parse(result);
                    var duration = data.RootElement.GetProperty("duration").GetInt32();
                    var currentTime = data.RootElement.GetProperty("current").GetInt32();
                    
                    _totalFrames = (int)(duration / (1000.0 / _fps));
                    _currentFrameNum = (int)(currentTime / (1000.0 / _fps));
                    
                    txtCurrentFrame.Text = _currentFrameNum.ToString();
                    txtLastFrame.Text = _totalFrames.ToString();
                }
            }
            catch { }
        };
        _frameTimer.Start();
    }

    private void StopFrameCounter()
    {
        _frameTimer?.Stop();
    }

    private async void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_tempHtmlPath) || !File.Exists(_tempHtmlPath))
        {
            MessageBox.Show("No animation to export. Please generate first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = "MP4 Video|*.mp4",
            DefaultExt = ".mp4",
            FileName = "motion_design"
        };

        if (saveDialog.ShowDialog() != true) return;

        btnExport.IsEnabled = false;
        txtStatus.Text = "Exporting...";

        try
        {
            var outputPath = saveDialog.FileName;
            var width = int.Parse(txtWidth.Text);
            var height = int.Parse(txtHeight.Text);

            var framesDir = Path.Combine(Path.GetTempPath(), "frames");
            if (Directory.Exists(framesDir))
                Directory.Delete(framesDir, true);
            Directory.CreateDirectory(framesDir);

            txtStatus.Text = "Restarting animation...";
            
            try { _fps = double.Parse(txtFPS.Text); } catch { _fps = 30; }
            var frameDelay = 1000.0 / _fps;
            
            var animationInfo = await webPreview.CoreWebView2.ExecuteScriptAsync(@"
                (function() {
                    var anims = document.getAnimations();
                    if (anims.length === 0) return 5000;
                    var maxDuration = 0;
                    anims.forEach(a => {
                        var timing = a.effect ? a.effect.getComputedTiming() : null;
                        var dur = timing ? timing.duration : 0;
                        if (!dur || dur === Infinity || dur === 0) dur = 5000;
                        maxDuration = Math.max(maxDuration, dur);
                    });
                    return maxDuration;
                })()
            ");
            
            var animationDuration = 5000;
            if (!string.IsNullOrEmpty(animationInfo) && animationInfo != "null")
            {
                animationDuration = int.Parse(animationInfo);
            }
            
            var frameCount = (int)(animationDuration / frameDelay);
            if (frameCount < 10) frameCount = 10;
            
            txtLastFrame.Text = frameCount.ToString();
            
            await webPreview.CoreWebView2.ExecuteScriptAsync(@"
                document.body.style.opacity = '0.99';
                var elements = document.querySelectorAll('*');
                for (var el of elements) {
                    el.style.animation = 'none';
                    el.offsetHeight;
                    el.style.animation = null;
                }
                document.documentElement.style.animation = 'none';
                document.documentElement.offsetHeight;
                document.documentElement.style.animation = null;
            ");
            await Task.Delay(100);

            txtStatus.Text = "Recording frames...";
            
            for (int i = 0; i < frameCount; i++)
            {
                var framePath = Path.Combine(framesDir, $"frame_{i:D5}.png");
                var frameFile = new FileStream(framePath, FileMode.Create);
                await webPreview.CoreWebView2.CapturePreviewAsync(Microsoft.Web.WebView2.Core.CoreWebView2CapturePreviewImageFormat.Png, frameFile);
                frameFile.Close();
                
                txtCurrentFrame.Text = (i + 1).ToString();
                txtStatus.Text = $"Recording... {i + 1}/{frameCount}";
                await Task.Delay((int)frameDelay);
            }

            txtStatus.Text = "Encoding video...";
            var ffmpegExe = GetFFmpegPath();
            
            var args = $"-y -framerate {_fps} -i \"{framesDir}\\frame_%05d.png\" -c:v libx264 -pix_fmt yuv420p -crf 23 \"{outputPath}\"";
            
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = ffmpegExe,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process != null)
            {
                var completed = await Task.Run(() => process.WaitForExit(60000));
                
                if (completed && process.ExitCode == 0)
                {
                    txtStatus.Text = "Экспорт завершен!";
                    MessageBox.Show($"Видео сохранено: {outputPath}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (completed)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    txtStatus.Text = $"Ошибка экспорта";
                    MessageBox.Show($"Ошибка FFmpeg: {error}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    process.Kill();
                    txtStatus.Text = "Таймаут экспорта";
                    MessageBox.Show("Превышен таймаут экспорта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                txtStatus.Text = "Ошибка запуска FFmpeg";
                MessageBox.Show("Не удалось запустить FFmpeg", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try { Directory.Delete(framesDir, true); } catch { }
            btnExport.IsEnabled = true;
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Ошибка экспорта: {ex.Message}";
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnExport.IsEnabled = true;
        }
    }

    private string GetFFmpegPath()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(appDir, "ffmpeg.exe");
    }
}
