namespace FileFlows.Client.Components
{
    using Microsoft.AspNetCore.Components;
    using FileFlows.Shared;

    public partial class Fieldset : ComponentBase
    {
        private string _Title;
        private string _OriginalTitle;
#pragma warning disable BL0007 
        [Parameter]
        public string Title
        {
            get => _Title;
            set
            {
                if (_OriginalTitle == value)
                    return;
                _OriginalTitle = value;
                _Title = Translater.TranslateIfNeeded(value);
            }
        }
#pragma warning restore BL0007 

        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}