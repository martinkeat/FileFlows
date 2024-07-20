using Avalonia.Controls.Chrome;
using Avalonia.Threading;
using FileFlows.Node.Utils;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;

namespace FileFlows.Node.Ui;

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
    readonly NativeMenu menu = new();
    internal static MainWindow? Instance;

    public MainWindow()
    {
        Instance = this;
        _trayIcon = new TrayIcon();
        InitializeComponent();

        var dc = new MainWindowViewModel(this)
        {
            CustomTitle = Globals.IsWindows
        };

        ExtendClientAreaChromeHints =
            dc.CustomTitle ? ExtendClientAreaChromeHints.NoChrome : ExtendClientAreaChromeHints.Default;
        ExtendClientAreaToDecorationsHint = dc.CustomTitle;
        this.MaxHeight = dc.CustomTitle ? 380 : 350;
        this.Height = dc.CustomTitle ? 380 : 350;

        DataContext = dc;
        _trayIcon.IsVisible = true;

        _trayIcon.Icon = new WindowIcon(AvaloniaLocator.Current.GetService<IAssetLoader>()?.Open(new Uri($"avares://FileFlows.Node/Ui/icon.ico")));

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

    protected override void HandleWindowStateChanged(WindowState state)
    {
        base.HandleWindowStateChanged(state);
        if(Globals.IsWindows && state == WindowState.Minimized)
            this.Hide();
    }

    private void _trayIcon_Clicked(object? sender, EventArgs e)
    {
        this.WindowState = WindowState.Normal;
        this.Show();
    }

    private void MainWindow_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        //this is only needed if we dont render the chrome title bar, this allows dragging from anywhere in the UI to move it
        //leave this code here in case we switch back to no chrome
        var pointer = e.GetCurrentPoint(this);
        //if (pointer.Pointer.Captured is Border)
        {
            BeginMoveDrag(e);
        }
    }

    internal void ForceQuit()
    {
        ConfirmedQuit = true;
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                this.Close();
            }
            catch (Exception)
            {
            }
        });
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
        string url = AppSettings.Instance.ServerUrl;
        if (string.IsNullOrWhiteSpace(url))
            return;
        
        if (Globals.IsWindows)
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
        else
            Process.Start(new ProcessStartInfo("xdg-open", url));
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
    
    /// <summary>
    /// Minimizes the application
    /// </summary>
    public void Minimize()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            this.Hide();
        else   
            this.WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// Saves and registers the node on the server
    /// </summary>
    public async Task SaveRegister()
    {
        if (Program.Manager == null)
            return;
        try
        {
            var result = await Program.Manager.Register();
            if (result.Success == false)
                ShowMessage("Register Failed", result.Message);
            else
                ShowMessage("Registered", "Successfully registered with the FileFlows server.");
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed registering: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }
    
    /// <summary>
    /// Shows a message box message
    /// </summary>
    /// <param name="title">the title of the message</param>
    /// <param name="message">the text of the message</param>
    void ShowMessage(string title, string message)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var window = new MessageBox(message, title);
            window.Show();
        });
    }

    /// <summary>
    /// Opens up the logging directory
    /// </summary>
    public void OpenLoggingDirectory()
    {
        Process.Start(new ProcessStartInfo() 
        {
            FileName = DirectoryHelper.LoggingDirectory,
            UseShellExecute = true,
            Verb = "open"
        });
    }
}

public class MainWindowViewModel:INotifyPropertyChanged
{ 
    /// <summary>
    /// Gets or sets if a custom title should be rendered
    /// </summary>
    public bool CustomTitle { get; set; }
    private MainWindow Window { get; set; }
    /// <summary>
    /// Gets ors sets the Version string
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Gets or sets if the window is enabled, it will be disabled during registration
    /// </summary>
    public bool Enabled { get; set; } = true;

    private string _ServerUrl = string.Empty;
    /// <summary>
    /// Gets or sets the URL of the FileFlows Server
    /// </summary>
    public string ServerUrl
    {
        get => _ServerUrl;
        set
        {
            if (_ServerUrl?.EmptyAsNull() != value?.EmptyAsNull())
            {
                _ServerUrl = value ?? string.Empty;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServerUrl)));
            }
        }
    }


    private string _AccessToken = string.Empty;
    /// <summary>
    /// Gets or sets the Access Token
    /// </summary>
    public string AccessToken
    {
        get => _AccessToken;
        set
        {
            if (_AccessToken?.EmptyAsNull() != value?.EmptyAsNull())
            {
                _AccessToken = value ?? string.Empty;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccessToken)));
            }
        }
    }
    /// <summary>
    /// Gets if start minimized should be shown 
    /// </summary>
    public bool ShowStartMinimized
    {
        get
        {
            if (OperatingSystem.IsMacOS())
                return false;
            return true;
        }
    }
    
    /// <summary>
    /// Gets or sets if the app should start minimized
    /// </summary>
    public bool StartMinimized
    {
        get => AppSettings.Instance.StartMinimized;
        set
        {
            if (AppSettings.Instance.StartMinimized != value)
            {
                AppSettings.Instance.StartMinimized = value;
                AppSettings.Instance.Save();
            }
        } 
    }
    
    /// <summary>
    /// Event that is fired when a property value is changed
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Launches the WebConsole
    /// </summary>
    public void Launch()
    {
        if(string.IsNullOrWhiteSpace(ServerUrl) || ServerUrl == "http://")
            return;
        
        if (Regex.IsMatch(ServerUrl, "^http(s)?://") == false)
            ServerUrl = "http://" + ServerUrl;
        if (ServerUrl.EndsWith("/") == false)
            ServerUrl += "/";

        AppSettings.Instance.ServerUrl = ServerUrl;

        Window.Launch();
    }

    
    /// <summary>
    /// Quits the application
    /// </summary>
    public void Quit() => Window.Quit();

    /// <summary>
    /// Hides the UI
    /// </summary>
    public void Hide() => Window.Minimize();

    /// <summary>
    /// Opens the logs 
    /// </summary>
    public void OpenLogs() => Window.OpenLoggingDirectory();

    /// <summary>
    /// Saves and registers the Node on the server
    /// </summary>
    public void SaveRegister()
    {
        if(string.IsNullOrWhiteSpace(ServerUrl) || ServerUrl == "http://")// || string.IsNullOrWhiteSpace(TempPath))
            return;
        
        if (Regex.IsMatch(ServerUrl, "^http(s)?://") == false)
            ServerUrl = "http://" + ServerUrl;
        if (ServerUrl.EndsWith("/") == false)
            ServerUrl += "/";
        
        AppSettings.Instance.AccessToken = AccessToken;
        AppSettings.Instance.ServerUrl = ServerUrl;

        Enabled = false;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
        Task.Run(async () =>
        {
            try
            {
                await Window.SaveRegister();
            }
            catch (Exception ex)
            {
                Logger.Instance.ELog("Error Registering: " + ex.Message + Environment.NewLine + ex.StackTrace);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Enabled = true;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
            });
        });
    }

    /// <summary>
    /// Model for the main window
    /// </summary>
    /// <param name="window">the main window instance</param>
    public MainWindowViewModel(MainWindow window)
    {
        this.Window = window;
        this.Version = "FileFlows Node Version: " + Globals.Version;
        
        ServerUrl = AppSettings.Instance.ServerUrl;
        AccessToken = AppSettings.Instance.AccessToken;
    }

    /// <summary>
    /// Opens up a browser to select the temporary path
    /// </summary>
    public async Task Browse()
    {
        OpenFolderDialog ofd = new OpenFolderDialog();
        var result = await ofd.ShowAsync(Window);
    }
}