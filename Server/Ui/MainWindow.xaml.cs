namespace FileFlows.Server.Ui;

using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Controls.ApplicationLifetimes;

/// <summary>
/// Main window for Server application
/// </summary>
public class MainWindow : Window
{
    private readonly TrayIcon _trayIcon;
    NativeMenu menu = new();

    public MainWindow()
    {
        _trayIcon = new TrayIcon();
        InitializeComponent();
        
        var dc = new MainWindowViewModel(this)
        {
            CustomTitle = Globals.IsWindows
        };

        ExtendClientAreaChromeHints =
            dc.CustomTitle ? ExtendClientAreaChromeHints.NoChrome : ExtendClientAreaChromeHints.Default;
        ExtendClientAreaToDecorationsHint = dc.CustomTitle;
        this.MaxHeight = dc.CustomTitle ? 260 : 230;
        this.Height = dc.CustomTitle ? 260 : 230;

        DataContext = dc;
        
        _trayIcon.IsVisible = true;

        _trayIcon.Icon = new WindowIcon(AvaloniaLocator.Current.GetService<IAssetLoader>()?.Open(new Uri($"avares://FileFlows.Server/Ui/icon.ico")));

        //this.Events().Closing.Subscribe(_ =>
        //{
        //    _trayIcon.IsVisible = false;
        //    _trayIcon.Dispose();
        //});

        AddMenuItem("Open", () => this.Launch());
        AddMenuItem("Quit", () => this.Quit());

        _trayIcon.Menu = menu;
        _trayIcon.Clicked += _trayIcon_Clicked;

        PointerPressed += MainWindow_PointerPressed;
    }

    private void _trayIcon_Clicked(object? sender, EventArgs e)
    {
        this.Show();
    }

    private void MainWindow_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }


    private bool ConfirmedQuit = false;
    protected override void OnClosing(CancelEventArgs e)
    {
        if (ConfirmedQuit == false)
        {
            e.Cancel = true;
            var task = new Confirm("Are you sure you want to quit?", "Quit").ShowDialog<bool>(this);
            Task.Run(async () =>
            {
                await Task.Delay(1);
                ConfirmedQuit = task.Result;
                
                if (ConfirmedQuit)
                {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                    {
                        lifetime.Shutdown();
                    }
                }
            });
        }
        else
        {
            this._trayIcon.Menu = null;
            this._trayIcon.IsVisible = false;
        
            base.OnClosing(e);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AddMenuItem(string label, Action action)
    {
        NativeMenuItem item = new();
        item.Header = label;
        //item.Icon = AvaloniaLocator.Current.GetService<IAssetLoader>()?.Open(new Uri($"avares://FileFlows.Server/Ui/icon.ico"));
        item.Click += (s, e) =>
        {
            action();
        };
        menu.Add(item);
    }


    /// <summary>
    /// Launches the server URL in a browser
    /// </summary>
    public void Launch()
    {
        string url = $"http://{Environment.MachineName}:{WebServer.Port}/";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
        else
            Process.Start(new ProcessStartInfo("xdg-open", url));
    }

    protected override void HandleWindowStateChanged(WindowState state)
    {
        base.HandleWindowStateChanged(state);
        if(Globals.IsWindows && state == WindowState.Minimized)
            this.Hide();
    }

    /// <summary>
    /// Quit the application
    /// </summary>
    public void Quit()
    {
        this.WindowState = WindowState.Normal;
        this.Show();
        this.Close();
    }

    public void Minimize()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            this.Hide();
        else   
            this.WindowState = WindowState.Minimized;
    }
}

public class MainWindowViewModel
{ 
    /// <summary>
    /// Gets or sets if a custom title should be rendered
    /// </summary>
    public bool CustomTitle { get; set; }
    private MainWindow Window { get; set; }
    public string ServerUrl { get; set; }
    public string Version { get; set; }

    public void Launch() => Window.Launch();
    public void Quit() => Window.Quit();

    public void Hide() => Window.Minimize();

    public MainWindowViewModel(MainWindow window)
    {
        this.Window = window;
        this.ServerUrl = $"http://{Environment.MachineName.ToLower()}:{WebServer.Port}/";
        this.Version = "FileFlows Version: " + Globals.Version;
    }
}