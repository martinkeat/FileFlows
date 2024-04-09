using FileFlows.DataLayer;

namespace FileFlowTests.Tests.DataLayerTests;

internal class TestDataHelper
{
    private ILogger Logger;
    private DatabaseAccessManager Dam;
    
    public TestDataHelper(ILogger logger, DatabaseAccessManager dam)
    {
        Logger = logger;
        Dam = dam;
    }

    private Library InsertLibrary(DatabaseAccessManager dam, string name, bool enabled = true, int holdMinutes = 0, string schedule = "")
    {
        var lib = new Library()
        {
            Uid = Guid.NewGuid(),
            Name = name,
            Enabled = enabled,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
            Description = "this is a test description",
            Path = "/" + name.ToLowerInvariant(),
            Scan = true,
            HoldMinutes = holdMinutes,
            LastScanned = DateTime.UtcNow,
            Schedule = schedule?.EmptyAsNull() ?? new string('1', 672)
        };
        dam.FileFlowsObjectManager.AddOrUpdateObject(lib, null).Wait();
        return lib;
    }

    public class BulkInsertResult
    {
        public List<LibraryFile> Held { get; set; } = new();
        public List<LibraryFile> Disabled { get; set; } = new();
        public List<LibraryFile> OutOfSchedule { get; set; } = new();
        public List<LibraryFile> Active { get; set; } = new();

        public List<Library> Libraries = new();
    }

    public BulkInsertResult BulkInsert(int count = 10_000)
    {
        var rand = new Random(DateTime.UtcNow.Microsecond);


        var libDisabled = InsertLibrary(Dam, "Disabled", enabled: false);
        var libHeld = InsertLibrary(Dam, "Held", holdMinutes: 600);
        var libOutOfSchedule = InsertLibrary(Dam, "Held", schedule: new string('0', 672));
        var libActive = InsertLibrary(Dam, "Active");

        BulkInsertResult result = new();
        result.Libraries.AddRange(new[] { libDisabled, libHeld, libOutOfSchedule, libActive });

        List<LibraryFile> files = new();
        for (int i = 0; i < count; i++)
        {
            var lib = rand.Next(0, 100) switch
            {
                > 50 => libActive,
                > 30 => libHeld,
                > 15 => libDisabled,
                _ => libOutOfSchedule
            };
            LibraryFile file = new()
            {
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow,
                Uid = Guid.NewGuid(),
                Name = lib.Path + "/fake/file_" + (i + 1).ToString("D4") + ".mkv",
                RelativePath = "file_" + (i + 1).ToString("D4") + ".mkv",
                Fingerprint = "",
                Flags = LibraryFileFlags.None,
                LibraryName = lib.Name,
                LibraryUid = lib.Uid,
                OriginalSize = rand.NextInt64(1000, 1_000_000_000_000_000),
                CreationTime = DateTime.UtcNow,
                LastWriteTime = DateTime.UtcNow,
            };
            if (lib == libHeld)
                file.HoldUntil = DateTime.UtcNow.AddMinutes(60);
            files.Add(file);
            if(lib == libDisabled)
                result.Disabled.Add(file);
            else if(lib == libOutOfSchedule)
                result.OutOfSchedule.Add(file);
            else if(lib == libHeld)
                result.Held.Add(file);
            else if(lib == libActive)
                result.Active.Add(file);
        }

        Dam.LibraryFileManager.InsertBulk(files.ToArray()).Wait();
        //
        // if (dbType == DatabaseType.Sqlite)
        //     continue;

        var results = Dam.LibraryFileManager.GetTotal().Result;
        Assert.AreEqual(files.Count, results);
        return result;
    }
}