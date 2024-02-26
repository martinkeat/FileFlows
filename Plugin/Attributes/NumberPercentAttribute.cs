namespace FileFlows.Plugin.Attributes;

/// <summary>
/// A Input Number GUI element that will allow for integers or percentages
/// </summary>
public class NumberPercentAttribute : FormInputAttribute
{
    /// <summary>
    /// Gets or sets the unit for htis
    /// </summary>
    public string Unit { get; set; }
    
    /// <summary>
    /// Gets or sets the default value
    /// </summary>
    public int DefaultValue { get; set; }
    
    /// <summary>
    /// Gets or sets if the percentage should be shown by default
    /// </summary>
    public bool DefaultPercentage { get; set; }

    /// <summary>
    /// Constructs a new instance
    /// </summary>
    /// <param name="order">the order this will appear in the GUI</param>
    /// <param name="unit">the unit to show</param>
    /// <param name="defaultValue">the default value for the field</param>
    /// <param name="defaultPercentage">if the percentage should be shown by default</param>
    public NumberPercentAttribute(int order, string unit, int defaultValue, bool defaultPercentage) : base(FormInputType.NumberPercent, order)
    {
        this.Unit = unit;
        this.DefaultValue = defaultValue;
        this.DefaultPercentage = defaultPercentage;
    }
}
