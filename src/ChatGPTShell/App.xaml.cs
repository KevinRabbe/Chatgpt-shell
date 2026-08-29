using System.Windows;
using ChatGPTShell.Persistence;
using ChatGPTShell.Web;

namespace ChatGPTShell;

public partial class App : Application
{
    public WebViewEnvironmentService WebViewEnvironment { get; } = new();

    public WorkspacePersistenceService WorkspacePersistence { get; } = new();
}
