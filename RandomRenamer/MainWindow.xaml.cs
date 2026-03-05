using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using TF = TagLib;

namespace MP3RandomRenamer;

public partial class MainWindow : Window
{
    private readonly Random _random;
    private readonly string _namesFilePath;
    private List<string> _randomNames = [];
    private List<string> _itemsToProcess = [];
    private bool _isExpanded;

    public MainWindow()
    {
        InitializeComponent();
        
        _random = new Random();
        _namesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "names.txt");
        
        LoadRandomNames();
        TxtAuthor.Text = "NGB";
    }

    private void LoadRandomNames()
    {
        if (File.Exists(_namesFilePath))
        {
            var lines = File.ReadAllLines(_namesFilePath, Encoding.UTF8);
            _randomNames = [.. lines.Where(l => !string.IsNullOrWhiteSpace(l))];
        }
        else
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("MP3RandomRenamer.names.txt");
            if (stream != null)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var content = reader.ReadToEnd();
                var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                _randomNames = [.. lines.Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l))];
                
                File.WriteAllLines(_namesFilePath, _randomNames, Encoding.UTF8);
            }
            else
            {
                MessageBox.Show("Файл names.txt не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        UpdateNamesListBox();
    }

    private void SaveRandomNames() => File.WriteAllLines(_namesFilePath, _randomNames, Encoding.UTF8);

    private void UpdateNamesListBox()
    {
        LstNames.Items.Clear();
        foreach (var name in _randomNames)
            LstNames.Items.Add(name);
    }

    #region Window Dragging

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double click - maximize/restore
            WindowState = WindowState == WindowState.Maximized 
                ? WindowState.Normal 
                : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    #endregion

    #region Drag & Drop

    private void DragDropPanel_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            DragDropBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4F46E5"));
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private void DragDropPanel_DragLeave(object sender, DragEventArgs e)
    {
        DragDropBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6366F1"));
    }

    private void DragDropPanel_Drop(object sender, DragEventArgs e)
    {
        DragDropBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6366F1"));
        
        _itemsToProcess.Clear();
        TxtLog.Clear();

        if (e.Data.GetData(DataFormats.FileDrop) is string[] droppedItems)
        {
            TxtLog.Text = $"Файлы для обработки:\n{new string('-', 40)}\n";

            foreach (var item in droppedItems)
            {
                if (Directory.Exists(item))
                {
                    var files = Directory.GetFiles(item);
                    _itemsToProcess.AddRange(files);
                    DragLabel.Text = $"Папка: {Path.GetFileName(item)}\nНайдено файлов: {files.Length}";
                    TxtLog.Text += $"Папка: {Path.GetFileName(item)}\n";
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        TxtLog.Text += $"   {fileInfo.Name} ({fileInfo.Length / 1024.0:F1} КБ)\n";
                    }
                }
                else if (File.Exists(item))
                {
                    _itemsToProcess.Add(item);
                }
            }

            foreach (var item in droppedItems)
            {
                if (File.Exists(item) && !_itemsToProcess.Contains(item))
                {
                    var fileInfo = new FileInfo(item);
                    TxtLog.Text += $"   {fileInfo.Name} ({fileInfo.Length / 1024.0:F1} КБ)\n";
                }
            }

            TxtLog.Text += $"\n{new string('-', 40)}\n";
            TxtLog.Text += $"Всего: {_itemsToProcess.Count} файл(ов)\n\n";

            DragLabel.Text = _itemsToProcess.Count == 1
                ? $"Файл: {Path.GetFileName(_itemsToProcess[0])}"
                : $"Выбрано файлов: {_itemsToProcess.Count}";

            ShowLogPanel();
        }
    }

    private void DragDropBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Выберите папку с MP3 файлами"
        };

        if (dialog.ShowDialog() == true)
        {
            var files = Directory.GetFiles(dialog.FolderName);
            _itemsToProcess = [.. files];
            
            TxtLog.Text = $"Файлы для обработки:\n{new string('-', 40)}\n";
            TxtLog.Text += $"Папка: {Path.GetFileName(dialog.FolderName)}\n";
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                TxtLog.Text += $"   {fileInfo.Name} ({fileInfo.Length / 1024.0:F1} КБ)\n";
            }
            TxtLog.Text += $"\n{new string('-', 40)}\n";
            TxtLog.Text += $"Всего: {_itemsToProcess.Count} файл(ов)\n\n";

            DragLabel.Text = $"Выбрано файлов: {_itemsToProcess.Count}";
            ShowLogPanel();
        }
    }

    private void ShowLogPanel()
    {
        if (_isExpanded) return;
        
        _isExpanded = true;
        LogPanel.Visibility = Visibility.Visible;
        
        var newHeight = Height + 200;
        var screenHeight = SystemParameters.WorkArea.Height;
        
        if (newHeight > screenHeight)
            newHeight = screenHeight;
        
        Height = newHeight;
        
        var newTop = (screenHeight - newHeight) / 2;
        if (newTop > 0) Top = newTop;
    }

    private void HideLogPanel()
    {
        if (!_isExpanded) return;
        
        _isExpanded = false;
        LogPanel.Visibility = Visibility.Collapsed;
        Height = 750;
        
        var screenHeight = SystemParameters.WorkArea.Height;
        Top = (screenHeight - Height) / 2;
    }

    #endregion

    #region Buttons

    private void BtnAddName_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SimpleInputDialog("Добавление имени", "Введите новое имя:");
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
        {
            var name = dialog.InputText.Trim();
            if (!_randomNames.Contains(name))
            {
                _randomNames.Add(name);
                UpdateNamesListBox();
                SaveRandomNames();
            }
            else
            {
                MessageBox.Show("Такое имя уже существует в списке.", "Предупреждение", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void BtnRemoveName_Click(object sender, RoutedEventArgs e)
    {
        if (LstNames.SelectedIndex >= 0)
        {
            _randomNames.RemoveAt(LstNames.SelectedIndex);
            UpdateNamesListBox();
            SaveRandomNames();
        }
        else
        {
            MessageBox.Show("Выберите имя для удаления.", "Предупреждение", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnEditTable_Click(object sender, RoutedEventArgs e)
    {
        var editorWindow = new NamesEditorWindow(_randomNames);
        if (editorWindow.ShowDialog() == true)
        {
            _randomNames = editorWindow.GetNames();
            UpdateNamesListBox();
            SaveRandomNames();
        }
    }

    private void BtnRename_Click(object sender, RoutedEventArgs e)
    {
        if (_itemsToProcess.Count == 0)
        {
            MessageBox.Show("Перетащите папку или файлы для переименования.", "Предупреждение", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_randomNames.Count == 0)
        {
            MessageBox.Show("Добавьте хотя бы одно слово в библиотеку случайных слов.", "Предупреждение", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var author = TxtAuthor.Text.Trim();
        if (string.IsNullOrEmpty(author))
        {
            MessageBox.Show("Введите имя автора.", "Предупреждение", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TxtLog.Clear();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var title = TxtTitle.Text.Trim();

        foreach (var filePath in _itemsToProcess)
        {
            try
            {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext != ".mp3")
                {
                    TxtLog.Text += $"ПРОПУСК: {Path.GetFileName(filePath)} (не MP3)\n";
                    continue;
                }

                var directory = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException("Invalid file path.");
                var oldFileName = Path.GetFileName(filePath);
                var randomName = _randomNames[_random.Next(_randomNames.Count)];
                var newBaseName = $"{author} - {title} {randomName}";

                var newFileName = newBaseName;
                var counter = 1;
                while (usedNames.Contains($"{newFileName}{ext}"))
                {
                    newFileName = $"{newBaseName} ({counter})";
                    counter++;
                }

                var fullNewName = $"{newFileName}{ext}";
                var newFilePath = Path.Combine(directory, fullNewName);

                File.Move(filePath, newFilePath);
                usedNames.Add(fullNewName);
                
                try
                {
                    using var mp3File = TF.File.Create(newFilePath);
                    mp3File.Tag.Performers = [author];
                    mp3File.Tag.Title = $"{title} {randomName}";
                    mp3File.Tag.Comment = null;
                    mp3File.Save();
                }
                catch
                {
                    // Ignore tag errors
                }

                TxtLog.Text += $"OK: {oldFileName} -> {fullNewName}\n";
            }
            catch (Exception ex)
            {
                TxtLog.Text += $"ОШИБКА: {Path.GetFileName(filePath)} - {ex.Message}\n";
            }
        }

        _itemsToProcess.Clear();
        DragLabel.Text = "Перетащите папку или файлы сюда";
    }

    #endregion
}
