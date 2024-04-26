namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Attribute to add a file size to a form
/// </summary>
public class FileSizeAttribute : FormInputAttribute
{
    /// <summary>
    /// Initailizes a new instance of the attribute
    /// </summary>
    /// <param name="order">the order</param>
    public FileSizeAttribute(int order) : base(FormInputType.FileSize, order)
    {
    }
}