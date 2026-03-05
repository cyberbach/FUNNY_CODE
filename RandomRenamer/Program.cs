using System.Windows;

namespace MP3RandomRenamer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        new Application().Run(new MainWindow());
    }
}
