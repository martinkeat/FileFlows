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
    /// <param name="lpClassName">The class name.</param>
    /// <param name="lpWindowName">The window name.</param>
    /// <returns>The handle (HWND) of the window if found, otherwise IntPtr.Zero.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    

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
}