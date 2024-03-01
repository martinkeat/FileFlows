using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace FileFlows.Node.Ui;

internal class App : Application
{
    public override void Initialize()
    {
        base.Initialize();
        
        var window = new MainWindow();
        if(AppSettings.Instance.StartMinimized == false)
            window.Show();
    }
    
    
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Set the property to run as a background application
            desktop.MainWindow.ShowInTaskbar = false;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
