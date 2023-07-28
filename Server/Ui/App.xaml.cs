using Avalonia;

namespace FileFlows.Server.Ui;

internal class App : Application
{
    public override void Initialize()
    {
        base.Initialize();
        
        var window = new MainWindow();
        if(AppSettings.Instance.StartMinimized == false)
            window.Show();
    }
}
