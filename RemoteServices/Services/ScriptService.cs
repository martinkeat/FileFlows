// using System.Text.Encodings.Web;
// using FileFlows.Plugin;
//
// namespace FileFlows.RemoteServices;
//
// /// <summary>
// /// A service used to get script data from the FileFlows server
// /// </summary>
// public class ScriptService:Service, IScriptService
// {
//     /// <summary>
//     /// Get all scripts
//     /// </summary>
//     /// <returns>a collection of scripts</returns>
//     public async Task<IEnumerable<Script>> GetAll()
//     {
//         try
//         {
//             string url = $"{ServiceBaseUrl}/api/script";
//             var result = await HttpHelper.Get<List<Script>>(url);
//             if (result.Success == false)
//                 throw new Exception(result.Body);
//             return result.Data;
//         }
//         catch (Exception ex)
//         {
//             Logger.Instance?.WLog("Failed to get scripts: " + ex.Message);
//             return new List<Script>();
//         }
//     }
//
//     /// <summary>
//     /// Get a script
//     /// </summary>
//     /// <param name="name">The name of the script</param>
//     /// <param name="type">the type of script to get</param>
//     /// <returns>the script</returns>
//     public async Task<Script> Get(string name, ScriptType type)
//     {
//         try
//         {
//             string encoded = UrlEncoder.Create().Encode(name);
//             string url = $"{ServiceBaseUrl}/api/script/{encoded}?type=" + type;
//             Logger.Instance.ILog("Request script from: " + url);
//             var result = await HttpHelper.Get<Script>(url);
//             if (result.Success == false)
//                 throw new Exception(result.Body);
//             return result.Data;
//         }
//         catch (Exception ex)
//         {
//             Logger.Instance?.WLog("Failed to get script: " + ex.Message);
//             return new Script { Code = string.Empty, Name = string.Empty };
//         }
//     }
//
//     /// <summary>
//     /// Gets or sets a function used to load new instances of the service
//     /// </summary>
//     /// <param name="name">The name of the script</param>
//     /// <param name="type">the type of script</param>
//     /// <returns>the script code</returns>
//     public async Task<string> GetCode(string name, ScriptType type)
//     {
//         try
//         {
//             string encoded = UrlEncoder.Create().Encode(name);
//             string url = $"{ServiceBaseUrl}/api/script/{encoded}/code?type=" + type;
//             Logger.Instance.ILog("Request script code from: " + url);
//             var result = await HttpHelper.Get<string>(url);
//             if (result.Success == false)
//                 throw new Exception(result.Body);
//             return result.Data;
//         }
//         catch (Exception ex)
//         {
//             Logger.Instance?.WLog("Failed to get script code: " + ex.Message);
//             return string.Empty;
//         }
//     }
// }