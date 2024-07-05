using System.Text;
using FileFlows.DataLayer.Reports.Helpers;

namespace FileFlows.DataLayer.Reports;

/// <summary>
/// Builds a reports HTML
/// </summary>
public class ReportBuilder
{
    /// <summary>
    /// The string builder
    /// </summary>
    private StringBuilder _builder = new();

    /// <summary>
    /// Appends a line to the report
    /// </summary>
    /// <param name="text">the text to add</param>
    public void AppendLine(string text)
        => _builder.AppendLine(text);
    
    /// <summary>
    /// Starts a row
    /// </summary>
    /// <param name="columns">the number of columns</param>
    public void StartRow(int columns)
        => _builder.AppendLine($"<div class=\"report-row report-row-{columns}\">");

    /// <summary>
    /// Ends a row
    /// </summary>
    public void EndRow()
        => _builder.AppendLine("</div>");

    /// <summary>
    /// Generates the period summary box
    /// </summary>
    /// <param name="minDateUtc">the report min date</param>
    /// <param name="maxDateUtc">the report max date</param>
    public void AddPeriodSummaryBox(DateTime minDateUtc, DateTime maxDateUtc)
    {
        string periodText = minDateUtc.ToLocalTime().ToString("d MMM") + " - " +
                            maxDateUtc.ToLocalTime().ToString("d MMM");
        _builder.AppendLine(ReportSummaryBox.Generate("Period", periodText, ReportSummaryBox.IconType.Clock,
            ReportSummaryBox.BoxColor.Info));
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
        => _builder.AppendLine(ReportSummaryBox.Generate(title, value, icon, color));
    
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
        => _builder.AppendLine(ReportSummaryBox.Generate(title, value.ToString("N0"), icon, color));

    /// <inheritdoc />
    public override string ToString()
        => _builder.ToString();
}