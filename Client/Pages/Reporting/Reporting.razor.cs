using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for reports
/// </summary>
public partial class Reporting  : ListPage<Guid, ReportDefinition>
{
    /// <summary>
    /// Gets or sets the JS Runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the report form editor component
    /// </summary>
    private Editor ReportFormEditor { get; set; }
    
    /// <inheritdoc />
    public override string ApiUrl => "/api/report";

    /// <inheritdoc />
    public override string FetchUrl => $"{ApiUrl}/definitions";

    /// <inheritdoc />
    protected override bool Licensed()
        => Profile.LicensedFor(LicenseFlags.Reporting);
    
    /// <summary>
    /// Reference to JS Report class
    /// </summary>
    private IJSObjectReference jsReports;


    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var jsObjectReference = await jsRuntime.InvokeAsync<IJSObjectReference>("import", $"./Pages/Reporting/Reporting.razor.js?v={Globals.Version}");
        jsReports = await jsObjectReference.InvokeAsync<IJSObjectReference>("createReporting", [DotNetObjectReference.Create(this)]);
    }
}