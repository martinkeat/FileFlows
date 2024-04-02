// namespace FileFlows.RemoteServices;
//
// /// <summary>
// /// Service for communicating with FileFlows server for libraries
// /// </summary>
// public class LibraryService : Service, ILibraryService
// {
//     /// <summary>
//     /// Gets a library by its UID
//     /// </summary>
//     /// <param name="uid">The UID of the library</param>
//     /// <returns>An instance of the library if found</returns>
//     public async Task<Library> GetByUidAsync(Guid uid)
//     {
//         try
//         {
//             var result = await HttpHelper.Get<Library>($"{ServiceBaseUrl}/api/library/" + uid.ToString());
//             if (result.Success == false)
//                 throw new Exception("Failed to locate library: " + result.Body);
//             return result.Data;
//         }
//         catch (Exception ex)
//         {
//             Logger.Instance?.WLog("Failed to get library: " + uid + " => " + ex.Message);
//             return null;
//         }
//     }
//
//     /// <summary>
//     /// Gets all libraries in the system
//     /// </summary>
//     /// <returns>a list of all libraries</returns>
//     public async Task<List<Library>> GetAllAsync()
//     {
//         try
//         {
//             var result = await HttpHelper.Get<List<Library>>($"{ServiceBaseUrl}/api/library");
//             if (result.Success == false)
//                 throw new Exception("Failed to load libraries: " + result.Body);
//             return result.Data;
//         }
//         catch (Exception ex)
//         {
//             Logger.Instance?.WLog("Failed to get libraries => " + ex.Message);
//             return null;
//         }
//         
//     }
// }