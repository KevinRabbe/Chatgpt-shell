using System.Windows;
using System.Windows.Controls;
using ChatGPTShell.Web;

namespace ChatGPTShell.Panels;

public partial class ChatPanel : UserControl
{
    private ChatPanelDefinition? _definition;
    private WebViewEnvironmentService? _environmentService;
    private bool _initialized;

    public ChatPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public event EventHandler? DefinitionChanged;

    public void Configure(
        ChatPanelDefinition definition,
        WebViewEnvironmentService environmentService)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
        TitleText.Text = definition.Title;

        if (IsLoaded)
        {
            _ = InitializeAsync();
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_initialized || _definition is null || _environmentService is null)
        {
            return;
        }

        _initialized = true;

        try
        {
            var environment = await _environmentService.GetAsync();
            await WebView.EnsureCoreWebView2Async(environment);

            WebView.CoreWebView2.NavigationCompleted += (_, _) => CaptureConversationUrl();
            WebView.CoreWebView2.Navigate(_definition.ConversationUrl);
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
        if (_definition is null || WebView.CoreWebView2 is null)
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

    private static bool IsChatGptHost(string host) =>
        host.Equals("chatgpt.com", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".chatgpt.com", StringComparison.OrdinalIgnoreCase);
}
