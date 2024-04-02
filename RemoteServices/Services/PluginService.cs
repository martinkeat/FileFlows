// namespace FileFlows.RemoteServices;
//
// /// <summary>
// /// Plugin service
// /// </summary>
// public class PluginService : Service, IPluginService
// {
//
//     /// <summary>
//     /// Download a plugin
//     /// </summary>
//     /// <param name="plugin">the plugin to download</param>
//     /// <returns>the byte data of the plugin</returns>
//     public async Task<byte[]> Download(PluginInfo plugin)
//     {
//         try
//         {
//             var result = await HttpHelper.Get<byte[]>($"{ServiceBaseUrl}/api/plugin/download-package/{plugin.PackageName}");
//             if (result.Success == false)
//                 throw new Exception(result.Body);
//             return result.Data;
//         }
//         catch (Exception ex)
//         {
//             Logger.Instance?.WLog("Failed to download plugin package: " + ex.Message);
//             return new byte[] { };
//         }
//     }
//
//     /// <summary>
//     /// Get all plugin infos
//     /// </summary>
//     /// <returns>all plugin infos</returns>
//     public async Task<List<PluginInfo>> GetAllAsync()
//     {
//         try
//         {
//             var result = await HttpHelper.Get<List<PluginInfo>>($"{ServiceBaseUrl}/api/plugin");
//             if (result.Success == false)
//                 throw new Exception(result.Body);
//             return result.Data;
//         }
//         catch (Exception ex)
//         {
//             Logger.Instance?.WLog("Failed to get plugin infos: " + ex.Message);
//             return new List<PluginInfo>();
//         }
//     }
//
//     /// <summary>
//     /// Gets the settings json for a plugin
//     /// </summary>
//     /// <param name="pluginPackageName">the name of the plugin package</param>
//     /// <returns>the settings json</returns>
//     public async Task<string> GetSettingsJson(string pluginPackageName)
//     {
//         try
//         {
//             var result = await HttpHelper.Get<string>($"{ServiceBaseUrl}/api/plugin/{pluginPackageName}/settings");
//             if (result.Success == false)
//                 throw new Exception(result.Body);
//             return result.Data;
//         }
//         catch (Exception ex)
//         {
//             Logger.Instance?.WLog("Failed to get plugin settings: " + ex.Message);
//             return String.Empty;
//         }
//     }
//
//     /// <summary>
//     /// Updates plugin info
//     /// </summary>
//     /// <param name="pluginInfo">the plugin info</param>
//     /// <returns>the updated plugininfo</returns>
//     /// <exception cref="NotImplementedException">This not yet implemented</exception>
//     public Task<PluginInfo> Update(PluginInfo pluginInfo)
//     {
//         throw new NotImplementedException();
//     }
// }
