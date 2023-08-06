using Microsoft.JSInterop;
using FileFlows.Client.Components;

namespace FileFlows.Client.Services
{
    public interface IClipboardService
    {
        Task CopyToClipboard(string text);
    }

    public class ClipboardService : IClipboardService
    {
        private IJSRuntime JsRuntime { get; set; }

        public ClipboardService(IJSRuntime jsRuntime)
        {
            this.JsRuntime = jsRuntime;
        }

        /// <summary>
        /// Copies text to the clipboard and shows a toast
        /// </summary>
        /// <param name="text">th text to copy</param>
        public async Task CopyToClipboard(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            await JsRuntime.InvokeVoidAsync("ff.copyToClipboard", text);
            Toast.ShowInfo(Translater.Instant("Labels.CopiedToClipboard", new { text }));
        }
    }
}
