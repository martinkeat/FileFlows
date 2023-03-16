using System;
using FileFlows.Plugin;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Models;
using FileFlows.Shared;

namespace FileFlowTests.Tests.LibraryFiles;

[TestClass]
public class ListTests:LibraryFileTest
{
    [TestMethod]
    public void GetNextFile()
    {
        var service = new FileFlows.Server.Services.LibraryFileService();
        var nextResult = service.GetNext(Globals.InternalNodeName, 
            Globals.InternalNodeUid, Globals.Version.ToString(), Guid.NewGuid()).Result;
        Assert.IsNotNull(nextResult);
        Assert.AreEqual(NextLibraryFileStatus.Success, nextResult.Status);
        Assert.IsNotNull(nextResult.File);
    }
    
    [TestMethod]
    public void GetNextFile_NodeOnlyOneLibrary()
    {
        var lib = Libraries[3];
        InternalNode.Libraries = new List<ObjectReference>
        {
            new () { Uid = lib.Uid, Name = lib.Name, Type = lib.GetType().FullName }
        };
        InternalNode.AllLibraries = ProcessingLibraries.Only;
        
        var service = new FileFlows.Server.Services.LibraryFileService();
        var nextResult = service.GetNext(Globals.InternalNodeName, 
            Globals.InternalNodeUid, Globals.Version.ToString(), Guid.NewGuid()).Result;
        Assert.IsNotNull(nextResult);
        Assert.AreEqual(NextLibraryFileStatus.Success, nextResult.Status);
        Assert.IsNotNull(nextResult.File);
        var next = nextResult.File;
        Assert.AreEqual(lib.Name, next.LibraryName);
        Assert.AreEqual(lib.Uid, next.LibraryUid);
    }
    
    [TestMethod]
    public void GetNextFile_NodeAllExcept()
    {
        var lib = Libraries[3];
        InternalNode.Libraries = Libraries.Where(x => x != lib).Select(
            x => new ObjectReference() { Uid = x.Uid, Name = x.Name, Type = x.GetType().FullName }
        ).ToList();
        InternalNode.AllLibraries = ProcessingLibraries.AllExcept;
        
        var service = new FileFlows.Server.Services.LibraryFileService();
        var nextResult = service.GetNext(Globals.InternalNodeName, 
            Globals.InternalNodeUid, Globals.Version.ToString(), Guid.NewGuid()).Result;
        Assert.IsNotNull(nextResult);
        Assert.AreEqual(NextLibraryFileStatus.Success, nextResult.Status);
        Assert.IsNotNull(nextResult.File);
        var next = nextResult.File;
        Assert.AreEqual(lib.Name, next.LibraryName);
        Assert.AreEqual(lib.Uid, next.LibraryUid);
    }
    
    [TestMethod]
    public void GetNextFile_OldestFirst()
    {
        var lib = AllowLibrary(ProcessingOrder.OldestFirst);
        var possibleFiles = Files.Values.Where(x => x.LibraryUid == lib.Uid && x.Status == FileStatus.Unprocessed).ToList(); 
        var oldestFile = possibleFiles.OrderBy(x => x.CreationTime).First();
        ExpectFile(oldestFile);
    }
    
    [TestMethod]
    public void GetNextFile_NewestFirst()
    {
        var lib = AllowLibrary(ProcessingOrder.NewestFirst);
        var possibleFiles = Files.Values.Where(x => x.LibraryUid == lib.Uid && x.Status == FileStatus.Unprocessed).ToList(); 
        var oldestFile = possibleFiles.OrderByDescending(x => x.CreationTime).First();
        ExpectFile(oldestFile);
    }
    
    [TestMethod]
    public void GetNextFile_SmallestFirst()
    {
        var lib = AllowLibrary(ProcessingOrder.SmallestFirst);
        var possibleFiles = Files.Values.Where(x => x.LibraryUid == lib.Uid && x.Status == FileStatus.Unprocessed).ToList(); 
        var oldestFile = possibleFiles.OrderBy(x => x.FinalSize).First();
        ExpectFile(oldestFile);
    }
    
    [TestMethod]
    public void GetNextFile_LargestFirst()
    {
        var lib = AllowLibrary(ProcessingOrder.SmallestFirst);
        var possibleFiles = Files.Values.Where(x => x.LibraryUid == lib.Uid && x.Status == FileStatus.Unprocessed).ToList(); 
        var oldestFile = possibleFiles.OrderByDescending(x => x.FinalSize).First();
        ExpectFile(oldestFile);
    }
    
    [TestMethod]
    public void GetNextFile_AsFound()
    {
        var lib = AllowLibrary(ProcessingOrder.AsFound);
        var possibleFiles = Files.Values.Where(x => x.LibraryUid == lib.Uid && x.Status == FileStatus.Unprocessed).ToList(); 
        var oldestFile = possibleFiles.OrderByDescending(x => x.DateCreated).First();
        ExpectFile(oldestFile);
    }

    private Library AllowLibrary(ProcessingOrder order)
    {
        var lib = Libraries.First(x => x.ProcessingOrder == order);
        InternalNode.Libraries = new List<ObjectReference>
        {
            new () { Uid = lib.Uid, Name = lib.Name, Type = lib.GetType().FullName }
        };
        InternalNode.AllLibraries = ProcessingLibraries.Only;
        return lib;
    }

    private void ExpectFile(LibraryFile expected)
    {
        var service = new FileFlows.Server.Services.LibraryFileService();
        var nextResult = service.GetNext(Globals.InternalNodeName, 
            Globals.InternalNodeUid, Globals.Version.ToString(), Guid.NewGuid()).Result;
        Assert.IsNotNull(nextResult);
        Assert.AreEqual(NextLibraryFileStatus.Success, nextResult.Status);
        Assert.IsNotNull(nextResult.File);
        var next = nextResult.File;
        Assert.AreEqual(expected.LibraryName, next.LibraryName);
        Assert.AreEqual(expected.LibraryUid, next.LibraryUid);
        Assert.AreEqual(expected.Name, next.Name);
        Assert.AreEqual(expected.Uid, next.Uid);
        
    }
}