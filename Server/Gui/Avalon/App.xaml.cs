using Avalonia;

namespace FileFlows.Server.Gui.Avalon;

internal class App : Avalonia.Application
{
    public override void Initialize()
    {
        base.Initialize();
        
        var window = new MainWindow();
        if(AppSettings.Instance.StartMinimized == false)
            window.Show();
    }
    

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(bool messagebox = false)
        => (messagebox ? AppBuilder.Configure<MessageApp>() : AppBuilder.Configure<App>())
            .UsePlatformDetect();
}
