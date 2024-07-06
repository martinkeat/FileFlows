using System.Text.Json;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.Shared.Validators;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// The editor for a report
/// </summary>
public partial class Reporting
{
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