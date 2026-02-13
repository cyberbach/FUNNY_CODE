namespace AI_Broom_MP3;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 410);
        Text = "AI Broom MP3 - Удаление AI-отпечатков";

        // Настройка формы для поддержки drag-and-drop
        AllowDrop = true;
        DragEnter += new DragEventHandler(MainForm_DragEnter);
        DragDrop += new DragEventHandler(MainForm_DragDrop);
        DragLeave += new EventHandler(MainForm_DragLeave);

        // Label для вывода текста
        System.Windows.Forms.Label textLabel = new System.Windows.Forms.Label();
        textLabel.AutoSize = true;
        textLabel.Location = new System.Drawing.Point(20, 20);
       textLabel.Location =  new Point( (this.ClientSize.Width - textLabel.Width) / 2, 20);
        textLabel.TextAlign = ContentAlignment.MiddleCenter; // Выравнивание текста по центру
        textLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; // Заякорить по горизонтали
        textLabel.Text = "AI Broom MP3 - Удаление AI-отпечатков из MP3 файлов";
        textLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
        textLabel.AllowDrop = true;
        textLabel.DragEnter += new DragEventHandler(MainForm_DragEnter);
        textLabel.DragDrop += new DragEventHandler(MainForm_DragDrop);
        textLabel.DragLeave += new EventHandler(MainForm_DragLeave);
        Controls.Add(textLabel);

        // Таблица для отображения краткой информации о файле (3 колонки: ключ, текущий, финальный)
        System.Windows.Forms.TableLayoutPanel fileInfoTable = new System.Windows.Forms.TableLayoutPanel();
        fileInfoTable.Name = "fileInfoTable";
        fileInfoTable.AutoSize = false;
        fileInfoTable.Size = new System.Drawing.Size(760, 230);
        fileInfoTable.Location = new System.Drawing.Point(20, 60);
        fileInfoTable.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;
        fileInfoTable.ColumnCount = 3;
        fileInfoTable.RowCount = 1;
        fileInfoTable.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
        fileInfoTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
        fileInfoTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
        fileInfoTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
        fileInfoTable.AllowDrop = true;
        fileInfoTable.DragEnter += new DragEventHandler(MainForm_DragEnter);
        fileInfoTable.DragDrop += new DragEventHandler(MainForm_DragDrop);
        fileInfoTable.DragLeave += new EventHandler(MainForm_DragLeave);
        // Placeholder cell
        var lblKey = new System.Windows.Forms.Label();
        lblKey.Text = "Информация";
        lblKey.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        lblKey.Dock = System.Windows.Forms.DockStyle.Fill;
        lblKey.AllowDrop = true;
        lblKey.DragEnter += new DragEventHandler(MainForm_DragEnter);
        lblKey.DragDrop += new DragEventHandler(MainForm_DragDrop);
        lblKey.DragLeave += new EventHandler(MainForm_DragLeave);
        var lblValue = new System.Windows.Forms.Label();
        lblValue.Text = "Перетащите MP3 файл сюда для анализа";
        lblValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        lblValue.Dock = System.Windows.Forms.DockStyle.Fill;
        lblValue.AllowDrop = true;
        lblValue.DragEnter += new DragEventHandler(MainForm_DragEnter);
        lblValue.DragDrop += new DragEventHandler(MainForm_DragDrop);
        lblValue.DragLeave += new EventHandler(MainForm_DragLeave);
        lblValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
        var lblFinal = new System.Windows.Forms.Label();
        lblFinal.Text = "Финальный файл";
        lblFinal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        lblFinal.Dock = System.Windows.Forms.DockStyle.Fill;
        lblFinal.AllowDrop = true;
        lblFinal.DragEnter += new DragEventHandler(MainForm_DragEnter);
        lblFinal.DragDrop += new DragEventHandler(MainForm_DragDrop);
        lblFinal.DragLeave += new EventHandler(MainForm_DragLeave);
        lblFinal.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
        fileInfoTable.Controls.Add(lblKey, 0, 0);
        fileInfoTable.Controls.Add(lblValue, 1, 0);
        fileInfoTable.Controls.Add(lblFinal, 2, 0);
        Controls.Add(fileInfoTable);

        // (removed bottom detailed area — showing details only in top table)

        // Кнопка Сброс
        System.Windows.Forms.Button resetButton = new System.Windows.Forms.Button();
        resetButton.Name = "resetButton";
        resetButton.Location = new System.Drawing.Point(20, 300);
        resetButton.Size = new System.Drawing.Size(200, 40);
        resetButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        resetButton.Text = "Сброс";
        resetButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
        resetButton.Click += new System.EventHandler(ResetButton_Click);
        resetButton.Visible = false;
        Controls.Add(resetButton);

        // Кнопка Анализ
        System.Windows.Forms.Button analyzeButton = new System.Windows.Forms.Button();
        analyzeButton.Name = "analyzeButton";
        analyzeButton.Location = new System.Drawing.Point(280, 300);
        analyzeButton.Size = new System.Drawing.Size(200, 40);
        analyzeButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        analyzeButton.Text = "Анализ";
        analyzeButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
        analyzeButton.Click += new System.EventHandler(AnalyzeButton_Click);
        analyzeButton.Visible = false;
        Controls.Add(analyzeButton);

        // Кнопка Удаление отпечатков
        System.Windows.Forms.Button removeFingerprintsButton = new System.Windows.Forms.Button();
        removeFingerprintsButton.Name = "removeFingerprintsButton";
        removeFingerprintsButton.Location = new System.Drawing.Point(540, 300);
        removeFingerprintsButton.Size = new System.Drawing.Size(200, 40);
        removeFingerprintsButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        removeFingerprintsButton.Text = "Удаление отпечатков";
        removeFingerprintsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
        removeFingerprintsButton.Click += new System.EventHandler(RemoveFingerprintsButton_Click);
        removeFingerprintsButton.Visible = false;
        Controls.Add(removeFingerprintsButton);

        // Кнопка для закрытия приложения
        System.Windows.Forms.Button closeButton = new System.Windows.Forms.Button();
        closeButton.Location = new System.Drawing.Point(20, 360);
        closeButton.Size = new System.Drawing.Size(200, 40);
        closeButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        closeButton.Text = "Закрыть";
        closeButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
        closeButton.Click += new System.EventHandler(CloseButton_Click);
        Controls.Add(closeButton);
    }

    #endregion
}
