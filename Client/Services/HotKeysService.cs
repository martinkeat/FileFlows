using Microsoft.JSInterop;

namespace FileFlows.Client.Services
{
    public interface IHotKeysService
    {
        void RegisterHotkey(string name, string key, bool ctrl = false, bool alt = false, bool? shift = null, Action callback = null);
        void DeregisterHotkey(string name);
    }

    public class HotKeysService : IHotKeysService
    {
        private static readonly List<HotKey> RegisteredHotKeys = new List<HotKey>();

        private IJSRuntime JsRuntime { get; set; }

        public HotKeysService(IJSRuntime jsRuntime)
        {
            this.JsRuntime = jsRuntime;
            jsRuntime.InvokeVoidAsync("eval", "window.addEventListener('keydown', (event) => { " +
                "let tag = document.activeElement && document.activeElement.tagName ? document.activeElement.tagName.toLowerCase() : '';" +
                "if(tag === 'input' || tag === 'textarea' || tag === 'select') return;" +
                "let args = { Key: event.key, Shift: event.shiftKey, Ctrl: event.ctrlKey, Alt: event.altKey};" +
                "setTimeout(() => { DotNet.invokeMethodAsync('Client', 'HotkeysKeyDown', args)}, 1);" + // timeout here so we dont take the key
                "})");
        }

        public void RegisterHotkey(string name, string key, bool ctrl = false, bool alt = false, bool? shift = null, Action callback = null)
        {
            RegisteredHotKeys.Add(new HotKey
            {
                Name = name,
                Key = key,
                Ctrl = ctrl,
                Shift = shift,
                Alt = alt,
                Callback = callback
            });
        }

        public void DeregisterHotkey(string name)
        {
            RegisteredHotKeys.RemoveAll(x => x.Name == name); 
        }

        [JSInvokable("HotkeysKeyDown")]
        public static void HotkeysKeyDown(HotKey hotkey)
        {
            Logger.Instance.ILog("Hot keys registered: " + RegisteredHotKeys.Count);
            if (RegisteredHotKeys.Any() == false)
                return;
            var list = RegisteredHotKeys.Select(x => x).ToList();
            list.Reverse();
            foreach(var rhk in list)
            {
                if (rhk.Key.ToLower() != hotkey.Key.ToLower())
                    continue;
                if (rhk.Ctrl != hotkey.Ctrl)
                    continue;
                if (rhk.Alt != hotkey.Alt)
                    continue;
                if (rhk.Shift != null && rhk.Shift.Value != hotkey.Shift) // shift null so we can easily register ? ant not need shift + ?
                    continue;
                try
                {
                    rhk?.Callback();
                    break;
                }
                catch (Exception) { }
            }
        }

    }


    public class HotKey
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public bool Ctrl { get; set; }
        public bool? Shift { get; set; }
        public bool Alt { get; set; }

        public Action Callback { get; set; }
    }
}
