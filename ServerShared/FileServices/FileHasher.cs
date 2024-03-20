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

            using (var stream = new BufferedStream(FileOpenHelper.OpenRead_NoLocks(filePath), 1200000))
            {
                // The rest remains the same
                var sha = SHA256.Create();
                byte[] checksum = await sha.ComputeHashAsync(stream);
                return BitConverter.ToString(checksum).Replace("-", string.Empty);
            }

            using var hasher = SHA256.Create();
            byte[]? hash;

            if (fileInfo.Length > 100_000_000)
            {
                using var stream = FileOpenHelper.OpenRead_NoLocks(filePath);
                const int chunkSize = 100_000_000; // 100MB chunks
                var buffer = new byte[chunkSize];

                int bytesRead;
                hasher.Initialize();

                do
                {
                    bytesRead = stream.Read(buffer, 0, chunkSize);
                    hasher.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                } while (bytesRead == chunkSize);

                hasher.TransformFinalBlock(buffer, 0, bytesRead);
                hash = hasher.Hash;
            }
            else
            {
                await using var stream = FileOpenHelper.OpenRead_NoLocks(filePath);
                hash = await hasher.ComputeHashAsync(stream);
            }

            string hashStr = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            return hashStr;
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