using System;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Services;

namespace FileFlowTests.Tests.LibraryFiles;

/// <summary>
/// Tests base for library file test
/// </summary>
public abstract class LibraryFileTest
{
    [TestInitialize]
    public void TestInitialize()
    {
        Globals.IsUnitTesting = true;
        SetTestData();
    }

    private void SetTestData()
    {
        // need to get libraries somehow, or to moq libraries
        
        var dict = new Dictionary<Guid, LibraryFile>();
        var rand = new Random(DateTime.Now.Millisecond);
        for (int i = 0; i < 1000; i++)
        {
            foreach (var status in new[] { FileStatus.Unprocessed, FileStatus.Duplicate, FileStatus.Processed })
            {
                var file = new LibraryFile();
                file.Uid = Guid.NewGuid();
                file.Status = status;
                file.Name = file.Uid.ToString() + ".mkv";
                file.OriginalSize = rand.NextInt64(1_000_0000, 10_000_000_000);
                file.CreationTime = DateTime.Now.AddMinutes(-rand.Next(0, 1000));
                dict.Add(file.Uid, file);
            }
        }
        
        new FileFlows.Server.Services.LibraryFileService().SetData(dict);
    }
}