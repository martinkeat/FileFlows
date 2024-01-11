namespace FileFlows.Shared.Models;

/// <summary>
/// Model for the settings in the UI, differs from the actual settings model as this some combined settings
/// </summary>
public class SettingsUiModel:Settings
{

    /// <summary>
    /// Gets or sets the license email
    /// </summary>
    public string LicenseEmail { get; set; }

    /// <summary>
    /// Gets or sets the licensed key
    /// </summary>
    public string LicenseKey { get; set; }
    
    /// <summary>
    /// Gets or sets the number of they can process
    /// </summary>
    public string LicenseFiles { get; set; }

    /// <summary>
    /// Gets the license flags for the user
    /// </summary>
    public LicenseFlags LicenseFlags { get; set; }
    /// <summary>
    /// Gets the license expiry date for the user
    /// </summary>
    public DateTime LicenseExpiryDate { get; set; }
    /// <summary>
    /// Gets the licensed processing nodes for the user
    /// </summary>
    public int LicenseProcessingNodes { get; set; }
    /// <summary>
    /// Gets the licensed status for the user
    /// </summary>
    public string LicenseStatus { get; set; }

    /// <summary>
    /// Gets or sets the type of database to use
    /// </summary>
    public DatabaseType DbType { get; set; }
    
    /// <summary>
    /// Gets or sets the db server to use
    /// </summary>
    public string DbServer { get; set; }
    
    /// <summary>
    /// Gets or sets the db port to use
    /// </summary>
    public int DbPort { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the database
    /// </summary>
    public string DbName { get; set; }
    
    /// <summary>
    /// Gets or sets the user used to connect to the database
    /// </summary>
    public string DbUser { get; set; }
    
    /// <summary>
    /// Gets or sets the password used to connect to the database
    /// </summary>
    public string DbPassword { get; set; }

    /// <summary>
    /// Gets or sets if the database should be recreated if it already exists
    /// </summary>
    public bool RecreateDatabase { get; set; }
    
    /// <summary>
    /// Gets or sets the file server all list
    /// </summary>
    public string FileServerAllowedPathsString { get; set; }

}