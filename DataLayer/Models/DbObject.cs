namespace FileFlows.DataLayer.Models;

using NPoco;

/// <summary>
/// DbObject is an object that is saved to the database
/// </summary>
[PrimaryKey(nameof(Uid), AutoIncrement = false)]
internal class DbObject
{
    /// <summary>
    /// Gets or sets the UID of the object
    /// </summary>
    public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the object
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type (FullName of object type)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the object was created
    /// </summary>
    public DateTime DateCreated { get; set; }
    
    /// <summary>
    /// Gets or sets when the object was last modified
    /// </summary>
    public DateTime DateModified { get; set; }
    
    /// <summary>
    /// Gets or sets the custom JSON data for the object
    /// </summary>
    public string Data { get; set; } = string.Empty;
}