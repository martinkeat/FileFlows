using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client;

/// <summary>
/// The main Application
/// </summary>
public partial class App : ComponentBase
{
    /// <summary>
    /// The instance of the application
    /// </summary>
    public static App Instance { get; private set; }
    public delegate void DocumentClickDelegate();
    public event DocumentClickDelegate OnDocumentClick;
    public delegate void WindowBlurDelegate();
    public event WindowBlurDelegate OnWindowBlur;

    [Inject] public HttpClient Client { get; set; }
    [Inject] public IJSRuntime jsRuntime { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }
    [Inject] private FFLocalStorageService LocalStorage { get; set; }
    
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] private ProfileService ProfileService { get; set; }
    public bool LanguageLoaded { get; set; } = false;

    public int DisplayWidth { get; private set; }
    public int DisplayHeight { get; private set; }

    public bool IsMobile => DisplayWidth > 0 && DisplayWidth <= 768;

    public FileFlows.Shared.Models.Flow NewFlowTemplate { get; set; }

    public static int PageSize { get; set; }

    /// <summary>
    /// Delegate for the on escape event
    /// </summary>
    public delegate void EscapePushed(OnEscapeArgs args);

    /// <summary>
    /// Event that is fired when the escape key is pushed
    /// </summary>
    public event EscapePushed OnEscapePushed;

    // public FileFlowsStatus FileFlowsSystem { get; private set; }
    
    /// <summary>
    /// Gets or sets if the nav menu is collapsed
    /// </summary>
    public bool NavMenuCollapsed { get; set; }

    //public delegate void FileFlowsSystemUpdated(FileFlowsStatus system);

    //public event FileFlowsSystemUpdated OnFileFlowsSystemUpdated;
    

    public async Task LoadLanguage(string language, bool loadPlugin = true)
    {
        List<string> langFiles = new();

        bool nonEnglishLanguage = string.IsNullOrWhiteSpace(language) == false &&
                                  language.ToLowerInvariant() != "en" && Regex.IsMatch(language, "^[a-z]{2,3}$",
                                      RegexOptions.IgnoreCase);
        langFiles.Add(await LoadLanguageFile("i18n/en.json?version=" + Globals.Version));
        if (nonEnglishLanguage)
        {
            var other = await LoadLanguageFile("i18n/" + language + ".json?version=" + Globals.Version);
            if (string.IsNullOrWhiteSpace(other) == false)
                langFiles.Add(other);
        }

        if (loadPlugin)
        {
            langFiles.Add(
                await LoadLanguageFile("/api/plugin/language/en.json?ts=" + DateTime.UtcNow.ToFileTime()));
            if (nonEnglishLanguage)
            {
                var other = await LoadLanguageFile("/api/plugin/language/" + language + ".json?version=" +
                                                   Globals.Version);
                if (string.IsNullOrWhiteSpace(other) == false)
                    langFiles.Add(other);
            }
        }

        Translater.Init(langFiles.ToArray());
    }

    /// <summary>
    /// Reinitialize the app after a login
    /// </summary>
    public async Task Reinitialize(bool forced = false)
    {
        var token = await LocalStorage.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token) == false)
        {
            HttpHelper.Client.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue("Bearer", token);
        }

        if (forced || NavigationManager.Uri.Contains("/login") == false)
        {
            var profile = await ProfileService.Get();
            await LoadLanguage(profile.Language);
        }
        else
        {
            await LoadLanguage(null, loadPlugin: false);
        }
        LanguageLoaded = true;
        StateHasChanged();
    }

    private async Task<string> LoadLanguageFile(string url)
    {
        return (await HttpHelper.Get<string>(url)).Data ?? "";
    }

    public async Task SetPageSize(int pageSize)
    {
        PageSize = pageSize;
        await LocalStorage.SetItemAsync(nameof(PageSize), pageSize);
    }

    protected override async Task OnInitializedAsync()
    {
        Instance = this;
        ClientConsoleLogger.jsRuntime = jsRuntime;
        new ClientConsoleLogger();
        HttpHelper.Client = Client;
        PageSize = await LocalStorage.GetItemAsync<int>(nameof(PageSize));
        if (PageSize < 100 || PageSize > 5000)
            PageSize = 1000;

        var dimensions = await jsRuntime.InvokeAsync<Dimensions>("ff.deviceDimensions");
        DisplayWidth = dimensions.width;
        DisplayHeight = dimensions.height;
        var dotNetObjRef = DotNetObjectReference.Create(this);
        _ = jsRuntime.InvokeVoidAsync("ff.onEscapeListener", new object[] { dotNetObjRef });
        _ = jsRuntime.InvokeVoidAsync("ff.attachEventListeners", new object[] { dotNetObjRef });
        _ = jsRuntime.InvokeVoidAsync("ff.setCSharp",  new object[] { dotNetObjRef });

        await Reinitialize();

        this.StateHasChanged();
    }

    record Dimensions(int width, int height);

    /// <summary>
    /// Method called by javascript for events we listen for
    /// </summary>
    /// <param name="eventName">the name of the event</param>
    [JSInvokable]
    public void EventListener(string eventName)
    {
        if(eventName == "WindowBlur")
            OnWindowBlur?.Invoke();
        else if(eventName == "DocumentClick")
            OnDocumentClick?.Invoke(); ;
    }

    /// <summary>
    /// Escape was pushed
    /// </summary>
    [JSInvokable]
    public void OnEscape(OnEscapeArgs args)
    {
        OnEscapePushed?.Invoke(args);
    }
    
    
    /// <summary>
    /// Opens a url
    /// </summary>
    [JSInvokable]
    public async Task<bool> OpenUrl(string url)
    {
        var profile = await ProfileService.Get();
        if (profile.IsWebView == false)
            return false;
        await HttpHelper.Post("/api/system/open-url?url=" + HttpUtility.UrlEncode(url));
        return true;
    }

    /// <summary>
    /// Navigates to a route
    /// </summary>
    [JSInvokable]
    public void NavigateTo(string url)
    {
        NavigationManager.NavigateTo(url);
        StateHasChanged();
    }

    public async Task OpenHelp(string url)
    {
        var profile = await ProfileService.Get();
        if (profile.IsWebView == false)
            await jsRuntime.InvokeVoidAsync("ff.open", url, true);
        else  
            await OpenUrl(url);
    }
    
}


/// <summary>
/// Args for on escape event
/// </summary>
public class OnEscapeArgs
{
    /// <summary>
    /// Gets if there is a modal visible
    /// </summary>
    public bool HasModal { get; init; }

    /// <summary>
    /// Gets if the log partial viewer is open 
    /// </summary>
    public bool HasLogPartialViewer { get; init; }
}