using System.Windows;
using System.Windows.Controls;
using ChatGPTShell.Web;

namespace ChatGPTShell.Panels;

public partial class ChatPanel : UserControl, IDisposable
{
    private ChatPanelDefinition? _definition;
    private WebViewEnvironmentService? _environmentService;
    private bool _initialized;
    private bool _disposed;

    public ChatPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public Guid PanelId => _definition?.Id ?? Guid.Empty;

    public event EventHandler? DefinitionChanged;

    public event EventHandler? AddRequested;

    public event EventHandler? FocusRequested;

    public event EventHandler? CloseRequested;

    public void Configure(
        ChatPanelDefinition definition,
        WebViewEnvironmentService environmentService)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ChatPanel));
        }

        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
        TitleText.Text = definition.Title;

        if (IsLoaded)
        {
            _ = InitializeAsync();
        }
    }

    public void SetCloseEnabled(bool enabled)
    {
        CloseButton.IsEnabled = enabled;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Loaded -= OnLoaded;

        DefinitionChanged = null;
        AddRequested = null;
        FocusRequested = null;
        CloseRequested = null;

        WebView.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_disposed || _initialized || _definition is null || _environmentService is null)
        {
            return;
        }

        _initialized = true;

        try
        {
            var environment = await _environmentService.GetAsync();

            if (_disposed)
            {
                return;
            }

            await WebView.EnsureCoreWebView2Async(environment);

            if (_disposed)
            {
                return;
            }

            WebView.CoreWebView2.NavigationCompleted += (_, _) => CaptureConversationUrl();
            WebView.CoreWebView2.HistoryChanged += (_, _) => CaptureConversationUrl();
            WebView.CoreWebView2.Navigate(_definition.ConversationUrl);
        }
        catch (Exception) when (_disposed)
        {
            // Disposing a panel while WebView2 is initializing is an expected lifecycle path.
        }
        catch (Exception exception)
        {
            _initialized = false;
            TitleText.Text = $"{_definition.Title} — failed to start";
            MessageBox.Show(
                exception.Message,
                "ChatGPT Shell",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CaptureConversationUrl()
    {
        if (_disposed || _definition is null || WebView.CoreWebView2 is null)
        {
            return;
        }

        if (!Uri.TryCreate(WebView.CoreWebView2.Source, UriKind.Absolute, out var uri)
            || !IsChatGptHost(uri.Host))
        {
            return;
        }

        var currentUrl = uri.AbsoluteUri;

        if (string.Equals(_definition.ConversationUrl, currentUrl, StringComparison.Ordinal))
        {
            return;
        }

        _definition.ConversationUrl = currentUrl;
        DefinitionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        AddRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnFocusClick(object sender, RoutedEventArgs e)
    {
        FocusRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsChatGptHost(string host) =>
        host.Equals("chatgpt.com", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".chatgpt.com", StringComparison.OrdinalIgnoreCase);
}
