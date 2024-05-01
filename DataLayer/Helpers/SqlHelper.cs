namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Helper for SQL
/// </summary>
public class SqlHelper
{
    /// <summary>
    /// Cleans SQL and removes empty lines and comments
    /// </summary>
    /// <param name="sql">the SQL to clean</param>
    /// <returns>the cleaned SQL</returns>
    public static string CleanSql(string sql)
    {
        var lines = sql.Replace("\r\n", "\n")
            .Split('\n')
            .Where(x => string.IsNullOrWhiteSpace(x) == false)
            .Where(x => x.Trim().StartsWith("--") == false);
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Escapes a string so it is safe to be used in a sql command
    /// </summary>
    /// <param name="input">the string to escape</param>
    /// <returns>the escaped string</returns>
    public static string Escape(string input) =>
        input == null ? "''" : "'" + input.Replace("'", "''") + "'";
    
    // /// <summary>
    // /// Adds a skip to a sql command
    // /// </summary>
    // /// <param name="sql">the sql command</param>
    // /// <param name="skip">the number of items to skip</param>
    // /// <param name="rows">the number of rows to fetch</param>
    // /// <returns>the updated SQL</returns>
    // public static string Skip(string sql, int skip, int rows)
    // {
    //     if (skip + rows == 0)
    //         return sql;
    //     if (DbHelper.UseMemoryCache)
    //     {
    //         // sqlite
    //         return sql + $" limit {rows} offset {skip}";
    //     }
    //
    //     // mysql
    //     return sql + $" limit {skip}, {rows}";
    // }
    // /// <summary>
    // /// Adds a skip to a sql command
    // /// </summary>
    // /// <param name="sql">the sql command</param>
    // /// <param name="limit">the number of rows to fetch</param>
    // /// <returns>the updated SQL</returns>
    // public static string Limit(string sql, int limit)
    // {
    //     if (DbHelper.UseMemoryCache)
    //     {
    //         // sqlite
    //         return sql + $" limit {limit} ";
    //     }
    //     // mysql
    //     return sql + $" limit 0, {limit}";
    // }
    //
    // /// <summary>
    // /// Creates a time difference sql select
    // /// </summary>
    // /// <param name="start">the start column</param>
    // /// <param name="end">the end column</param>
    // /// <param name="asColumn">the name of the result</param>
    // /// <returns>the sql select statement</returns>
    // public static string TimestampDiffSeconds(string start, string end, string asColumn)
    // {
    //     if (DbHelper.UseMemoryCache)
    //     {
    //         // sqlite
    //         return $" timestampdiff(second, {start}, {end}) AS {asColumn} ";
    //     }
    //
    //     return $" timestampdiff(second, {start}, {end}) AS {asColumn} ";
    // }
    //
    // /// <summary>
    // /// Creates a day of week sql select
    // /// </summary>
    // /// <param name="column">the date column</param>
    // /// <param name="asColumn">the name of the result</param>
    // /// <returns>the sql select statement</returns>
    // public static string DayOfWeek(string column, string asColumn = null)
    // {
    //     if (DbHelper.UseMemoryCache)
    //     {
    //         // sqlite
    //         // sqlite is 0 based, 0 == sunday, mysql and ms sql server is 1 based
    //         return $"(strftime('%w', {column}) + 1)" + (string.IsNullOrEmpty(asColumn) ? string.Empty : $" as {asColumn}");
    //     }
    //
    //     // mysql dayofweek is 1 based, convert to 0 based
    //     return $"DAYOFWEEK({column})" + (string.IsNullOrEmpty(asColumn) ? string.Empty : $" as {asColumn}");
    // }
    // /// <summary>
    // /// Creates a hour sql select
    // /// </summary>
    // /// <param name="column">the date column</param>
    // /// <param name="asColumn">the name of the result</param>
    // /// <returns>the sql select statement</returns>
    // public static string Hour(string column, string asColumn = null)
    // {
    //     if (DbHelper.UseMemoryCache)
    //     {
    //         // sqlite
    //         return $"strftime('%H', {column})" + (string.IsNullOrEmpty(asColumn) ? string.Empty : $" as {asColumn}");
    //     }
    //
    //     return $"hour({column})" + (string.IsNullOrEmpty(asColumn) ? string.Empty : $" as {asColumn}");
    // }
    //
    // /// <summary>
    // /// Gets the variable for "now" as in the current datetime
    // /// </summary>
    // /// <returns>the SQL variable for now</returns>
    // public static object Now()
    // {
    //     if (DbHelper.UseMemoryCache)
    //         return "datetime(current_timestamp, 'localtime')";
    //     return "now()";
    // }
    //
    // /// <summary>
    // /// Gets the variable for a random value
    // /// </summary>
    // /// <returns>the SQL variable for random</returns>
    // public static string Random()
    // { 
    //     if(DbHelper.UseMemoryCache)
    //         return "random()";
    //     return "rand()";
    // }
    
    /// <summary>
    /// Creates a select parameter for a JSON value
    /// </summary>
    /// <param name="column">the column to extract from eg "Data"</param>
    /// <param name="jsonProperty">the json property eg "MyValue"</param>
    /// <param name="asColumn">the name of the result</param>
    /// <returns>the json select eg "JSON_EXTRACT('$.Data.MyValue')</returns>
    public static string JsonValue(string column, string jsonProperty, string? asColumn = null)
    {
        return $"JSON_EXTRACT({column}, '$.{jsonProperty}')" + (string.IsNullOrEmpty(asColumn) ? string.Empty : $" as {asColumn}");
    }
}