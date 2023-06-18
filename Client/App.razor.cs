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
        [Inject] private Blazored.LocalStorage.ILocalStorageService LocalStorage { get; set; }
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
        

        public async Task LoadLanguage()
        {
            string langFile = await LoadLanguageFile("i18n/en.json?version=" + Globals.Version);
#if (DEMO)
            string pluginLang = await LoadLanguageFile("i18n/en.plugins.json?ts=" + System.DateTime.Now.ToFileTime());
#else
            string pluginLang = await LoadLanguageFile("/api/plugin/language/en.json?ts=" + System.DateTime.Now.ToFileTime());
#endif
            Translater.Init(langFile, pluginLang);
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
            Settings = (await HttpHelper.Get<FileFlows.Shared.Models.Settings>("/api/settings")).Data ??
                       new FileFlows.Shared.Models.Settings();
            await LoadAppInfo();
            await LoadLanguage();
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