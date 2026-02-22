using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using QRCoder;

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}

class MainForm : Form
{
    private TextBox _textBox;
    private Button _generateButton;
    private Button _saveButton;
    private PictureBox _pictureBox;
    private Bitmap? _qrBitmap;

    public MainForm()
    {
        Text = "GenQRLite";
        ClientSize = new Size(450, 500);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        _textBox = new TextBox
        {
            Location = new Point(20, 15),
            Size = new Size(390, 25),
            Font = new Font("Segoe UI", 10)
        };
        Controls.Add(_textBox);

        _generateButton = new Button
        {
            Text = "Сгенерировать QR",
            Location = new Point(20, 50),
            Size = new Size(150, 30),
            Font = new Font("Segoe UI", 9)
        };
        _generateButton.Click += GenerateButton_Click;
        Controls.Add(_generateButton);

        _saveButton = new Button
        {
            Text = "Сохранить как...",
            Location = new Point(260, 50),
            Size = new Size(150, 30),
            Font = new Font("Segoe UI", 9),
            Enabled = false
        };
        _saveButton.Click += SaveButton_Click;
        Controls.Add(_saveButton);

        _pictureBox = new PictureBox
        {
            Location = new Point(20, 90),
            Size = new Size(390, 390),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            SizeMode = PictureBoxSizeMode.Zoom
        };
        Controls.Add(_pictureBox);

        var label = new Label
        {
            Text = "QR-код появится здесь",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        _pictureBox.Controls.Add(label);
    }

    private void GenerateButton_Click(object? sender, EventArgs e)
    {
        string text = string.IsNullOrWhiteSpace(_textBox.Text) ? "Введите текст" : _textBox.Text;

        _qrBitmap?.Dispose();

        var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCoder.QRCode(qrData);
        _qrBitmap = qrCode.GetGraphic(10);

        _pictureBox.Image = _qrBitmap;
        _pictureBox.Controls.Clear();
        _saveButton.Enabled = true;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        if (_qrBitmap == null) return;

        using var saveDialog = new SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            DefaultExt = "png",
            FileName = "qrcode.png",
            Title = "Сохранить QR-код"
        };

        if (saveDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                string filePath = saveDialog.FileName;
                if (!filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    filePath += ".png";
                }
                _qrBitmap.Save(filePath, ImageFormat.Png);
                MessageBox.Show($"QR-код сохранён:\n{filePath}", "GenQRLite", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения:\n{ex.Message}", "GenQRLite", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _qrBitmap?.Dispose();
        base.OnFormClosing(e);
    }
}
