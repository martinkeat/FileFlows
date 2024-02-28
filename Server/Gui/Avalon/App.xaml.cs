using Avalonia;

namespace FileFlows.Server.Gui.Avalon;

internal class App : Application
{
    public override void Initialize()
    {
        base.Initialize();
        
        var window = new MainWindow();
        if(AppSettings.Instance.StartMinimized == false)
            window.Show();
    }
    
    /// <summary>
    /// Opens a web view at the given URL
    /// </summary>
    public static void Open()
    {
        var appBuilder = BuildAvaloniaApp();
        appBuilder.StartWithClassicDesktopLifetime(new string[] { });
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(bool messagebox = false)
        => (messagebox ? AppBuilder.Configure<MessageApp>() : AppBuilder.Configure<App>())
            .UsePlatformDetect();
}
