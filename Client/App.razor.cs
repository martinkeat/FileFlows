using System.Text.RegularExpressions;
using System.Web;

namespace FileFlows.Client
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using FileFlows.Shared;
    using FileFlows.Shared.Helpers;

    public partial class App : ComponentBase
    {
        public static App Instance { get; private set; }
        
        public delegate void DocumentClickDelegate();
        public event DocumentClickDelegate OnDocumentClick;
        public delegate void WindowBlurDelegate();
        public event WindowBlurDelegate OnWindowBlur;

        [Inject] public HttpClient Client { get; set; }
        [Inject] public IJSRuntime jsRuntime { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] private FFLocalStorageService LocalStorage { get; set; }
        public bool LanguageLoaded { get; set; } = false;

        public int DisplayWidth { get; private set; }
        public int DisplayHeight { get; private set; }

        public bool IsMobile => DisplayWidth > 0 && DisplayWidth <= 768;

        public FileFlows.Shared.Models.Flow NewFlowTemplate { get; set; }

        public static FileFlows.Shared.Models.Settings Settings;

        public static int PageSize { get; set; }

        /// <summary>
        /// Delegate for the on escape event
        /// </summary>
        public delegate void EscapePushed(OnEscapeArgs args);

        /// <summary>
        /// Event that is fired when the escape key is pushed
        /// </summary>
        public event EscapePushed OnEscapePushed;

        public FileFlowsStatus FileFlowsSystem { get; private set; }
        
        /// <summary>
        /// Gets or sets if the nav menu is collapsed
        /// </summary>
        public bool NavMenuCollapsed { get; set; }

        public delegate void FileFlowsSystemUpdated(FileFlowsStatus system);

        public event FileFlowsSystemUpdated OnFileFlowsSystemUpdated;
        

        public async Task LoadLanguage(string language)
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

            langFiles.Add(
                await LoadLanguageFile("/api/plugin/language/en.json?ts=" + System.DateTime.Now.ToFileTime()));
            if (nonEnglishLanguage)
            {
                var other = await LoadLanguageFile("/api/plugin/language/" + language + ".json?version=" + Globals.Version);
                if (string.IsNullOrWhiteSpace(other) == false)
                    langFiles.Add(other);
                
            }
            Translater.Init(langFiles.ToArray());
        }

        public async Task LoadAppInfo()
        {
            FileFlowsSystem = (await HttpHelper.Get<FileFlowsStatus>("/api/settings/fileflows-status")).Data;
            this.StateHasChanged();
            this.OnFileFlowsSystemUpdated?.Invoke(FileFlowsSystem);
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
            Settings = (await HttpHelper.Get<FileFlows.Shared.Models.Settings>("/api/settings")).Data ??
                       new FileFlows.Shared.Models.Settings();
            await LoadAppInfo();
            await LoadLanguage(Settings.Language);
            LanguageLoaded = true;
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
        public async Task OnEscape(OnEscapeArgs args)
        {
            OnEscapePushed?.Invoke(args);
        }
        
        
        /// <summary>
        /// Escape was pushed
        /// </summary>
        [JSInvokable]
        public async Task<bool> OpenUrl(string url)
        {
            if (App.Instance.FileFlowsSystem.IsWebView == false)
                return false;
            await HttpHelper.Post("/api/system/open-url?url=" + HttpUtility.UrlEncode(url));
            return true;
        }

        public void OpenHelp(string url)
        {
            if (Instance.FileFlowsSystem.IsWebView) 
                _ = OpenUrl(url);
            else
            {
                _ = jsRuntime.InvokeVoidAsync("ff.open", url, true);
            }
        }
        
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