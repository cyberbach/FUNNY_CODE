using System.Windows;

namespace MP3RandomRenamer;

public partial class SimpleInputDialog : Window
{
    public string InputText => InputTextBox.Text;

    public SimpleInputDialog(string title, string prompt, string defaultValue = "")
    {
        InitializeComponent();
        
        TitleText.Text = title;
        Title = title;
        PromptText.Text = prompt;
        InputTextBox.Text = defaultValue;
        
        InputTextBox.Focus();
        InputTextBox.SelectAll();
        
        InputTextBox.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                DialogResult = true;
                Close();
            }
        };
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
