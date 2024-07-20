namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Attribute for Text inputs
/// </summary>
/// <param name="order">the order this input appears</param>
public class TextAttribute(int order) : FormInputAttribute(FormInputType.Text, order) { }