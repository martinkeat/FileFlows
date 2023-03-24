namespace FileFlowTests.Tests.LibraryFiles;

/// <summary>
/// Tests base for library file test
/// </summary>
public abstract class LibraryFileTest
{
    protected Random rand = new Random(DateTime.Now.Millisecond);
    protected Settings Settings { get; set; }
    protected ProcessingNode Node { get; set; }
    protected ProcessingNode InternalNode { get; set; }
    
    protected List<Library> Libraries { get; set; }

    protected Dictionary<Guid, LibraryFile> Files { get; set; }
    
    [TestInitialize]
    public void TestInitialize()
    {
        Globals.IsUnitTesting = true;
        MoqSettingsService();
        MoqNodeService();
        MoqLibraryService();
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
        // var moq = new Moq.Mock<INodeService>();
        // FileFlows.Server.Services.NodeService.Loader = () => moq.Object;
        Node = new()
        {
            Uid = Guid.NewGuid(),
            Name = "UnitTestNode",
            Enabled = true,
            Version = Globals.Version.ToString(),
            AllLibraries = ProcessingLibraries.All
        };
        InternalNode = new()
        {
            Uid = Globals.InternalNodeUid,
            Name = Globals.InternalNodeName,
            Enabled = true,
            Version = Globals.Version.ToString(),
            AllLibraries = ProcessingLibraries.All
        };

        FileFlows.Server.Services.NodeService.SetData(new List<ProcessingNode>()
        {
            InternalNode, Node
        });
        // moq.Setup(x => x.GetByUidAsync(It.Is<Guid>(y => y == Node.Uid)))
        //     .Returns(() => Task.FromResult(Node));
        // moq.Setup(x => x.GetByUidAsync(It.Is<Guid>(y => y == Globals.InternalNodeUid)))
        //     .Returns(() => Task.FromResult(InternalNode));
    }

    private void MoqLibraryService()
    {
        // var moq = new Moq.Mock<ILibraryService>();
        // FileFlows.Server.Services.LibraryService.Loader = () => moq.Object;

        Libraries = new List<Library>();
        var orders = new[]
        {
            ProcessingOrder.Random, ProcessingOrder.LargestFirst, ProcessingOrder.SmallestFirst,
            ProcessingOrder.OldestFirst, ProcessingOrder.AsFound, ProcessingOrder.NewestFirst
        }.OrderBy(x => rand.Next());
        foreach (var order in orders)
        {
            var lib = new Library();
            lib.Uid = Guid.NewGuid();
            lib.Name = "Library " + order + " [" + Libraries.Count + "]";
            lib.ProcessingOrder = order;
            lib.Priority = rand.NextEnum<ProcessingPriority>();
            lib.Enabled = true;
            lib.Schedule = new string('1', 672);
            Libraries.Add(lib);
        }

        FileFlows.Server.Services.LibraryService.SetData(Libraries);
        // moq.Setup(x => x.GetByUidAsync(It.IsAny<Guid>()))
        //     .Returns((Guid uid) =>
        //         Task.FromResult(Libraries.FirstOrDefault(x => x.Uid == uid))!
        //     );
        // moq.Setup(x => x.GetAllAsync())
        //     .Returns(() => Task.FromResult((List<Library>)Libraries));
    }

    private void SetTestData()
    {
        // need to get libraries somehow, or to moq libraries
        
        var dict = new Dictionary<Guid, LibraryFile>();
        foreach (var lib in Libraries)
        {
            for (int i = 0; i < 1000; i++)
            {
                foreach (var status in new[] { FileStatus.Unprocessed, FileStatus.Duplicate, FileStatus.Processed })
                {
                    var file = NewFile(lib, status);
                    dict.Add(file.Uid, file);
                }
            }
        }

        Files = dict;
        
        new FileFlows.Server.Services.LibraryFileService().SetData(dict);
    }

    /// <summary>
    /// Create a new test file
    /// </summary>
    /// <param name="library">The library this file will belong to</param>
    /// <param name="status">the status of this file</param>
    /// <returns>the new test file</returns>
    protected LibraryFile NewFile(Library library, FileStatus status = FileStatus.Unprocessed)
    {   
        var file = new LibraryFile();
        file.Uid = Guid.NewGuid();
        file.Status = status;
        file.Name = file.Uid.ToString() + ".mkv";
        file.LibraryName = library.Name;
        file.LibraryUid = library.Uid;
        file.Library = new()
        {
            Uid = library.Uid,
            Name = library.Name,
            Type = library.GetType().FullName
        };
        file.OriginalSize = rand.NextInt64(1_000_0000, 10_000_000_000);
        file.DateCreated = DateTime.Now.AddSeconds(-rand.Next(0, 1000 * 60));
        file.DateModified = DateTime.Now.AddSeconds(-rand.Next(0, 1000 * 60));
        file.CreationTime = DateTime.Now.AddSeconds(-rand.Next(0, 1000 * 60));
        file.LastWriteTime = DateTime.Now.AddSeconds(-rand.Next(0, 1000 * 60));
        return file;
    }
}