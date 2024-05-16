namespace FileFlows.Client.Components.Common
{
    using Microsoft.AspNetCore.Components;

    public partial class FlowTableColumn<TItem>:ComponentBase where TItem : notnull
    {
        [CascadingParameter] FlowTable<TItem> Table { get; set; }

        [Parameter]
        public RenderFragment Header { get; set; }

        [Parameter]
        public RenderFragment<TItem> Cell { get; set; }

        [Parameter]
        public bool Hidden { get; set; }
        /// <summary>
        /// Gets or sets if this is a pre-field
        /// </summary>
        [Parameter] public bool Pre { get; set; }
        /// <summary>
        /// Gets or sets if this has no height field set
        /// </summary>
        [Parameter] public bool NoHeight { get; set; }

        /// <summary>
        /// Gets or sets the class name for the table column
        /// </summary>
        public string ClassName => string.IsNullOrWhiteSpace(Width) ? "fillspace" : string.Empty;
        [Parameter] public string Width { get; set; }

        /// <summary>
        /// Gets or sets the width for mobile devices
        /// </summary>
        [Parameter]
        public string MobileWidth { get; set; }


        /// <summary>
        /// Gets or sets the width for very large displays
        /// </summary>
        [Parameter]
        public string LargeWidth { get; set; }
        
        /// <summary>
        /// Gets or sets if the header alignment
        /// </summary>
        [Parameter]
        public FlowTableAlignment HeaderAlign { get; set; }
        
        /// <summary>
        /// Gets or sets if the column alignment
        /// </summary>
        [Parameter] public FlowTableAlignment ColumnAlign { get; set; }

        /// <summary>
        /// Shortcut for setting both header and column alignment
        /// </summary>
        [Parameter]
        public FlowTableAlignment? Align { get; set; }

        private string _MinWidth = string.Empty;    
        /// <summary>
        /// Gets or sets the minimum width of the column
        /// </summary>
        [Parameter]
        public string MinWidth { get; set; }

        [Parameter]
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets the style
        /// </summary>
        public string Style => string.IsNullOrEmpty(MinWidth) ? string.Empty : $"min-width: {MinWidth};";

        protected override void OnInitialized()
        {
            this.Table.AddColumn(this);
        }

    }
}

/// <summary>
/// Alignment options for a flow table
/// </summary>
public enum FlowTableAlignment 
{
    /// <summary>
    /// Left align
    /// </summary>
    Left,
    /// <summary>
    /// Center align
    /// </summary>
    Center,
    /// <summary>
    /// Right align
    /// </summary>
    Right
}