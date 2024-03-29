using Avalonia;

namespace FileFlows.Node.Ui;

/// <summary>
/// The Node application
/// </summary>
internal class App : Application
{
    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();
        
        var window = new MainWindow();
        if(AppSettings.Instance.StartMinimized == false || OperatingSystem.IsMacOS())
            window.Show();
    }
}
