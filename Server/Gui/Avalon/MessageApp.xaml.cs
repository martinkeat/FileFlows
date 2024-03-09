using Avalonia;

namespace FileFlows.Server.Gui.Avalon;

internal class MessageApp : Avalonia.Application
{
    public override void Initialize()
    {
        base.Initialize();
        
        var window = new MessageBox("FileFlows is already running.", "FileFlows");
        window.Show();
    }
}
