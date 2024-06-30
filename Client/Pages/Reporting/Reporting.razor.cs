using FileFlows.Client.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for reports
/// </summary>
public partial class Reporting  : ListPage<Guid, ReportDefinition>
{
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
}