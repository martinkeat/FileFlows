namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Attribute to show a math value
/// </summary>
public class MathValueAttribute : FormInputAttribute
{
    /// <summary>
    /// Initializes a new instance of the mnath value attribute
    /// </summary>
    /// <param name="order">the order this field will appear</param>
    public MathValueAttribute(int order) : base(FormInputType.MathValue, order) { }
}
