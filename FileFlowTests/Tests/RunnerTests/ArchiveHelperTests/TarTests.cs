#if(DEBUG)
using FileFlows.FlowRunner.Helpers;

namespace FileFlowTests.Tests.RunnerTests.ArchiveHelperTests;

/// <summary>
/// Tests TAR files
/// </summary>
[TestClass]
public class TarTests : TestBase
{
    /// <summary>
    /// Tests a tar file can be extracted
    /// </summary>
    [TestMethod]
    public void Basic()
    {
        string rar = $"{TestFilesDir}/archives/tar.tar";
        var helper = new ArchiveHelper(Logger, "rar", "unrar", "7z");
        var result = helper.Extract(rar, TempPath, (percent) =>
        {
            Logger.ILog("Percent: " + percent);
        });
        if(result.Failed(out string error))
            Assert.Fail(error);
        Assert.IsTrue(result.Value);
    }
    
    /// <summary>
    /// Tests a tar file can be extracted 2
    /// </summary>
    [TestMethod]
    public void Basic2()
    {
        string rar = $"{TestFilesDir}/archives/tar2.tar";
        var helper = new ArchiveHelper(Logger, "rar", "unrar", "7z");
        var result = helper.Extract(rar, TempPath);
        if(result.Failed(out string error))
            Assert.Fail(error);
        Assert.IsTrue(result.Value);
    }
}
#endif