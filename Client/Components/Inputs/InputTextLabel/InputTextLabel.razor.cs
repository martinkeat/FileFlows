namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;

    public partial class InputTextLabel : Input<object>
    {
        [Parameter] public bool Pre { get; set; }
        [Parameter] public bool Link { get; set; }
        [Parameter] public string Formatter { get; set; }
        
        /// <summary>
        /// Gets or sets if this is an error label
        /// </summary>
        [Parameter] public bool Error { get; set; }

        private bool isHtml = false;
        
        /// <summary>
        /// Gets or sets the clipboard service
        /// </summary>
        [Inject] IClipboardService ClipboardService { get; set; }   

        private string StringValue { get; set; }

        private string lblTooltip;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            this.FormatStringValue();
            this.lblTooltip = Translater.Instant("Labels.CopyToClipboard");
        }

        protected override void ValueUpdated()
        {
            base.ValueUpdated();
            FormatStringValue();
        }

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
                    sValue = dt.ToString("d MMMM yyyy, h:mm:ss tt");
                else
                    sValue = Value.ToString();
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

        async Task CopyToClipboard()
        {
            await ClipboardService.CopyToClipboard(this.StringValue);

        }
    }
}