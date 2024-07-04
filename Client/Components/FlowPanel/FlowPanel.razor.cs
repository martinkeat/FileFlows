using Microsoft.Extensions.Logging;

namespace FileFlows.Client.Components;

using FileFlows.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

/// <summary>
/// A flow panel is container that contains fields for an editor form
/// </summary>
public partial class FlowPanel:ComponentBase
{
    /// <summary>
    /// Gets or sets the fields in the flow panel
    /// </summary>
    [Parameter]
    public List<ElementField> Fields { get; set; }
    
//     private List<ElementField> _Fields;
//     
// #pragma warning disable BL0007
//     /// <summary>
//     /// Gets or sets the fields in the flow panel
//     /// </summary>
//     [Parameter]
//     public List<ElementField> Fields
//     {
//         get => _Fields;
//         set
//         {
//             _Fields = value;
//             this.StateHasChanged();
//         }
//     }
// #pragma warning restore BL0007

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

    // public void SetFields(List<ElementField> fields)
    // {
    //     Logger.Instance.ILog("Setting fields", fields);
    //     this._Fields = fields;
    //     this.StateHasChanged();
    // }
}
