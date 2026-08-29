using System.Windows;
using ChatGPTShell.Persistence;
using ChatGPTShell.Roles;
using ChatGPTShell.Web;

namespace ChatGPTShell;

public partial class App : Application
{
    public WebViewEnvironmentService WebViewEnvironment { get; } = new();

    public WorkspacePersistenceService WorkspacePersistence { get; } = new();

    public RoleLibraryService RoleLibrary { get; } = new();
}
