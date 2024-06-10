using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components;

/// <summary>
/// Component for showing a toast message
/// </summary>
public partial class Toast : ComponentBase
{
    /// <summary>
    /// Gets or sets the javascript runtime
    /// </summary>
    [Inject]
    public IJSRuntime jsRuntime { get; set; }

    /// <summary>
    /// Gets or sets the instance
    /// </summary>
    static Toast Instance { get; set; }

    private bool Visible { get; set; }

    private LinkedList<ToastItem> ToastList = new();

    private ToastItem? Latest;
    private bool HasItems;

    private enum ToastLevel
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }

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
        => Instance?.Show(ToastLevel.Error, message, duration);
    
    /// <summary>
    /// Show an error on an editor 
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public static void ShowEditorError(string message, int duration = 5_000)
        => Instance?.Show(ToastLevel.Error, message, duration, editor: true);

    /// <summary>
    /// Show an information message
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public static void ShowInfo(string message, int duration = 5_000)
        => Instance?.Show(ToastLevel.Info, message, duration);

    /// <summary>
    /// Show an editor success message
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public static void ShowEditorSuccess(string message, int duration = 5_000)
        => Instance?.Show(ToastLevel.Success, message, duration, editor: true);
    
    /// <summary>
    /// Show an success message
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public static void ShowSuccess(string message, int duration = 5_000)
        => Instance?.Show(ToastLevel.Success, message, duration);

    /// <summary>
    /// Show an warning message
    /// </summary>
    /// <param name="message">the message</param>
    /// <param name="duration">the duration in milliseconds to show the message</param>
    public static void ShowWarning(string message, int duration = 5_000)
        => Instance?.Show(ToastLevel.Warning, message, duration);

    /// <summary>
    /// Shows the actual toast message
    /// </summary>
    /// <param name="level">the toast level</param>
    /// <param name="message">the message of the toast</param>
    /// <param name="duration">the duration to show the toast for</param>
    /// <param name="editor">if this is shown for a editor</param>
    void Show(ToastLevel level, string message, int duration, bool editor = false)
    {
        //=> _ = Instance.jsRuntime.InvokeVoidAsync("ff.toast", type, Translater.TranslateIfNeeded(message), string.Empty, duration);

        ToastItem toastItem = new()
        {
            Message = Translater.TranslateIfNeeded(message),
            Level = level,
            Editor = editor,
            Icon = level switch
            {
                ToastLevel.Warning => "exclamation-triangle",
                ToastLevel.Success => "check-circle",
                ToastLevel.Info => "info-circle",
                ToastLevel.Error => "times-circle",
                _ => ""
            }
        };
        if (editor || Latest == null || Latest.Level <= level)
        {
            Latest = toastItem;
        }

        if (editor == false)
        {
            lock (ToastList)
            {
                ToastList.AddLast(toastItem);
                while (ToastList.Count > 10)
                {
                    ToastList.RemoveFirst();
                }

                HasItems = true;
            }
        }

        _ = RemoveAfterDelay(toastItem, duration);

        StateHasChanged();
        
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            toastItem.Show = true;
            StateHasChanged();
        });
    }

    async Task RemoveAfterDelay(ToastItem toastItem, int duration)
    {
        // Delay for the specified duration
        await Task.Delay(duration);

        if (Latest == toastItem)
        {
            toastItem.Hide = true;
            StateHasChanged();
            await Task.Delay(300);
            
            Latest = null;
            StateHasChanged();
        }
    }

    /// <summary>
    /// A toast item
    /// </summary>
    private class ToastItem
    {
        /// <summary>
        /// Gets or sets the type of level
        /// </summary>
        public ToastLevel Level { get; init; }

        /// <summary>
        /// Gets or sets the message of the toast
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// Gets or sets the icon of the toast
        /// </summary>
        public string Icon { get; init; }
        
        /// <summary>
        /// Gets if this is for a editor
        /// </summary>
        public bool Editor { get; init; }
        
        /// <summary>
        /// Gets if this is shown, this is so we can animate it
        /// </summary>
        public bool Show { get; set; }
        
        /// <summary>
        /// Gets if this is hidden, this is so we can animate it
        /// </summary>
        public bool Hide { get; set; }
    }

    /// <summary>
    /// Toggles if the notifications are visible
    /// </summary>
    private void ToggleVisible()
    {
        lock (ToastList)
        {
            if (Visible)
            {
                Latest = null;
                Visible = false;
            }
            else if (ToastList.Any())
            {
                Visible = true;
                Latest = null;
            }

        }
    }

    /// <summary>
    /// Clears the latest item
    /// </summary>
    private void ClearLatest()
        => Latest = null;

    /// <summary>
    /// Removes a specific item
    /// </summary>
    /// <param name="item">the item to remove</param>
    private EventCallback RemoveItem(ToastItem item)
    {
        lock (ToastList)
        {
            ToastList.Remove(item);
            HasItems = ToastList.Count > 0;
            Latest = null;
            Visible = HasItems;
        }
        return EventCallback.Empty;
    }
}