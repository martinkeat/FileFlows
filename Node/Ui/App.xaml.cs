using Avalonia;

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
}
