using System.Diagnostics;

namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// A helper for interacting with files 
/// </summary>
public class FileHelper
{
    /// <summary>
    /// Removes illegal file/path characters from a string
    /// </summary>
    /// <param name="input">the string to clean</param>
    /// <returns>the original string with all illegal characters removed</returns>
    public static string RemoveIllegalCharacters(string input)
    {
        string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        foreach (var c in invalid)
            input = input.Replace(c.ToString(), string.Empty);
        return input;
    }

    /// <summary>
    /// Calculates a fingerprint for a file
    /// </summary>
    /// <param name="file">The filename</param>
    /// <returns>The fingerprint</returns>
    public static string CalculateFingerprint(string file)
    {
        try
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.Exists == false)
                return string.Empty;

            using var hasher = System.Security.Cryptography.SHA256.Create();
            byte[] hash;
            if (fileInfo.Length > 100_000_000)
            {
                // compute hash on first 100MB to speed it update
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
                var bytes = new byte[100_000_000];
                int realLength = stream.Read(bytes, 0, bytes.Length);
                hash = hasher.ComputeHash(bytes, 0, realLength);
            }
            else
            {
                using var stream = FileOpenHelper.OpenRead_NoLocks(file);
                hash = hasher.ComputeHash(stream);
            }

            string hashStr = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            return hashStr;
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog($"Failed to calculate fingerprint for file '{file}': {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            return string.Empty;
        }
        finally
        {
            GC.Collect();
        }
    }
    
    
    /// <summary>
    /// Makes a file executable on linux
    /// </summary>
    /// <param name="file">the file to make executable</param>
    /// <returns>if successful or not</returns>
    public static bool MakeExecutable(string file)
    {
        try
        {
            var fi = new FileInfo(file);
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo("/bin/bash", $"-c \"chmod +x {fi.Name}\"")
            {
                WorkingDirectory = fi.DirectoryName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            process.Start();
            string output = process.StandardError.ReadToEnd();
            Console.WriteLine(output);
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
                return true;
            Logger.Instance?.ELog($"Failed making executable:" + process.StartInfo.FileName,
                process.StartInfo.Arguments + Environment.NewLine + output);
            if (string.IsNullOrWhiteSpace(error) == false)
                Logger.Instance?.ELog($"Making Executable error output:" + output);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog($"Failed making executable: " + file + " => " + ex.Message);
            return false;
        }
    }
}
