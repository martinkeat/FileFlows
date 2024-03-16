using System.Text;
using System.Text.Json;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Helpers;
using FileFlows.ServerShared.Models.StatisticModels;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Upgrades;

/// <summary>
/// Upgrades for 24.03.2
/// Changing DateTimes to UTC
/// </summary>
public class Upgrade_24_03_2
{
    /// <summary>
    /// Run the upgrade
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the database type</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    public Result<bool> Run(ILogger logger, DatabaseType dbType, string connectionString)
    {
        if (dbType == DatabaseType.Sqlite)
            return RunSqlite(logger, connectionString);
        else
            return RunMySql(logger, connectionString);

    }

    /// <summary>
    /// Run the upgrade for a MySql Server
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    private Result<bool> RunMySql(ILogger logger, string connectionString)
    {
        var connector = new DatabaseConnectors.MySqlConnector(logger, connectionString);
        using var db = connector.GetDb(true).Result;
        
        db.Db.BeginTransaction();
        try
        {
            // DbObject
            var dbos = db.Db.Fetch<DbObjectUpgrade>("select * from DbObject");
            StringBuilder sql = new();
            foreach (var dbo in dbos)
            {
                dbo.DateCreated = DateTimeHelper.LocalToUtc(dbo.DateCreated);
                dbo.DateModified = DateTimeHelper.LocalToUtc(dbo.DateModified);
                sql.AppendLine("update DbObject set DateCreated = " + connector.FormatDateQuoted(dbo.DateCreated) +
                               ", DateModified = " + connector.FormatDateQuoted(dbo.DateModified) +
                               $" where Uid = '{dbo.Uid}';");
            }

            db.Db.Execute(sql.ToString());


            // DbRevision
            var dbr = db.Db.Fetch<RevisionedObject>("select * from RevisionedObject");
            sql.Clear();
            foreach (var r in dbr)
            {
                r.RevisionDate = DateTimeHelper.LocalToUtc(r.RevisionDate);
                r.RevisionCreated = DateTimeHelper.LocalToUtc(r.RevisionCreated);
                sql.AppendLine("update RevisionedObject set RevisionDate = " +
                               connector.FormatDateQuoted(r.RevisionDate) +
                               ", RevisionCreated = " + connector.FormatDateQuoted(r.RevisionCreated) +
                               $" where Uid = '{r.Uid}';");
            }

            db.Db.Execute(sql.ToString());

            // LibraryFiles
            var libFiles = db.Db.Fetch<LibraryFileUpgrade>(
                "select Uid, DateCreated, DateModified, ProcessingStarted, ProcessingEnded, HoldUntil, CreationTime, LastWriteTime from LibraryFile");
            sql.Clear();
            foreach (var lf in libFiles)
            {
                lf.DateCreated = DateTimeHelper.LocalToUtc(lf.DateCreated);
                lf.DateModified = DateTimeHelper.LocalToUtc(lf.DateModified);
                lf.ProcessingStarted = DateTimeHelper.LocalToUtc(lf.ProcessingStarted);
                lf.ProcessingEnded = DateTimeHelper.LocalToUtc(lf.ProcessingEnded);
                lf.HoldUntil = DateTimeHelper.LocalToUtc(lf.HoldUntil);
                lf.CreationTime = DateTimeHelper.LocalToUtc(lf.CreationTime);
                lf.LastWriteTime = DateTimeHelper.LocalToUtc(lf.LastWriteTime);
                sql.AppendLine("update LibraryFile set DateCreated = " +
                               connector.FormatDateQuoted(lf.DateCreated) +
                               ", DateModified = " + connector.FormatDateQuoted(lf.DateModified) +
                               ", ProcessingStarted = " + connector.FormatDateQuoted(lf.ProcessingStarted) +
                               ", ProcessingEnded = " + connector.FormatDateQuoted(lf.ProcessingEnded) +
                               ", HoldUntil = " + connector.FormatDateQuoted(lf.HoldUntil) +
                               ", CreationTime = " + connector.FormatDateQuoted(lf.CreationTime) +
                               ", LastWriteTime = " + connector.FormatDateQuoted(lf.LastWriteTime) +
                               $" where Uid = '{lf.Uid}';");
            }

            db.Db.Execute(sql.ToString());

            db.Db.Execute(
                "update DbObject set Name = REPLACE(Name, 'PluginsSettings_', '') , Type = 'FileFlows.ServerShared.Models.PluginSettingsModel' where Type = 'FileFlows.Server.Models.PluginSettingsModel'");


            UpgradeStatistics(logger, db, true);

            db.Db.CompleteTransaction();

            return true;
        }
        catch (Exception ex)
        {
            db.Db.AbortTransaction();
#if(DEBUG)
            return Result<bool>.Fail(ex.Message + Environment.NewLine + ex.StackTrace);
#else
            return Result<bool>.Fail(ex.Message);
#endif
        }

    }

    /// <summary>
    /// Run the upgrade for a SQLite Server
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connectionString">the database connection string</param>
    /// <returns>the upgrade result</returns>
    private Result<bool> RunSqlite(ILogger logger, string connectionString)
    {
        try
        {
            var connector = new SQLiteConnector(logger, connectionString);
            using var db = connector.GetDb(true).Result;
            db.Db.Execute(@"
        UPDATE DbObject SET 
        DateCreated = datetime(DateCreated, 'utc'),
        DateModified = datetime(DateModified, 'utc')
        ");
            db.Db.Execute(@"
        UPDATE RevisionedObject SET 
        RevisionDate = datetime(RevisionDate, 'utc'),
        RevisionCreated = datetime(RevisionCreated, 'utc')
        ");
        
            db.Db.Execute(@"
        UPDATE LibraryFile SET 
        DateCreated = datetime(DateCreated, 'utc'),
        DateModified = datetime(DateModified, 'utc'),
        CreationTime = datetime(CreationTime, 'utc'),
        LastWriteTime = datetime(LastWriteTime, 'utc'),
        HoldUntil = datetime(HoldUntil, 'utc'),
        ProcessingStarted = datetime(ProcessingStarted, 'utc'),
        ProcessingEnded = datetime(ProcessingEnded, 'utc')
        ");

            string minDate = connector.FormatDateQuoted(new DateTime(1970, 1, 1));
            db.Db.Execute($"UPDATE LibraryFile SET HoldUntil = {minDate} where HoldUntil < '1970-01-01'");
            db.Db.Execute($"UPDATE LibraryFile SET ProcessingStarted = {minDate} where ProcessingStarted < '1970-01-01'");
            db.Db.Execute($"UPDATE LibraryFile SET ProcessingEnded =  {minDate} where ProcessingEnded < '1970-01-01'");
            
            db.Db.Execute(
                "update DbObject set Name = REPLACE(Name, 'PluginsSettings_', ''), Type = 'FileFlows.ServerShared.Models.PluginSettingsModel' where Type = 'FileFlows.Server.Models.PluginSettingsModel'");

            db.Db.Execute(@"
CREATE TABLE DbLogMessage
(
    ClientUid       VARCHAR(36)        NOT NULL,
    LogDate         datetime,
    Type            int                NOT NULL,
    Message         TEXT               NOT NULL
);");
            db.Db.Execute("CREATE INDEX IF NOT EXISTS idx_DbLogMessage_ClientUid ON DbLogMessage (ClientUid)");
            db.Db.Execute("CREATE INDEX IF NOT EXISTS idx_DbLogMessage_LogDate ON DbLogMessage (LogDate);");
            
            UpgradeStatistics(logger, db, false);

            return true;
        }
        catch (Exception ex)
        {
#if(DEBUG)
            return Result<bool>.Fail(ex.Message + Environment.NewLine + ex.StackTrace);
#else
            return Result<bool>.Fail(ex.Message);
#endif
        }
    }

    /// <summary>
    /// Upgrades the statistics table to the new format
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="connector">the database connector</param>
    /// <param name="mySql">true if using mysql, otherwise false</param>
    private void UpgradeStatistics(ILogger logger, DatabaseConnection connector, bool mySql)
    {
        logger.ILog("Convert old statistics data");
        var old = connector.Db.Fetch<DbStatisticOld>("select * from DbStatistic")
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key, x => x.ToList());

        Dictionary<string, object> newStats = new();
        foreach (string key in old.Keys)
        {
            if (key == "COMIC_PAGES")
            {
                var totals = new Average();
                foreach (var stat in old[key])
                {
                    if (totals.Data.TryAdd((int)stat.NumberValue, 1) == false)
                        totals.Data[(int)stat.NumberValue] += 1;
                }

                newStats.Add(key, totals);
            }
            else
            {
                
                var totals = new RunningTotals();
                foreach (var stat in old[key])
                {
                    if (totals.Data.TryAdd(stat.StringValue, 1) == false)
                        totals.Data[stat.StringValue] += 1;
                }

                newStats.Add(key, totals);
            }
        }

        string newTable = $@"DROP TABLE DbStatistic; CREATE TABLE DbStatistic
(
    Name            varchar(255)       {(mySql ? "COLLATE utf8_unicode_ci" : "")}      NOT NULL          PRIMARY KEY,
    Type            int                NOT NULL,
    Data            TEXT               {(mySql ? "COLLATE utf8_unicode_ci" : "")}      NOT NULL
)";
        connector.Db.Execute(newTable);
        foreach (var key in newStats.Keys)
        {
            connector.Db.Execute("insert into DbStatistic (Name, Type, Data) values (@0, @1, @2)",
                key, 
                (int)(key == "COMIC_PAGES" ? StatisticType.Average : StatisticType.RunningTotals), 
                JsonSerializer.Serialize(newStats[key]));
        }


        // processing heatmap, after upgrade, so now UTC dates
        logger.ILog("Creating processing heatmap data");

        var processedDates =
            connector.Db.Fetch<DateTime>($"select ProcessingStarted from LibraryFile where Status = 1");
        Heatmap heatmap = new();
        foreach (var dt in processedDates)
        {
            int quarter = TimeHelper.GetQuarter(dt);
            if (heatmap.Data.TryAdd(quarter, 1) == false)
                heatmap.Data[quarter] += 1;
        }

        connector.Db.Execute($"insert into DbStatistic (Name, Type, Data) " +
                             $" values (@0, {((int)StatisticType.Heatmap)}, @1)",
            Globals.STAT_PROCESSING_TIMES_HEATMAP, JsonSerializer.Serialize(heatmap));


        // storage saved
        logger.ILog("Creating storage saved data");
        string sql = $@"SELECT LibraryName as Library,
            SUM(CAST(OriginalSize AS {(mySql ? "SIGNED" : "INTEGER")})) AS OriginalSize,
            SUM(CAST(FinalSize AS {(mySql ? "SIGNED" : "INTEGER")})) AS FinalSize,
            COUNT(*) AS TotalFiles
        FROM LibraryFile
        GROUP BY LibraryName";
        var storageSaved = new StorageSaved() { Data = connector.Db.Fetch<StorageSavedData>(sql) };

        connector.Db.Execute("insert into DbStatistic (Name, Type, Data) values " +
                             $" (@0, {((int)StatisticType.StorageSaved)}, @1)",
            Globals.STAT_STORAGE_SAVED, JsonSerializer.Serialize(storageSaved));

        // total files
        logger.ILog("Creating total files data");
        int totalProcessed = connector.Db.ExecuteScalar<int>("select count(*) from LibraryFile where Status = 1");
        int totalFailed = connector.Db.ExecuteScalar<int>("select count(*) from LibraryFile where Status = 4");

        connector.Db.Execute("insert into DbStatistic (Name, Type, Data)" +
                             $" values (@0, {(int)StatisticType.RunningTotals}, @1)",
            Globals.STAT_TOTAL_FILES, JsonSerializer.Serialize(new RunningTotals()
            {
                Data = new()
                {
                    { nameof(FileStatus.Processed), totalProcessed },
                    { nameof(FileStatus.ProcessingFailed), totalFailed },
                }
            }));
    }

    /// <summary>
    /// Represents an object in the database that will be upgraded.
    /// </summary>
    private class DbObjectUpgrade
    {
        /// <summary>
        /// Gets or sets the unique identifier of the object.
        /// </summary>
        public Guid Uid { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the object was created.
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the object was last modified.
        /// </summary>
        public DateTime DateModified { get; set; }
    }

    /// <summary>
    /// Represents a file in the library that will be upgraded.
    /// </summary>
    private class LibraryFileUpgrade
    {
        /// <summary>
        /// Gets or sets the unique identifier of the file.
        /// </summary>
        public Guid Uid { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the file was created.
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the file was last modified.
        /// </summary>
        public DateTime DateModified { get; set; }

        /// <summary>
        /// Gets or sets the date and time when processing of the file started.
        /// </summary>
        public DateTime ProcessingStarted { get; set; }

        /// <summary>
        /// Gets or sets the date and time when processing of the file ended.
        /// </summary>
        public DateTime ProcessingEnded { get; set; }

        /// <summary>
        /// Gets or sets the date and time until which the file is held.
        /// </summary>
        public DateTime HoldUntil { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the file was created in the filesystem.
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the file was last written to in the filesystem.
        /// </summary>
        public DateTime LastWriteTime { get; set; }
    }
    
    
    /// <summary>
    /// Statistic saved in the database
    /// </summary>
    public class DbStatisticOld
    {
        /// <summary>
        /// Gets or sets when the statistic was recorded
        /// </summary>
        public DateTime LogDate { get; set; }

        /// <summary>
        /// Gets or sets the name of the statistic
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type
        /// </summary>
        public StatisticType Type { get; set; }
    
        /// <summary>
        /// Gets or sets the string value
        /// </summary>
        public string StringValue { get; set; }
    
        /// <summary>
        /// Gets or sets the number value
        /// </summary>
        public double NumberValue { get; set; }
        
        /// <summary>
        /// Statistic types
        /// </summary>
        public enum StatisticType
        {
            /// <summary>
            /// String statistic
            /// </summary>
            String = 0,
            /// <summary>
            /// Number statistic
            /// </summary>
            Number = 1
        }
    }
}