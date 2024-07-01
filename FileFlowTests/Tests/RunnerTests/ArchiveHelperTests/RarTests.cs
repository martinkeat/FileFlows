#if(DEBUG)

using FileFlows.FlowRunner.Helpers;

namespace FileFlowTests.Tests.RunnerTests.ArchiveHelperTests;

/// <summary>
/// Tests RAR files
/// </summary>
[TestClass]
public class RarTests : TestBase
{
    /// <summary>
    /// Tests a multipart rar file can be extracted
    /// </summary>
    [TestMethod]
    public void MutlipartRarTest_Internal()
    {
        string rar = $"{TestFilesDir}/archives/multi-part-rar/multi-part-rar.rar";
        var helper = new ArchiveHelper(Logger, "rar", "unrar", "7z");
        var result = helper.ExtractMultipartRar(rar, TempPath);
        if(result.Failed(out string error))
            Assert.Fail(error);
        Assert.IsTrue(result.Value);
    }
}
#endif