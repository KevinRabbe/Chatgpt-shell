using System.Windows;
using ChatGPTShell.Panels;

namespace ChatGPTShell;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var panel = new ChatPanelDefinition
        {
            Title = "ChatGPT",
            ConversationUrl = "https://chatgpt.com/"
        };

        var app = (App)Application.Current;
        ChatHost.Configure(panel, app.WebViewEnvironment);
    }
}
