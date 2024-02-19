namespace FileFlows.Plugin.Attributes;

/// <summary>
/// A text input that accepts variables
/// </summary>
public class TextVariableAttribute : FormInputAttribute
{
    /// <summary>
    /// Constructs a text variable attribute
    /// </summary>
    /// <param name="order">the order this field appears on the page</param>
    public TextVariableAttribute(int order) : base(FormInputType.TextVariable, order) { }
}