namespace FileFlows.Client.Components.Inputs
{
    using Microsoft.AspNetCore.Components;

    public partial class InputTextLabel : Input<object>
    {
        [Parameter] public bool Pre { get; set; }
        [Parameter] public bool Link { get; set; }
        [Parameter] public string Formatter { get; set; }

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
                if(string.IsNullOrWhiteSpace(Formatter) == false)
                    sValue = FileFlows.Shared.Formatters.Formatter.Format(Formatter, Value);
                else  if (Value is long longValue)
                    sValue = string.Format("{0:n0}", longValue);
                else if (Value is int intValue)
                    sValue = string.Format("{0:n0}", intValue);
                else if (Value is DateTime dt)
                    sValue = dt.ToString("d MMMM yyyy, h:mm:ss tt");
                else
                    sValue = Value.ToString();
            }
            StringValue = sValue;
        }

        async Task CopyToClipboard()
        {
            await ClipboardService.CopyToClipboard(this.StringValue);

        }
    }
}