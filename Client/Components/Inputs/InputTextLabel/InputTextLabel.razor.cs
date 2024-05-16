namespace FileFlows.Client.Components.Inputs;

using Microsoft.AspNetCore.Components;

/// <summary>
/// Input that shows a text label
/// </summary>
public partial class InputTextLabel : Input<object>
{
    /// <summary>
    /// Gets or sets if this should be rendered in a pre tag
    /// </summary>
    [Parameter] public bool Pre { get; set; }
    /// <summary>
    /// Gets or sets if this is a link
    /// </summary>
    [Parameter] public bool Link { get; set; }
    /// <summary>
    /// Gets or sets the formatter to use when displaying the label
    /// </summary>
    [Parameter] public string Formatter { get; set; }
    
    /// <summary>
    /// Gets or sets if this is an error label
    /// </summary>
    [Parameter] public bool Error { get; set; }

    /// <summary>
    /// If this is HTML or unsafe text
    /// </summary>
    private bool isHtml = false;
    
    /// <summary>
    /// Gets or sets the clipboard service
    /// </summary>
    [Inject] IClipboardService ClipboardService { get; set; }   

    /// <summary>
    /// Gets or sets the string value
    /// </summary>
    private string StringValue { get; set; }
    
    /// <summary>
    /// The label for the tooltip
    /// </summary>
    private string lblTooltip;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        this.FormatStringValue();
        this.lblTooltip = Translater.Instant("Labels.CopyToClipboard");
    }

    /// <inheritdoc />
    protected override void ValueUpdated()
    {
        base.ValueUpdated();
        FormatStringValue();
    }

    /// <summary>
    /// Formats the string value
    /// </summary>
    void FormatStringValue()
    {
        string sValue = string.Empty;
        if (Value != null)
        {
            if (Formatter?.ToLowerInvariant() == "markdown")
                sValue = FormatMarkdown(Value as string);
            else if(string.IsNullOrWhiteSpace(Formatter) == false)
                sValue = FileFlows.Shared.Formatters.Formatter.Format(Formatter, Value);
            else  if (Value is long longValue)
                sValue = $"{longValue:n0}";
            else if (Value is int intValue)
                sValue = $"{intValue:n0}";
            else if (Value is DateTime dt)
                sValue = dt.ToLocalTime().ToString("d MMMM yyyy, h:mm:ss tt");
            else
                sValue = Value.ToString() ?? string.Empty;
        }
        StringValue = sValue;
    }

    /// <summary>
    /// Renders a markdown string as HTML
    /// </summary>
    /// <param name="value">the markdown string</param>
    /// <returns>the HTML of the string</returns>
    private string FormatMarkdown(string value)
    {
        if(string.IsNullOrWhiteSpace(value))
            return value ?? string.Empty;

        isHtml = true;
        return Markdig.Markdown.ToHtml(value);
    }

    /// <summary>
    /// Copies the string to the clipboard
    /// </summary>
    async Task CopyToClipboard()
    {
        await ClipboardService.CopyToClipboard(this.StringValue);

    }
}