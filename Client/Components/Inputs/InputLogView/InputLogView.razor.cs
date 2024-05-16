using Microsoft.JSInterop;
using System.Text;
using System.Timers;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;
public partial class InputLogView : Input<string>, IDisposable
{
    [Parameter] public string RefreshUrl { get; set; }

    [Parameter] public int RefreshSeconds { get; set; }

    private string Colorized { get; set; }

    private string PreviousValue { get; set; }

    private Timer RefreshTimer;
    private bool Refreshing = false;
    private bool scrollToBottom = false;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        this.Colorized = Colorize(this.Value);

        if (string.IsNullOrEmpty(RefreshUrl) == false)
        {
            this.RefreshTimer = new Timer();
            this.RefreshTimer.AutoReset = true;
            this.RefreshTimer.Interval = RefreshSeconds > 0 ? RefreshSeconds * 1000 : 10_000;
            this.RefreshTimer.Elapsed += RefreshTimerElapsed!;
            this.RefreshTimer.Start();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (scrollToBottom)
        {
            await jsRuntime.InvokeVoidAsync("ff.scrollToBottom", new object[]{  ".editor .fields"});
            scrollToBottom = false;
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        if (this.RefreshTimer != null)
        {
            this.RefreshTimer.Stop();
            this.RefreshTimer.Elapsed -= RefreshTimerElapsed!;
        }
    }

    private void RefreshTimerElapsed(object sender, EventArgs e)
    {
        if (Refreshing)
            return;
        Refreshing = true;
        Task.Run(async () =>
        {
            try
            {
                bool nearBottom = await jsRuntime.InvokeAsync<bool>("ff.nearBottom", new object[]{ ".editor .fields"});
                
                var refreshResult = await HttpHelper.Get<string>(this.RefreshUrl);
                if (refreshResult.Success == false)
                    return;
                this.Value = refreshResult.Data;
                this.Colorized = Colorize(this.Value);
                this.scrollToBottom = nearBottom;
                this.StateHasChanged();
            }
            catch (Exception) { }
            finally
            {
                Refreshing = false;
            }
        });
    }

    private string Colorize(string log)
    {
        if (log == null)
            return string.Empty;

        if (log.IndexOf("<div") >= 0)
            return log;

        StringBuilder colorized = new StringBuilder();
        if (string.IsNullOrWhiteSpace(PreviousValue) == false && log.StartsWith(PreviousValue))
        {
            // this avoid us from redoing stuff we've already done
            colorized.Append(this.Colorized);
            log = log.Substring(PreviousValue.Length).TrimStart();
        }
        PreviousValue = log;

        colorized.Append(LogToHtml.Convert(log));
        string result = colorized.ToString();
        return result;
    }

}