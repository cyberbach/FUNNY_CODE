using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using TF = TagLib;

namespace MP3RandomRenamer;

/// <summary>
/// Main form for the MP3 Random Renamer application.
/// </summary>
internal partial class MainForm : Form
{
    #region Fields

    // UI Controls
    private Panel _dragDropPanel = null!;
    private Label _dragDropLabel = null!;
    private Label _dragDropIcon = null!;
    private TextBox _txtAuthor = null!;
    private TextBox _txtTitle = null!;
    private ListBox _lstNames = null!;
    private Button _btnAddName = null!;
    private Button _btnRemoveName = null!;
    private Button _btnEditTable = null!;
    private Button _btnRename = null!;
    private TextBox _txtLog = null!;
    private Panel _logPanel = null!;

    // Data
    private List<string> _randomNames;
    private readonly Random _random;
    private readonly string _namesFilePath;
    private List<string> _itemsToProcess;

    // Window dragging
    private bool _isDragging;
    private Point _dragStartPoint;

    // Colors
    private static readonly Color PrimaryColor = Color.FromArgb(99, 102, 241);
    private static readonly Color PrimaryHoverColor = Color.FromArgb(79, 70, 229);
    private static readonly Color SecondaryColor = Color.FromArgb(168, 85, 247);
    private static readonly Color BackgroundColor = Color.FromArgb(15, 23, 42);
    private static readonly Color SurfaceColor = Color.FromArgb(30, 41, 59);
    private static readonly Color SurfaceHoverColor = Color.FromArgb(51, 65, 85);
    private static readonly Color TextPrimaryColor = Color.FromArgb(248, 250, 252);
    private static readonly Color TextSecondaryColor = Color.FromArgb(148, 163, 184);
    private static readonly Color ErrorColor = Color.FromArgb(239, 68, 68);

    #endregion

    #region Constructor

    public MainForm()
    {
        _randomNames = [];
        _random = new Random();
        _namesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "names.txt");
        _itemsToProcess = [];

        InitializeComponent();
        LoadRandomNames();
        _txtAuthor.Text = "NGB";
        ApplyDragToControl(this);
    }

    #endregion

    #region Event Handlers - Window Dragging

    private void ApplyDragToControl(Control control)
    {
        control.MouseDown += Form_MouseDown;
        control.MouseMove += Form_MouseMove;
        control.MouseUp += Form_MouseUp;
        foreach (Control child in control.Controls)
            ApplyDragToControl(child);
    }

    private void Form_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _dragStartPoint = new Point(e.X, e.Y);
        }
    }

    private void Form_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var screenPoint = PointToScreen(new Point(e.X, e.Y));
            Location = new Point(screenPoint.X - _dragStartPoint.X, screenPoint.Y - _dragStartPoint.Y);
        }
    }

    private void Form_MouseUp(object? sender, MouseEventArgs e) => _isDragging = false;

    #endregion

    #region Event Handlers - Drag & Drop

    private void DragDropPanel_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
            _dragDropPanel.BackColor = SurfaceHoverColor;
            _dragDropIcon.ForeColor = PrimaryHoverColor;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    private void DragDropPanel_DragDrop(object? sender, DragEventArgs e)
    {
        _dragDropPanel.BackColor = SurfaceColor;
        _dragDropIcon.ForeColor = PrimaryColor;
        _itemsToProcess.Clear();
        _txtLog.Clear();

        if (e.Data?.GetData(DataFormats.FileDrop) is string[] droppedItems)
        {
            _txtLog.AppendText($"📂 Файлы для обработки:{Environment.NewLine}");
            _txtLog.AppendText(new string('-', 50) + Environment.NewLine);

            foreach (var item in droppedItems)
            {
                if (Directory.Exists(item))
                {
                    var files = Directory.GetFiles(item);
                    _itemsToProcess.AddRange(files);
                    _dragDropLabel.Text = $"📁 Папка: {Path.GetFileName(item)}\nНайдено файлов: {files.Length}";
                    _txtLog.AppendText($"📁 Папка: {Path.GetFileName(item)}{Environment.NewLine}");
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        _txtLog.AppendText($"   📄 {fileInfo.Name} ({fileInfo.Length / 1024.0:F1} КБ){Environment.NewLine}");
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
                    _txtLog.AppendText($"   📄 {fileInfo.Name} ({fileInfo.Length / 1024.0:F1} КБ){Environment.NewLine}");
                }
            }

            _txtLog.AppendText(new string('-', 50) + Environment.NewLine);
            _txtLog.AppendText($"Всего: {_itemsToProcess.Count} файл(ов){Environment.NewLine}{Environment.NewLine}");

            _dragDropLabel.Text = _itemsToProcess.Count == 1
                ? $"📄 Файл: {Path.GetFileName(_itemsToProcess[0])}"
                : $"📄 Выбрано файлов: {_itemsToProcess.Count}";

            ShowLogPanel();
        }
    }

    private void ShowLogPanel()
    {
        if (_logPanel.Visible) return;
        
        _logPanel.Visible = true;
        
        // Расширяем окно для отображения журнала (добавляем высоту журнала + отступ)
        const int logExpandHeight = 200;
        var newHeight = Height + logExpandHeight;
        var maxHeight = Screen.PrimaryScreen?.WorkingArea.Height ?? 900;
        
        // Проверяем, что окно не выйдет за пределы экрана
        if (newHeight > maxHeight)
        {
            newHeight = maxHeight;
        }
        
        // Центрируем окно по вертикали после расширения
        var newY = (maxHeight - newHeight) / 2;
        if (newY < 0) newY = 0;
        
        Height = newHeight;
        Top = newY;
        
        UpdateFormRegion();
    }

    private void HideLogPanel()
    {
        if (!_logPanel.Visible) return;
        
        _logPanel.Visible = false;
        
        // Возвращаем исходный размер окна
        Height = 620;
        
        // Центрируем окно
        var maxHeight = Screen.PrimaryScreen?.WorkingArea.Height ?? 900;
        var newY = (maxHeight - Height) / 2;
        if (newY < 0) newY = 0;
        Top = newY;
        
        UpdateFormRegion();
    }

    private void DragDropPanel_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(PrimaryColor, 2) { DashStyle = DashStyle.Dash };
        var rect = new Rectangle(10, 10, _dragDropPanel.Width - 20, _dragDropPanel.Height - 20);
        using var path = CreateRoundedRectanglePath(rect, 15);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawPath(pen, path);
    }

    #endregion

    #region Event Handlers - Buttons

    private void BtnAddName_Click(object? sender, EventArgs e)
    {
        using var dialog = new SimpleInputDialog("Добавление имени", "Введите новое имя:");
        if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.InputText))
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
                MessageBox.Show("Такое имя уже существует в списке.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void BtnRemoveName_Click(object? sender, EventArgs e)
    {
        if (_lstNames.SelectedIndex >= 0)
        {
            _randomNames.RemoveAt(_lstNames.SelectedIndex);
            UpdateNamesListBox();
            SaveRandomNames();
        }
        else
        {
            MessageBox.Show("Выберите имя для удаления.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnEditTable_Click(object? sender, EventArgs e)
    {
        using var editorForm = new NamesEditorForm(_randomNames);
        if (editorForm.ShowDialog() == DialogResult.OK)
        {
            _randomNames = editorForm.GetNames();
            UpdateNamesListBox();
            SaveRandomNames();
        }
    }

    private void BtnRename_Click(object? sender, EventArgs e)
    {
        if (_itemsToProcess.Count == 0)
        {
            MessageBox.Show("Перетащите папку или файлы для переименования.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_randomNames.Count == 0)
        {
            MessageBox.Show("Добавьте хотя бы одно слово в библиотеку случайных слов.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var author = _txtAuthor.Text.Trim();
        if (string.IsNullOrEmpty(author))
        {
            MessageBox.Show("Введите имя автора.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _txtLog.Clear();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var title = _txtTitle.Text.Trim();

        foreach (var filePath in _itemsToProcess)
        {
            try
            {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                if (ext != ".mp3")
                {
                    _txtLog.AppendText($"⏭️  ПРОПУСК: {Path.GetFileName(filePath)} (не MP3){Environment.NewLine}");
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
                UpdateMp3Tags(newFilePath, author, $"{title} {randomName}");
                _txtLog.AppendText($"✅ {oldFileName} → {fullNewName}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                _txtLog.AppendText($"❌ ОШИБКА: {Path.GetFileName(filePath)} - {ex.Message}{Environment.NewLine}");
            }
        }

        _itemsToProcess.Clear();
        _dragDropLabel.Text = "Перетащите папку или файлы сюда";
        _dragDropIcon.ForeColor = PrimaryColor;
    }

    #endregion

    #region Helper Methods

    private void LoadRandomNames()
    {
        if (File.Exists(_namesFilePath))
        {
            var lines = File.ReadAllLines(_namesFilePath, Encoding.UTF8);
            _randomNames = [.. lines.Where(l => !string.IsNullOrWhiteSpace(l))];
        }
        else
        {
            _randomNames = [.. DefaultNames];
            SaveRandomNames();
        }
        UpdateNamesListBox();
    }

    private void SaveRandomNames() => File.WriteAllLines(_namesFilePath, _randomNames, Encoding.UTF8);

    private void UpdateNamesListBox()
    {
        _lstNames.Items.Clear();
        foreach (var name in _randomNames)
            _lstNames.Items.Add(name);
    }

    private static void UpdateMp3Tags(string filePath, string author, string title)
    {
        try
        {
            using var mp3File = TF.File.Create(filePath);
            mp3File.Tag.Performers = [author];
            mp3File.Tag.Title = title;
            mp3File.Tag.Comment = null;
            mp3File.Save();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to update MP3 tags: {ex.Message}", ex);
        }
    }

    private static void StyleButton(Button button, string text, Color backColor, bool isPrimary = false)
    {
        button.Text = text;
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = backColor;
        button.ForeColor = TextPrimaryColor;
        button.Font = new Font("Segoe UI", isPrimary ? 14F : 11F, isPrimary ? FontStyle.Bold : FontStyle.Bold);
        button.Cursor = Cursors.Hand;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(backColor, 0.1f);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.1f);
    }

    private static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void UpdateFormRegion()
    {
        var rect = ClientRectangle;
        Region = Region.FromHrgn(CreateRoundRectRgn(rect.Left, rect.Top, rect.Right, rect.Bottom, 20, 20));
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

    #endregion
}
