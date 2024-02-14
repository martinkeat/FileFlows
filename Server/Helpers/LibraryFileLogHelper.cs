using System.Text;
using FileFlows.Server.Controllers;
using FileFlows.Shared.Helpers;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Helper class to wrap reading/writing library file logs
/// </summary>
public class LibraryFileLogHelper
{
    /// <summary>
    /// Gets the html log file of a library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="lines">[Optional] if set and if a html log file does not exist, will convert the last specified amount of plain text lines</param>
    /// <returns>the html log file</returns>
    public static string GetHtmlLog(Guid uid, int lines = 0)
    {
        var logFile = Path.Combine(DirectoryHelper.LibraryFilesLoggingDirectory, uid.ToString());

        if (File.Exists(logFile + ".html.gz"))
            return Gzipper.DecompressFileToString(logFile + ".html.gz", lines: lines, bytes: 2_000_000);

        string plainText = GetLog(uid);
        if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;
    
        if(lines > 0)
        {
            var logLines = plainText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (logLines.Length > lines)
                plainText = String.Join(Environment.NewLine, logLines.TakeLast(lines));
        }

        return LogToHtml.Convert(plainText);
    }
    
    /// <summary>
    /// Gets the plain text log file of a library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <returns>the plain text log file</returns>
    public static string GetLog(Guid uid)
    {
        var logFile = Path.Combine(DirectoryHelper.LibraryFilesLoggingDirectory, uid.ToString());

        if (File.Exists(logFile + ".log"))
        {
            StringBuilder log = new StringBuilder();
            using FileStream fs = new FileStream(logFile + ".log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader sr = new StreamReader(fs);
            while (sr.Peek() >= 0)
            {
                log.AppendLine(sr.ReadLine());
            }
            return log.ToString();
        }

        if (File.Exists(logFile + ".log.gz"))
            return Gzipper.DecompressFileToString(logFile + ".log.gz");
        
        return string.Empty;
    }
    
    /// <summary>
    /// Gets the plain text log file of a library file
    /// </summary>
    /// <param name="uid">The UID of the library file</param>
    /// <param name="content">the log file content to save</param>
    /// <param name="saveHtml">if the html version should also be saved</param>
    /// <returns>the plain text log file</returns>
    public static Task SaveLog(Guid uid, string content, bool saveHtml = false)
    {
        var logFile = Path.Combine(DirectoryHelper.LibraryFilesLoggingDirectory, uid.ToString());

        Gzipper.CompressToFile(logFile + ".log.gz", content);
        TryDeleteFile(logFile + ".log");
            
        if (saveHtml)
        {
            string html = LogToHtml.Convert(content);
            Gzipper.CompressToFile(logFile + ".html.gz", html);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Appends a string to the log file
    /// </summary>
    /// <param name="uid">the UID of the library file</param>
    /// <param name="content">the content to append</param>
    public static async Task AppendToLog(Guid uid, string content)
    {
        var logFile = Path.Combine(DirectoryHelper.LibraryFilesLoggingDirectory, uid + ".log");
        if (File.Exists(logFile) == false)
        {
            await File.WriteAllTextAsync(logFile, content);
        }
        else
        {
            using var fs = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter sr = new StreamWriter(fs);
            await sr.WriteLineAsync(content);
            await sr.FlushAsync();
        }
    }

    /// <summary>
    /// Checks if the html version of the log file exists
    /// </summary>
    /// <param name="uid">the UID of the library file</param>
    /// <returns>true if the html log exists</returns>
    public static bool HtmlLogExists(Guid uid)
    {
        var logFile = Path.Combine(DirectoryHelper.LibraryFilesLoggingDirectory, uid + ".html.gz");
        return File.Exists(logFile);
    }

    /// <summary>
    /// Creates an HTML version of a log file and saves it
    /// </summary>
    /// <param name="uid">the UID of the library file</param>
    public static void CreateHtmlOfLog(Guid uid)
    {
        string plaintext = GetLog(uid);
        if (string.IsNullOrWhiteSpace(plaintext))
            return;
        
        var logFile = Path.Combine(DirectoryHelper.LibraryFilesLoggingDirectory, uid + ".html.gz");
        string html = LogToHtml.Convert(plaintext);
        Gzipper.CompressToFile(logFile, html);
    }

    /// <summary>
    /// Deletes all logs for a library file
    /// </summary>
    /// <param name="uid">the UID of the library file</param>
    public static void DeleteLogs(Guid uid)
    {
        var logFile = Path.Combine(DirectoryHelper.LibraryFilesLoggingDirectory, uid.ToString());
        TryDeleteFile(logFile + ".log");
        TryDeleteFile(logFile + ".log.gz");
        TryDeleteFile(logFile + ".html");
        TryDeleteFile(logFile + ".html.gz");
    }

    /// <summary>
    /// Try to delete a file,
    /// checks if the file exists first
    /// </summary>
    /// <param name="file">the name of the file to delete</param>
    private static void TryDeleteFile(string file)
    {
        try
        {
            if(File.Exists(file))
                File.Delete(file);
        }
        catch (Exception)
        {
        }
    }
}