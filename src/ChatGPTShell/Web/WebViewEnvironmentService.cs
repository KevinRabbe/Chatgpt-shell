using System.IO;
using Microsoft.Web.WebView2.Core;

namespace ChatGPTShell.Web;

public sealed class WebViewEnvironmentService
{
    private readonly Lazy<Task<CoreWebView2Environment>> _environment;

    public WebViewEnvironmentService()
    {
        _environment = new Lazy<Task<CoreWebView2Environment>>(CreateEnvironmentAsync);
    }

    public Task<CoreWebView2Environment> GetAsync() => _environment.Value;

    private static async Task<CoreWebView2Environment> CreateEnvironmentAsync()
    {
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTShell",
            "WebView2");

        Directory.CreateDirectory(userDataFolder);

        return await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder,
            options: null);
    }
}
