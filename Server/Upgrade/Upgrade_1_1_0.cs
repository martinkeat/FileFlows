using FileFlows.Server.Database.Managers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Upgrade;

/// <summary>
/// Upgrade to FileFlows v1.1.0
/// </summary>
public class Upgrade_1_1_0
{
    /// <summary>
    /// Runs the update
    /// </summary>
    /// <param name="settings">the settings</param>
    public void Run(Settings settings)
    {
        Logger.Instance.ILog("Upgrade running, running 1.1.0 upgrade script");
        ConvertNodesProcessing();
    }

    private void ConvertNodesProcessing()
    {
        var manager = DbHelper.GetDbManager();
        string sql =
            "update DbObject set Data = json_set(Data, '$.AllLibraries', @1) where Type = 'FileFlows.Shared.Models.ProcessingNode' and ";
        if(manager is SqliteDbManager)
            sql  += " json_extract(Data, '$.AllLibraries') = @0";
        else
            sql  += " json_value(Data, '$.AllLibraries') = @0";
            
        // in 1.1.0 we changed ProcessingLibraries so 0 is All and 1 is Only
        // this is so the default value of 0, means it will process all libraries and not need the user to edit the node
        manager.Execute(sql, new object [] {0, 99}).Wait(); // first set 0s to 99
        manager.Execute(sql, new object [] {1, 0}).Wait(); // now set 1s to 0s
        manager.Execute(sql, new object [] {99, 1}).Wait(); // now set the 99s to 1
    }
}