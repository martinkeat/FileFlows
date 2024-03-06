namespace FileFlows.DataLayer;


/// <summary>
/// Statistic types
/// </summary>
public enum StatisticType
{
    /// <summary>
    /// String statistic
    /// </summary>
    String = 0,
    /// <summary>
    /// Number statistic
    /// </summary>
    Number = 1
}

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