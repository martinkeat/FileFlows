namespace FileFlows.Managers;

/// <summary>
/// Initializes any manager settings
/// </summary>
public class Initializer
{
    // may move this
    public static void Init(string encryptionKey)
    {
        DataLayer.Helpers.Decrypter.EncryptionKey = encryptionKey;
    }
}