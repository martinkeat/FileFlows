namespace FileFlows.Client.Components.Common
{
    using Microsoft.AspNetCore.Components;

    public partial class FlowTableColumn<TItem>:ComponentBase
    {
        [CascadingParameter] FlowTable<TItem> Table { get; set; }

        [Parameter]
        public RenderFragment Header { get; set; }

        [Parameter]
        public RenderFragment<TItem> Cell { get; set; }

        [Parameter]
        public bool Hidden { get; set; }
        [Parameter] public bool Pre { get; set; }

        string _Width = string.Empty;
        string className = "fillspace";
        string style = string.Empty;
        [Parameter]
        public string Width
        {
            get => _Width;
            set
            {
                _Width = value ?? string.Empty;
                if (_Width != string.Empty) {
                    className = string.Empty;
                    //style = $"width:{_Width};min-width:{_Width};max-width:{_Width}";
                    //style = string.Empty;
                }
                else{
                    //style = string.Empty;
                }
            }
        }

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

        private FlowTableAlignment _HeaderAlign, _ColumnAlign;
        /// <summary>
        /// Gets or sets if the header alignment
        /// </summary>
        [Parameter]
        public FlowTableAlignment HeaderAlign { get => _HeaderAlign; set => _HeaderAlign = value; }
        
        /// <summary>
        /// Gets or sets if the column alignment
        /// </summary>
        [Parameter]
        public FlowTableAlignment ColumnAlign { get => _ColumnAlign; set => _ColumnAlign = value; }


        /// <summary>
        /// Shortcut for setting both header and column alignment
        /// </summary>
        [Parameter]
        public FlowTableAlignment Align
        {
            set
            {
                _ColumnAlign = value;
                _HeaderAlign = value;
            }
        }
        
        private string _MinWidth = string.Empty;    
        /// <summary>
        /// Gets or sets the minimum width of the column
        /// </summary>
        [Parameter]
        public string MinWidth
        {
            get => _MinWidth;
            set
            {
                _MinWidth = value ?? string.Empty;
                if (_MinWidth == string.Empty)
                    style = string.Empty;
                else
                    style = $"min-width: {_MinWidth};";

            }
        }

        [Parameter]
        public string ColumnName { get; set; }


        public string ClassName => className;
        public string Style => style;

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