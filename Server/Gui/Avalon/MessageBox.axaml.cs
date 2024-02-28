using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace FileFlows.Server.Gui.Avalon;

public partial class MessageBox : Window
{
    public MessageBox()
    {
        InitializeComponent();
        PointerPressed += MessageBox_PointerPressed;
    }
    public MessageBox(string message, string title = "")
    {
        InitializeComponent();
        PointerPressed += MessageBox_PointerPressed;
        if (string.IsNullOrWhiteSpace(title) == false)
            this.Title = title;
        var dc = new MessageBoxViewModel(this, message)
        {
            CustomTitle = Globals.IsWindows
        };
        DataContext = dc;

        this.ExtendClientAreaChromeHints = 
            dc.CustomTitle ? ExtendClientAreaChromeHints.NoChrome : ExtendClientAreaChromeHints.Default;
        ExtendClientAreaToDecorationsHint = dc.CustomTitle;
        this.MaxHeight = dc.CustomTitle ? 150 : 120;
        this.Height = dc.CustomTitle ? 150 : 120;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    private void MessageBox_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
}


public class MessageBoxViewModel
{ 
    /// <summary>
    /// Gets or sets if a custom title should be rendered
    /// </summary>
    public bool CustomTitle { get; set; }
    private MessageBox Window { get; set; }
    public string Message { get; set; } = string.Empty;

    public void Ok() => Window.Close();

    public MessageBoxViewModel(MessageBox window, string message)
    {
        this.Window = window;
        this.Message = message;
    }

}