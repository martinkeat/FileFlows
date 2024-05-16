using System.Collections;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// An input table
/// </summary>
public partial class InputTable:Input<object>
{
    /// <summary>
    /// Gets or sets the type of items rendered in the table
    /// </summary>
    [Parameter] public Type TableType { get; set; }
    
    /// <summary>
    /// Gets or sets the columns to show
    /// </summary>
    [Parameter] public List<InputTableColumn> Columns { get; set; }
    
    /// <summary>
    /// Gets or sets the action to execute when an item is selected,
    /// either via double click or touch
    /// </summary>
    [Parameter] public Action<object> SelectAction { get; set; }

    private Dictionary<string, PropertyInfo> Properties = new();

    /// <summary>
    /// The data rendered in the table
    /// </summary>
    private IList Data;

    /// <summary>
    /// If the table allow selection and has a select action
    /// </summary>
    private bool AllowSelection;

    protected override void OnInitialized()
    {
        Properties = TableType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(x => x.Name, x => x);
        
        Data = Value as IList;
        if (Data == null)
        {
            Type genericListType = typeof(List<>).MakeGenericType(TableType);
            Data = (IList)Activator.CreateInstance(genericListType)!;
        }

        AllowSelection = SelectAction != null;
    }

    private string GetValueString(InputTableColumn column, object value)
    {
        if (value == null || string.IsNullOrWhiteSpace(column.Property) || Properties.ContainsKey(column.Property) == false)
            return string.Empty;
        var prop = Properties[column.Property];
        var pValue = prop.GetValue(value);
        if (pValue == null)
            return string.Empty;
        if (pValue is DateTime dt)
            return dt.ToLocalTime().ToString(CultureInfo.CurrentCulture);
        return pValue.ToString();
    }

    private void Select(object item)
        => SelectAction?.Invoke(item);
}


/// <summary>
/// An input table column
/// </summary>
public class InputTableColumn
{
    /// <summary>
    /// Gets the name of the column
    /// This is the text rendered in the header
    /// </summary>
    public string Name { get; init; }
    /// <summary>
    /// Gets the width of the column
    /// </summary>
    public string Width { get; init; }
    /// <summary>
    /// Gets the property to render in this column, this is the property from the row object
    /// </summary>
    public string Property { get; init; }
}