using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;

namespace FileFlowTests.Tests.CacheControllers;

/// <summary>
/// Test base for cache controller tests
/// </summary>
public abstract class CacheControllerTestBase
{
    static CacheControllerTestBase()
    {
        DirectoryHelper.Init(false, false);
        FlowDbConnection.Initialize(true);
        DbHelper.Initialize();
    }
}