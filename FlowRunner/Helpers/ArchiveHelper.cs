using System.IO.Compression;
using System.Text.RegularExpressions;
using FileFlows.FlowRunner.Helpers.ArchiveHelpers;
using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// Archive helper
/// </summary>
public partial class ArchiveHelper : IArchiveHelper
{
    private readonly ILogger Logger;
    private readonly RarArchiveHelper rarHelper;
    /// <summary>
    /// Initialises a new instance of the image helper
    /// </summary>
    /// <param name="args">the Node Parameters</param>
    public ArchiveHelper(NodeParameters args)
    {
        Logger = args.Logger;
        rarHelper = new(args);
    }

    /// <summary>
    /// Gets if the archive is a rar file
    /// </summary>
    /// <param name="archivePath">the archive path</param>
    /// <returns>true if is a rar file, otherwise false</returns>
    private bool IsRar(string archivePath)
        => IsRarRegex().IsMatch(archivePath);

    /// <inheritdoc />
    public Result<bool> FileExists(string archivePath, string file)
    {
        // Check if the archive file exists
        if (File.Exists(archivePath) == false)
            return Result<bool>.Fail("Archive file not found: " + archivePath);

        try
        {
            // Open the zip archive
            using ZipArchive archive = ZipFile.OpenRead(archivePath);
            return archive.Entries.Any(x => x.FullName.ToLowerInvariant().Equals(file.ToLowerInvariant()));
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> AddToArchive(string archivePath, string file)
    {
        // Check if the archive file exists
        if (File.Exists(archivePath) == false)
            return Result<bool>.Fail("Archive file not found: " + archivePath);
        
        if (archivePath.ToLowerInvariant().EndsWith(".rar") || archivePath.ToLowerInvariant().EndsWith(".cbr"))
            return rarHelper.AddToArchive(archivePath, file);
        
        try
        {
            // Open the zip archive
            using ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Update);
            
            // Create a new entry for the file
            archive.CreateEntryFromFile(file, Path.GetFileName(file));

            // Successfully added the file
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<int> GetFileCount(string archivePath, string pattern)
    {
        // Check if the archive file exists
        if (File.Exists(archivePath) == false)
            return Result<int>.Fail("Archive file not found: " + archivePath);
        bool isRar = IsRar(archivePath);
        try
        {
            var rgxFiles = new Regex(pattern, RegexOptions.IgnoreCase);
            using var archive = ArchiveFactory.Open(archivePath);
            var files = archive.Entries.Where(entry => !entry.IsDirectory).ToArray();
            return files.Count(x => x.Key != null && rgxFiles.IsMatch(x.Key));
        }
        catch(Exception ex) when (isRar && ex.Message.Contains("Unknown Rar Header"))
        {
            return rarHelper.GetFileCount(archivePath, pattern); 
        }
    }

    /// <inheritdoc />
    public Result<bool> Compress(string path, string output, string pattern = "",
        bool allDirectories = true, Action<float>? percentCallback = null)
    {
        if (IsRar(output))
            return rarHelper.Compress(path, output, pattern, allDirectories, percentCallback);
        
        var dir = new DirectoryInfo(path);
        if (dir.Exists)
        {
            var files = dir.GetFiles(pattern?.StartsWith("*") == true ? pattern : "*", allDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if (string.IsNullOrWhiteSpace(pattern) == false && pattern.StartsWith("*") == false)
            {
                var regex = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                files = files.Where(x => regex.IsMatch(x.Name)).ToArray();
            }
            using FileStream fs = new FileStream(output, FileMode.Create);
            using ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create);
            percentCallback?.Invoke(0);
            float current = 0;
            float count = files.Length;
            foreach (var file in files)
            {
                ++count;
                string relative = file.FullName.Substring(dir.FullName.Length + 1);
                try
                {
                    arch.CreateEntryFromFile(file.FullName, relative, CompressionLevel.SmallestSize);
                }
                catch (Exception ex)
                {
                    Logger?.WLog("Failed to add file to zip: " + file.FullName + " => " + ex.Message);
                }

                float percent = (current / count) * 100f;
                percentCallback?.Invoke(percent);
            }

            percentCallback?.Invoke(100);
            return true;
        }
        
        if(File.Exists(path))
        {
            percentCallback?.Invoke(0);
            try
            {
                using FileStream fs = new FileStream(output, FileMode.Create);
                using ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create);
                arch.CreateEntryFromFile(path, FileHelper.GetShortFileName(path), CompressionLevel.SmallestSize);
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail(ex.Message);
            }
            percentCallback?.Invoke(100);
            return true;
        }

        return Result<bool>.Fail("Path does not exist");
    }

    /// <inheritdoc />
    public Result<bool> Extract(string archivePath, string destinationPath, Action<float>? percentCallback = null)
    {
        // Check if the archive file exists
        if (File.Exists(archivePath) == false)
            return Result<bool>.Fail("Archive file not found: " + archivePath);
        
        try
        {
            using Stream stream = File.OpenRead(archivePath);
            using var reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                // Determine the type of the archive entry
                if (reader.Entry.IsDirectory)
                {
                    // Skip directories, as we're interested in files
                    continue;
                }

                // Extract the file
                var entryPath = Path.Combine(destinationPath, reader.Entry.Key);
                Logger?.ILog("Extracting file: " + entryPath);
                reader.WriteEntryToDirectory(destinationPath, new ExtractionOptions()
                {
                    ExtractFullPath = false,     // Extract files without full path
                    Overwrite = true            // Overwrite existing files
                });
            }

            return true;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed to extract archive: " + ex.Message);
        }
    }

    /// <summary>
    /// Precompiled is rar regex
    /// </summary>
    /// <returns>the rar regex</returns>
    [GeneratedRegex(@"\.(rar|cbr)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IsRarRegex();
}