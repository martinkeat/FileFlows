using Avalonia;
using FileFlows.Node.Ui;

namespace FileFlows.Node.Helpers;

/// <summary>
/// Helper for the GUI
/// </summary>
public class GuiHelper
{
    /// <summary>
    /// Shows a message on the GUI
    /// </summary>
    /// <param name="title">the title of the message</param>
    /// <param name="message">the message to show</param>
    public static void ShowMessage(string title, string message)
    {
        if (OperatingSystem.IsMacOS())
        {
            // Escape double quotes in the message
            string escapedMessage = message.Replace("\"", "\"\"");
            string escapedTitle = title.Replace("\"", "\"\"");

            // Create a temporary AppleScript file with the escaped message
            string script = $"display dialog \"{escapedMessage}\" buttons {{\"OK\"}} default button \"OK\" with title \"{escapedTitle}\"";
            string scriptPath = "/tmp/display_message.scpt";
            System.IO.File.WriteAllText(scriptPath, script);

            // Execute the AppleScript using osascript
            Process.Start("osascript", scriptPath);
        }
        else
        {
            try
            {
                var appBuilder = AppBuilder.Configure<MessageApp>();
                appBuilder.Instance.DataContext = message;
                appBuilder.StartWithClassicDesktopLifetime(new string[]{});
            }
            catch (Exception)
            {
            }
        }
    }
    
}