namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Attribute for Date Compare inputs
/// </summary>
/// <param name="order">the order this input appears</param>
public class DateCompareAttribute(int order) : FormInputAttribute(FormInputType.DateCompare, order) { }