namespace FileFlowTests.Tests;

/// <summary>
/// Test base file
/// </summary>
public class TestBase
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
        get => testContextInstance;
        set => testContextInstance = value;
    }

    /// <summary>
    /// When the test starts
    /// </summary>
    [TestInitialize]
    public void TestStarted()
    {
        Logger.Writer = (message) => TestContext.WriteLine(message);
    }

    /// <summary>
    /// The test logger
    /// </summary>
    public readonly TestLogger Logger = new ();

    /// <summary>
    /// The directory with the test files
    /// </summary>
    protected readonly string TestFilesDir = "/home/john/src/ff-files/test-files";

    /// <summary>
    /// The temp path to use during testing
    /// </summary>
    protected readonly string TempPath = "/home/john/src/ff-files/temp";
}