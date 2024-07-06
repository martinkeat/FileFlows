using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for reports
/// </summary>
public partial class Reporting  : ComponentBase
{
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] public NavigationManager NavigationManager { get; set; }

    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] protected ProfileService ProfileService { get; set; }

    /// <summary>
    /// If scheduled reports is selected
    /// </summary>
    private bool ScheduledReportsSelected;
    
    /// <summary>
    /// The sky box items
    /// </summary>
    private List<FlowSkyBoxItem<bool>> SkyboxItems;
    

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var profile = await ProfileService.Get();
        if (profile.LicensedFor(LicenseFlags.Reporting) == false)
        {
            NavigationManager.NavigateTo("/");
            return;
        }
        
        SkyboxItems = new()
        {
            new()
            {
                Name = Translater.Instant("Pages.Reporting.Labels.Reports"),
                Value = false,
                Icon = "fas fa-chart-pie"
            },
            new()
            {
                Name = Translater.Instant("Pages.Reporting.Labels.ScheduledReports"),
                Value = true,
                Icon = "fas fa-clock"
            },
        };
    }
    
    
    private void SetSelected(FlowSkyBoxItem<bool> item)
    {
        ScheduledReportsSelected = item.Value;
        this.StateHasChanged();
    }
}