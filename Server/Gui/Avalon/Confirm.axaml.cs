using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace FileFlows.Server.Gui.Avalon;

public partial class Confirm : Window
{
    public Confirm()
    {
        InitializeComponent();
    }
    public Confirm(string message, string title = "")
    {
        InitializeComponent();
        if (string.IsNullOrWhiteSpace(title) == false)
            this.Title = title;
        
        var dc = new ConfirmViewModel(this, message)
        {
            CustomTitle = Globals.IsWindows
        };
        DataContext = dc;

        this.ExtendClientAreaChromeHints = 
            dc.CustomTitle ? ExtendClientAreaChromeHints.NoChrome : ExtendClientAreaChromeHints.Default;
        ExtendClientAreaToDecorationsHint = dc.CustomTitle;
        this.MaxHeight = dc.CustomTitle ? 160 : 130;
        this.Height = dc.CustomTitle ? 160 : 130;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}


public class ConfirmViewModel
{ 
    /// <summary>
    /// Gets or sets if a custom title should be rendered
    /// </summary>
    public bool CustomTitle { get; set; }
    private Confirm Window { get; set; }
    
    public string Message { get; set; } = string.Empty;

    public void Yes() => Window.Close(true);

    public void No() => Window.Close(false);

    public ConfirmViewModel(Confirm window, string message)
    {
        this.Window = window;
        this.Message = message;
    }

}