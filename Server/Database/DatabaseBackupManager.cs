using MySql.Data.MySqlClient;

namespace FileFlows.Server.Database;
/// <summary>
/// A utility class for managing database backups.
/// </summary>
public class DatabaseBackupManager
{
    /// <summary>
    /// Creates a database backup with the specified connection string, backup directory, and version.
    /// </summary>
    /// <param name="connectionString">The connection string for the database.</param>
    /// <param name="backupDirectory">The directory where the backup file will be stored.</param>
    /// <param name="version">The version of the backup.</param>
    /// <returns>The name of the backup file.</returns>
    public static string CreateBackup(string connectionString, string backupDirectory, Version version)
    {
        string backupFileName = $"FileFlows-{version.Major}.{version.Minor}.{version.Build}.sql";
        string backupFilePath = Path.Combine(backupDirectory, backupFileName);

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                using (MySqlBackup mb = new MySqlBackup(cmd))
                {
                    cmd.Connection = conn;
                    conn.Open();
                    mb.ExportToFile(backupFilePath);
                    conn.Close();
                }
            }
        }

        DeleteOldBackups(backupDirectory);

        return backupFileName;
    }

    /// <summary>
    /// Deletes older backup files if there are more than 5 files in the backup directory.
    /// </summary>
    /// <param name="backupDirectory">The directory where the backup files are stored.</param>
    private static void DeleteOldBackups(string backupDirectory)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(backupDirectory);
        FileInfo[] backupFiles = directoryInfo.GetFiles("FileFlows-*.sql")
            .OrderByDescending(file => file.LastWriteTime)
            .ToArray();

        if (backupFiles.Length > 5)
        {
            for (int i = 5; i < backupFiles.Length; i++)
            {
                backupFiles[i].Delete();
            }
        }
    }
}
