using System.Web;
using FileFlows.DataLayer.Reports.Helpers;
using FileFlows.Plugin;
using FileFlows.Plugin.Formatters;
using FileFlows.Shared.Formatters;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Reports;

/// <summary>
/// Report that shows the savings for libraries 
/// </summary>
public class LibrarySavings : Report
{
    /// <inheritdoc />
    public override Guid Uid => new Guid("32099382-eda1-4694-bc74-5a6584e40684");
    /// <inheritdoc />
    public override string Name => "Library Savings";
    /// <inheritdoc />
    public override string Description => "Shows the space savings for your libraries";
    /// <inheritdoc />
    public override string Icon => "fas fa-hdd";
    /// <inheritdoc />
    public override ReportSelection LibrarySelection => ReportSelection.Any;

    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model, bool emailing)
    {
        using var db = await GetDb();
        string sql =
            $"select {Wrap("LibraryUid")}, {Wrap("LibraryName")}, {Wrap("OriginalSize")}, " +
            $"{Wrap("FinalSize")} from {Wrap("LibraryFile")} where {Wrap("Status")} = 1";
        
        AddLibrariesToSql(model, ref sql);
        AddPeriodToSql(model, ref sql);
        (DateTime? minDateUtc, DateTime? maxDateUtc) = GetPeriod(model);

        var files = await db.Db.FetchAsync<LibrarySavingsData>(sql);
        var totalFinal = files.Sum(x => x.FinalSize);
        var totalOriginal = files.Sum(x => x.OriginalSize);
        var totalSavings = totalOriginal - totalFinal;

        var formatter = new SizeFormatter();
        // Group by LibraryUid and calculate savings
        var librarySavings = files
            .GroupBy(lib => lib.LibraryUid)
            .Select(group =>
            {
                var originalSize = group.Sum(lib => lib.OriginalSize);
                var finalSize = group.Sum(lib => lib.FinalSize);
                var savings = originalSize - finalSize;
                var savingsPercentage = originalSize > 0 ? (savings / (double)originalSize) : 0;

                return new
                {
                    TotalFiles = group.Count(),
                    Library = group.First().LibraryName,
                    OriginalSize = formatter.Format(originalSize, null!),
                    FinalSize = formatter.Format(finalSize, null!),
                    Savings = formatter.Format(Math.Abs(savings), null!),
                    Percentage = savingsPercentage * 100
                };
            })
            .OrderBy(x => x.Library.ToLowerInvariant())
            .ToList();
        
        if (librarySavings.Count == 0)
            return string.Empty;

        ReportBuilder builder = new(emailing);
        
        builder.StartRow(3);
        builder.AddPeriodSummaryBox(minDateUtc ?? DateTime.MinValue, maxDateUtc ?? DateTime.MaxValue);
        builder.AddSummaryBox("Total Files", files.Count, ReportSummaryBox.IconType.File, ReportSummaryBox.BoxColor.Info);
        if(totalSavings >= 0)
            builder.AddSummaryBox("Total Savings", FileSizeFormatter.Format(totalSavings), ReportSummaryBox.IconType.HardDrive, ReportSummaryBox.BoxColor.Success);
        else
            builder.AddSummaryBox("Total Loss", FileSizeFormatter.Format(Math.Abs(totalSavings)), ReportSummaryBox.IconType.HardDrive, ReportSummaryBox.BoxColor.Error);
        
        builder.EndRow();
        
        foreach (var lib in librarySavings)
        {
            var iconClass = lib.Percentage switch
            {
                > 100 => "error",
                > 60 => "warning", 
                _ => "success"
            };
            var iconColor = lib.Percentage switch
            {
                > 100 => ReportSummaryBox.BoxColor.Error,
                > 60 => ReportSummaryBox.BoxColor.Warning,
                _ => ReportSummaryBox.BoxColor.Success
            };
            builder.StartRow(1);

            string savedLabel = lib.Percentage > 100 ? "Storage Lost" : "Storage Saved";
            if (emailing == false)
            {
                builder.AddRowItem("<div class=\"report-flex-data\">" +
                                   $"<div class=\"icon {iconClass}\">{(emailing ? ReportSummaryBox.GetEmailIcon(ReportSummaryBox.IconType.Folder, iconColor) :
                                       ReportSummaryBox.GetIcon(ReportSummaryBox.IconType.Folder))}</div>" +
                                   $"<div class=\"title\">{HttpUtility.HtmlEncode(lib.Library)}</div>" +
                                   $"<div class=\"info-box\"><span class=\"ib-title\">Total Files</span><span class=\"ib-value\">{lib.TotalFiles:N0}</span></div>" +
                                   $"<div class=\"info-box\"><span class=\"ib-title\">{savedLabel}</span><span class=\"ib-value\">{lib.Savings}</span></div>" + 
                                   builder.GetProgressBarHtml(lib.Percentage, emailing) +
                                   "</div>");
            }
            else
            {
                builder.AddRowItem($@"
<table class=""report-flex-data"" style=""width:100%"">
    <tr>
        <td class=""icon {iconClass}"">{ReportSummaryBox.GetEmailIcon(ReportSummaryBox.IconType.Folder, iconColor)}</td>
        <td class=""title"">{HttpUtility.HtmlEncode(lib.Library)}</td>
        <td class=""info-box""><span class=""ib-title"">Total Files</span><span class=""ib-value"">{lib.TotalFiles:N0}</span></td>
        <td class=""info-box""><span class=""ib-title"">{savedLabel}</span><span class=""ib-value"">{lib.Savings}</span></td>
        <td class=""percent"">{builder.GetProgressBarHtml(lib.Percentage, emailing)}</td>
    </tr>
</table>");
                
            }

            // builder.AddSummaryBox(lib.Library + " Files", lib.TotalFiles, ReportSummaryBox.IconType.File, ReportSummaryBox.BoxColor.Info);
            // builder.AddSummaryBox("Savings", lib.Savings, ReportSummaryBox.IconType.HardDrive, lib.Percentage > 100 ?  ReportSummaryBox.BoxColor.Error : ReportSummaryBox.BoxColor.Success);
            // builder.AddProgressBar(lib.Percentage);
            builder.EndRow();
        }

        //return TableGenerator.Generate(librarySavings, dontWrap: true);
        return builder.ToString();
    }

    /// <summary>
    /// Class for the library savings data
    /// </summary>
    public class LibrarySavingsData
    {
        /// <summary>
        /// Gets or sets the UID of the library
        /// </summary>
        public Guid LibraryUid { get; set; }

        /// <summary>
        /// Gets or sets the Name of the library
        /// </summary>
        public string LibraryName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the size of the original library file
        /// </summary>
        public long OriginalSize { get; set; }
    
        /// <summary>
        /// Gets or sets the size of the final file after processing
        /// </summary>
        public long FinalSize { get; set; }
    }
}