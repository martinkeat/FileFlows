using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Services;

/// <summary>
/// Search functions for Library File Service
/// </summary>
public partial class LibraryFileService
{
    /// <summary>
    /// Performance a search for library files
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>a list of matching library files</returns>
    public async Task<IEnumerable<LibraryFile>> Search(LibraryFileSearchModel filter)
    {
        List<string> wheres = new();
        List<object> parameters = new List<object>();
        if (filter.FromDate > new DateTime(2000, 1, 1) && filter.ToDate < DateTime.Now.AddYears(100))
        {
            parameters.Add(filter.FromDate);
            parameters.Add(filter.ToDate);
            wheres.Add("DateCreated > @0");
            wheres.Add("DateCreated < @1");
        }

        if (string.IsNullOrWhiteSpace(filter.Path) == false)
        {
            // mysql better search, need to redo this
            // and match(Name) against (SearchText IN BOOLEAN MODE)
            //wheres.Add($"( match(Name) against (@{paramIndex}) IN BOOLEAN MODE) or match(OutputPath) against (@{paramIndex}) IN BOOLEAN MODE))");
            
            int paramIndex = parameters.Count;
            parameters.Add("%" + filter.Path.Replace(" ", "%") + "%");
            wheres.Add($"( lower(Name) like lower(@{paramIndex}) or lower(OutputPath) like lower(@{paramIndex}))");
        }
        if (string.IsNullOrWhiteSpace(filter.LibraryName) == false)
        {
            int paramIndex = parameters.Count;
            parameters.Add("%" + filter.LibraryName + "%");
            wheres.Add($"lower(LibraryName) like lower(@{paramIndex})");
        }
        string sql = SqlHelper.Skip("select * from LibraryFile where " + string.Join(" and ", wheres), skip: 0, rows: filter.Limit > 0 ? filter.Limit : 100);
        var results =  await Database_Fetch<LibraryFile>(sql, parameters);
        return results;
    }


}