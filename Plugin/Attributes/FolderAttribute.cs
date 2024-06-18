namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Attribute to show a folder picker
/// </summary>
public class FolderAttribute : FormInputAttribute
{
    /// <summary>
    /// Initializes a new instance of the folder attribute
    /// </summary>
    /// <param name="order">the order this field will appear</param>
    public FolderAttribute(int order) : base(FormInputType.Folder, order) { }
}
