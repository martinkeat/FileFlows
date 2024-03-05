using System.IO;

namespace FileFlowTests.Tests.DataLayerTests;

public class DbLayerTest
{
    /// <summary>
    /// The test context instance
    /// </summary>
    private TestContext testContextInstance;

    /// <summary>
    /// Gets or sets the test context
    /// </summary>
    public TestContext TestContext
    {
        get { return testContextInstance; }
        set { testContextInstance = value; }
    }

    /// <summary>
    /// The test logger
    /// </summary>
    protected TestLogger Logger;

    protected string TempPath = "/home/john/src/FileFlowsTests";
    
    public DbLayerTest()
    {
        Logger = new () { Writer = (message) => TestContext.WriteLine(message) };
        FileFlows.DataLayer.Helpers.Decrypter.EncryptionKey = "FileFlowsUnitTests";
        if (Directory.Exists(TempPath) == false)
            Directory.CreateDirectory(TempPath);
    }
}