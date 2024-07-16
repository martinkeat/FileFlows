using System.Text.RegularExpressions;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using System.Timers;
using System.Web;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Log Page
/// </summary>
public partial class Log : ComponentBase
{
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] protected IJSRuntime jsRuntime { get; set; }
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] NavigationManager NavigationManager { get; set; }
    private string lblDownload, lblSearch, lblSearching;
    private string DownloadUrl;
    private bool scrollToBottom = false;

    private bool Searching = false;
    
    private Timer AutoRefreshTimer;
    
    private Dictionary<string, List<LogFile>> LoggingSources = new ();
    /// <summary>
    /// The selected source/key from the Logging Sources
    /// </summary>
    private string SelectedSource { get; set; }
    /// <summary>
    /// Gets or sets the log entries in the current log file being viewed
    /// </summary>
    private List<LogEntry> LogEntries { get; set; } = new();
    /// <summary>
    /// Gets or sets the log entries in the current log file being viewed
    /// </summary>
    private List<LogEntry> FilteredLogEntries { get; set; } = new();

    /// <summary>
    /// The active log file
    /// </summary>
    private LogFile? SearchFile;

    /// <summary>
    /// Gets the current file being viewed
    /// </summary>
    private string? CurrentFile;

    /// <summary>
    /// Gets the current log text
    /// </summary>
    private string? CurrentLogText;

    /// <summary>
    /// Gets or sets the search text
    /// </summary>
    private string SearchText { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets if higher severity messages should be included
    /// </summary>
    public bool SearchIncludeHigherSeverity { get; set; } = true;
    /// <summary>
    /// Gets or sets the search type
    /// </summary>
    public LogType SearchType { get; set; } = LogType.Info;

    /// <summary>
    /// The active search model
    /// </summary>
    private LogSearchModel ActiveSearchModel;

    /// <summary>
    /// The error message if the search failed
    /// </summary>
    private string? ErrorMessage;

    /// <summary>
    /// If there is an error
    /// </summary>
    public bool HasError = false;
    
    protected override void OnInitialized()
    {
        ActiveSearchModel = new()
        {
            Message = SearchText,
            Type = SearchType,
            TypeIncludeHigherSeverity = SearchIncludeHigherSeverity
        };
        this.lblSearch = Translater.Instant("Labels.Search");
        this.lblSearching = Translater.Instant("Labels.Searching");
        this.lblDownload = Translater.Instant("Labels.Download");
#if (DEBUG)
        this.DownloadUrl = "http://localhost:6868/api/fileflows-log/download";
#else
        this.DownloadUrl = "/api/fileflows-log/download";
#endif
        _ = Initialise();
    }
    
    async Task Initialise()
    {
        Blocker.Show();
        LoggingSources = (await HttpHelper.Get<Dictionary<string, List<LogFile>>>("/api/fileflows-log/log-sources")).Data;

        SelectedSource = LoggingSources.Keys.FirstOrDefault();
        if (string.IsNullOrEmpty(SelectedSource) == false)
        {
            ActiveSearchModel.ActiveFile = LoggingSources[SelectedSource].First();
            SearchFile = LoggingSources[SelectedSource].First();
        }
        NavigationManager.LocationChanged += NavigationManager_LocationChanged!;
        
        await Refresh(true);
        
        AutoRefreshTimer = new Timer();
        AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed!;
        AutoRefreshTimer.Interval = 5_000;
        AutoRefreshTimer.AutoReset = true;
        AutoRefreshTimer.Start();
        
        Blocker.Hide();
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(100); // 100ms
                await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", [".log-view .log", true]);
                await Task.Delay(400); // 500ms
                await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", [".log-view .log", true]);
                await Task.Delay(200); // 700ms
                await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", [".log-view .log", true]);
                await Task.Delay(300); // 1second
                await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", [".log-view .log", true]);
            });
        }
        if (scrollToBottom)
        {
            await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", [".log-view .log"]);
            scrollToBottom = false;
        }
    }

    private void NavigationManager_LocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        Dispose();
    }

    /// <summary>
    /// Disposes of this page and its timer
    /// </summary>
    public void Dispose()
    {
        if (AutoRefreshTimer != null)
        {
            AutoRefreshTimer.Stop();
            AutoRefreshTimer.Elapsed -= AutoRefreshTimerElapsed!;
            AutoRefreshTimer.Dispose();
            AutoRefreshTimer = null;
        }
    }
    
    /// <summary>
    /// The timer elapsed
    /// </summary>
    /// <param name="sender">the sender who triggered this timer</param>
    /// <param name="e">the event arguments</param>
    void AutoRefreshTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (Searching || ActiveSearchModel.ActiveFile?.Active != true)
            return;
        
        _ = Refresh();
    }

    /// <summary>
    /// Performs a search
    /// </summary>
    async Task Search()
    {
        this.Searching = true;
        try
        {
            ActiveSearchModel.Message = SearchText;
            ActiveSearchModel.Type = SearchType;
            ActiveSearchModel.TypeIncludeHigherSeverity = SearchIncludeHigherSeverity;
            ActiveSearchModel.ActiveFile = SearchFile;
            
            Blocker.Show(lblSearching);
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
            this.Searching = false;
            StateHasChanged();
        }
    }

    async Task Refresh(bool forceScrollToBottom = false)
    {
        bool sameFile = CurrentFile == ActiveSearchModel.ActiveFile.FileName;
        if (sameFile == false || SearchFile?.Active == true)
        {
            HasError = false;
            ErrorMessage = null;
            CurrentFile = ActiveSearchModel.ActiveFile.FileName;
            bool nearBottom = sameFile && ActiveSearchModel.ActiveFile.Active && LogEntries?.Any() == true && 
                              await jsRuntime.InvokeAsync<bool>("ff.nearBottom", [".log-view .log"]);
            var response = await HttpHelper.Get<string>("/api/fileflows-log/download?source=" +
                                                        HttpUtility.UrlEncode(ActiveSearchModel.ActiveFile.FileName));
            if (response.Success)
            {
                if (sameFile && ActiveSearchModel.ActiveFile.Active)
                {
                    string log = response.Body[CurrentLogText.Length..].TrimStart();
                    if (string.IsNullOrWhiteSpace(log) == false)
                    {
                        this.LogEntries.AddRange(SplitLog(log));
                    }
                }
                else
                {
                    this.LogEntries = SplitLog(response.Data);
                }
                if(ActiveSearchModel.ActiveFile.Active)
                    CurrentLogText = response.Body;
                
                ApplyFilter();

                this.scrollToBottom = forceScrollToBottom || nearBottom;
                this.StateHasChanged();
            }
            else
            {
                HasError = false;
                ErrorMessage = response.Body;
            }
        }
        else
        {
            ApplyFilter();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Applies the filter
    /// </summary>
    private void ApplyFilter()
    {
        bool hasSearchText = string.IsNullOrWhiteSpace(ActiveSearchModel.Message) == false;
        FilteredLogEntries = LogEntries.Where(x =>
        {
            if (x.Severity != ActiveSearchModel.Type)
            {
                if (ActiveSearchModel.TypeIncludeHigherSeverity == false)
                    return false;

                if ((int)x.Severity > (int)ActiveSearchModel.Type)
                    return false;
            }

            if (hasSearchText == false)
                return true;

            return x.Message.Contains(ActiveSearchModel.Message, StringComparison.InvariantCultureIgnoreCase);
        }).ToList();
    }

    /// <summary>
    /// Handles the active file selection change
    /// </summary>
    /// <param name="args">the change event arguments</param>
    private void HandleSelection(ChangeEventArgs args)
    {
        // Find the LogFile object corresponding to the selected ShortName
        SearchFile = LoggingSources.SelectMany(kv => kv.Value)
            .FirstOrDefault(file => file.FileName == args.Value?.ToString());
    }
    
    /// <summary>
    /// Downloads the log
    /// </summary>
    private async Task DownloadLog()
    {
        var result = await HttpHelper.Get<string>(DownloadUrl + "?source=" + HttpUtility.UrlEncode(SearchFile.FileName));
        if (result.Success == false)
        {
            Toast.ShowError(Translater.Instant("Pages.Log.Labels.FailedToDownloadLog"));
            return;
        }

        await jsRuntime.InvokeVoidAsync("ff.saveTextAsFile", SearchFile.FileName, result.Body);
    }
    
    /// <summary>
    /// Splits the log string into individual log entries.
    /// Each log entry includes the date, severity, and message.
    /// </summary>
    /// <param name="log">The complete log string to split.</param>
    /// <returns>A list of LogEntry objects representing individual log entries.</returns>
    public List<LogEntry> SplitLog(string log)
    {
        var logEntries = new List<LogEntry>();

        // Regex patterns
        var messagePattern = @"^\s*(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} \[[A-Z]+\] .*)$";
        var entryPattern = @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[([A-Z]+)\] (.*)$";

        var messageRegex = new Regex(messagePattern, RegexOptions.Multiline);
        var entryRegex = new Regex(entryPattern);

        // Use Regex.Matches to find all log messages
        var messageMatches = messageRegex.Matches(log);
        foreach (Match messageMatch in messageMatches)
        {
            var message = messageMatch.Groups[1].Value.Trim();

            // Use entry regex to extract date, severity, and message
            var entryMatch = entryRegex.Match(message);
            if (entryMatch.Success)
            {
                var entry = new LogEntry
                {
                    Date = entryMatch.Groups[1].Value.Trim()[11..], // remove date from string, only show time
                    Severity = entryMatch.Groups[2].Value.Trim().ToLowerInvariant() switch
                    {
                        "errr" => LogType.Error,
                        "warn" => LogType.Warning,
                        "dbug" => LogType.Debug,
                        _ => LogType.Info  
                    },
                    SeverityText = entryMatch.Groups[2].Value.Trim(),
                    Message = entryMatch.Groups[3].Value.Trim()
                };
                logEntries.Add(entry);
            }
        }

        return logEntries;
    }
    
    /// <summary>
    /// Represents a log entry containing date, severity, and message.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets or sets the date and time of the log entry.
        /// </summary>
        public string Date { get; init; }

        /// <summary>
        /// Gets or sets the severity level of the log entry.
        /// </summary>
        public LogType Severity { get; init; }
        
        /// <summary>
        /// Gets or sets the severity text label
        /// </summary>
        public string SeverityText { get; init; }

        /// <summary>
        /// Gets or sets the message content of the log entry.
        /// </summary>
        public string Message { get; init; }
    }
    
    
}




/// <summary>
/// A model used to search the log 
/// </summary>
public class LogSearchModel
{
    /// <summary>
    /// Gets or sets the file being searched
    /// </summary>
    public LogFile ActiveFile { get; set; }
    /// <summary>
    /// Gets or sets what to search for in the message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets what log type to search for
    /// </summary>
    public LogType Type { get; set; }
    
    /// <summary>
    /// Gets or sets if the search results should include log messages greater than the specified type
    /// </summary>
    public bool TypeIncludeHigherSeverity { get; set; }
}