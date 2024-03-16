// using System.Data;
// using System.Data.Common;
// using System.Text.RegularExpressions;
// using FileFlows.DataLayer.Helpers;
// using FileFlows.Plugin;
// using FileFlows.ServerShared;
//
// namespace FileFlows.DataLayer;
//
// /// <summary>
// /// Database wrapper so can log every command to the database
// /// </summary>
// public class FlowDatabase:NPoco.Database
// {
//     /// <summary>
//     /// Gets the logger for the database
//     /// </summary>
//     public static FileLogger Logger { get;private set; }
//
//     static FlowDatabase()
//     {
//         Logger = new FileLogger(DirectoryHelper.LoggingDirectory, "FileFlowsDB", register: false);
//     }
//     
//     /// <summary>
//     /// Constructs an instance of the flow database
//     /// </summary>
//     /// <param name="connectionString">the connection string</param>
//     public FlowDatabase(string connectionString) : base(connectionString, null,
//         MySqlConnector.MySqlConnectorFactory.Instance)
//     {
//         
//     }
//
//     private Dictionary<int, DateTime> ExecutedCommands = new Dictionary<int, DateTime>();
//
//     private readonly Dictionary<string, string> NodeNames = new();
//
//     private string GetNodeName(string uid)
//     {
//         if (NodeNames.ContainsKey(uid))
//             return NodeNames[uid];
//         string name = this.ExecuteScalar<string>("select Name from DbObject where Uid = @0", uid)?.EmptyAsNull() ?? "UNKNOWN";
//         lock (NodeNames)
//         {
//             if(NodeNames.ContainsKey(uid) == false)
//                 NodeNames.Add(uid, name);
//         }
//
//         return name;
//     }
//
//     protected override void OnExecutingCommand(DbCommand cmd)
//     {
//         int hashCode = cmd.GetHashCode();
//         lock (ExecutedCommands)
//         {
//             if (ExecutedCommands.ContainsKey(hashCode))
//                 ExecutedCommands[hashCode] = DateTime.UtcNow;
//             else
//                 ExecutedCommands.Add(hashCode, DateTime.UtcNow);
//         }
//         //
//         // string sql = GetCommandText(cmd);
//         // //Logger.Log((LogType) 999, $"Executing [{hashCode.ToString("00000000")}]: " + sql);
//         // if (sql.StartsWith(
//         //         "SELECT `Uid` AS `Uid`, `Name` AS `Name`, `Type` AS `Type`, `DateCreated` AS `DateCreated`, `DateModified` AS `DateModified`, `Data` AS `Data` FROM `DbObject` where Type=\"FileFlows.Shared.Models.LibraryFile\" and Uid"))
//         // {
//         //     Logger.Log(LogType.Debug, "SQL: " + sql + Environment.NewLine + Environment.StackTrace);
//         // }
//         
//     }
//
//     protected override void OnExecutedCommand(DbCommand cmd)
//     {
//         int hashCode = cmd.GetHashCode();
//         DateTime started;
//         lock (ExecutedCommands)
//         {
//             if (ExecutedCommands.ContainsKey(hashCode) == false)
//                 return;
//             started = ExecutedCommands[hashCode];
//             ExecutedCommands.Remove(hashCode);
//         }
//
//         var time = DateTime.UtcNow.Subtract(started);
//         string sql = GetCommandText(cmd);
//         if (sql.Contains("LogMessage"))
//             return; // dont log these sql = "LOGMESSAGE";
//         else if (sql.Contains("call GetLibraryFiles(1,") && sql.Contains(", 0, 10, \"\", 0)"))
//             sql = "DASHBOARD: RECENTLY FINISHED";
//         else if (sql.Contains("call GetLibraryFiles(0,") && sql.Contains(", 0, 10, \"\", 0)"))
//             sql = "DASHBOARD: UPCOMING";
//         else if(sql.StartsWith("SELECT DAYOFWEEK(js_ProcessingStarted) AS day, HOUR(js_ProcessingStarted) as hour, COUNT(Uid) as count"))
//             sql = "DASHBOARD: ProcessingHeatMap";
//         else if(sql.StartsWith("update DbObject set Data = json_set(Data, '$.LastSeen', "))
//         {
//             string lastSeen = Regex.Match(sql, @"(?<=('\$\.LastSeen', '))[^']+").Value;
//             string node = Regex.Match(sql, @"(?<=(and Uid = '))[^']+").Value;
//             node = GetNodeName(node);
//             sql = $"LastSeen '{node}': {lastSeen}";
//         }
//         else if(sql.StartsWith("UPDATE `DbObject` SET `Name` = \""))
//         {
//             string name = Regex.Match(sql, @"(?<=(UPDATE `DbObject` SET `Name` = ""))[^""]+").Value;
//             sql = $"Updated DbObject '{name}'";
//         }
//         LogType lg = LogType.Info;
//         if (time.TotalSeconds > 1)
//             lg = LogType.Error;
//         if (time.TotalMilliseconds > 100)
//             lg = LogType.Warning;
//         
//         Logger.Log(lg, $"Executed [{hashCode.ToString("00000000")}] [{time}]: " + sql);
//     }
//
//     private string GetCommandText(DbCommand cmd)
//     {
//         if (cmd.Parameters.Count == 0)
//             return cmd.CommandText;
//         
//         string sql = cmd.CommandText;
//         int count = -1;
//         foreach (DbParameter param in cmd.Parameters)
//         {
//             ++count;
//             if (param.DbType == DbType.String || param.DbType == DbType.Guid)
//                 sql = sql.Replace("@" + count, "\"" + param.Value.ToString().Replace("\"", "\"\"") + "\"");
//             else
//                 sql = sql.Replace("@" + count, param.Value.ToString());
//         }
//
//         return sql;
//     }
// }