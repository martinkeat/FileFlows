using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

public partial class InputRenderer
{
    /// <summary>
    /// Gets or sets if this input belongs to a script editor
    /// </summary>
    public bool IsScript { get; set; }

    /// <summary>
    /// Gets or sets the field this is rendering
    /// </summary>
    [Parameter]
    public ElementField field { get; set; }
    
    /// <summary>
    /// Gets or sets the Editor this flow panel is used in
    /// </summary>
    [CascadingParameter] public EditorBase Editor { get; set; }

    /// <summary>
    /// Gets or sets an event that is called when the editor is saved
    /// </summary>
    [Parameter] public EventCallback OnSubmit { get; set; }
    
    /// <summary>
    /// Gets or sets an event that is called when the editor is closed
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// Updates a value
    /// </summary>
    /// <param name="field">the field whose value is being updated</param>
    /// <param name="value">the value of the field</param>
    protected void UpdateValue(ElementField field, object value) => Editor.UpdateValue(field, value);

    /// <summary>
    /// Gets a parameter value for a field
    /// </summary>
    /// <param name="field">the field to get the value for</param>
    /// <param name="parameter">the name of the parameter</param>
    /// <param name="default">the default value if not found</param>
    /// <typeparam name="T">the type of parameter</typeparam>
    /// <returns>the parameter value</returns>
    protected T GetParameter<T>(ElementField field, string parameter, T @default = default(T)) => Editor.GetParameter<T>(field, parameter, @default);
    
    
    /// <summary>
    /// Gets the minimum and maximum from a range validator (if exists)
    /// </summary>
    /// <param name="field">The field to get the range for</param>
    /// <returns>the range</returns>
    protected (int min, int max) GetRange(ElementField field) => Editor.GetRange(field);

    /// <summary>
    /// Gets a value for a field
    /// </summary>
    /// <param name="field">the field whose value to get</param>
    /// <param name="default">the default value if none is found</param>
    /// <typeparam name="T">the type of value to get</typeparam>
    /// <returns>the value</returns>
    protected T GetValue<T>(string field, T @default = default(T)) => Editor.GetValue<T>(field, @default);

    /// <summary>
    /// Gets a value for a field
    /// </summary>
    /// <param name="field">the field whose value to get</param>
    /// <param name="type">the type of value to get</param>
    /// <returns>the value</returns>
    protected object GetValue(string field, Type type) => Editor.GetValue(field, type);
    
    /// <summary>
    /// Get the name of the type this editor is editing
    /// </summary>
    protected string TypeName => Editor.TypeName;

    protected override Task OnParametersSetAsync()
    {
        this.IsScript = TypeName != null && Regex.IsMatch(TypeName, @"^Flow\.Parts\.([0-9A-Fa-f]{8}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{4}[-][0-9A-Fa-f]{12})");
        return base.OnParametersSetAsync();
    }

    /// <summary>
    /// Updates the code in a template
    /// </summary>
    /// <param name="template">the template value</param>
    /// <param name="elementField">the template element to update</param>
    private void UpdateTemplateCode(object template, ElementField elementField)
    {
        var strTemplate = template as string;
        if (string.IsNullOrEmpty(strTemplate))
            return;
        UpdateValue(elementField, strTemplate);
    }
}