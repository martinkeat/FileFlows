using System.Text;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Helpers;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Upgrades;

/// <summary>
/// Upgrades for 24.03.2
/// Changing DateTimes to UTC
/// </summary>
class Upgrade_24_03_2
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
        // Get the local timezone of the machine
        TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
        string localTimeZoneName = localTimeZone.Id;

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
                sql.AppendLine("update RevisionedObject set RevisionDate = " + connector.FormatDateQuoted(r.RevisionDate) +
                               ", RevisionCreated = " + connector.FormatDateQuoted(r.RevisionCreated) +
                               $" where Uid = '{r.Uid}';");
            }
            db.Db.Execute(sql.ToString());
            
            // LibraryFiles
            var libFiles = db.Db.Fetch<LibraryFileUpgrade>("select Uid, DateCreated, DateModified, ProcessingStarted, ProcessingEnded, HoldUntil, CreationTime, LastWriteTime from LibraryFile");
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
                sql.AppendLine("update LibraryFile set DateCreated = " + connector.FormatDateQuoted(lf.DateCreated) +
                               ", DateModified = " + connector.FormatDateQuoted(lf.DateModified) +
                               ", ProcessingStarted = " + connector.FormatDateQuoted(lf.ProcessingStarted) +
                               ", ProcessingEnded = " + connector.FormatDateQuoted(lf.ProcessingEnded) +
                               ", HoldUntil = " + connector.FormatDateQuoted(lf.HoldUntil) +
                               ", CreationTime = " + connector.FormatDateQuoted(lf.CreationTime) +
                               ", LastWriteTime = " + connector.FormatDateQuoted(lf.LastWriteTime) +
                               $" where Uid = '{lf.Uid}';");
            }
            db.Db.Execute(sql.ToString());

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
}