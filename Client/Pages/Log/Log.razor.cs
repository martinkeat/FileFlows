using System.Text.RegularExpressions;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components;
using System.Timers;
using System.Web;
using FileFlows.Client.Helpers;
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
    private LogFile? ActiveFile;

    /// <summary>
    /// Gets the current file being viewed
    /// </summary>
    private string? CurrentFile;

    /// <summary>
    /// Gets the current log text
    /// </summary>
    private string? CurrentLogText;

    private readonly LogSearchModel SearchModel = new()
    {
        Message = string.Empty,
        Type = LogType.Info,
        TypeIncludeHigherSeverity = true
    };

    protected override void OnInitialized()
    {

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

        var firstKey = LoggingSources.Keys.FirstOrDefault();
        if (string.IsNullOrEmpty(firstKey) == false)
        {
            ActiveFile = LoggingSources[firstKey].First();
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
        if (Searching || ActiveFile?.Active != true)
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
        bool sameFile = CurrentFile == ActiveFile.FileName;
        if (sameFile == false || ActiveFile?.Active == true)
        {
            CurrentFile = ActiveFile.FileName;
            bool nearBottom = sameFile && ActiveFile.Active && LogEntries?.Any() == true && 
                              await jsRuntime.InvokeAsync<bool>("ff.nearBottom", [".log-view .log"]);
            var response = await HttpHelper.Get<string>("/api/fileflows-log/download?source=" +
                                                        HttpUtility.UrlEncode(ActiveFile.FileName));
            if (response.Success)
            {
                if (sameFile && ActiveFile.Active)
                {
                    string log = response.Body.Substring(CurrentLogText.Length).TrimStart();
                    if (string.IsNullOrWhiteSpace(log) == false)
                    {
                        this.LogEntries.AddRange(SplitLog(log));
                    }
                }
                else
                {
                    this.LogEntries = SplitLog(response.Data);
                }
                if(ActiveFile.Active)
                    CurrentLogText = response.Body;
                
                ApplyFilter();

                this.scrollToBottom = forceScrollToBottom || nearBottom;
                this.StateHasChanged();
            }
            else
            {
                LogEntries = new()
                {
                    new()
                    {
                        Date = "",
                        Severity = LogType.Error,
                        Message = response.Body,
                        SeverityText = ""
                    }
                };
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
        bool hasSearchText = string.IsNullOrWhiteSpace(SearchModel.Message) == false;
        FilteredLogEntries = LogEntries.Where(x =>
        {
            if (SearchModel.Type != null && x.Severity != SearchModel.Type)
            {
                if (SearchModel.TypeIncludeHigherSeverity == false)
                    return false;

                if ((int)x.Severity > (int)SearchModel.Type)
                    return false;
            }

            if (hasSearchText == false)
                return true;

            return x.Message.Contains(SearchModel.Message, StringComparison.InvariantCultureIgnoreCase);
        }).ToList();
    }

    /// <summary>
    /// Handles the active file selection change
    /// </summary>
    /// <param name="args">the change event arguments</param>
    private void HandleSelection(ChangeEventArgs args)
    {
        // Find the LogFile object corresponding to the selected ShortName
        ActiveFile = LoggingSources.SelectMany(kv => kv.Value)
            .FirstOrDefault(file => file.FileName == args.Value?.ToString());
    }
    
    /// <summary>
    /// Downloads the log
    /// </summary>
    private async Task DownloadLog()
    {
        var result = await HttpHelper.Get<string>(DownloadUrl + "?source=" + HttpUtility.UrlEncode(ActiveFile.FileName));
        if (result.Success == false)
        {
            Toast.ShowError(Translater.Instant("Pages.Log.Labels.FailedToDownloadLog"));
            return;
        }

        await jsRuntime.InvokeVoidAsync("ff.saveTextAsFile", ActiveFile.FileName, result.Body);
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
                    Severity = entryMatch.Groups[2].Value.Trim() switch
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
