using System.Configuration;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace TGHistoryRipper;

public partial class App : Application
{
    static App()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
