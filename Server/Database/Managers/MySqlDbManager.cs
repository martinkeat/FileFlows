using System.Numerics;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared;
using FileFlows.Shared.Models;
using MySqlConnector;
using NPoco;
using DatabaseType = FileFlows.Shared.Models.DatabaseType;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// A database manager used to communicate with a MySql/MariaDB Database
/// </summary>
public class MySqlDbManager: DbManager
{
    internal readonly string CreateDbRevisionedObjectTableScript = @$"
        CREATE TABLE {nameof(RevisionedObject)}(
            Uid             VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL          PRIMARY KEY,
            RevisionUid     VARCHAR(36)        COLLATE utf8_unicode_ci      NOT NULL,
            RevisionName    VARCHAR(1024)      COLLATE utf8_unicode_ci      NOT NULL,
            RevisionType    VARCHAR(255)       COLLATE utf8_unicode_ci      NOT NULL,
            RevisionDate    datetime           default           now(),
            RevisionCreated datetime           default           now(),
            RevisionData    MEDIUMTEXT         COLLATE utf8_unicode_ci      NOT NULL
        );
";
    
    /// <summary>
    /// Gets the method for random in the SQL
    /// </summary>
    public override string RandomMethod => "RAND()";
    
    /// <summary>
    /// Creates an instance of a MySqlDbManager
    /// </summary>
    /// <param name="connectionString">a mysql connection string</param>
    public MySqlDbManager(string connectionString)
    {
        ConnectionString = connectionString;
    }

    protected override NPoco.Database GetDbInstance()
    {
        return new FlowDatabase(ConnectionString);
    }
    
    private string GetDatabaseName(string connectionString)
        => Regex.Match(ConnectionString, @"(?<=(Database=))[a-zA-Z0-9_\-]+").Value;

    protected override DbCreateResult CreateDatabase(bool recreate)
    {
        string connString = Regex.Replace(ConnectionString, "(^|;)Database=[^;]+", "");
        if (connString.StartsWith(";"))
            connString = connString[1..];
        string dbName = GetDatabaseName(ConnectionString);
        
        using var db = new NPoco.Database(connString, null, MySqlConnector.MySqlConnectorFactory.Instance);
        bool exists = string.IsNullOrEmpty(db.ExecuteScalar<string>("select schema_name from information_schema.schemata where schema_name = @0", dbName)) == false;
        if (exists)
        {
            if(recreate == false)
                return DbCreateResult.AlreadyExisted;
            Logger.Instance.ILog("Dropping existing database");
            db.Execute($"drop database {dbName};");
        }

        Logger.Instance.ILog("Creating Database");
        return db.Execute("create database " + dbName + " character set utf8 collate 'utf8_unicode_ci';") > 0 ? DbCreateResult.Created : DbCreateResult.Failed;
    }

    // protected override void CreateStoredProcedures()
    // {
    //     Logger.Instance.ILog("Creating Stored Procedures");
    //     using var db = new NPoco.Database(ConnectionString + ";Allow User Variables=True", null, MySqlConnector.MySqlConnectorFactory.Instance);
    //     
    //     
    //     var scripts = GetStoredProcedureScripts("MySql");
    //     foreach (var script in scripts)
    //     {
    //         Logger.Instance.ILog("Creating script: " + script.Key);
    //         var sql = script.Value.Replace("@", "@@");
    //         try
    //         {
    //             db.Execute(sql);
    //         }
    //         catch (Exception ex)
    //         {
    //             Logger.Instance.WLog($"Failed executing script '{script.Key}: {ex.Message}");
    //         }
    //     }
    // }

    protected override bool CreateDatabaseStructure()
    {
        Logger.Instance.ILog("Creating Database Structure");
        
        using var db = new NPoco.Database(ConnectionString, null, MySqlConnector.MySqlConnectorFactory.Instance);
        string sqlTables = GetSqlScript("MySql", "Tables.sql", clean: true);
        Logger.Instance.ILog("SQL Tables:\n" + sqlTables);
        db.Execute(sqlTables);
        
        return true;
    }

    private readonly List<(Guid ClientUid, LogType Type, string Message, DateTime Date)> LogMessages = new();
    
    /// <summary>
    /// Logs a message to the database
    /// </summary>
    /// <param name="clientUid">The UID of the client, use Guid.Empty for the server</param>
    /// <param name="type">the type of log message</param>
    /// <param name="message">the message to log</param>
    public override async Task Log(Guid clientUid, LogType type, string message)
    {
        // by bucketing this it greatly improves speed
        List<(Guid ClientUid, LogType Type, string Message, DateTime Date)> toInsert = new();
        lock (LogMessages)
        {
            LogMessages.Add((clientUid, type, message, DateTime.Now));
            if (LogMessages.Count > 20)
            {
                toInsert = LogMessages.ToList();
                LogMessages.Clear();
            }
        }
        if(toInsert?.Any() == true)
        {
            using (var db = await GetDb())
            {
                try
                {
                    foreach (var msg in toInsert)
                    {
                        await db.Db.ExecuteAsync(
                            $"insert into {nameof(DbLogMessage)} ({nameof(DbLogMessage.ClientUid)}, {nameof(DbLogMessage.LogDate)}, {nameof(DbLogMessage.Type)}, {nameof(DbLogMessage.Message)}) values " +
                            $"(@0, @1, @2, @3)",
                            msg.ClientUid, msg.Date, (int)msg.Type, msg.Message);
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        // using (var db = await GetDb())
        // {
        //     await db.Db.ExecuteAsync(
        //         $"insert into {nameof(DbLogMessage)} ({nameof(DbLogMessage.ClientUid)}, {nameof(DbLogMessage.Type)}, {nameof(DbLogMessage.Message)}) " +
        //         $" values (@0, @1, @2)", clientUid, type, message);
        // }
    }

    /// <summary>
    /// Prune old logs from the database
    /// </summary>
    /// <param name="maxLogs">the maximum number of log messages to keep</param>
    public override async Task PruneOldLogs(int maxLogs)
    {
        try
        {
            using (var db = await GetDb())
            {
                //await db.Db.ExecuteAsync($"call DeleteOldLogs({maxLogs});");
                string sql = $@"
DELETE FROM DbLogMessage
WHERE LogDate < (
    SELECT LogDate
    FROM (
        SELECT LogDate
        FROM DbLogMessage
        ORDER BY LogDate DESC
        LIMIT {maxLogs}
    ) AS subquery
    ORDER BY LogDate ASC
    LIMIT 1
);
";
                await db.Db.ExecuteAsync(sql);
            }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Searches the log using the given filter
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the messages found in the log</returns>
    public override async Task<IEnumerable<DbLogMessage>> SearchLog(LogSearchModel filter)
    {
        if (filter.Source == "DATABASE" || filter.Source == "HTTP")
        {
            // just read those file.. er no
        }
        string clientUid = filter.Source.EmptyAsNull() ?? Guid.Empty.ToString();
        string from = filter.FromDate.ToString("yyyy-MM-dd HH:mm:ss");
        string to = filter.ToDate.ToString("yyyy-MM-dd HH:mm:ss");
        string sql = $"select * from {nameof(DbLogMessage)} " +
                     $"where ({nameof(DbLogMessage.LogDate)} between '{from}' and '{to}') " +
                     $" and {nameof(DbLogMessage.ClientUid)} = '{clientUid}' " +
                     (filter.Type != null ?
                         $" and {nameof(DbLogMessage.Type)} {(filter.TypeIncludeHigherSeverity ? "<=" : "=")} {(int)filter.Type}" 
                     : "") +
                     (filter.Message?.EmptyAsNull() != null ? $" and {nameof(DbLogMessage.Message)} like @2 " : "") +
                     $" order by {nameof(DbLogMessage.LogDate)} desc " +
                     " limit 1000";
        List<DbLogMessage> results;
        DateTime dt = DateTime.Now;
        using (var db = await GetDb())
        {
            results = await db.Db.FetchAsync<DbLogMessage>(sql, filter.FromDate, filter.ToDate,
                string.IsNullOrWhiteSpace(filter.Message) ? string.Empty : "%" + filter.Message.Trim() + "%");
        }

        // need to reverse them as they're ordered newest at top
        results.Reverse();
        return results;
    }

    /// <summary>
    /// Gets the failure flow for a particular library
    /// </summary>
    /// <param name="libraryUid">the UID of the library</param>
    /// <returns>the failure flow</returns>
    public override async Task<Flow> GetFailureFlow(Guid libraryUid)
    {
        DbObject dbObject;
        using (var db = await GetDb())
        {
            string sql = $@"select * from DbObject where Type = '{typeof(Flow).FullName}' " +
                         $@"and JSON_EXTRACT(Data,'$.Type') = {(int)FlowType.Failure} " +
                         "and JSON_EXTRACT(Data,'$.Default') = 1 " +
                         "and JSON_EXTRACT(Data,'$.Enabled') = 1 ";
                
            dbObject = await db.Db.SingleOrDefaultAsync<DbObject>(sql);
        }

        return ConvertFromDbObject<Flow>(dbObject);
    }
    
    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public override async Task<IEnumerable<Statistic>> GetStatisticsByName(string name)
    {
        List<DbStatistic> stats;
        using (var db = await GetDb())
        {
            stats = await db.Db.FetchAsync<DbStatistic>("where Name = @0", name);
        }

        var results = new List<Statistic>();
        foreach (var stat in stats)
        {
            if(stat.Type == StatisticType.Number)
                results.Add(new () { Name = stat.Name, Value = stat.NumberValue});
            if(stat.Type == StatisticType.String)
                results.Add(new () { Name = stat.Name, Value = stat.StringValue});
        }

        return results;
    }
    
    /// <summary>
    /// Tests the connection to a database
    /// </summary>
    /// <param name="server">the server address</param>
    /// <param name="name">the database name</param>
    /// <param name="port">the database port</param>
    /// <param name="user">the connecting user</param>
    /// <param name="password">the password to use</param>
    /// <returns>any error or empty string if successful</returns>
    public string Test(string server, string name, int port, string user, string password)
    {
        try
        {
            var builder = new MySqlConnector.MySqlConnectionStringBuilder();
            builder["Server"] = server;
            if (port > 0)
                builder["Port"] = port;
            builder["Uid"] = user;
            builder["Pwd"] = password;
            string connString = builder.ConnectionString;
            using var db = new NPoco.Database(connString, null, MySqlConnector.MySqlConnectorFactory.Instance);
            bool exists = string.IsNullOrEmpty(db.ExecuteScalar<string>("select schema_name from information_schema.schemata where schema_name = @0", name)) == false;
            return string.Empty;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// Gets a database connection string
    /// </summary>
    /// <param name="server">the database server</param>
    /// <param name="database">the database name</param>
    /// <param name="port">the database port</param>
    /// <param name="user">the connecting user</param>
    /// <param name="password">the password for the connection</param>
    /// <returns>the database connection string</returns>
    public string GetConnectionString(string server, string database, int port, string user, string password)
    {
        var builder = new MySqlConnector.MySqlConnectionStringBuilder();
        builder["Server"] = server;
        if (port > 0)
            builder["Port"] = port;
        builder["Database"] = database?.EmptyAsNull() ?? "FileFlows";
        builder["Uid"] = user;
        builder["Pwd"] = password;
        string connstr = builder.ConnectionString;
        connstr = connstr.Replace("User ID", "Uid");
        return connstr;
    }

    /// <summary>
    /// Gets the database name from the connection string
    /// </summary>
    /// <returns>the database name</returns>
    public string GetDatabaseName()
    {
        var builder = new MySqlConnector.MySqlConnectionStringBuilder(this.ConnectionString);
        return builder["Database"].ToString();
    }

    /// <summary>
    /// Populates the database settings from a connection string
    /// </summary>
    /// <param name="settings">the settings to populate</param>
    /// <param name="connectionString">the connection string to parse</param>
    internal void PopulateSettings(SettingsUiModel settings, string connectionString)
    {
        var builder = new MySqlConnector.MySqlConnectionStringBuilder(connectionString);
        settings.DbType = DatabaseType.MySql;
        settings.DbServer = builder["Server"].ToString();
        settings.DbName = builder["Database"].ToString();
        settings.DbUser = builder["Uid"].ToString();
        settings.DbPassword = builder["Pwd"].ToString();
        if (builder.ContainsKey("Port") && int.TryParse(builder["Port"].ToString(), out int port))
            settings.DbPort = port;
        else
            settings.DbPort = 3306;
    }

    private readonly Dictionary<Guid, DateTime> NodeLastSeen = new();

    /// <summary>
    /// Updates the last seen of a node
    /// </summary>
    /// <param name="uid">the UID of the node</param>
    public override async Task UpdateNodeLastSeen(Guid uid)
    {
        lock (NodeLastSeen)
        {
            if (NodeLastSeen.ContainsKey(uid))
            {
                if (NodeLastSeen[uid] > DateTime.Now.AddSeconds(-20))
                    return; // so recent, don't record it
                NodeLastSeen[uid] = DateTime.Now;
            }
            else
            {
                NodeLastSeen.Add(uid, DateTime.Now);
            }
        }


        string dt = DateTime.Now.ToString("o"); // same format as json

        using (var db = await GetDb())
        {
            string sql =
                $"update DbObject set Data = json_set(Data, '$.LastSeen', '{dt}') where Type = 'FileFlows.Shared.Models.ProcessingNode' and Uid = '{uid}'";
            await db.Db.ExecuteAsync(sql);
        }
    }


    /// <summary>
    /// Gets if a column exists in the given table
    /// </summary>
    /// <param name="table">the table name</param>
    /// <param name="column">the column to look for</param>
    /// <returns>true if it exists, otherwise false</returns>
    public override async Task<bool> ColumnExists(string table, string column)
    {
        string dbName = GetDatabaseName(this.ConnectionString);
        using var db = await GetDb();
        var result = db.Db.ExecuteScalar<int>($@"SELECT count(*) 
        FROM information_schema.COLUMNS 
        WHERE 
            TABLE_SCHEMA = @0 
        AND TABLE_NAME = @1 
        AND COLUMN_NAME = @2", dbName, table, column);
        Logger.Instance.ILog($"Result of columns exists '{table}.{column}': " + (result > 0));
        bool exists = result > 0;
        return exists;
    }
}