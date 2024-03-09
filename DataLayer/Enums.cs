namespace FileFlows.DataLayer;


/// <summary>
/// Database creation result
/// </summary>
public enum DbCreateResult
{
    /// <summary>
    /// Failed to create
    /// </summary>
    Failed = 0,
    /// <summary>
    /// Database created
    /// </summary>
    Created = 1,
    /// <summary>
    /// Database already existed
    /// </summary>
    AlreadyExisted = 2
}