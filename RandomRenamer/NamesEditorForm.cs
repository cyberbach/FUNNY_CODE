using System.Runtime.InteropServices;

namespace MP3RandomRenamer;

/// <summary>
/// Simple input dialog for text entry.
/// </summary>
internal class SimpleInputDialog : Form
{
    private readonly TextBox _textBox;
    public string InputText => _textBox.Text;

    private static readonly Color BackgroundColor = Color.FromArgb(15, 23, 42);
    private static readonly Color SurfaceColor = Color.FromArgb(30, 41, 59);
    private static readonly Color PrimaryColor = Color.FromArgb(99, 102, 241);
    private static readonly Color TextPrimaryColor = Color.FromArgb(248, 250, 252);
    private static readonly Color TextSecondaryColor = Color.FromArgb(148, 163, 184);

    public SimpleInputDialog(string title, string prompt, string defaultValue = "")
    {
        _textBox = new TextBox();
        InitializeComponent(title, prompt, defaultValue);
    }

    private void InitializeComponent(string title, string prompt, string defaultValue)
    {
        Text = title;
        Size = new Size(450, 180);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);
        BackColor = BackgroundColor;
        ForeColor = TextPrimaryColor;

        const int cornerRadius = 16;
        UpdateRegion(cornerRadius);
        Resize += (s, e) => UpdateRegion(cornerRadius);

        // Header
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = SurfaceColor
        };
        Controls.Add(headerPanel);

        var lblTitle = new Label
        {
            Text = title,
            AutoSize = true,
            Location = new Point(20, 15),
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = TextPrimaryColor,
            BackColor = Color.Transparent
        };
        headerPanel.Controls.Add(lblTitle);

        // Prompt
        var lblPrompt = new Label
        {
            Text = prompt,
            AutoSize = true,
            Location = new Point(20, 65),
            ForeColor = TextSecondaryColor,
            BackColor = Color.Transparent
        };
        Controls.Add(lblPrompt);

        // TextBox
        _textBox.Location = new Point(20, 90);
        _textBox.Size = new Size(410, 35);
        _textBox.Text = defaultValue;
        _textBox.BackColor = SurfaceColor;
        _textBox.ForeColor = TextPrimaryColor;
        _textBox.BorderStyle = BorderStyle.None;
        _textBox.Font = new Font("Segoe UI", 10F);
        _textBox.Padding = new Padding(10, 8, 10, 8);
        _textBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };
        Controls.Add(_textBox);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Location = new Point(20, 135),
            Size = new Size(410, 35),
            FlowDirection = FlowDirection.RightToLeft
        };

        var btnCancel = new Button
        {
            Text = "Отмена",
            Width = 100,
            Height = 35,
            BackColor = SurfaceColor,
            ForeColor = TextSecondaryColor,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        buttonPanel.Controls.Add(btnCancel);

        var btnOk = new Button
        {
            Text = "OK",
            Width = 100,
            Height = 35,
            BackColor = PrimaryColor,
            ForeColor = TextPrimaryColor,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.OK
        };
        btnOk.FlatAppearance.BorderSize = 0;
        buttonPanel.Controls.Add(btnOk);

        Controls.Add(buttonPanel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void UpdateRegion(int radius)
    {
        var rect = ClientRectangle;
        Region = Region.FromHrgn(CreateRoundRectRgn(rect.Left, rect.Top, rect.Right, rect.Bottom, radius, radius));
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
}

/// <summary>
/// Dialog form for editing the list of random names.
/// </summary>
internal partial class NamesEditorForm : Form
{
    private DataGridView _dataGridView = null!;
    private Button _btnAdd = null!;
    private Button _btnRemove = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private List<string> _names;

    private static readonly Color BackgroundColor = Color.FromArgb(15, 23, 42);
    private static readonly Color SurfaceColor = Color.FromArgb(30, 41, 59);
    private static readonly Color SurfaceHoverColor = Color.FromArgb(51, 65, 85);
    private static readonly Color PrimaryColor = Color.FromArgb(99, 102, 241);
    private static readonly Color SecondaryColor = Color.FromArgb(168, 85, 247);
    private static readonly Color TextPrimaryColor = Color.FromArgb(248, 250, 252);
    private static readonly Color TextSecondaryColor = Color.FromArgb(148, 163, 184);

    public NamesEditorForm(List<string> initialNames)
    {
        InitializeComponent();
        _names = new List<string>(initialNames);
        LoadData();
    }

    private void InitializeComponent()
    {
        // NamesEditorForm
        Text = "Редактирование случайных слов";
        Size = new Size(650, 550);
        MinimumSize = new Size(550, 450);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);
        BackColor = BackgroundColor;
        ForeColor = TextPrimaryColor;

        const int cornerRadius = 16;
        UpdateRegion(cornerRadius);
        Resize += (s, e) => UpdateRegion(cornerRadius);

        // headerPanel
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = SurfaceColor
        };
        Controls.Add(headerPanel);

        // lblTitle
        var lblTitle = new Label
        {
            Text = "✏️ Редактирование случайных слов",
            AutoSize = true,
            Location = new Point(20, 15),
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = TextPrimaryColor,
            BackColor = Color.Transparent
        };
        headerPanel.Controls.Add(lblTitle);

        // _dataGridView
        _dataGridView = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            BackgroundColor = SurfaceColor,
            ForeColor = TextPrimaryColor,
            GridColor = SurfaceHoverColor,
            BorderStyle = BorderStyle.None,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 40,
            ColumnHeadersDefaultCellStyle =
            {
                BackColor = SurfaceColor,
                ForeColor = TextSecondaryColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            },
            DefaultCellStyle =
            {
                BackColor = SurfaceColor,
                ForeColor = TextPrimaryColor,
                Font = new Font("Segoe UI", 9F),
                SelectionBackColor = PrimaryColor,
                SelectionForeColor = TextPrimaryColor
            }
        };
        Controls.Add(_dataGridView);

        // column
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = "Слово",
            DataPropertyName = "Name",
            FillWeight = 100
        };
        _dataGridView.Columns.Add(column);

        // buttonPanel
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 55,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(20),
            BackColor = SurfaceColor
        };

        // _btnCancel
        _btnCancel = new Button
        {
            Text = "Отмена",
            Size = new Size(100, 38),
            BackColor = SurfaceHoverColor,
            ForeColor = TextSecondaryColor,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        _btnCancel.FlatAppearance.BorderSize = 0;
        buttonPanel.Controls.Add(_btnCancel);

        // _btnSave
        _btnSave = new Button
        {
            Text = "💾 Сохранить",
            Size = new Size(100, 38),
            BackColor = PrimaryColor,
            ForeColor = TextPrimaryColor,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        buttonPanel.Controls.Add(_btnSave);

        // _btnRemove
        _btnRemove = new Button
        {
            Text = "🗑️ Удалить",
            Size = new Size(100, 38),
            BackColor = SecondaryColor,
            ForeColor = TextPrimaryColor,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnRemove.FlatAppearance.BorderSize = 0;
        _btnRemove.Click += BtnRemove_Click;
        buttonPanel.Controls.Add(_btnRemove);

        // _btnAdd
        _btnAdd = new Button
        {
            Text = "➕ Добавить",
            Size = new Size(100, 38),
            BackColor = PrimaryColor,
            ForeColor = TextPrimaryColor,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnAdd.FlatAppearance.BorderSize = 0;
        _btnAdd.Click += BtnAdd_Click;
        buttonPanel.Controls.Add(_btnAdd);

        Controls.Add(buttonPanel);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;
    }

    private void LoadData()
    {
        _dataGridView.Rows.Clear();
        foreach (var name in _names)
            _dataGridView.Rows.Add(name);
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var dialog = new SimpleInputDialog("Добавление слова", "Введите новое слово:");
        if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.InputText))
        {
            var name = dialog.InputText.Trim();
            if (!_names.Contains(name))
            {
                _names.Add(name);
                _dataGridView.Rows.Add(name);
            }
            else
            {
                MessageBox.Show("Такое слово уже существует в списке.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void BtnRemove_Click(object? sender, EventArgs e)
    {
        if (_dataGridView.CurrentRow != null)
        {
            var index = _dataGridView.CurrentRow.Index;
            _names.RemoveAt(index);
            _dataGridView.Rows.RemoveAt(index);
        }
        else
        {
            MessageBox.Show("Выберите слово для удаления.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public List<string> GetNames()
    {
        if (_dataGridView.IsCurrentCellDirty)
            _dataGridView.EndEdit();

        var result = new List<string>();
        foreach (DataGridViewRow row in _dataGridView.Rows)
        {
            if (row.Cells[0].Value is string value && !string.IsNullOrWhiteSpace(value))
                result.Add(value.Trim());
        }
        return result;
    }

    private void UpdateRegion(int radius)
    {
        var rect = ClientRectangle;
        Region = Region.FromHrgn(CreateRoundRectRgn(rect.Left, rect.Top, rect.Right, rect.Bottom, radius, radius));
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
}
