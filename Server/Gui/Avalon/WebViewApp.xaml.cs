using Avalonia;

namespace FileFlows.Server.Gui.Avalon;

/// <summary>
/// App for the avalonia web view
/// </summary>
internal class WebViewApp : Application
{
    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();
        
        var window = new WebViewWindow();
        if(AppSettings.Instance.StartMinimized == false)
            window.Show();
    }
}
