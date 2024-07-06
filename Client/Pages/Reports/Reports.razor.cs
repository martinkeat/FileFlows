using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for reports
/// </summary>
public partial class Reports : ListPage<Guid, ReportDefinition>
{
    /// <summary>
    /// Gets or sets the report form editor component
    /// </summary>
    private Editor ReportFormEditor { get; set; }
    
    /// <inheritdoc />
    public override string ApiUrl => "/api/report";

    /// <inheritdoc />
    public override string FetchUrl => $"{ApiUrl}/definitions";
    
    /// <summary>
    /// Launches the report
    /// </summary>
    /// <param name="rd">the report definition</param>
    private Task Launch(ReportDefinition rd)
        => Edit(rd);

    /// <inheritdoc />
    public override Task<bool> Edit(ReportDefinition rd)
    {
        NavigationManager.NavigateTo($"/report/{rd.Uid}");
        return Task.FromResult(true);
    }
}