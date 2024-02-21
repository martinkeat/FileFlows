using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v24.02
/// </summary>
public class Upgrade_24_02
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 24.02 upgrade script");
        AddFailureReasonField();
        AddProcessOnNodeUidField();
    }

    private void AddFailureReasonField()
    {
        var manager = DbHelper.GetDbManager();
        if (manager.ColumnExists("LibraryFile", "FailureReason").Result)
            return;
        Logger.Instance.ILog("LibraryFile.FailureReason does not exist, adding");

        string sql = "ALTER TABLE LibraryFile " +
                     " ADD FailureReason               VARCHAR(512) ";
        if (manager is MySqlDbManager)
            sql += " COLLATE utf8_unicode_ci ";
        sql += " NOT NULL    DEFAULT('')".Trim();
        
        manager.Execute(sql, null).Wait();
    }

    private void AddProcessOnNodeUidField()
    {
        var manager = DbHelper.GetDbManager();
        if (manager.ColumnExists("LibraryFile", "ProcessOnNodeUid").Result)
            return;
        Logger.Instance.ILog("LibraryFile.ProcessOnNodeUid does not exist, adding");

        string sql = "ALTER TABLE LibraryFile " +
                     " ADD ProcessOnNodeUid               varchar(36) ";
        if (manager is MySqlDbManager)
            sql += " COLLATE utf8_unicode_ci ";
        sql += " NOT NULL    DEFAULT('')".Trim();
        
        manager.Execute(sql, null).Wait();
    }
}