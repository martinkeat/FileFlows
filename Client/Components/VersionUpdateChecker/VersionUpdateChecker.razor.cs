using Microsoft.AspNetCore.Components;
using FileFlows.Client.Shared;
using System.Timers;
using System.Threading.Tasks;
using System;

namespace FileFlows.Client.Components;

/// <summary>
/// Component that checks for an updated version
/// </summary>
public partial class VersionUpdateChecker : IDisposable
{
    /// <summary>
    /// Gets or sets if the version update notification is dismissed
    /// </summary>
    private bool Dismissed { get; set; }
    /// <summary>
    /// Get or sets if there is an update available
    /// </summary>
    private bool UpdateAvailable { get; set; }
    /// <summary>
    /// Gets or sets the current latest version available
    /// </summary>
    private Version LatestVersion { get; set; }
    /// <summary>
    /// Timer to auto recheck for a new version
    /// </summary>
    private Timer AutoRefreshTimer;
    /// <summary>
    /// Labels for the update available
    /// </summary>
    private string lblUpdateAvailable, lblUpdateAvailableSuffix;
    
    /// <summary>
    /// Gets or sets the Local Storage instance
    /// </summary>
    [Inject] private FFLocalStorageService LocalStorage { get; set; }


    /// <summary>
    /// Initializes this component
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        this.LatestVersion = new Version(Globals.Version);
        AutoRefreshTimer = new Timer();
        AutoRefreshTimer.Elapsed += AutoRefreshTimerElapsed!;
        AutoRefreshTimer.Interval = 3600 * 1000; // once an hour, dont need to hammer it
        AutoRefreshTimer.AutoReset = true;
        AutoRefreshTimer.Start();
        await Refresh();
    }
    
    /// <summary>
    /// Disposes of this component
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
    /// Called when the timer elapses
    /// </summary>
    /// <param name="sender">the sender</param>
    /// <param name="e">the timer event args</param>
    void AutoRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        => _ = Refresh();

    /// <summary>
    /// Refreshes the latest version available
    /// </summary>
    private async Task Refresh()
    {
        var result = await HttpHelper.Get<string>("/api/settings/check-update-available");
        if (result.Success == false || string.IsNullOrWhiteSpace(result.Data))
            return;
        try
        {
            // new version 
            LatestVersion = new Version(result.Data);
            Dismissed = await IsVersionDismissed(LatestVersion);
            UpdateAvailable = true;

            string versionString = LatestVersion.ToString();
            string lbl = Translater.Instant("Labels.UpdateAvailable", new { version = versionString });
            int index = lbl.IndexOf(versionString);
            if (lbl.EndsWith(versionString))
            {
                lblUpdateAvailable = lbl.Substring(0, index);
                lblUpdateAvailableSuffix = String.Empty;
            } 
            else
            {
                this.lblUpdateAvailable = lbl.Substring(0, index);
                this.lblUpdateAvailableSuffix = lbl.Substring(index + versionString.Length);
            }
            this.StateHasChanged();
        }                
        catch (Exception) { }
    }

    /// <summary>
    /// Dismissed the version update
    /// </summary>
    void Dismiss()
    {
        _ = LocalStorage.SetItemAsync("DismissedVersion", LatestVersion.ToString());
        this.Dismissed = true;
        this.StateHasChanged();
    }

    /// <summary>
    /// Checks if the version is dismissed
    /// </summary>
    /// <param name="version">the version number to check</param>
    /// <returns></returns>
    private async Task<bool> IsVersionDismissed(Version version)
    {
        var dismissed = await LocalStorage.GetItemAsync<string>("DismissedVersion");
        return dismissed == version.ToString();
    }

}