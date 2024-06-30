using FileFlows.Plugin;
using FileFlows.Plugin.Formatters;
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
    public override ReportSelection LibrarySelection => ReportSelection.AnyRequired;

    /// <inheritdoc />
    public override async Task<Result<string>> Generate(Dictionary<string, object> model)
    {
        using var db = await GetDb();
        string sql =
            $"select {Wrap("LibraryUid")}, {Wrap("LibraryName")}, {Wrap("OriginalSize")}, " +
            $"{Wrap("FinalSize")} from {Wrap("LibraryFile")} where {Wrap("Status")} = 1";
        
        AddLibrariesToSql(model, ref sql);
        AddPeriodToSql(model, ref sql);

        var libraries = await db.Db.FetchAsync<LibrarySavingsData>(sql);

        var formatter = new SizeFormatter();
        // Group by LibraryUid and calculate savings
        var groupedSavings = libraries
            .GroupBy(lib => lib.LibraryUid)
            .Select(group =>
            {
                var originalSize = group.Sum(lib => lib.OriginalSize);
                var finalSize = group.Sum(lib => lib.FinalSize);
                var savings = originalSize - finalSize;
                var savingsPercentage = originalSize > 0 ? (savings / (double)originalSize) : 0;

                return new
                {
                    Library = group.First().LibraryName,
                    OriginalSize = formatter.Format(originalSize, null!),
                    FinalSize = formatter.Format(finalSize, null!),
                    Savings = formatter.Format(savings, null!),
                    Percentage = savingsPercentage * 100
                };
            })
            .OrderBy(x => x.Library.ToLowerInvariant())
            .ToList();

        return GenerateHtmlTable(groupedSavings);
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