using System.Security.Cryptography;

namespace FileFlows.ServerShared.FileServices;

/// <summary>
/// Calculates a hash for a file
/// </summary>
public static class FileHasher
{
    /// <summary>
    /// Calculates the SHA256 hash for the file located at the specified file path asynchronously.
    /// </summary>
    /// <param name="filePath">The path of the file for which the hash needs to be calculated.</param>
    /// <returns>The computed SHA256 hash of the file as a byte array.</returns>
    public static async Task<string> Hash(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                return string.Empty;

            using var stream = new BufferedStream(FileOpenHelper.OpenRead_NoLocks(filePath), 1200000);
            // The rest remains the same
            var sha = SHA256.Create();
            byte[] checksum = await sha.ComputeHashAsync(stream);
            return BitConverter.ToString(checksum).Replace("-", string.Empty);
        }
        catch (Exception ex)
        {
            Logger.Instance?.ELog($"Failed to calculate fingerprint for file '{filePath}': {ex.Message}{Environment.NewLine}{ex.StackTrace}");
            return string.Empty;
        }
        finally
        {
            GC.Collect();
        }
    }
}