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
        
        wwwroot = Path.Combine(wwwroot, "i18n", $"{langCode}.json");

        List<string> json = new List<string>();
        if (File.Exists(wwwroot))
            json.Add(File.ReadAllText(wwwroot));
        else
            Logger.Instance.ILog("Language file not found: " + wwwroot);

        foreach (var file in new DirectoryInfo(DirectoryHelper.PluginsDirectory)
                                    .GetFiles($"*{langCode}.json", SearchOption.AllDirectories)
                                    .OrderByDescending(x => x.CreationTime)
                                    .DistinctBy(x => x.Name))
        {
            json.Add(File.ReadAllText(file.FullName));
        }
        Translater.Init(json.ToArray());
    }
}
