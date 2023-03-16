using System;
using System.Threading.Tasks;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Services;
using Moq;

namespace FileFlowTests.Tests.LibraryFiles;

/// <summary>
/// Tests base for library file test
/// </summary>
public abstract class LibraryFileTest
{
    protected Settings Settings { get; set; }
    protected ProcessingNode Node { get; set; }
    protected ProcessingNode InternalNode { get; set; }
    
    [TestInitialize]
    public void TestInitialize()
    {
        Globals.IsUnitTesting = true;
        MoqSettingsService();
        MoqNodeService();
        SetTestData();
    }

    private void MoqSettingsService()
    {
        var moq = new Moq.Mock<ISettingsService>();
        FileFlows.Server.Services.SettingsService.Loader = () => moq.Object;
        Settings = new();
        moq.Setup(x => x.Get())
            .Returns(() => Task.FromResult(Settings));
    }
    private void MoqNodeService()
    {
        var moq = new Moq.Mock<INodeService>();
        FileFlows.Server.Services.NodeService.Loader = () => moq.Object;
        Node = new()
        {
            Uid = Guid.NewGuid(),
            Name = "UnitTestNode",
            Enabled = true,
            Version = Globals.Version.ToString()
        };
        InternalNode = new()
        {
            Uid = Globals.InternalNodeUid,
            Name = Globals.InternalNodeName,
            Enabled = true,
            Version = Globals.Version.ToString()
        };
        moq.Setup(x => x.GetByUid(It.Is<Guid>(y => y == Node.Uid)))
            .Returns(() => Task.FromResult(Node));
        moq.Setup(x => x.GetByUid(It.Is<Guid>(y => y == Globals.InternalNodeUid)))
            .Returns(() => Task.FromResult(InternalNode));
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