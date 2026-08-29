using System.Windows;
using ChatGPTShell.Web;

namespace ChatGPTShell;

public partial class App : Application
{
    public WebViewEnvironmentService WebViewEnvironment { get; } = new();
}
