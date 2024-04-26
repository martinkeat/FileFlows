namespace FileFlows.Plugin.Attributes;

/// <summary>
/// TextArea attribute which adds a TextArea to an editor
/// </summary>
public class TextAreaAttribute : FormInputAttribute
{
    /// <summary>
    /// Gets if variables should be shown
    /// </summary>
    public bool Variables { get; init; }

    /// <summary>
    /// Initializes a new instance of the text area attribute
    /// </summary>
    /// <param name="order">the order the input appears</param>
    /// <param name="variables">if variables should be shown</param>
    public TextAreaAttribute(int order, bool variables = false) : base(FormInputType.TextArea, order)
    {
        Variables = variables;
    }
}