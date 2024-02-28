using Avalonia;

namespace FileFlows.Server.Gui.Avalon;

/// <summary>
/// Webview in avalonia
/// </summary>
public class AvalonWebView
{

    /// <summary>
    /// Opens a web view at the given URL
    /// </summary>
    /// <param name="url">the url to the FileFlows UI</param>
    public static void Open(string url)
    {
        var appBuilder = AppBuilder.Configure<WebViewApp>().UsePlatformDetect();
        appBuilder.StartWithClassicDesktopLifetime(new string[] { });
    }
    
}