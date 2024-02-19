namespace FileFlows.Plugin.Attributes;

/// <summary>
/// A text input that accepts a template
/// </summary>
public class TemplateAttribute : FormInputAttribute
{
    /// <summary>
    /// Gets or sets an optional property that contains template definitions to help the user create a template
    /// </summary>
    public string OptionsProperty { get; set; }

    /// <summary>
    /// Constructs a template attribute
    /// </summary>
    /// <param name="order">the order this field appears on the page</param>
    /// <param name="optionsProperty">Optional property that contains template options</param>
    public TemplateAttribute(int order, string optionsProperty = null) : base(FormInputType.Template, order)
    {
        this.OptionsProperty = optionsProperty;
    }
}