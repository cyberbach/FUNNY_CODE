using System.Windows;
using System.Windows.Input;

namespace MP3RandomRenamer;

public partial class NamesEditorWindow : Window
{
    private List<string> _names;

    public NamesEditorWindow(List<string> initialNames)
    {
        InitializeComponent();
        
        _names = new List<string>(initialNames);
        LoadData();
    }

    private void LoadData()
    {
        NamesListBox.Items.Clear();
        foreach (var name in _names)
            NamesListBox.Items.Add(name);
    }

    public List<string> GetNames()
    {
        var result = new List<string>();
        foreach (var item in NamesListBox.Items)
        {
            if (item is string value && !string.IsNullOrWhiteSpace(value))
                result.Add(value.Trim());
        }
        return result;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            DragMove();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SimpleInputDialog("Добавление слова", "Введите новое слово:");
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
        {
            var name = dialog.InputText.Trim();
            if (!_names.Contains(name))
            {
                _names.Add(name);
                NamesListBox.Items.Add(name);
            }
            else
            {
                MessageBox.Show("Такое слово уже существует в списке.", "Предупреждение", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (NamesListBox.SelectedIndex >= 0)
        {
            _names.RemoveAt(NamesListBox.SelectedIndex);
            NamesListBox.Items.RemoveAt(NamesListBox.SelectedIndex);
        }
        else
        {
            MessageBox.Show("Выберите слово для удаления.", "Предупреждение", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
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
