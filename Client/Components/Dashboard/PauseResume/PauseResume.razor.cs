using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dashboard;

public partial class PauseResume: IDisposable
{
    /// <summary>
    /// Gets or sets the paused service instance
    /// </summary>
    [Inject] private IPausedService PausedService { get; set; }
    
    private SystemInfo SystemInfo = new ();
    
    private string lblPauseLabel;

    protected override void OnInitialized()
    {
        lblPauseLabel = PausedService.PausedLabel;
        PausedService.OnPausedLabelChanged += PausedServiceOnOnPausedLabelChanged;
    }


    public void Dispose()
    {
        PausedService.OnPausedLabelChanged -= PausedServiceOnOnPausedLabelChanged;
    }

    private void PausedServiceOnOnPausedLabelChanged(string label)
    {
        if (lblPauseLabel == label)
            return;
        
        lblPauseLabel = label;
        StateHasChanged();
    }

    private async Task TogglePaused()
    {
        if (PausedService.IsPaused)
            await PausedService.Resume();
        else
            await PausedService.Pause();
    }
}