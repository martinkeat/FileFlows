using FileFlows.Plugin;

namespace FileFlows.Managers;

/// <summary>
/// Initializes any manager settings
/// </summary>
public class Initializer
{
    // may move this
    public static Result<bool> Init(ILogger logger,DatabaseType dbType, string connectionString, string encryptionKey)
    {
        DataLayer.Helpers.Decrypter.EncryptionKey = encryptionKey;
        return DatabaseAccessManager.Initialize(logger, dbType, connectionString);
    }
}