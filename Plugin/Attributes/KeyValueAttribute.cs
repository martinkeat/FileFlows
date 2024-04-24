namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Represents a custom attribute for key-value pairs in a form input.
/// </summary>
public class KeyValueAttribute : FormInputAttribute
{
    /// <summary>
    /// Gets or sets the property that holds additional options for the key-value pair.
    /// </summary>
    public string? OptionsProperty { get; set; }
    
    /// <summary>
    /// Gets or sets if variables should be shown
    /// </summary>
    public bool ShowVariables { get; set; }
    
    /// <summary>
    /// Gets or sets if duplicates are allowed
    /// </summary>
    public bool AllowDuplicates { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyValueAttribute"/> class.
    /// </summary>
    /// <param name="order">The order of the form input.</param>
    /// <param name="optionsProperty">The property that holds additional options for the key-value pair.</param>
    /// <param name="showVariables">if variables should be shown</param>
    /// <param name="allowDuplicates">if duplicates are allowed</param>
    public KeyValueAttribute(int order, string? optionsProperty = null, bool showVariables = false, bool allowDuplicates = false) : base(
        FormInputType.KeyValue, order)
    {
        this.OptionsProperty = optionsProperty;
        this.ShowVariables = showVariables;
        this.AllowDuplicates = allowDuplicates;
    }
}
