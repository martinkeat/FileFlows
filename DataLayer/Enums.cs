namespace FileFlows.DataLayer;


/// <summary>
/// Database creation result
/// </summary>
public enum DbCreateResult
{
    /// <summary>
    /// Database created
    /// </summary>
    Created = 1,
    /// <summary>
    /// Database already existed
    /// </summary>
    AlreadyExisted = 2
}