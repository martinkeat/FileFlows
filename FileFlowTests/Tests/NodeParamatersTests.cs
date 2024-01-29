using System.IO;
using FileFlows.ServerShared.FileServices;
using Microsoft.AspNetCore.Routing.Constraints;

namespace FileFlowTests.Tests;

/// <summary>
/// Tests for NodeParameters
/// </summary>
[TestClass]
public class NodeParamatersTests
{
    [TestMethod]
    public void InitFileTests()
    {
        var original = Path.Combine(Path.GetTempPath(), "original.mkv");
        File.WriteAllText(original, "test");
        
        var nodeParameters = new NodeParameters(original, null, false, @"C:\media", new LocalFileService());

        var file = Path.Combine(Path.GetTempPath(), "somefile.mp4");
        File.WriteAllText(file, "test");
        
        nodeParameters.InitFile(file);
        
        Assert.AreEqual(".mp4", nodeParameters.Variables["ext"]);
        Assert.AreEqual("somefile.mp4", nodeParameters.Variables["file.Name"]);
        Assert.AreEqual("somefile", nodeParameters.Variables["file.NameNoExtension"]);
        
        Assert.AreEqual(".mkv", nodeParameters.Variables["file.Orig.Extension"]);
        Assert.AreEqual("original.mkv", nodeParameters.Variables["file.Orig.FileName"]);
        Assert.AreEqual("original", nodeParameters.Variables["file.Orig.FileNameNoExtension"]);
        Assert.AreEqual("original.mkv", nodeParameters.Variables["file.Orig.Name"]);
        Assert.AreEqual("original", nodeParameters.Variables["file.Orig.NameNoExtension"]);
    }
}