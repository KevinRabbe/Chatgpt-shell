using System.Windows;
using ChatGPTShell.Workspaces;

namespace ChatGPTShell;

public partial class MainWindow : Window
{
    private WorkspaceDefinition? _workspace;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        try
        {
            var app = (App)Application.Current;
            _workspace = await app.WorkspacePersistence.LoadOrCreateAsync();

            var panel = _workspace.Panels.FirstOrDefault(candidate => candidate.Id == _workspace.ActivePanelId)
                ?? _workspace.Panels[0];

            _workspace.ActivePanelId = panel.Id;
            ChatHost.DefinitionChanged += OnChatDefinitionChanged;
            ChatHost.Configure(panel, app.WebViewEnvironment);

            await app.WorkspacePersistence.SaveAsync(_workspace);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "ChatGPT Shell — workspace failed to load",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Close();
        }
    }

    private async void OnChatDefinitionChanged(object? sender, EventArgs e)
    {
        if (_workspace is null)
        {
            return;
        }

        try
        {
            var app = (App)Application.Current;
            await app.WorkspacePersistence.SaveAsync(_workspace);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "ChatGPT Shell — workspace failed to save",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
