using System.Runtime.InteropServices;
using FileFlows.Server.Controllers;
using FileFlows.Server.Services;
using FileFlows.ServerShared.Workers;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
#if(!DEBUG)
using System.Runtime.InteropServices;
using FileFlows.Server.Controllers;
using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;
#endif

namespace FileFlows.Server.Workers;

public class TelemetryReporter: Worker
{
    public TelemetryReporter() : base(ScheduleType.Daily, 5)
    {
        Trigger();
    }

    protected override void Execute()
    {
        if (Environment.GetEnvironmentVariable("DevBox") == "1")
            return;
        try
        {
#if (DEBUG)
            return;
#else
        var settings = new SettingsController().Get().Result;
        if (settings?.DisableTelemetry == true)
            return; // they have turned it off, dont report anything

        bool isDocker = Program.Docker;
        bool isMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        bool isWindows = !isDocker && !isMacOs && !isLinux;

        TelemetryData data = new TelemetryData();
        data.ClientUid = settings.Uid;
        data.Version = Globals.Version.ToString();
        data.ProcessingNodes = new NodeService().GetAll().Count();
        data.Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
        data.OS = isDocker ? "Docker" :
                  isMacOs ? "MacOS" :
                  isLinux ? "Linux" :
                  "Windows";
        var libFiles = new LibraryFileController().GetAll(null).Result;
        data.FilesFailed = libFiles.Where(x => x.Status == FileStatus.ProcessingFailed).Count();
        data.FilesProcessed = libFiles.Where(x => x.Status == FileStatus.Processed).Count();
        var flows = new FlowService().GetAll();
        var dictNodes = new Dictionary<string, int>();
        foreach(var fp in flows?.SelectMany(x => x.Parts)?.ToArray() ?? new FlowPart[] { })
        {
            if (fp == null)
                continue;
            if (dictNodes.ContainsKey(fp.FlowElementUid))
                dictNodes[fp.FlowElementUid] = dictNodes[fp.FlowElementUid] + 1;
            else
                dictNodes.Add(fp.FlowElementUid, 1);
        }
        data.Nodes = dictNodes.Select(x => new TelemetryDataSet
        {
            Name = x.Key,
            Count = x.Value
        }).ToList();

        var libraries = new LibraryService().GetAll();
        dictNodes.Clear();
        foreach(var lib in libraries?.Where(x => string.IsNullOrEmpty(x.Template) == false) ?? new List<Library>())
        {
            if (dictNodes.ContainsKey(lib.Template))
                dictNodes[lib.Template] = dictNodes[lib.Template] + 1;
            else
                dictNodes.Add(lib.Template, 1);
        }
        data.LibraryTemplates = dictNodes.Select(x => new TelemetryDataSet
        {
            Name = x.Key,
            Count = x.Value
        }).ToList();


        dictNodes.Clear();
        foreach (var lib in flows?.Where(x => string.IsNullOrEmpty(x.Template) == false) ?? new List<Flow>())
        {
            if (dictNodes.ContainsKey(lib.Template))
                dictNodes[lib.Template] = dictNodes[lib.Template] + 1;
            else
                dictNodes.Add(lib.Template, 1);
        }
        data.FlowTemplates = dictNodes.Select(x => new TelemetryDataSet
        {
            Name = x.Key,
            Count = x.Value
        }).ToList();

#if(DEBUG)
        var task = HttpHelper.Post("https://localhost:7197/api/telemetry", data);
#else
        var task = HttpHelper.Post("https://fileflows.com/api/telemetry", data);
        
#endif
        task.Wait();

#endif
        }
        catch (Exception)
        {
            // FF-410: silent fail, may not have an internet connection
        }
    }



    public class TelemetryData
    {
        public Guid ClientUid { get; set; }

        public string Version { get; set; }

        public string OS { get; set; }
        public string Architecture { get; set; }
        public int ProcessingNodes { get; set; }

        public List<TelemetryDataSet> Nodes { get; set; }
        public List<TelemetryDataSet> LibraryTemplates { get; set; }
        public List<TelemetryDataSet> FlowTemplates { get; set; }

        public int FilesProcessed { get; set; }
        public int FilesFailed { get; set; }

    }

    public class TelemetryDataSet
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}
