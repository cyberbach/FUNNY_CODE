using NAudio.Wave;
using System.Numerics;
using NAudio.Lame;
using System.Linq;
using System.Runtime.InteropServices;

namespace AI_BROOM_MP3;

/// <summary>
/// Основная форма приложения: приём MP3 через Drag&Drop, анализ аудио и удаление отпечатков.
/// Содержит методы визуального обновления таблицы, простую детекцию AI-отпечатков
/// и несколько алгоритмов обработки (FFT, STFT-проходы и т.д.).
/// </summary>
public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }
    private string? currentFilePath = null;

    // Поддержка перетаскивания окна за тело
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    /// <summary>
    /// Обработчик загрузки формы: выполняет начальную настройку интерфейса.
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // Подогнать высоты лейблов при запуске
        AdjustLabelHeights();
    }

    /// <summary>
    /// Обработчик изменения размера формы (зарезервировано для возможной пересборки лейаутов).
    /// </summary>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        //AdjustLabelHeights();
    }

    /// <summary>
    /// Подбирает высоты верхней таблицы и лейблов так, чтобы они не мешали кнопкам внизу.
    /// Защищено try/catch для устойчивости при динамических изменениях UI.
    /// </summary>
    private void AdjustLabelHeights()
    {
        try
        {
            System.Windows.Forms.TableLayoutPanel? topTable = null;
            int buttonsTop = int.MaxValue;

            foreach (Control c in Controls)
            {
                if (c is System.Windows.Forms.TableLayoutPanel tbl && tbl.Name == "fileInfoTable")
                {
                    topTable = tbl;
                }
                else if (c is System.Windows.Forms.TableLayoutPanel ft && ft.Name == "fileInfoTable")
                {
                    // already handled above, keep for clarity
                }
                else if (c is System.Windows.Forms.Button btn)
                {
                    buttonsTop = Math.Min(buttonsTop, btn.Location.Y);
                }
            }


            if (topTable == null)
                return;

            if (buttonsTop == int.MaxValue)
            {
                // fallback: reserve bottom area of 140 px
                buttonsTop = ClientSize.Height - 140;
            }

            // compute available vertical area between table top and buttonsTop
            int topY = topTable.Location.Y;
            int available = Math.Max(120, buttonsTop - topY - 20);

            // allocate proportionally: table 40%, full label 60% of available
            int tableH = Math.Max(60, (int)(available * 0.4));
            int fullH = Math.Max(80, available - tableH - 10);

            // Make the top table fill the available area up to the buttons
            //int tableH = Math.Max(80, available - 10);

            topTable.Height = tableH;
        }
        catch
        {
            // ignore layout errors
        }
    }

    /// <summary>
    /// Рекурсивно навешивает обработчики MouseDown на контролы (кроме кнопок),
    /// чтобы можно было перетаскивать окно, зацепив за клиентскую область.
    /// </summary>
    private void AttachDragHandlers(Control control)
    {
        // Allow dragging by most client controls including labels and tables.
        // Avoid attaching to Buttons to keep their click behavior intact.
        if (!(control is System.Windows.Forms.Button) && control != this)
        {
            control.MouseDown += Form_MouseDown;
        }
        foreach (Control child in control.Controls)
        {
            AttachDragHandlers(child);
        }
    }

    /// <summary>
    /// Рекурсивный поиск контрола по имени в иерархии контролов.
    /// </summary>
    private Control? FindControlRecursive(Control parent, string name)
    {
        foreach (Control c in parent.Controls)
        {
            if (c.Name == name) return c;
            var found = FindControlRecursive(c, name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Переопределение WndProc: делает клиентскую область перемещаемой (HTCAPTION),
    /// но не мешает интерактивным контролам и зонам Drag&Drop.
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x84;
        const int HTCLIENT = 1;
        const int HTCAPTION = 2;

        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);
            int result = m.Result.ToInt32();
            if (result == HTCLIENT && !isDragOver)
            {
                // Convert lParam to client point
                int lp = m.LParam.ToInt32();
                int x = (short)(lp & 0xFFFF);
                int y = (short)((lp >> 16) & 0xFFFF);
                Point clientPt = PointToClient(new Point(x, y));
                Control? ctl = this.GetChildAtPoint(clientPt);
                if (ctl == null)
                {
                    m.Result = (IntPtr)HTCAPTION;
                    return;
                }
                // Don't override interactive controls or controls that accept drops
                if (ctl is System.Windows.Forms.ButtonBase || ctl is System.Windows.Forms.TextBoxBase || ctl is System.Windows.Forms.ComboBox || ctl is System.Windows.Forms.NumericUpDown || ctl is System.Windows.Forms.DataGridView || ctl is System.Windows.Forms.ListBox || ctl is System.Windows.Forms.CheckBox || ctl is System.Windows.Forms.RadioButton)
                {
                    return;
                }
                if (!ctl.AllowDrop)
                {
                    m.Result = (IntPtr)HTCAPTION;
                    return;
                }
            }
            return;
        }

        base.WndProc(ref m);
    }

    /// <summary>
    /// Обработчик MouseDown, стартует перемещение окна при зажатой левой кнопке мыши.
    /// </summary>
    private void Form_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }
    }

    // Track when an external drag operation is over the form so WndProc
    // doesn't treat the client area as caption and block drag events.
    private bool isDragOver = false;

    private void CloseButton_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    /// <summary>
    /// Сбрасывает состояние UI: очищает таблицу, скрывает кнопки и сбрасывает путь на файл.
    /// </summary>
    private void ResetButton_Click(object sender, EventArgs e)
    {
        // Найти Label для отображения информации о файле
        System.Windows.Forms.Label? fileInfoLabel = null;
        foreach (Control control in Controls)
        {
            if (control is System.Windows.Forms.Label && control.Name == "fileInfoLabel")
            {
                fileInfoLabel = (System.Windows.Forms.Label)control;
                break;
            }
        }

        if (fileInfoLabel != null)
        {
            fileInfoLabel.Text = "Перетащите MP3 файл сюда для анализа";
        }

        // Скрыть кнопки
        foreach (Control control in Controls)
        {
            if (control is System.Windows.Forms.Button button)
            {
                if (button.Name == "resetButton" || button.Name == "analyzeButton" || button.Name == "removeFingerprintsButton")
                {
                    button.Visible = false;
                }
            }
        }

        // Очистить таблицу и сбросить путь к текущему файлу
        var tblPanel = FindControlRecursive(this, "fileInfoTable") as System.Windows.Forms.TableLayoutPanel;
        if (tblPanel != null)
        {
            tblPanel.Controls.Clear();
            tblPanel.RowCount = 0;
            // add placeholder three columns
            var k = new System.Windows.Forms.Label(); k.Text = "Информация"; k.Dock = System.Windows.Forms.DockStyle.Fill;
            var v = new System.Windows.Forms.Label(); v.Text = "Перетащите MP3 файл сюда для анализа"; v.Dock = System.Windows.Forms.DockStyle.Fill;
            var f = new System.Windows.Forms.Label(); f.Text = "Финальный файл"; f.Dock = System.Windows.Forms.DockStyle.Fill;
            tblPanel.RowCount = 1; tblPanel.RowStyles.Clear(); tblPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            k.AllowDrop = true; k.DragEnter += MainForm_DragEnter; k.DragDrop += MainForm_DragDrop; k.DragLeave += MainForm_DragLeave;
            v.AllowDrop = true; v.DragEnter += MainForm_DragEnter; v.DragDrop += MainForm_DragDrop; v.DragLeave += MainForm_DragLeave;
            f.AllowDrop = true; f.DragEnter += MainForm_DragEnter; f.DragDrop += MainForm_DragDrop; f.DragLeave += MainForm_DragLeave;
            tblPanel.Controls.Add(k, 0, 0); tblPanel.Controls.Add(v, 1, 0); tblPanel.Controls.Add(f, 2, 0);
        }
        currentFilePath = null;
    }

    /// <summary>
    /// Выполняет анализ текущего MP3: собирает метрики, запускает детектор и обновляет таблицу.
    /// </summary>
    private void AnalyzeButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(currentFilePath) || !File.Exists(currentFilePath))
        {
            MessageBox.Show("Сначала перетащите MP3 файл на форму для анализа.", "Анализ",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var mp3 = new Mp3FileReader(currentFilePath);
            TimeSpan duration = mp3.TotalTime;
            var wf = mp3.WaveFormat;

            // Оценочный средний битрейт (kbps)
            var fileSizeBytes = new FileInfo(currentFilePath).Length;
            double bitrateKbps = 0;
            if (duration.TotalSeconds > 0)
            {
                bitrateKbps = (fileSizeBytes * 8.0) / duration.TotalSeconds / 1000.0;
            }

            // Build detailed info map for the top table
            var vals = ComputeAudioFeatureValues(currentFilePath);
            var infoMap = new Dictionary<string,string>
            {
                ["Файл"] = Path.GetFileName(currentFilePath),
                ["Длительность"] = duration.ToString("c"),
                ["Оценочный битрейт"] = $"{bitrateKbps:F0} kbps",
                ["Частота дискретизации"] = wf.SampleRate.ToString(),
                ["Каналы"] = wf.Channels.ToString(),
                ["Бит на сэмпл"] = wf.BitsPerSample.ToString(),
                ["RMS"] = vals.rms.ToString("F4"),
                ["Zero-cross rate (approx./s)"] = vals.zcr.ToString("F1"),
                ["High-freq energy ratio"] = vals.highFreqRatio.ToString("P2"),
                ["Peak density (peaks/s)"] = vals.peakDensity.ToString("F1")
            };

            var (likelyAI, score) = DetectAIFingerprint(currentFilePath);
            infoMap["AI-отпечаток"] = (likelyAI ? "Возможно" : "Маловероятно") + $" {score:P0}";

            UpdateInfoTable(infoMap);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при анализе файла: {ex.Message}", "Анализ",
                           MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Запускает продвинутую процедуру удаления отпечатков и показывает результат в UI.
    /// </summary>
    private void RemoveFingerprintsButton_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(currentFilePath) || !File.Exists(currentFilePath))
        {
            MessageBox.Show("Сначала перетащите MP3 файл на форму для анализа.", "Удаление отпечатков",
                           MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            string outMp3 = Path.Combine(Path.GetDirectoryName(currentFilePath) ?? ".", Path.GetFileNameWithoutExtension(currentFilePath) + "_clean.mp3");
            Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            // Advanced removal which produces MP3 and returns final info map
            var finalMap = RemoveFingerprintsAdvanced(currentFilePath, outMp3);
            Cursor = Cursors.Default;

            // Enrich finalMap with computed metrics for cleaned file
            try
            {
                var vals = ComputeAudioFeatureValues(outMp3);
                finalMap["RMS"] = vals.rms.ToString("F4");
                finalMap["Zero-cross rate (approx./s)"] = vals.zcr.ToString("F1");
                finalMap["High-freq energy ratio"] = vals.highFreqRatio.ToString("P2");
                finalMap["Peak density (peaks/s)"] = vals.peakDensity.ToString("F1");
                var (likelyAI, score) = DetectAIFingerprint(outMp3);
                finalMap["AI-отпечаток"] = (likelyAI ? "Возможно" : "Маловероятно") + $" {score:P0}";
            }
            catch { }

            // Update UI: top table shows original vs final
            var fileInfo = new FileInfo(currentFilePath);
            string lastWrite = fileInfo.LastWriteTime.ToString("G");
            var origMap = new Dictionary<string,string> {
                ["Файл"] = fileInfo.Name,
                ["Размер"] = $"{fileInfo.Length / 1024.0:F2} KB",
                ["Путь"] = fileInfo.FullName,
                ["Последнее изменение"] = lastWrite,
                ["Тип"] = "MP3 аудиофайл"
            };
            UpdateInfoTable(origMap, finalMap);

            MessageBox.Show($"Отпечатки удалены — файл сохранён: {outMp3}", "Удаление отпечатков",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Cursor = Cursors.Default;
            MessageBox.Show($"Ошибка при удалении отпечатков: {ex.Message}", "Удаление отпечатков",
                           MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Обработка DragEnter: если перенесён MP3-файл — разрешаем операцию Copy.
    /// </summary>
    private void MainForm_DragEnter(object? sender, DragEventArgs e)
    {
        isDragOver = true;
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            object? data = e.Data.GetData(DataFormats.FileDrop);
            if (data is string[] files && files.Length > 0 && files[0]?.ToLower()?.EndsWith(".mp3") == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
    }

    /// <summary>
    /// DragLeave: сбрасывает флаг внешнего перетаскивания.
    /// </summary>
    private void MainForm_DragLeave(object? sender, EventArgs e)
    {
        isDragOver = false;
    }

    /// <summary>
    /// DragDrop: при отпускании MP3 обновляет информацию о файле в UI.
    /// </summary>
    private void MainForm_DragDrop(object? sender, DragEventArgs e)
    {
        isDragOver = false;
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            object? data = e.Data.GetData(DataFormats.FileDrop);
            if (data is string[] files && files.Length > 0 && files[0]?.ToLower()?.EndsWith(".mp3") == true)
            {
                // Update UI on drop
                DisplayFileInfo(files[0]);
            }
        }
    }

    /// <summary>
    /// Собирает и отображает базовую информацию о файле (параметры, метрики, путь, размер и т.д.).
    /// </summary>
    private void DisplayFileInfo(string filePath)
    {
        try
        {
            // Получение информации о файле и базовых аудио-метрик
            FileInfo fileInfo = new FileInfo(filePath);
            DateTime lastWriteTime = fileInfo.LastWriteTime;
            string lastWriteTimeStr = lastWriteTime.ToString("G")!;

            using var mp3 = new Mp3FileReader(filePath);
            var wf = mp3.WaveFormat;
            TimeSpan duration = mp3.TotalTime;
            var fileSizeBytes = fileInfo.Length;
            double bitrateKbps = 0;
            if (duration.TotalSeconds > 0) bitrateKbps = (fileSizeBytes * 8.0) / duration.TotalSeconds / 1000.0;

            // numeric feature values
            var vals = ComputeAudioFeatureValues(filePath);

            var map = new Dictionary<string, string>
            {
                ["Файл"] = fileInfo.Name,
                ["Длительность"] = duration.ToString("c"),
                ["Оценочный битрейт"] = $"{bitrateKbps:F0} kbps",
                ["Частота дискретизации"] = wf.SampleRate.ToString(),
                ["Каналы"] = wf.Channels.ToString(),
                ["Бит на сэмпл"] = wf.BitsPerSample.ToString(),
                ["Размер"] = $"{fileInfo.Length / 1024.0:F2} KB",
                ["Путь"] = fileInfo.FullName,
                ["Последнее изменение"] = lastWriteTimeStr,
                ["Тип"] = "MP3 аудиофайл",
                ["RMS"] = vals.rms.ToString("F4"),
                ["Zero-cross rate (approx./s)"] = vals.zcr.ToString("F1"),
                ["High-freq energy ratio"] = vals.highFreqRatio.ToString("P1"),
                ["Peak density (peaks/s)"] = vals.peakDensity.ToString("F1")
            };

            // Обновить таблицу информации (ищет по имени `fileInfoTable`)
            UpdateInfoTable(map);

            // Сохраняем путь к текущему файлу для дальнейшего анализа
            currentFilePath = filePath;

            // Показать кнопки после успешной загрузки файла
            foreach (Control control in Controls)
            {
                if (control is System.Windows.Forms.Button button)
                {
                    if (button.Name == "resetButton" || button.Name == "analyzeButton" || button.Name == "removeFingerprintsButton")
                    {
                        button.Visible = true;
                    }
                }
            }

            // Ensure table is visible and force a UI refresh
            foreach (Control c in Controls)
            {
                if (c is System.Windows.Forms.TableLayoutPanel tbl && tbl.Name == "fileInfoTable") tbl.Visible = true;
            }
            this.Refresh();
            Application.DoEvents();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при обработке файла: {ex.Message}", "Ошибка",
                           MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Простая детекция AI-отпечатка на основе спектральной плоскостности и других признаков.
    /// Возвращает (likelyAI, score) — булев флаг и оценочный скор в диапазоне 0..1.
    /// </summary>
    private (bool likelyAI, double score) DetectAIFingerprint(string path)
    {
        try
        {
            // Compute spectral flatness as before
            using var mp3 = new Mp3FileReader(path);
            var sampleProvider = mp3.ToSampleProvider();
            int sampleRate = mp3.WaveFormat.SampleRate;

            int maxSeconds = 30;
            int maxSamples = sampleRate * maxSeconds * mp3.WaveFormat.Channels;
            var buffer = new float[4096 * 8];
            var samples = new List<float>();
            int read;
            while ((read = sampleProvider.Read(buffer, 0, Math.Min(buffer.Length, maxSamples - samples.Count))) > 0 && samples.Count < maxSamples)
            {
                for (int i = 0; i < read; i++) samples.Add(buffer[i]);
            }
            if (samples.Count < 1024) return (false, 0.0);

            int frameSize = 4096;
            int hop = 2048;
            var window = HammingWindow(frameSize);
            var flatnessList = new List<double>();
            for (int pos = 0; pos + frameSize <= samples.Count; pos += hop)
            {
                var frame = new Complex[frameSize];
                for (int i = 0; i < frameSize; i++) frame[i] = new Complex(samples[pos + i] * window[i], 0);
                FFT(frame);
                var mags = frame.Take(frameSize / 2).Select(c => c.Magnitude + 1e-12).ToArray();
                double arith = mags.Average();
                double geom = Math.Exp(mags.Select(m => Math.Log(m)).Average());
                double flatness = geom / arith;
                flatnessList.Add(flatness);
            }
            double avgFlatness = flatnessList.Average();

            // Also compute numeric audio features to combine signals
            var feats = ComputeAudioFeatureValues(path);

            // Normalize components into 0..1
            double normFlat = Math.Clamp((avgFlatness - 0.15) / (0.6 - 0.15), 0.0, 1.0);
            double normHigh = Math.Clamp((feats.highFreqRatio - 0.05) / 0.4, 0.0, 1.0); // >~0.2-0.3 suspicious
            double normPeak = Math.Clamp(feats.peakDensity / 10.0, 0.0, 1.0); // peaks per second

            // Weighted combination
            double score = 0.6 * normFlat + 0.3 * normHigh + 0.1 * normPeak;
            bool likelyAI = score > 0.5;
            return (likelyAI, score);
        }
        catch
        {
            return (false, 0.0);
        }
    }

    /// <summary>
    /// Возвращает массив весов окна Хэмминга длины n.
    /// </summary>
    private double[] HammingWindow(int n)
    {
        var w = new double[n];
        for (int i = 0; i < n; i++) w[i] = 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (n - 1));
        return w;
    }

    /// <summary>
    /// Обновляет `fileInfoTable` значениями из словаря. Если задан finalMap — добавляет третий столбец для итоговых значений.
    /// </summary>
    private void UpdateInfoTable(Dictionary<string, string> map, Dictionary<string, string>? finalMap = null)
    {
        foreach (Control control in Controls)
        {
            if (control is System.Windows.Forms.TableLayoutPanel tbl && tbl.Name == "fileInfoTable")
            {
                tbl.Controls.Clear();
                tbl.RowStyles.Clear();
                int row = 0;
                // Build row key list as union of original and final keys so third column aligns
                var keys = new List<string>();
                foreach (var k in map.Keys) keys.Add(k);
                if (finalMap != null)
                {
                    foreach (var k in finalMap.Keys) if (!keys.Contains(k)) keys.Add(k);
                }
                tbl.RowCount = Math.Max(1, keys.Count);
                foreach (var key in keys)
                {
                    tbl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
                    var keyLbl = new System.Windows.Forms.Label();
                    keyLbl.Text = key;
                    keyLbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                    keyLbl.AllowDrop = true;
                    keyLbl.DragEnter += MainForm_DragEnter;
                    keyLbl.DragDrop += MainForm_DragDrop;
                    keyLbl.DragLeave += MainForm_DragLeave;
                    keyLbl.Dock = System.Windows.Forms.DockStyle.Fill;

                    var valLbl = new System.Windows.Forms.Label();
                    valLbl.Text = map.TryGetValue(key, out var v) ? v : string.Empty;
                    valLbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                    valLbl.AllowDrop = true;
                    valLbl.DragEnter += MainForm_DragEnter;
                    valLbl.DragDrop += MainForm_DragDrop;
                    valLbl.DragLeave += MainForm_DragLeave;
                    valLbl.Dock = System.Windows.Forms.DockStyle.Fill;

                    tbl.Controls.Add(keyLbl, 0, row);
                    tbl.Controls.Add(valLbl, 1, row);

                    if (finalMap != null)
                    {
                        var finalLbl = new System.Windows.Forms.Label();
                        finalLbl.Text = finalMap.TryGetValue(key, out var fv) ? fv : string.Empty;
                        finalLbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                        finalLbl.AllowDrop = true;
                        finalLbl.DragEnter += MainForm_DragEnter;
                        finalLbl.DragDrop += MainForm_DragDrop;
                        finalLbl.DragLeave += MainForm_DragLeave;
                        finalLbl.Dock = System.Windows.Forms.DockStyle.Fill;
                        tbl.Controls.Add(finalLbl, 2, row);
                    }
                    row++;
                }
                return;
            }
        }
    }

    /// <summary>
    /// In-place Cooley-Tukey FFT (radix-2). Используется для спектрального анализа.
    /// </summary>
    private void FFT(Complex[] buffer)
    {
        int n = buffer.Length;
        int bits = (int)Math.Log2(n);
        for (int i = 0; i < n; i++)
        {
            int j = ReverseBits(i, bits);
            if (j > i)
            {
                var tmp = buffer[i]; buffer[i] = buffer[j]; buffer[j] = tmp;
            }
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = -2 * Math.PI / len;
            Complex wlen = new Complex(Math.Cos(angle), Math.Sin(angle));
            for (int i = 0; i < n; i += len)
            {
                Complex w = Complex.One;
                for (int j = 0; j < len / 2; j++)
                {
                    Complex u = buffer[i + j];
                    Complex v = buffer[i + j + len / 2] * w;
                    buffer[i + j] = u + v;
                    buffer[i + j + len / 2] = u - v;
                    w *= wlen;
                }
            }
        }
    }

    /// <summary>
    /// Обратное FFT, реализовано через сопряжение (конструкция: conjugate -> FFT -> conjugate / n).
    /// </summary>
    private void IFFT(Complex[] buffer)
    {
        int n = buffer.Length;
        for (int i = 0; i < n; i++) buffer[i] = Complex.Conjugate(buffer[i]);
        FFT(buffer);
        for (int i = 0; i < n; i++) buffer[i] = Complex.Conjugate(buffer[i]) / n;
    }

    /// <summary>
    /// Переворачивает младшие <paramref name="bits"/> бит числа <paramref name="x"/>.
    /// Требуется для перестановки индексов в FFT.
    /// </summary>
    private int ReverseBits(int x, int bits)
    {
        int y = 0;
        for (int i = 0; i < bits; i++)
        {
            y = (y << 1) | (x & 1);
            x >>= 1;
        }
        return y;
    }

    /// <summary>
    /// Вычисляет набор строковых аудио-метрик: RMS, ZCR, спектральный центроид, доля энергии в ВЧ и плотность пиков.
    /// Возвращает читаемый многострочный отчёт.
    /// </summary>
    private string ComputeAudioFeatures(string path)
    {
        try
        {
            using var mp3 = new Mp3FileReader(path);
            var sp = mp3.ToSampleProvider();
            int sampleRate = mp3.WaveFormat.SampleRate;
            int channels = mp3.WaveFormat.Channels;

            int maxSeconds = 30;
            int maxSamples = sampleRate * maxSeconds * channels;
            var buffer = new float[4096 * 8];
            var samples = new List<float>();
            int read;
            while ((read = sp.Read(buffer, 0, Math.Min(buffer.Length, maxSamples - samples.Count))) > 0 && samples.Count < maxSamples)
            {
                for (int i = 0; i < read; i++) samples.Add(buffer[i]);
            }

            if (samples.Count == 0) return "Нет доступных сэмплов";

            // Mono mix
            if (channels > 1)
            {
                var mono = new float[samples.Count / channels];
                for (int i = 0, j = 0; i + channels - 1 < samples.Count; i += channels, j++)
                {
                    float sum = 0;
                    for (int c = 0; c < channels; c++) sum += samples[i + c];
                    mono[j] = sum / channels;
                }
                samples = mono.ToList();
            }

            // RMS and ZCR
            double sumsq = 0; long zeroCross = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                double v = samples[i];
                sumsq += v * v;
                if (i > 0 && Math.Sign(samples[i]) != Math.Sign(samples[i - 1])) zeroCross++;
            }
            double rms = Math.Sqrt(sumsq / samples.Count);
            double zcr = zeroCross / (double)samples.Count * sampleRate; // per second approx

            // Spectral metrics per frame
            int frameSize = 4096;
            int hop = 2048;
            var window = HammingWindow(frameSize);
            var centroids = new List<double>();
            var highEnergyRatios = new List<double>();
            var peakCounts = new List<int>();

            for (int pos = 0; pos + frameSize <= samples.Count; pos += hop)
            {
                var frame = new Complex[frameSize];
                for (int i = 0; i < frameSize; i++) frame[i] = new Complex(samples[pos + i] * window[i], 0);
                FFT(frame);
                var mags = frame.Take(frameSize / 2).Select(c => c.Magnitude + 1e-12).ToArray();
                double total = mags.Sum();
                if (total <= 0) continue;
                // centroid
                double binFreq = sampleRate / (double)frameSize;
                double centroid = 0;
                for (int k = 0; k < mags.Length; k++) centroid += k * binFreq * mags[k];
                centroid /= total;
                centroids.Add(centroid);

                // high freq energy (>8kHz)
                int cutoffBin = (int)(8000 / binFreq);
                cutoffBin = Math.Min(cutoffBin, mags.Length - 1);
                double high = mags.Skip(cutoffBin).Sum();
                highEnergyRatios.Add(high / total);

                // peak count
                double avg = mags.Average();
                int peaks = 0;
                for (int k = 1; k < mags.Length - 1; k++) if (mags[k] > mags[k - 1] && mags[k] > mags[k + 1] && mags[k] > avg * 3) peaks++;
                peakCounts.Add(peaks);
            }

            double avgCentroid = centroids.Count > 0 ? centroids.Average() : 0;
            double avgHighRatio = highEnergyRatios.Count > 0 ? highEnergyRatios.Average() : 0;
            double avgPeakDensity = peakCounts.Count > 0 ? peakCounts.Average() / (hop / (double)sampleRate) : 0; // peaks per second

            return $"RMS: {rms:F4}\n" +
                   $"Zero-cross rate (approx./s): {zcr:F1}\n" +
                   $"Spectral centroid (Hz): {avgCentroid:F0}\n" +
                   $"High-freq energy ratio (>8kHz): {avgHighRatio:P1}\n" +
                   $"Peak density (peaks/s): {avgPeakDensity:F1}";
        }
        catch
        {
            return "Ошибка при вычислении доп. метрик";
        }
    }

    /// <summary>
    /// Возвращает числовые значения метрик (rms, zcr, centroid, highFreqRatio, peakDensity) для внутреннего использования.
    /// </summary>
    private (double rms, double zcr, double centroid, double highFreqRatio, double peakDensity) ComputeAudioFeatureValues(string path)
    {
        try
        {
            using var mp3 = new Mp3FileReader(path);
            var sp = mp3.ToSampleProvider();
            int sampleRate = mp3.WaveFormat.SampleRate;
            int channels = mp3.WaveFormat.Channels;

            int maxSeconds = 30;
            int maxSamples = sampleRate * maxSeconds * channels;
            var buffer = new float[4096 * 8];
            var samples = new List<float>();
            int read;
            while ((read = sp.Read(buffer, 0, Math.Min(buffer.Length, maxSamples - samples.Count))) > 0 && samples.Count < maxSamples)
            {
                for (int i = 0; i < read; i++) samples.Add(buffer[i]);
            }
            if (samples.Count == 0) return (0, 0, 0, 0, 0);

            if (channels > 1)
            {
                var mono = new float[samples.Count / channels];
                for (int i = 0, j = 0; i + channels - 1 < samples.Count; i += channels, j++)
                {
                    float sum = 0;
                    for (int c = 0; c < channels; c++) sum += samples[i + c];
                    mono[j] = sum / channels;
                }
                samples = mono.ToList();
            }

            double sumsq = 0; long zeroCross = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                double v = samples[i];
                sumsq += v * v;
                if (i > 0 && Math.Sign(samples[i]) != Math.Sign(samples[i - 1])) zeroCross++;
            }
            double rms = Math.Sqrt(sumsq / samples.Count);
            double zcr = zeroCross / (double)samples.Count * sampleRate;

            int frameSize = 4096;
            int hop = 2048;
            var window = HammingWindow(frameSize);
            var centroids = new List<double>();
            var highEnergyRatios = new List<double>();
            var peakCounts = new List<int>();

            for (int pos = 0; pos + frameSize <= samples.Count; pos += hop)
            {
                var frame = new Complex[frameSize];
                for (int i = 0; i < frameSize; i++) frame[i] = new Complex(samples[pos + i] * window[i], 0);
                FFT(frame);
                var mags = frame.Take(frameSize / 2).Select(c => c.Magnitude + 1e-12).ToArray();
                double total = mags.Sum();
                if (total <= 0) continue;
                double binFreq = sampleRate / (double)frameSize;
                double centroid = 0;
                for (int k = 0; k < mags.Length; k++) centroid += k * binFreq * mags[k];
                centroid /= total;
                centroids.Add(centroid);

                int cutoffBin = (int)(8000 / binFreq);
                cutoffBin = Math.Min(cutoffBin, mags.Length - 1);
                double high = mags.Skip(cutoffBin).Sum();
                highEnergyRatios.Add(high / total);

                double avg = mags.Average();
                int peaks = 0;
                for (int k = 1; k < mags.Length - 1; k++) if (mags[k] > mags[k - 1] && mags[k] > mags[k + 1] && mags[k] > avg * 3) peaks++;
                peakCounts.Add(peaks);
            }

            double avgCentroid = centroids.Count > 0 ? centroids.Average() : 0;
            double avgHighRatio = highEnergyRatios.Count > 0 ? highEnergyRatios.Average() : 0;
            double avgPeakDensity = peakCounts.Count > 0 ? peakCounts.Average() / (hop / (double)sampleRate) : 0;

            return (rms, zcr, avgCentroid, avgHighRatio, avgPeakDensity);
        }
        catch
        {
            return (0, 0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Пытается прочитать параметры MP3 (sampleRate, channels, bitsPerSample, bitrate) из первого фрейма.
    /// Возвращает разумные значения по умолчанию при ошибке.
    /// </summary>
    private (int sampleRate, int channels, int bitsPerSample, int bitrateKbps) GetMp3Parameters(string path)
    {
        try
        {
            int bitrate = 0;
            using (var fs = File.OpenRead(path))
            {
                var frame = NAudio.Wave.Mp3Frame.LoadFromStream(fs);
                if (frame != null) bitrate = frame.BitRate;
            }
            using var mp3 = new Mp3FileReader(path);
            int sr = mp3.WaveFormat.SampleRate;
            int ch = mp3.WaveFormat.Channels;
            int bps = mp3.WaveFormat.BitsPerSample > 0 ? mp3.WaveFormat.BitsPerSample : 16;
            return (sr, ch, bps, bitrate);
        }
        catch
        {
            return (44100, 2, 16, 192);
        }
    }

    /// <summary>
    /// Консервативный STFT-проход для ослабления полос-водяных знаков и высокочастотных компонентов.
    /// Результат сохраняется в WAV по пути <paramref name="outputWavPath"/>.
    /// </summary>
    private void RemoveFingerprints(string inputMp3Path, string outputWavPath)
    {
        // Read samples
        using var mp3 = new Mp3FileReader(inputMp3Path);
        var sp = mp3.ToSampleProvider();
        int sampleRate = mp3.WaveFormat.SampleRate;
        int channels = mp3.WaveFormat.Channels;
        int bitsPerSample = mp3.WaveFormat.BitsPerSample > 0 ? mp3.WaveFormat.BitsPerSample : 16;

        var buffer = new float[4096 * 16];
        var samples = new List<float>();
        int read;
        while ((read = sp.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++) samples.Add(buffer[i]);
        }

        if (samples.Count == 0) throw new InvalidOperationException("Нет аудиоданных в файле");

        // Deinterleave to per-channel arrays
        int framesPerChannel = samples.Count / channels;
        var channelSamples = new float[channels][];
        for (int ch = 0; ch < channels; ch++)
        {
            channelSamples[ch] = new float[framesPerChannel];
        }
        for (int i = 0; i < framesPerChannel; i++)
        {
            for (int ch = 0; ch < channels; ch++)
            {
                channelSamples[ch][i] = samples[i * channels + ch];
            }
        }

        int frameSize = 4096;
        int hop = frameSize / 2;
        var window = HammingWindow(frameSize);

        // Process each channel independently and store results
        var processedPerChannel = new double[channels][];
        for (int ch = 0; ch < channels; ch++)
        {
            var chSamples = channelSamples[ch];
            var output = new double[chSamples.Length + frameSize];

            int nFrames = Math.Max(1, (chSamples.Length - frameSize) / hop + 1);

            for (int f = 0; f < nFrames; f++)
            {
                int pos = f * hop;
                var frame = new Complex[frameSize];
                for (int i = 0; i < frameSize; i++)
                {
                    double s = 0;
                    if (pos + i < chSamples.Length) s = chSamples[pos + i];
                    frame[i] = new Complex(s * window[i], 0);
                }

                FFT(frame);

                double binFreq = sampleRate / (double)frameSize;

                // Attenuate selected bands
                var bands = new (int low, int high, double strength)[] {
                    (19000, 20000, 0.5),
                    (17500, 18500, 0.4),
                    (15000, 16000, 0.35),
                    (12000, 12500, 0.25),
                    (8000, 8500, 0.2),
                    (50, 200, 0.15)
                };
                for (int b = 0; b < bands.Length; b++)
                {
                    int lowBin = Math.Max(0, (int)Math.Floor(bands[b].low / binFreq));
                    int highBin = Math.Min(frameSize/2 - 1, (int)Math.Ceiling(bands[b].high / binFreq));
                    for (int k = lowBin; k <= highBin; k++)
                    {
                        frame[k] *= (1.0 - bands[b].strength);
                        int sym = frameSize - k - 1;
                        if (sym >= 0 && sym < frameSize) frame[sym] *= (1.0 - bands[b].strength);
                    }
                }

                // Gentle high-frequency rolloff above 16kHz
                for (int k = 0; k < frameSize/2; k++)
                {
                    double freq = k * binFreq;
                    if (freq >= 16000)
                    {
                        double att = Math.Clamp((freq - 16000) / 4000.0, 0.0, 1.0);
                        double factor = 1.0 - 0.35 * att;
                        frame[k] *= factor;
                        int sym = frameSize - k - 1;
                        if (sym >= 0 && sym < frameSize) frame[sym] *= factor;
                    }
                }

                // small phase randomization on high bins
                var rnd = new Random(f + ch);
                int hfStart = Math.Max(0, (int)(15000 / binFreq));
                for (int k = hfStart; k < frameSize/2; k++)
                {
                    double phase = rnd.NextDouble() * 0.04 - 0.02;
                    var c = frame[k];
                    double mag = c.Magnitude;
                    double ang = Math.Atan2(c.Imaginary, c.Real) + phase;
                    frame[k] = Complex.FromPolarCoordinates(mag, ang);
                    int sym = frameSize - k - 1;
                    if (sym >= 0 && sym < frameSize)
                    {
                        var cs = frame[sym];
                        double magSym = cs.Magnitude;
                        double angs = Math.Atan2(cs.Imaginary, cs.Real) - phase;
                        frame[sym] = Complex.FromPolarCoordinates(magSym, angs);
                    }
                }

                IFFT(frame);
                for (int i = 0; i < frameSize; i++)
                {
                    int idx = pos + i;
                    if (idx < output.Length) output[idx] += frame[i].Real; // analysis already windowed
                }
            }

            processedPerChannel[ch] = output;
        }

        // Normalize across all channels
        double max = 0.0;
        for (int ch = 0; ch < channels; ch++)
        {
            double localMax = processedPerChannel[ch].Take(framesPerChannel).Max(v => Math.Abs(v));
            if (localMax > max) max = localMax;
        }
        if (max < 1e-9) max = 1.0;
        double norm = 0.98 / max;

        // Write interleaved WAV with original channels and bits
        using var writer = new WaveFileWriter(outputWavPath, new WaveFormat(sampleRate, bitsPerSample, channels));
        for (int i = 0; i < framesPerChannel; i++)
        {
            for (int ch = 0; ch < channels; ch++)
            {
                double v = processedPerChannel[ch][i] * norm;
                if (v > 1.0) v = 1.0;
                if (v < -1.0) v = -1.0;
                writer.WriteSample((float)v);
            }
        }
        writer.Flush();
    }

    /// <summary>
    /// Продвинутая обёртка удаления отпечатков: выполняет спектральную коррекцию и пытается кодировать MP3
    /// (через LAME). Возвращает словарь с информацией о результирующем файле для UI.
    /// </summary>
    private Dictionary<string,string> RemoveFingerprintsAdvanced(string inputMp3Path, string outputMp3Path)
    {
        // We'll preserve original MP3 parameters (channels/sampleRate/bitrate) where possible.
        var origParams = GetMp3Parameters(inputMp3Path);
        int sampleRate = origParams.sampleRate;
        int channels = origParams.channels;
        int bitsPerSample = origParams.bitsPerSample;
        int origBitrateKbps = origParams.bitrateKbps;

        // Temporary WAV output
        string tempWav = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(inputMp3Path) + "_temp_clean.wav");
        // Run conservative removal first (overwrites temp WAV)
        RemoveFingerprints(inputMp3Path, tempWav);

        // Load temp WAV into float[] for spectral processing and deinterleave per channel
        using var wavReader = new AudioFileReader(tempWav);
        sampleRate = wavReader.WaveFormat.SampleRate; // trust WAV produced sampleRate
        channels = wavReader.WaveFormat.Channels;
        var samplesList = new List<float>();
        var buf = new float[4096];
        int read;
        while ((read = wavReader.Read(buf, 0, buf.Length)) > 0)
        {
            for (int i = 0; i < read; i++) samplesList.Add(buf[i]);
        }
        if (samplesList.Count == 0) throw new InvalidOperationException("No audio samples after initial pass");

        int framesPerChannel = samplesList.Count / channels;
        var channelSamples = new float[channels][];
        for (int ch = 0; ch < channels; ch++) channelSamples[ch] = new float[framesPerChannel];
        for (int i = 0; i < framesPerChannel; i++)
        {
            for (int ch = 0; ch < channels; ch++) channelSamples[ch][i] = samplesList[i * channels + ch];
        }

        int frameSize = 4096;
        int hop = frameSize / 2;
        var window = HammingWindow(frameSize);

        // Process each channel independently (same procedure as mono path previously)
        var processedPerChannel = new double[channels][];
        for (int ch = 0; ch < channels; ch++)
        {
            var chSamples = channelSamples[ch];
            var output = new double[chSamples.Length + frameSize];
            for (int pos = 0; pos + frameSize <= chSamples.Length; pos += hop)
            {
                var frame = new Complex[frameSize];
                for (int i = 0; i < frameSize; i++) frame[i] = new Complex(chSamples[pos + i] * window[i], 0);
                FFT(frame);
                var mags = frame.Take(frameSize / 2).Select(c => c.Magnitude + 1e-12).ToArray();

                double[] freqs = Enumerable.Range(0, frameSize / 2).Select(k => k * sampleRate / (double)frameSize).ToArray();
                int hfStart = Array.FindIndex(freqs, f => f >= 15000);
                if (hfStart < 0) hfStart = frameSize/2 - 1;
                double est = mags.Skip(hfStart).Average();
                double alpha = 0.8;
                for (int k = hfStart; k < frameSize / 2; k++)
                {
                    double mag = Math.Max(0.0, mags[k] - alpha * est);
                    double phase = Math.Atan2(frame[k].Imaginary, frame[k].Real);
                    frame[k] = Complex.FromPolarCoordinates(mag, phase);
                    int sym = frameSize - k - 1;
                    if (sym >= 0 && sym < frameSize) frame[sym] = Complex.FromPolarCoordinates(mag, -phase);
                }

                for (int k = 0; k < frameSize / 2; k++)
                {
                    double f = k * sampleRate / (double)frameSize;
                    if (f >= 8000 && f <= 12500)
                    {
                        double factor = 0.6;
                        frame[k] *= factor;
                        int sym = frameSize - k -1;
                        if (sym>=0 && sym<frameSize) frame[sym] *= factor;
                    }
                }

                var rnd = new Random(pos + ch);
                for (int k = hfStart; k < frameSize/2; k++)
                {
                    double phase = rnd.NextDouble() * 0.06 - 0.03;
                    var c = frame[k];
                    double mag = c.Magnitude;
                    double ang = Math.Atan2(c.Imaginary, c.Real) + phase;
                    frame[k] = Complex.FromPolarCoordinates(mag, ang);
                    int sym = frameSize - k - 1;
                    if (sym >= 0 && sym < frameSize)
                    {
                        var cs = frame[sym];
                        double magSym = cs.Magnitude;
                        double angs = Math.Atan2(cs.Imaginary, cs.Real) - phase;
                        frame[sym] = Complex.FromPolarCoordinates(magSym, angs);
                    }
                }

                IFFT(frame);
                for (int i = 0; i < frameSize; i++)
                {
                    int idx = pos + i;
                    if (idx < output.Length) output[idx] += frame[i].Real * window[i];
                }
            }
            processedPerChannel[ch] = output;
        }

        // Normalize across channels
        double max = 0.0;
        for (int ch = 0; ch < channels; ch++)
        {
            double localMax = processedPerChannel[ch].Take(framesPerChannel).Max(v => Math.Abs(v));
            if (localMax > max) max = localMax;
        }
        if (max < 1e-9) max = 1.0;
        double norm = 0.98 / max;

        string finalWav = Path.ChangeExtension(tempWav, ".final.wav");
        using (var writer = new WaveFileWriter(finalWav, new WaveFormat(sampleRate, bitsPerSample, channels)))
        {
            for (int i = 0; i < framesPerChannel; i++)
            {
                for (int ch = 0; ch < channels; ch++)
                {
                    double v = processedPerChannel[ch][i] * norm;
                    if (v > 1.0) v = 1.0;
                    if (v < -1.0) v = -1.0;
                    writer.WriteSample((float)v);
                }
            }
            writer.Flush();
        }

        // Try to encode to MP3 if LAME available, preserving original bitrate where possible
        try
        {
            using var reader = new AudioFileReader(finalWav);
            // Use FileStream constructor to pass explicit bitrate if available
            using var outFs = File.Create(outputMp3Path);
            int targetKbps = origBitrateKbps > 0 ? origBitrateKbps : 192;
            using var mp3Writer = new NAudio.Lame.LameMP3FileWriter(outFs, reader.WaveFormat, targetKbps);
            reader.CopyTo(mp3Writer);
            mp3Writer.Flush();
        }
        catch (Exception ex)
        {
            // If MP3 encoding fails, fall back to WAV copy
            File.Copy(finalWav, Path.ChangeExtension(outputMp3Path, ".wav"), true);
            throw new InvalidOperationException("MP3 encoding failed: " + ex.Message);
        }
        finally
        {
            try { File.Delete(tempWav); } catch { }
            try { File.Delete(Path.ChangeExtension(tempWav, ".final.wav")); } catch { }
        }

        // Build final info map including duration and standardized bitrate key
        var finInfo = new Dictionary<string,string>();
        try
        {
            var fi = new FileInfo(outputMp3Path);
            finInfo["Файл"] = fi.Name;
            finInfo["Размер"] = $"{fi.Length / 1024.0:F2} KB";
            finInfo["Путь"] = fi.FullName;
            finInfo["Тип"] = Path.GetExtension(fi.FullName).ToUpperInvariant().TrimStart('.') == "MP3" ? "MP3 аудиофайл" : "Аудиофайл";

            // Try to read exact params from produced MP3
            try
            {
                using var mp3r = new Mp3FileReader(outputMp3Path);
                var wf = mp3r.WaveFormat;
                finInfo["Длительность"] = mp3r.TotalTime.ToString("c");
                finInfo["Оценочный битрейт"] = GetMp3Parameters(outputMp3Path).bitrateKbps > 0 ? GetMp3Parameters(outputMp3Path).bitrateKbps.ToString() : (origBitrateKbps > 0 ? origBitrateKbps.ToString() : "?");
                finInfo["Частота дискретизации"] = wf.SampleRate.ToString();
                finInfo["Каналы"] = wf.Channels.ToString();
                finInfo["Бит на сэмпл"] = wf.BitsPerSample > 0 ? wf.BitsPerSample.ToString() : bitsPerSample.ToString();
            }
            catch
            {
                finInfo["Длительность"] = "?";
                finInfo["Оценочный битрейт"] = origBitrateKbps > 0 ? origBitrateKbps.ToString() : "?";
                finInfo["Частота дискретизации"] = sampleRate.ToString();
                finInfo["Каналы"] = channels.ToString();
                finInfo["Бит на сэмпл"] = bitsPerSample.ToString();
            }
        }
        catch { }
        return finInfo;
    }
}
