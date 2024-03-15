using System.Text.RegularExpressions;
using System.Timers;
using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;

namespace FileFlows.Client.Pages;

public partial class LibraryFiles : ListPage<Guid, LibaryFileListModel>, IDisposable
{
    private string filter = string.Empty;
    private FileStatus? filterStatus;
    public override string ApiUrl => "/api/library-file";
    [Inject] private INavigationService NavigationService { get; set; }
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] private ClientService ClientService { get; set; }
    
    [Inject] private IJSRuntime jsRuntime { get; set; }

    private FlowSkyBox<FileStatus> Skybox;

    private FileFlows.Shared.Models.FileStatus SelectedStatus;

    private int PageIndex;

    private string lblMoveToTop = "";

    private int Count;
    private string lblSearch, lblDeleteSwitch, lblForcedProcessing;

    private string TableIdentifier => "LibraryFiles_" + this.SelectedStatus; 

    SearchPane SearchPane { get; set; }
    private readonly LibraryFileSearchModel SearchModel = new()
    {
        Path = string.Empty
    };

    private string Title;
    private string lblLibraryFiles, lblFileFlowsServer;
    private int TotalItems;
    private List<FlowExecutorInfo> WorkerStatus = new ();
    private Timer AutoRefreshTimer;

    protected override string DeleteMessage => "Labels.DeleteLibraryFiles";
    
    private async Task SetSelected(FlowSkyBoxItem<FileStatus> status)
    {
        SelectedStatus = status.Value;
        this.PageIndex = 0;
        Title = lblLibraryFiles + ": " + status.Name;
        await this.Refresh();
        this.StateHasChanged();
    }

    public override string FetchUrl => $"{ApiUrl}/list-all?status={SelectedStatus}&page={PageIndex}&pageSize={App.PageSize}" +
                                       $"&filter={Uri.EscapeDataString(filterStatus == SelectedStatus ? filter ?? string.Empty : string.Empty)}";

    private string NameMinWidth = "20ch";

    public override async Task PostLoad()
    {
        if(App.Instance.IsMobile)
            this.NameMinWidth = this.Data?.Any() == true ? Math.Min(120, Math.Max(20, this.Data.Max(x => (x.Name?.Length / 2) ?? 0))) + "ch" : "20ch";
        else
            this.NameMinWidth = this.Data?.Any() == true ? Math.Min(120, Math.Max(20, this.Data.Max(x => (x.Name?.Length) ?? 0))) + "ch" : "20ch";
        await jsRuntime.InvokeVoidAsync("ff.scrollTableToTop");
    }

    protected override async Task PostDelete()
    {
        await RefreshStatus();
    }

    protected async override Task OnInitializedAsync()
    {
        this.SelectedStatus = FileFlows.Shared.Models.FileStatus.Unprocessed;
        lblForcedProcessing = Translater.Instant("Labels.ForceProcessing");
        lblMoveToTop = Translater.Instant("Pages.LibraryFiles.Buttons.MoveToTop");
        lblLibraryFiles = Translater.Instant("Pages.LibraryFiles.Title");
        lblFileFlowsServer = Translater.Instant("Pages.Nodes.Labels.FileFlowsServer");
        Title = lblLibraryFiles + ": " + Translater.Instant("Enums.FileStatus." + FileStatus.Unprocessed);
        this.lblSearch = Translater.Instant("Labels.Search");
        this.lblDeleteSwitch = Translater.Instant("Labels.DeleteLibraryFilesPhysicallySwitch");
        base.OnInitialized(true);
        
        
        AutoRefreshTimer = new Timer();
        AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed;
        AutoRefreshTimer.Interval = 10_000;
        AutoRefreshTimer.AutoReset = false;
        AutoRefreshTimer.Start();
    }

    /// <summary>
    /// Auto refresh timer elapsed
    /// </summary>
    /// <param name="sender">the sender</param>
    /// <param name="e">the event args</param>
    void AutoRefreshTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if(SelectedStatus == FileStatus.Processing)
            _ = Refresh(false);
    }
        

    /// <summary>
    /// Refreshes the page
    /// </summary>
    /// <param name="showBlocker">if the blocker should be shown or not</param>
    public override async Task Refresh(bool showBlocker = true)
    {
        AutoRefreshTimer?.Stop();
        try
        {
            await base.Refresh(showBlocker);
        }
        finally
        {
            AutoRefreshTimer?.Start();
        }
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        if (AutoRefreshTimer != null)
        {
            AutoRefreshTimer.Stop();
            AutoRefreshTimer.Elapsed -= AutoRefreshTimerElapsed;
            AutoRefreshTimer.Dispose();
            AutoRefreshTimer = null;
        }
    }

    private Task<RequestResult<List<LibraryStatus>>> GetStatus() => HttpHelper.Get<List<LibraryStatus>>(ApiUrl + "/status");

    /// <summary>
    /// Refreshes the top status bar
    /// This is needed when deleting items, as the list will not be refreshed, just items removed from it
    /// </summary>
    /// <returns></returns>
    private async Task RefreshStatus()
    {
        var result = await GetStatus();
        if (result.Success)
            RefreshStatus(result.Data.ToList());
    }
    
    private void RefreshStatus(List<LibraryStatus> data)
    {
       var order = new List<FileStatus> { FileStatus.Unprocessed, FileStatus.OutOfSchedule, FileStatus.Processing, FileStatus.Processed, FileStatus.FlowNotFound, FileStatus.ProcessingFailed };
       foreach (var s in order)
       {
           if (data.Any(x => x.Status == s) == false && s != FileStatus.FlowNotFound)
               data.Add(new LibraryStatus { Status = s });
       }

       foreach (var s in data)
           s.Name = Translater.Instant("Enums.FileStatus." + s.Status.ToString());

       var sbItems = new List<FlowSkyBoxItem<FileStatus>>();
       foreach (var status in data.OrderBy(x =>
                {
                    int index = order.IndexOf(x.Status);
                    return index >= 0 ? index : 100;
                }))
       {
           string icon = status.Status switch
           {
               FileStatus.Unprocessed => "far fa-hourglass",
               FileStatus.Disabled => "fas fa-toggle-off",
               FileStatus.Processed => "far fa-check-circle",
               FileStatus.Processing => "fas fa-file-medical-alt",
               FileStatus.FlowNotFound => "fas fa-exclamation",
               FileStatus.ProcessingFailed => "far fa-times-circle",
               FileStatus.OutOfSchedule => "far fa-calendar-times",
               FileStatus.Duplicate => "far fa-copy",
               FileStatus.MappingIssue => "fas fa-map-marked-alt",
               FileStatus.MissingLibrary => "fas fa-trash",
               FileStatus.OnHold => "fas fa-hand-paper",
               FileStatus.ReprocessByFlow => "fas fa-redo",
               _ => ""
           };
           if (status.Status != FileStatus.Unprocessed && status.Status != FileStatus.Processing && status.Status != FileStatus.Processed && status.Count == 0)
               continue;
           sbItems.Add(new ()
           {
               Count = status.Count,
               Icon = icon,
               Name = status.Name,
               Value = status.Status
           });
        }

        Skybox.SetItems(sbItems, SelectedStatus);
        this.Count = sbItems.Where(x => x.Value == SelectedStatus).Select(x => x.Count).FirstOrDefault();
        this.StateHasChanged();
    }

    /// <summary>
    /// Refreshes the worker status
    /// </summary>
    private async Task RefreshWorkerStatus()
    {
        this.WorkerStatus = await ClientService.GetExecutorInfo();
        if(this.SelectedStatus == FileStatus.Processing)
            this.StateHasChanged();
    }

    public override async Task<bool> Edit(LibaryFileListModel item)
    {
        await Helpers.LibraryFileEditor.Open(Blocker, Editor, item.Uid);
        return false;
    }

    public async Task MoveToTop()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to move

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/move-to-top", new ReferenceModel<Guid> { Uids = uids });                
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }
    
    public async Task Cancel()
    {
        var selected = Table.GetSelected().ToArray();
        if (selected.Length == 0)
            return; // nothing to cancel

        if (await Confirm.Show("Labels.Cancel",
            Translater.Instant("Labels.CancelItems", new { count = selected.Length })) == false)
            return; // rejected the confirm

        Blocker.Show();
        this.StateHasChanged();
        try
        {
            foreach(var item in selected)
                await HttpHelper.Delete($"/api/worker/by-file/{item.Uid}");

        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
        await Refresh();
    }


    public async Task ForceProcessing()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to reprocess

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/force-processing", new ReferenceModel<Guid> { Uids = uids });
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }
    
    public async Task Reprocess()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to reprocess

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/reprocess", new ReferenceModel<Guid> { Uids = uids });
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }

    protected override async Task<RequestResult<List<LibaryFileListModel>>> FetchData()
    {
        var request = await HttpHelper.Get<LibraryFileDatalistModel>(FetchUrl);

        if (request.Success == false)
        {
            return new RequestResult<List<LibaryFileListModel>>
            {
                Body = request.Body,
                Success = request.Success
            };
        }

        await RefreshWorkerStatus();
        RefreshStatus(request.Data?.Status?.ToList() ?? new List<LibraryStatus>());

        if (request.Headers.ContainsKey("x-total-items") &&
            int.TryParse(request.Headers["x-total-items"], out int totalItems))
        {
            Logger.Instance.ILog("### Total items from header: " + totalItems);
            this.TotalItems = totalItems;
        }
        else
        {
            var status = Skybox.SelectedItem;
            this.TotalItems = status?.Count ?? 0;
        }
        
        var result = new RequestResult<List<LibaryFileListModel>>
        {
            Body = request.Body,
            Success = request.Success,
            Data = request.Data.LibraryFiles.ToList()
        };
        Logger.Instance.ILog("FetchData: " + result.Data.Count);
        return result;
    }

    private async Task PageChange(int index)
    {
        PageIndex = index;
        await this.Refresh();
    }

    private async Task PageSizeChange(int size)
    {
        this.PageIndex = 0;
        await this.Refresh();
    }

    private async Task OnFilter(FilterEventArgs args)
    {
        if (this.filter?.EmptyAsNull() == args.Text?.EmptyAsNull())
        {
            this.filter = string.Empty;
            this.filterStatus = null;
            return;
        }

        int totalItems = Skybox.SelectedItem.Count;
        if (totalItems <= args.PageSize)
            return;
        this.filterStatus = this.SelectedStatus;
        // need to filter on the server side
        args.Handled = true;
        args.PageIndex = 0;
        this.PageIndex = 0;
        this.filter = args.Text;
        await this.Refresh();
        this.filter = args.Text; // ensures refresh didnt change the filter
    }

    /// <summary>
    /// Sets the table data, virtual so a filter can be set if needed
    /// </summary>
    /// <param name="data">the data to set</param>
    protected override void SetTableData(List<LibaryFileListModel> data)
    {
        if(string.IsNullOrWhiteSpace(this.filter) || SelectedStatus  != filterStatus)
            Table.SetData(data);
        else
            Table.SetData(data, filter: this.filter); 
    }

    private async Task Rescan()
    {
        this.Blocker.Show("Scanning Libraries");
        try
        {
            await HttpHelper.Post("/api/library/rescan-enabled");
            await Refresh();
            Toast.ShowSuccess(Translater.Instant("Pages.LibraryFiles.Labels.ScanTriggered"));
        }
        finally
        {
            this.Blocker.Hide();   
        }
    }

    private async Task Unhold()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to unhold

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/unhold", new ReferenceModel<Guid> { Uids = uids });
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }
    
    private async Task ToggleForce()
    {
        var selected = Table.GetSelected();
        var uids = selected.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing 

        Blocker.Show();
        try
        {
            await HttpHelper.Post(ApiUrl + "/toggle-force", new ReferenceModel<Guid> { Uids = uids });
        }
        finally
        {
            Blocker.Hide();
        }
        await Refresh();
    }
    
    Task Search() => NavigationService.NavigateTo("/library-files/search");


    async Task DeleteFile()
    {
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to delete
        var msg = Translater.Instant("Labels.DeleteLibraryFilesPhysicallyMessage", new { count = uids.Length });
        if ((await Confirm.Show("Labels.Delete", msg, switchMessage: lblDeleteSwitch, switchState: false, requireSwitch:true)).Confirmed == false)
            return; // rejected the confirm
        
        
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var deleteResult = await HttpHelper.Delete("/api/library-file/delete-files", new ReferenceModel<Guid> { Uids = uids });
            if (deleteResult.Success == false)
            {
                if(Translater.NeedsTranslating(deleteResult.Body))
                    Toast.ShowError( Translater.Instant(deleteResult.Body));
                else
                    Toast.ShowError( Translater.Instant("ErrorMessages.DeleteFailed"));
                return;
            }
            
            this.Data = this.Data.Where(x => uids.Contains(x.Uid) == false).ToList();

            await PostDelete();
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    async Task DownloadFile()
    {
        var file = Table.GetSelected()?.FirstOrDefault();
        if (file == null)
            return; // nothing to delete
        
        string url = "/api/library-file/download/" + file.Uid;
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        
        var apiResult = await HttpHelper.Get<string>($"{url}?test=true");
        if (apiResult.Success == false)
        {
            Toast.ShowError(apiResult.Body?.EmptyAsNull() ?? apiResult.Data?.EmptyAsNull() ?? "Failed to download.");
            return;
        }
        
        string name = file.Name.Replace("\\", "/");
        name = name.Substring(name.LastIndexOf("/", StringComparison.Ordinal) + 1);
        await jsRuntime.InvokeVoidAsync("ff.downloadFile", url, name);
    }

    async Task SetStatus(FileStatus status)
    {
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to mark
        
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var apiResult = await HttpHelper.Post($"/api/library-file/set-status/{status}", new ReferenceModel<Guid> { Uids = uids });
            if (apiResult.Success == false)
            {
                if(Translater.NeedsTranslating(apiResult.Body))
                    Toast.ShowError( Translater.Instant(apiResult.Body));
                else
                    Toast.ShowError( Translater.Instant("ErrorMessages.SetFileStatus"));
                return;
            }
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    static string[] IconTypes = new [] { "3g2", "3ga", "3gp", "7z", "aac", "aa", "accdb", "accdt", "ac", "adn", "aifc", "aiff", "aif", "ai", "ait", "amr", "ani", "apk", "applescript", "app", "asax", "asc", "ascx", "asf", "ash", "ashx", "asmx", "asp", "aspx", "asx", "aup", "au", "avi", "axd", "aze", "bak", "bash", "bat", "bin", "bmp", "bowerrc", "bpg", "browser", "bz2", "cab", "cad", "caf", "cal", "catalog", "cd", "cer", "cfg", "cfml", "cfm", "cgi", "class", "cmd", "codekit", "coffeelintignore", "coffee", "compile", "com", "config", "conf", "cpp", "cptx", "cr2", "crdownload", "crt", "crypt", "csh", "cson", "csproj", "css", "cs", "c", "csv", "cue", "dat", "dbf", "db", "deb", "dgn", "dist", "diz", "dll", "dmg", "dng", "docb", "docm", "doc", "docx", "dotm", "dot", "dotx", "download", "dpj", "ds_store", "dtd", "dwg", "dxf", "editorconfig", "el", "enc", "eot", "eps", "epub", "eslintignore", "exe", "f4v", "fax", "fb2", "filenames", "flac", "fla", "flv", "gadget", "gdp", "gem", "gif", "gitattributes", "gitignore", "go", "gpg", "gz", "handlebars", "hbs", "heic", "hsl", "hs", "h", "html", "htm", "ibooks", "icns", "ico", "ics", "idx", "iff", "ifo", "image", "img", "indd", "inf", "ini", "in", "iso", "j2", "jar", "java", "jpeg", "jpe", "jpg", "json", "jsp", "js", "jsx", "key", "kf8", "kmk", "ksh", "kup", "less", "lex", "licx", "lisp", "lit", "lnk", "lock", "log", "lua", "m2v", "m3u8", "m3u", "m4a", "m4r", "m4", "m4v", "map", "master", "mc", "mdb", "mdf", "md", "me", "midi", "mid", "mi", "mk", "mkv", "mm", "mobi", "mod", "mo", "mov", "mp2", "mp3", "mp4", "mpa", "mpd", "mpeg", "mpe", "mpga", "mpg", "mpp", "mpt", "msi", "msu", "m", "nef", "nes", "nfo", "nix", "npmignore", "odb", "ods", "odt", "ogg", "ogv", "ost", "otf", "ott", "ova", "ovf", "p12", "p7b", "pages", "part", "pcd", "pdb", "pdf", "pem", "pfx", "pgp", "phar", "php", "ph", "pkg", "plist", "pl", "pm", "png", "pom", "po", "pot", "potx", "pps", "ppsx", "pptm", "ppt", "pptx", "prop", "ps1", "psd", "psp", "ps", "pst", "pub", "pyc", "py", "qt", "ram", "rar", "ra", "raw", "rb", "rdf", "resx", "retry", "rm", "rom", "rpm", "rsa", "rss", "rtf", "rub", "ru", "sass", "scss", "sdf", "sed", "sh", "sitemap", "skin", "sldm", "sldx", "sln", "sol", "sqlite", "sql", "step", "stl", "svg", "swd", "swf", "swift", "sys", "tar", "tcsh", "tex", "tfignore", "tga", "tgz", "tiff", "tif", "tmp", "torrent", "ts", "tsv", "ttf", "twig", "txt", "udf", "vbproj", "vbs", "vb", "vcd", "vcs", "vdi", "vdx", "vmdk", "vob", "vscodeignore", "vsd", "vss", "vst", "vsx", "vtx", "war", "wav", "wbk", "webinfo", "webm", "webp", "wma", "wmf", "wmv", "woff2", "woff", "wps", "wsf", "xaml", "xcf", "xlm", "xlsm", "xls", "xlsx", "xltm", "xlt", "xltx", "xml", "xpi", "xps", "xrb", "xsd", "xsl", "xspf", "xz", "yaml", "yml", "zip", "zsh", "z" };

    private static string[] BasicExtensions = new[] { "doc", "iso", "pdf", "svg", "xml", "zip" };
    private static string[] VideoExtensions = new[] { "avi", "mkv", "mov", "mp4", "mpeg", "mpg", "ts", "webm" };
    private static string[] ImageExtensions = new[] { "bmp", "gif", "gif", "jpg", "png", "tiff", "webp" };
    private static string[] TextExtensions = new[] { "srt", "sub", "sup", "txt" };
    private static string[] ComicExtensions = new[] { "cb7", "cbr", "cbz" };

    /// <summary>
    /// Gets the image for the file
    /// </summary>
    /// <param name="path">the path of the file</param>
    /// <returns>the image to show</returns>
    private string GetExtensionImage(string path)
    {
        int index = path.LastIndexOf(".", StringComparison.Ordinal);
        if (index < 0)
            return "";
        
        
        string extension = path[(index + 1)..].ToLowerInvariant();
        if (BasicExtensions.Contains(extension))
            return $"filetypes/{extension}.svg";
        if (ComicExtensions.Contains(extension))
            return $"filetypes/comic/{extension}.svg";
        if (ImageExtensions.Contains(extension))
            return $"filetypes/image/{extension}.svg";
        if (TextExtensions.Contains(extension))
            return $"filetypes/text/{extension}.svg";
        if (VideoExtensions.Contains(extension))
            return $"filetypes/video/{extension}.svg";
        
        return "blank.svg";
    }
}