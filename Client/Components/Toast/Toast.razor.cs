using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components;

/// <summary>
/// Component for showing a toast message
/// </summary>
public partial class Toast:ComponentBase
{
    /// <summary>
    /// Gets or sets the javascript runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the instance
    /// </summary>
    static Toast Instance { get; set; }

    /// <summary>
    /// Initializes the component
    /// </summary>
    protected override void OnInitialized()
    {
        Instance = this;
    }


    /// <summary>
    /// Show an error message
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public static void ShowError(string message, int duration = 5_000)
        => _ = Instance.jsRuntime.InvokeVoidAsync("ff.toast", "error", Translater.TranslateIfNeeded(message), string.Empty, duration);

    /// <summary>
    /// Show an information message
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public static void ShowInfo(string message, int duration = 5_000)
        => _ = Instance.jsRuntime.InvokeVoidAsync("ff.toast", "info", Translater.TranslateIfNeeded(message), string.Empty, duration);
    
    /// <summary>
    /// Show an success message
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public static void ShowSuccess(string message, int duration = 5_000)
        => _ = Instance.jsRuntime.InvokeVoidAsync("ff.toast", "success", Translater.TranslateIfNeeded(message), string.Empty, duration);

    /// <summary>
    /// Show an warning message
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public static void ShowWarning(string message, int duration = 5_000)
        => _ = Instance.jsRuntime.InvokeVoidAsync("ff.toast", "warn", Translater.TranslateIfNeeded(message), string.Empty, duration);

}

