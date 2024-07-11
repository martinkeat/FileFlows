using System.Text;
using FileFlows.DataLayer.Reports.Helpers;

namespace FileFlows.DataLayer.Reports;

/// <summary>
/// Builds a reports HTML
/// </summary>
/// <param name="emailing">if this report is being emailed</param>
public class ReportBuilder(bool emailing)
{
    /// <summary>
    /// The string builder
    /// </summary>
    private StringBuilder _builder = new();

    /// <summary>
    /// Styling for email titles
    /// </summary>
    public const string EmailTitleStyling = "font-weight:600;font-size:16px";

    /// <summary>
    /// The background color of the box
    /// </summary>
    public const string BoxBackground = "#222426";


    // /// <summary>
    // /// Appends a line to the report
    // /// </summary>
    // /// <param name="text">the text to add</param>
    // public void AppendLine(string text)
    //     => _builder.AppendLine(text);

    /// <summary>
    /// The current number of columns in the row, used for emailing
    /// </summary>
    private int currentRowColumnCount = 0;

    /// <summary>
    /// Starts a row
    /// </summary>
    /// <param name="columns">the number of columns</param>
    /// <param name="className">Optional class name to add to the table when emailing</param>
    public void StartRow(int columns, string? className = null)
    {
        currentRowColumnCount = columns;
        if(emailing == false)
            _builder.AppendLine($"<div class=\"report-row report-row-{columns}\">");
        else 
            _builder.AppendLine($@"
<table class=""{(className ?? string.Empty)}"" style=""width:100%;table-layout: fixed;"" cellpadding=""0"" cellspacing=""10"" border=""0"">
    <tr>");
    }

    /// <summary>
    /// Ends a row
    /// </summary>
    public void EndRow()
    {
        currentRowColumnCount = 0;
        if (emailing == false)
            _builder.AppendLine("</div>");
        else
            _builder.AppendLine($@"
    </tr>
</table>");
    }

    /// <summary>
    /// Adds a row item to a row
    /// </summary>
    /// <param name="html">the HTML for the row item</param>
    public void AddRowItem(string html)
    {
        if (emailing == false)
            _builder.AppendLine(html);
        else
        {
            float percent = 100f / currentRowColumnCount;
            _builder.AppendLine(
                $"<td width=\"{percent}%\" style=\"border-radius:10px;background:{BoxBackground} !important;padding:10px;vertical-align:top;overflow:hidden\">{html}</td>");
        }

    }

    /// <summary>
    /// Generates the period summary box
    /// </summary>
    /// <param name="minDateUtc">the report min date</param>
    /// <param name="maxDateUtc">the report max date</param>
    public void AddPeriodSummaryBox(DateTime minDateUtc, DateTime maxDateUtc)
    {
        string periodText = minDateUtc.ToLocalTime().ToString("d MMM") + " - " +
                            maxDateUtc.ToLocalTime().ToString("d MMM");
        if (minDateUtc.Year < 2000 && maxDateUtc.Year > DateTime.Now.Year + 10)
            periodText = "All Time";

        var box = ReportSummaryBox.Generate("Period", periodText, ReportSummaryBox.IconType.Clock,
            ReportSummaryBox.BoxColor.Info, emailing);
        AddRowItem(box);
    }

    /// <summary>
    /// Generates a report summary box
    /// </summary>
    /// <param name="title">the title</param>
    /// <param name="value">the value to display</param>
    /// <param name="icon">the icon to show</param>
    /// <param name="color">the color</param>
    /// <returns>the HTML of the report summary box</returns>
    public void AddSummaryBox(string title, string value, ReportSummaryBox.IconType icon,
        ReportSummaryBox.BoxColor color)
        => AddRowItem(ReportSummaryBox.Generate(title, value, icon, color, emailing));

    /// <summary>
    /// Generates a report summary box
    /// </summary>
    /// <param name="title">the title</param>
    /// <param name="value">the value to display</param>
    /// <param name="icon">the icon to show</param>
    /// <param name="color">the color</param>
    /// <returns>the HTML of the report summary box</returns>
    public void AddSummaryBox(string title, int value, ReportSummaryBox.IconType icon,
        ReportSummaryBox.BoxColor color)
        => AddRowItem(ReportSummaryBox.Generate(title, value.ToString("N0"), icon, color, emailing));

    /// <summary>
    /// Adds a progress bar
    /// </summary>
    /// <param name="percent">the percent, 100 based, so 100% == 100</param>
    public void AddProgressBar(double percent)
        => _builder.AppendLine(GetProgressBarHtml(percent, emailing));

    /// <summary>
    /// Gets the progress bar HTML
    /// </summary>
    /// <param name="percent">the percent, 100 based, so 100% == 100</param>
    /// <param name="emailing">if the report is being emailed</param>
    /// <returns>the progress bar HTML</returns>
    public string GetProgressBarHtml(double percent, bool emailing)
    {
        if(emailing == false)
            return $"<div class=\"percentage {(percent > 100 ? "over-100" : "")}\">" +
            $"<div class=\"bar\" style=\"width:{Math.Min(percent, 100)}%\"></div>" +
            $"<span class=\"label\">{(percent / 100):P1}</span>" +
            "</div>";
        return @$"
<div class=""percentage {(percent > 100 ? "over-100" : "")}"" 
style=""background: linear-gradient(to right, rgba(51, 178, 223, 0.25) 0%, rgba(51, 178, 223, 0.5) {Math.Clamp(percent, 0, 100)}%, #181a1b {Math.Clamp(percent, 0, 100)}%) !important;"">
    <span style=""font-weight: 600;vertical-align: middle;line-height: normal;text-align: center;line-height: 48px;font-size: 15px;"">{(percent / 100):P1}</span>
</div>"; 
    }

    /// <inheritdoc />
    public override string ToString()
        => _builder.ToString();

    /// <summary>
    /// Starts a chart table row
    /// </summary>
    public void StartChartTableRow()
        => StartRow(2, "chart-table");
    
    /// <summary>
    /// Starts a large table row
    /// </summary>
    public void StartLargeTableRow()
        => StartRow(1, "large-table");
}