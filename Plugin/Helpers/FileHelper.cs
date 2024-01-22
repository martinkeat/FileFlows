using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace FileFlows.Plugin.Helpers;

/// <summary>
/// A helper for file functions
/// </summary>
public class FileHelper
{
    /// <summary>
    /// Gets or sets if file ownership should or should not be changed
    /// </summary>
    public static bool DontChangeOwner { get; set; }
    /// <summary>
    /// Gets or sets if file permissions should or should not be changed
    /// </summary>
    public static bool DontSetPermissions { get; set; }
    /// <summary>
    /// Gets or sets the file permissions used when setting permissions
    /// </summary>
    public static string Permissions { get; set; }
    
    /// <summary>
    /// Creates a directory if it doesn't already exist.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="directory">The path of the directory to create.</param>
    /// <returns>True if the directory already exists or if creation succeeds; otherwise, false.</returns>
    public static bool CreateDirectoryIfNotExists(ILogger logger, string directory)
    {
        if (string.IsNullOrEmpty(directory))
            return false;
        var di = new DirectoryInfo(directory);
        if (di.Exists)
            return true;

        if (IsWindows)
            di.Create();
        else
            CreateLinuxDir(logger, di);

        return di.Exists;
    }

    /// <summary>
    /// Gets a value indicating whether the operating system is Windows.
    /// </summary>
    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Gets a value indicating whether the operating system is Linux.
    /// </summary>
    private static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    
    /// <summary>
    /// Creates a directory on a Linux system.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="di">The DirectoryInfo object representing the directory to create.</param>
    /// <returns>True if directory creation succeeds; otherwise, false.</returns>
    public static bool CreateLinuxDir(ILogger logger, DirectoryInfo di)
    {
        if (di.Exists)
            return true;
        if (di.Parent != null && di.Parent.Exists == false)
        {
            if (CreateLinuxDir(logger, di.Parent) == false)
                return false;
        }
        logger?.ILog("Creating folder: " + di.FullName);

        string cmd = $"mkdir {EscapePathForLinux(di.FullName)}";

        try
        {
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo = new System.Diagnostics.ProcessStartInfo("/bin/bash", $"-c \"{cmd}\"");
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardError.ReadToEnd();
                Console.WriteLine(output);
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return ChangeOwner(logger, di.FullName);
                }
                logger?.ELog("Failed creating directory:" + process.StartInfo.FileName, process.StartInfo.Arguments + Environment.NewLine + output);
                if (string.IsNullOrWhiteSpace(error) == false)
                    logger?.ELog("Error output:" + output);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger?.ELog("Failed creating directory: " + di.FullName + " -> " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Changes the owner of a file or directory.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="filePath">The path to the file or directory.</param>
    /// <param name="recursive">True to change ownership recursively for all items within a directory; otherwise, false.</param>
    /// <param name="file">True if the provided path is a file; otherwise, false (assumed to be a directory).</param>
    /// <param name="ownerGroup">Optional owner:group to use, if not passed in the defaults will be used</param>
    /// <returns>True if changing ownership succeeds; otherwise, false.</returns>
    public static bool ChangeOwner(ILogger logger, string filePath, bool recursive = true, bool file = false, string ownerGroup = null)
    {
        if (DontChangeOwner)
        {
            logger?.ILog("ChangeOwner is turned off, skipping");
            return true;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return true; // its windows, lets just pretend we did this

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return true; // its macos, lets just pretend we did this
            
        bool log = filePath.Contains("Runner-") == false;

        if (file == false)
        {
            if (filePath.EndsWith(Path.DirectorySeparatorChar) == false)
                filePath += Path.DirectorySeparatorChar;

            if(log)
                logger?.ILog("Changing owner on folder: " + filePath);
        }
        else
        {
            if (log)
                logger?.ILog("Changing owner on file: " + filePath);
            recursive = false;
        }

        if (string.IsNullOrWhiteSpace(ownerGroup))
        {
            string puid = Environment.GetEnvironmentVariable("PUID")?.EmptyAsNull() ?? "nobody";
            string pgid = Environment.GetEnvironmentVariable("PGID")?.EmptyAsNull() ?? "users";
            ownerGroup = $"{puid}:{pgid}";
        }

        string cmd = $"chown{(recursive ? " -R" : "")} {ownerGroup} {EscapePathForLinux(filePath)}";
        if (log)
            logger?.ILog("Change owner command: " + cmd);

        try
        {
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo = new System.Diagnostics.ProcessStartInfo("/bin/bash", $"-c \"{cmd}\"");
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardError.ReadToEnd();
                Console.WriteLine(output);
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                    return SetPermissions(logger, filePath, file: file);
                logger?.WLog("Failed changing owner:" + process.StartInfo.FileName, process.StartInfo.Arguments + Environment.NewLine + output);
                if (string.IsNullOrWhiteSpace(error) == false)
                    logger?.WLog("Error output:" + output);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger?.WLog("Failed changing owner: " + filePath + " => " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sets permissions for a file or directory.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="filePath">The path to the file or directory.</param>
    /// <param name="recursive">True to apply permissions recursively to all items within a directory; otherwise, false.</param>
    /// <param name="file">True if the provided path is a file; otherwise, false (assumed to be a directory).</param>
    /// <returns>True if setting permissions succeeds; otherwise, false.</returns>
    public static bool SetPermissions(ILogger logger, string filePath, bool recursive = true, bool file = false, string permissions = null)
    {
        permissions = permissions?.EmptyAsNull() ?? Permissions?.EmptyAsNull() ?? "777";
        if (DontSetPermissions)
        {
            logger?.ILog("SetPermissions is turned off, skipping");
            return true;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return true; // its windows, lets just pretend we did this
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return true; // its macos, lets just pretend we did this

        bool log = filePath.Contains("Runner-") == false;

        if (file == false)
        {
            if (Directory.Exists(filePath) == false)
            {
                logger?.WLog("Directory does not exist, cannot set permissions: " + filePath);
                return false;
            }
            if (filePath.EndsWith(Path.DirectorySeparatorChar) == false)
                filePath += Path.DirectorySeparatorChar;
            if(log)
                logger?.ILog("Setting permissions on folder: " + filePath);
        }
        else
        {
            if (File.Exists(filePath) == false)
            {
                logger?.WLog("File does not exist, cannot set permissions: " + filePath);
                return false;
            }
            if (log)
                logger?.ILog("Setting permissions on file: " + filePath);
            recursive = false;
        }

        string cmd = $"chmod{(recursive ? " -R" : "")} {permissions} {EscapePathForLinux(filePath)}";

        try
        {
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo = new System.Diagnostics.ProcessStartInfo("/bin/bash", $"-c \"{cmd}\"");
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardError.ReadToEnd();
                Console.WriteLine(output);
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    logger?.ILog($"Permissions [{permissions}] set on file: {filePath}");
                    return true;
                }

                logger?.ELog("Failed setting permissions: " + process.StartInfo.FileName, process.StartInfo.Arguments + Environment.NewLine + output);
                if (string.IsNullOrWhiteSpace(error) == false)
                    logger?.ELog("Error output:" + output);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger?.ELog("Failed setting permissions: " + filePath + " => " + ex.Message);
            return false;
        }
    }
    
    /// <summary>
    /// Escapes a path to be compatible with Linux file systems.
    /// </summary>
    /// <param name="path">The path to escape.</param>
    /// <returns>An escaped path compatible with Linux file systems.</returns>
    public static string EscapePathForLinux(string path)
    {
        path = Regex.Replace(path, "([\\'\"\\$\\?\\*()\\s&;])", "\\$1");
        return path;
    }

    /// <summary>
    /// Saves a file with the provided data.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="file">The file path.</param>
    /// <param name="data">The byte array data to write to the file.</param>
    public static void SaveFile(ILogger logger, string file, byte[] data)
    {
        File.WriteAllBytes(file, data);
        if (IsWindows)
            return;
        ChangeOwner(logger, file, file:true);
    }
    
    /// <summary>
    /// Extracts a file to the specified destination.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="file">The file to extract.</param>
    /// <param name="destination">The destination directory to extract the file.</param>
    public static void ExtractFile(ILogger logger, string file, string destination)
    {
        System.IO.Compression.ZipFile.ExtractToDirectory(file, destination);
        if (IsWindows)
            return;
        ChangeOwner(logger,destination);
    }
    
    /// <summary>
    /// Extracts the short filename from the given path.
    /// </summary>
    /// <param name="path">The path from which to extract the short filename.</param>
    /// <returns>The short filename from the given path.</returns>
    public static string GetShortFileName(string path)
    {
        char separator;

        if (path.Contains('/') && !path.Contains('\\'))
            separator = '/';
        else if (path.Contains('\\') && !path.Contains('/'))
            separator = '\\';
        else
            separator = Path.DirectorySeparatorChar;

        var parts = path.Split(separator);
        return parts[^1]; // Using array index -1 to get the last element
    }
    
    
    /// <summary>
    /// Extracts the full directory path from the given path.
    /// </summary>
    /// <param name="path">The path from which to extract the directory path.</param>
    /// <returns>The full directory path from the given path.</returns>
    public static string GetDirectory(string path)
    {
        char separator;

        if (path.Contains('/') && !path.Contains('\\'))
            separator = '/';
        else if (path.Contains('\\') && !path.Contains('/'))
            separator = '\\';
        else
            separator = Path.DirectorySeparatorChar;

        var parts = path.Split(separator);

        if (parts.Length <= 1)
            return string.Empty;

        // If it's a file path, remove the filename from the parts
        if (File.Exists(path) || Directory.Exists(path))
        {
            var directoryParts = new string[parts.Length - 1];
            Array.Copy(parts, directoryParts, parts.Length - 1);
            return string.Join(separator.ToString(), directoryParts);
        }

        return string.Join(separator.ToString(), parts[..^1]); // Using array index to remove the last element (filename)
    }
    
    /// <summary>
    /// Retrieves the extension from the given path.
    /// </summary>
    /// <param name="path">The path from which to extract the extension.</param>
    /// <returns>The extension from the given path without the '.' character; an empty string if no extension is found.</returns>
    public static string GetExtension(string path)
    {
        string extension = Path.GetExtension(path);

        if (string.IsNullOrEmpty(extension) || extension == ".")
            return string.Empty;

        return extension.TrimStart('.'); // Removing the '.' character from the extension
    }
    
    /// <summary>
    /// Gets the name of the directory from the specified path.
    /// </summary>
    /// <param name="path">The path from which to extract the directory name.</param>
    /// <returns>The name of the directory from the given path.</returns>
    public static string GetDirectoryName(string path)
    {
        // Split the path based on directory separators
        string[] parts = path.Split('\\', '/');

        // Get the second-to-last element if the path has enough components
        if (parts.Length >= 2)
        {
            return parts[^2];
        }
        
        // If the path has fewer components, return the root directory or the only component
        return parts.Length == 1 ? parts[0] : string.Empty;
    }
    
    /// <summary>
    /// Checks if the provided path corresponds to a system directory.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path corresponds to a system directory, otherwise false.</returns>
    public static bool IsSystemDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var directoryInfo = new DirectoryInfo(path);

        // Check system directories for Windows
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            string windowsSystemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string programFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86Dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            if (directoryInfo.FullName.Equals(windowsSystemDir, StringComparison.OrdinalIgnoreCase) ||
                directoryInfo.FullName.Equals(programFilesDir, StringComparison.OrdinalIgnoreCase) ||
                directoryInfo.FullName.Equals(programFilesX86Dir, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        // Check system directories for Linux
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            string[] linuxSystemDirs =
            {
                "/bin", "/boot", "/dev", "/etc", "/lib", "/lib64",
                "/opt", "/proc", "/run", "/sbin", "/srv", "/sys",
                "/usr", "/var"
            };

            foreach (string sysDir in linuxSystemDirs)
            {
                if (directoryInfo.FullName.Equals(sysDir, StringComparison.OrdinalIgnoreCase) ||
                    directoryInfo.FullName.StartsWith(sysDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        // Check system directories for macOS
        else if (Environment.OSVersion.Platform == PlatformID.MacOSX)
        {
            string[] macSystemDirs =
            {
                "/Applications", "/Library", "/System", "/Users", "/Volumes"
            };

            foreach (string sysDir in macSystemDirs)
            {
                if (directoryInfo.FullName.Equals(sysDir, StringComparison.OrdinalIgnoreCase) ||
                    directoryInfo.FullName.StartsWith(sysDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
    /// <summary>
    /// Combines multiple path strings, detecting the path separator from the first directory.
    /// </summary>
    /// <param name="path1">The first path.</param>
    /// <param name="path2">The second path.</param>
    /// <param name="additionalPaths">Additional paths to combine.</param>
    /// <returns>The combined path.</returns>
    public static string Combine(string path1, string path2, params string[] additionalPaths)
    {
        List<string> paths = new ();
        paths.Add(path1);
        paths.Add(path2);

        if (additionalPaths != null)
            paths.AddRange(additionalPaths);

        char separator = '/';
        foreach (var sep in new char[] { '/', '\\' })
        {
            if (path1.Contains(sep))
            {
                separator = sep;
                break;
            }
        }

        string? combinedPath = paths[0]?.TrimEnd(separator);

        for (int i = 1; i < paths.Count; i++)
        {
            if (string.IsNullOrEmpty(paths[i]))
                continue;

            combinedPath += $"{separator}{paths[i].TrimStart(separator)}";
        }

        return combinedPath;
    }

    /// <summary>
    /// Converts Linux permission values to UnixFileMode enum.
    /// </summary>
    /// <param name="linuxPermissions">Linux permission value (e.g., 775, 777, 666, 700).</param>
    /// <returns>UnixFileMode enum value representing the input Linux permissions.</returns>
    public static UnixFileMode ConvertLinuxPermissionsToUnixFileMode(int linuxPermissions)
    { 
        const int maxPermissionValue = 777;

        // Ensure the Linux permission value is within the valid range.
        if (linuxPermissions < 0 || linuxPermissions > maxPermissionValue)
        {
            return UnixFileMode.None;
        }

        // Convert to string, pad with zeros and take the last 3 characters
        string permissionString = linuxPermissions.ToString().PadLeft(3, '0').Substring(0, 3);

        UnixFileMode result = UnixFileMode.None;

        // Process each character in the string
        for (int i = 0; i < permissionString.Length; i++)
        {
            char permissionChar = permissionString[i];

            // Check if it can do read/write/execute
            bool canRead = permissionChar == '4' || permissionChar == '5' || permissionChar == '6' || permissionChar == '7';
            bool canWrite = permissionChar == '2' || permissionChar == '3' || permissionChar == '6' || permissionChar == '7';
            bool canExecute = permissionChar == '1' || permissionChar == '3' || permissionChar == '5' || permissionChar == '7';

            // Set corresponding flags based on position
            if (canRead)
            {
                if (i % 3 == 0) result |= UnixFileMode.UserRead;
                else if (i % 3 == 1) result |= UnixFileMode.GroupRead;
                else if (i % 3 == 2) result |= UnixFileMode.OtherRead;
            }

            if (canWrite)
            {
                if (i % 3 == 0) result |= UnixFileMode.UserWrite;
                else if (i % 3 == 1) result |= UnixFileMode.GroupWrite;
                else if (i % 3 == 2) result |= UnixFileMode.OtherWrite;
            }

            if (canExecute)
            {
                if (i % 3 == 0) result |= UnixFileMode.UserExecute;
                else if (i % 3 == 1) result |= UnixFileMode.GroupExecute;
                else if (i % 3 == 2) result |= UnixFileMode.OtherExecute;
            }
        }

        return result;
    }

}
