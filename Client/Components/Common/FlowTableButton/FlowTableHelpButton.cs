using FileFlows.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace FileFlows.Client.Components.Common
{
    public class FlowTableHelpButton : FlowTableButton
    {
        [Inject] IJSRuntime jsRuntime { get; set; }

        [Parameter]
        public string HelpUrl { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            this._Icon = "fas fa-question-circle";
            this._Label = Translater.Instant("Labels.Help");
        }

        public override async Task OnClick()
        {
            string url = this.HelpUrl;            
            if (string.IsNullOrEmpty(HelpUrl))
                url = "https://fileflows.com/docs";
            else if (url.ToLower().StartsWith("http") == false)
                url = "https://fileflows.com/docs/" + url;
            await jsRuntime.InvokeVoidAsync("open", url.ToLower(), "_blank");
        }
    }
}
