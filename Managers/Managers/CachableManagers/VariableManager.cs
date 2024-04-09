using FileFlows.DataLayer.Helpers;
using FileFlows.Server.Helpers;
using FileFlows.ServerShared;
using FileFlows.ServerShared.Models;

namespace FileFlows.Managers;

/// <summary>
/// Manager for communicating with FileFlows server for variables
/// </summary>
public class VariableManager : CachedManager<Variable>
{
    /// <summary>
    /// Restore the default variables
    /// </summary>
    public void RestoreDefault()
    {
        var variables = LoadDataFromDatabase().Result;

        string programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");

        foreach (var variable in new[]
                 {
                     ("ffmpeg", VariableHelper.GetDefaultFFmpegLocation()),
                     ("ffprobe", VariableHelper.GetDefaultFFprobeLocation()),
                     ("unrar", Globals.IsWindows ? Path.Combine(programFiles, "WinRAR", "UnRAR.exe") : "unrar"),
                     ("rar", Globals.IsWindows ? Path.Combine(programFiles, "WinRAR", "Rar.exe") : "rar"),
                     ("7zip", Globals.IsWindows ? Path.Combine(programFiles, "7-Zip", "7z.exe") : "7z")
                 })
        {
            var dbVariable = variables.FirstOrDefault(x =>
                string.Equals(x.Name, variable.Item1, StringComparison.InvariantCultureIgnoreCase));
            if (dbVariable == null)
            {
                // doesnt exist, insert it
                try
                {
                    Update(new()
                    {
                        Name = variable.Item1,
                        Value = variable.Item2
                    }, auditDetails: AuditDetails.ForServer()).Wait();
                }
                catch (Exception ex)
                {
                    Logger.Instance.ELog($"Error inserting '{variable.Item1}: " + ex.Message);
                }
            }
            else if (Decrypter.IsPossiblyEncrypted(dbVariable.Value))
            {
                // bad encryption, restore the default
                dbVariable.Value = variable.Item2;
                Update(dbVariable, auditDetails: AuditDetails.ForServer()).Wait();
            }
        }
    }
}