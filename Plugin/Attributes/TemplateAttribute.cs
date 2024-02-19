namespace FileFlows.Plugin.Attributes;

/// <summary>
/// A text input that accepts a template
/// </summary>
public class TemplateAttribute : FormInputAttribute
{
    /// <summary>
    /// Constructs a template attribute
    /// </summary>
    /// <param name="order">the order this field appears on the page</param>
    public TemplateAttribute(int order) : base(FormInputType.Template, order) { }
}