namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Represents a custom attribute for key-value pairs in a form input.
/// </summary>
public class KeyValueAttribute : FormInputAttribute
{
    /// <summary>
    /// Gets or sets the property that holds additional options for the key-value pair.
    /// </summary>
    public string OptionsProperty { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyValueAttribute"/> class.
    /// </summary>
    /// <param name="order">The order of the form input.</param>
    /// <param name="optionsProperty">The property that holds additional options for the key-value pair.</param>
    public KeyValueAttribute(int order, string optionsProperty) : base(FormInputType.KeyValue, order)
    {
        this.OptionsProperty = optionsProperty;
    }
}
