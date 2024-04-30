namespace FileFlows.Client.ClientModels;

/// <summary>
/// Model that defines the basics of a form
/// </summary>
public class FormFieldsModel
{
    /// <summary>
    /// Gets or sets the model bound to the form
    /// </summary>
    public ExpandoObject Model { get; set; }
    /// <summary>
    /// Gets or sets the fields of the form
    /// </summary>
    public List<ElementField> Fields { get; set; }
}