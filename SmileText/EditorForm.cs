using System.Text;
using System.Runtime.InteropServices;

namespace SmileText;

public class EditorForm : Form
{
    private DataGridView dataGridView = null!;
    private Button addButton = null!;
    private Button deleteButton = null!;
    private Button saveButton = null!;

    // For drag by body
    private const int WM_NCHITTEST = 0x84;
    private const int HTCAPTION = 2;
    private const int HTCLIENT = 1;

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    public EditorForm()
    {
        Text = "Редактор смайлов";
        Width = 600;
        Height = 450;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;

        var label = new Label { Text = "Буква | Смайлы (через запятую)", Top = 10, Left = 20, Width = 300 };

        dataGridView = new DataGridView { Top = 35, Left = 20, Width = 540, Height = 300, AutoGenerateColumns = false };
        dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "Char", HeaderText = "Буква", Width = 80 });
        dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "Smiles", HeaderText = "Смайлы (через запятую)", Width = 440 });
        LoadSmiles();

        addButton = new Button { Text = "Добавить", Top = 350, Left = 20, Width = 100 };
        addButton.Click += AddClick;

        deleteButton = new Button { Text = "Удалить", Top = 350, Left = 130, Width = 100 };
        deleteButton.Click += DeleteClick;

        saveButton = new Button { Text = "Сохранить", Top = 350, Left = 460, Width = 100 };
        saveButton.Click += SaveClick;

        Controls.AddRange(new Control[] { label, dataGridView, addButton, deleteButton, saveButton });
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);
            if ((int)m.Result == HTCLIENT)
            {
                m.Result = (IntPtr)HTCAPTION;
            }
            return;
        }
        base.WndProc(ref m);
    }

    private void LoadSmiles()
    {
        dataGridView.Rows.Clear();
        foreach (var kvp in MainForm.SmileMap)
        {
            dataGridView.Rows.Add(kvp.Key.ToString(), kvp.Value);
        }
    }

    private void AddClick(object? sender, EventArgs e)
    {
        dataGridView.Rows.Add("", "");
    }

    private void DeleteClick(object? sender, EventArgs e)
    {
        if (dataGridView.SelectedRows.Count > 0)
        {
            dataGridView.Rows.RemoveAt(dataGridView.SelectedRows[0].Index);
        }
    }

    private void SaveClick(object? sender, EventArgs e)
    {
        var newMap = new Dictionary<char, string>();
        foreach (DataGridViewRow row in dataGridView.Rows)
        {
            if (row.Cells["Char"].Value?.ToString() is string charStr && charStr.Length == 1)
            {
                var c = charStr[0];
                var smiles = row.Cells["Smiles"].Value?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(smiles))
                {
                    newMap[c] = smiles;
                }
            }
        }
        MainForm.SmileMap = newMap;
        MessageBox.Show("Сохранено!");
        Close();
    }
}
