using System;
using FileFlows.ServerShared;

namespace FileFlowTests.Tests.LibraryFiles;

[TestClass]
public class ListTests:LibraryFileTest
{
    // [TestMethod]
    public void GetNextFile()
    {
        var service = new FileFlows.Server.Services.LibraryFileService();
        var next = service.GetNext(Globals.InternalNodeName, 
            Globals.InternalNodeUid, Globals.Version.ToString(), Guid.NewGuid()).Result;
        Assert.IsNotNull(next);
    }
}