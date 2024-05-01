using System.IO.Compression;
using System.Text;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Helper class for gzip functions 
/// </summary>
public class Gzipper
{
    /// <summary>
    /// Compresses a file
    /// </summary>
    /// <param name="inputFile">the input file to gzip</param>
    /// <param name="outputFile">the output file to save the new file to</param>
    /// <param name="deleteInput">if the input file should be deleted afterwards</param>
    /// <returns>true if successful</returns>
    public static bool CompressFile(string inputFile, string outputFile, bool deleteInput = false)
    {
        try
        {
            if (File.Exists(outputFile))
                File.Delete(outputFile);

            using (FileStream originalFileStream = FileOpenHelper.OpenRead_NoLocks(inputFile))
            {
                using FileStream compressedFileStream = File.Create(outputFile);
                using var compressor = new GZipStream(compressedFileStream, CompressionMode.Compress);
                originalFileStream.CopyTo(compressor);
            }

            if(deleteInput)
                File.Delete(inputFile);
            return true;
        }
        catch (Exception ex)
        {
            Shared.Logger.Instance?.WLog("Failed compressing file: " + ex.Message);
            return false;
        }
    }
    
    
    /// <summary>
    /// Decompresses a file
    /// </summary>
    /// <param name="inputFile">the input file to gzip</param>
    /// <param name="outputFile">the output file to save the new file to</param>
    /// <param name="deleteInput">if the input file should be deleted afterwards</param>
    /// <returns>true if successful</returns>
    public static bool DecompressFile(string inputFile, string outputFile, bool deleteInput = false)
    {
        try
        {
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            
            using FileStream compressedFileStream = FileOpenHelper.OpenRead_NoLocks(inputFile);
            using FileStream outputFileStream = File.Create(outputFile);
            using var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
            decompressor.CopyTo(outputFileStream);
            
            if(deleteInput)
                File.Delete(inputFile);
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Compresses a string and saves it to a specified file
    /// </summary>
    /// <param name="file">The file where to save the compressed contents</param>
    /// <param name="contents">the contents to compress</param>
    public static void CompressToFile(string file, string contents)
    {
        if (File.Exists(file))
            File.Delete(file);

        var bytes = Encoding.UTF8.GetBytes(contents);
        using var input = new MemoryStream(bytes);
        using FileStream compressedFileStream = File.Create(file);
        using var compressor = new GZipStream(compressedFileStream, CompressionMode.Compress);
        input.CopyTo(compressor);
    }

    /// <summary>
    /// Decompresses a file and returns the string contents
    /// </summary>
    /// <param name="inputFile">the file to decompress</param>
    /// <param name="lines">an optional number of lines to grab, 0 to grab all</param>
    /// <param name="bytes">an optional number of bytes to grab, 0 to grab all</param>
    /// <returns>the decompresses file contents</returns>
    public static string DecompressFileToString(string inputFile, int lines = 0, int bytes = 0)
    {
        using FileStream compressedFileStream = FileOpenHelper.OpenRead_NoLocks(inputFile);
        using MemoryStream outputStream = new MemoryStream();
        using var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
        
        if (lines > 0)
        {
            decompressor.CopyTo(outputStream);
            outputStream.Seek(0, SeekOrigin.Begin); // Reset the stream position to start
            using (StreamReader reader = new StreamReader(outputStream, Encoding.UTF8))
            {
                StringBuilder result = new StringBuilder();
                for (int i = 0; i < lines; i++)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;
                    result.AppendLine(line);
                }
                return result.ToString();
            }
        }
        else if (bytes > 0)
        {    
            const int bufferSize = 1024; // Adjust the buffer size as needed
            byte[] buffer = new byte[bufferSize];
            int totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = decompressor.Read(buffer, 0, Math.Min(bufferSize, bytes - totalBytesRead))) > 0)
            {
                outputStream.Write(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;

                if (totalBytesRead >= bytes)
                    break; // Break if the specified number of bytes has been reached
            }

            outputStream.Seek(0, SeekOrigin.Begin); // Reset the stream position to start
            return Encoding.UTF8.GetString(outputStream.ToArray());
        }
        else
        {
            decompressor.CopyTo(outputStream);
            outputStream.Seek(0, SeekOrigin.Begin); // Reset the stream position to start
            return Encoding.UTF8.GetString(outputStream.ToArray());
        }
    }
}