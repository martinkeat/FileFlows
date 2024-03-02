using System.Runtime.InteropServices;

namespace FileFlows.Server.Utils;

/// <summary>
/// Class used to show/hide the console on windows
/// </summary>
internal class WindowsConsoleManager
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    /// <summary>
    /// Finds a window by its title.
    /// </summary>
    /// <param name="windowTitle">The title of the window to find.</param>
    /// <returns>The handle (HWND) of the window if found, otherwise IntPtr.Zero.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    extern static bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    const uint WM_SETICON = 0x0080;
    const uint ICON_SMALL = 0;
    const uint ICON_BIG = 1;

    // public static void SetWindowIcon(IntPtr hWnd, System.Drawing.Icon icon)
    // {
    //     if (hWnd == IntPtr.Zero || icon == null)
    //         return;
    //
    //     IntPtr hIcon = icon.Handle;
    //     
    //     Properties
    //
    //     // Set small icon
    //     SendMessage(hWnd, WM_SETICON, (IntPtr)ICON_SMALL, hIcon);
    //     // Set big icon (optional)
    //     SendMessage(hWnd, WM_SETICON, (IntPtr)ICON_BIG, hIcon);
    // }
    //

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    /// <summary>
    /// Finds a window by its title.
    /// </summary>
    /// <param name="windowTitle">The title of the window to find.</param>
    /// <returns>The handle (HWND) of the window if found, otherwise IntPtr.Zero.</returns>
    public static IntPtr FindWindowByTitle(string windowTitle)
    {
        return FindWindow(null, windowTitle);
    }

    /// <summary>
    /// Hides the console
    /// </summary>
    public static void Hide()
    {
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_HIDE);
    }
    
    /// <summary>
    /// Shows the console
    /// </summary>
    public static void Show()
    {
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_SHOW);
    }

    public static void SetIcon(string title, string icon)
    {
        var handle = FindWindowByTitle(title);
        if (handle == IntPtr.Zero)
        {
            Logger.Instance.WLog("Failed to find window: " + title);
            return;
        }
        
        throw new NotImplementedException();
    }
}