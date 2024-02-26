namespace FileFlows.Plugin.Attributes;

/// <summary>
/// A Input Number GUI element that will allow for integers to be entered
/// </summary>
public class TimeAttribute : FormInputAttribute
{
    /// <summary>
    /// Constructs an instance of the time attribute
    /// </summary>
    /// <param name="order">the order the field appears in the form</param>
    public TimeAttribute(int order) : base(FormInputType.Time, order) { }
}
