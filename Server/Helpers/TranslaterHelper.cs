using FileFlows.Shared;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Helper for the translator
/// </summary>
public class TranslaterHelper
{
    private static string currentLanguage = null;
    
    /// <summary>
    /// Initializes the translator for the given language
    /// </summary>
    /// <param name="langCode">the language code</param>
    internal static void InitTranslater(string langCode = "en")
    {
        if (currentLanguage == langCode)
            return;
        currentLanguage = langCode;
        
        string appdir = DirectoryHelper.BaseDirectory + "/Server";
        string wwwroot = Path.Combine(appdir, $"wwwroot");
        #if(DEBUG)
        wwwroot = wwwroot[..wwwroot.IndexOf("/bin")] + "/wwwroot";
        #endif

        List<string> json = new List<string>();
        
        if (langCode != "en")
        {
            // add en as base
            LoadLanguage(wwwroot, "en", json);
        }
        LoadLanguage(wwwroot, langCode, json);
        Translater.Init(json.ToArray());
    }

    /// <summary>
    /// Loads the language strings into the json list
    /// </summary>
    /// <param name="wwwroot">the wwwroot path</param>
    /// <param name="langCode">the language code to load</param>
    /// <param name="json">the JSON</param>
    private static void LoadLanguage(string wwwroot, string langCode, List<string> json)
    {
        var langFile = Path.Combine(wwwroot, "i18n", $"{langCode}.json");

        if (File.Exists(langFile))
            json.Add(File.ReadAllText(langFile));
        else
            Logger.Instance.ILog("Language file not found: " + langFile);

        foreach (var file in new DirectoryInfo(DirectoryHelper.PluginsDirectory)
                     .GetFiles($"*{langCode}.json", SearchOption.AllDirectories)
                     .OrderByDescending(x => x.CreationTime)
                     .DistinctBy(x => x.Name))
        {
            json.Add(File.ReadAllText(file.FullName));
        }
        
    }
}
