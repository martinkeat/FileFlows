namespace FileFlows.Managers;

/// <summary>
/// Manager for the Auditing
/// </summary>
public class AuditManager
{
    private FairSemaphore _semaphore = new(1);

    /// <summary>
    /// Gets or sets if audits should be performed
    /// </summary>
    public static bool PerformAudits
    {
        get => DbAuditManager.PerformAudits;
        set => DbAuditManager.PerformAudits = value;
    }


    /// <summary>
    /// Performs a search of the audit log
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the result</returns>
    public Task<List<AuditEntry>> Search(AuditSearchFilter filter)
        => DatabaseAccessManager.Instance.AuditManager.Search(filter);

    /// <summary>
    /// Gets the audit history for a specific object
    /// </summary>
    /// <param name="type">the type of object</param>
    /// <param name="uid">the UID of the object</param>
    /// <returns>the audit history of the object</returns>
    public Task<List<AuditEntry>> ObjectHistory(string type, Guid uid)
        => DatabaseAccessManager.Instance.AuditManager.ObjectHistory(type, uid);
    
    /// <summary>
    /// Gets statistic by name
    /// </summary>
    /// <returns>the matching statistic</returns>
    public Task Audit(AuditEntry entry)
        => DatabaseAccessManager.Instance.AuditManager.Insert(entry);
}