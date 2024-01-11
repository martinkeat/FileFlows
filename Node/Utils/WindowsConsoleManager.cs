namespace FileFlows.Node.Utils;

internal class WindowsConsoleManager
{
    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    public static void Hide()
    {
        _ = Task.Run(async () =>
        {
            for (int i = 0; i < 5; i++)
            {
                var handle = GetConsoleWindow();
                ShowWindow(handle, SW_HIDE);
                await Task.Delay(250);
            }
        });
    }
    
    public static void Show()
    {
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_SHOW);
    }
}