// namespace FileFlows.RemoteServices;
//
// /// <summary>
// /// Service for communicating with FileFlows server for flows
// /// </summary>
// public class FlowService : Service, IFlowService
// {
//
//     /// <summary>
//     /// Gets a flow by its UID
//     /// </summary>
//     /// <param name="uid">The UID of the flow</param>
//     /// <returns>An instance of the flow if found, otherwise null</returns>
//     public async Task<Flow> GetByUidAsync(Guid uid)
//     {
//         try
//         {
//             var result = await HttpHelper.Get<Flow>($"{ServiceBaseUrl}/api/processing-node/flow/" + uid.ToString());
//             if (result.Success == false)
//                 throw new Exception("Failed to locate flow: " + result.Body);
//             return result.Data;
//         }
//         catch (Exception ex)
//         {
//             Logger.Instance?.WLog("Failed to get flow: " + uid + " => " + ex.Message);
//             return null;
//         }
//     }
//
//     /// <summary>
//     /// Gets the Failure Flow for a specific library
//     /// This is the flow that is called if the flow fails 
//     /// </summary>
//     /// <param name="libraryUid">The UID of the library</param>
//     /// <returns>An instance of the Failure Flow if found</returns>
//     public async Task<Flow?> GetFailureFlow(Guid libraryUid)
//     {
//         try
//         {
//             var result = await HttpHelper.Get<Flow>($"{ServiceBaseUrl}/api/processing-node/failure-flow-by-library/" + libraryUid);
//             if (result.Success == false)
//                 throw new Exception("Failed to locate flow: " + result.Body);
//             return result.Data;
//         }
//         catch (Exception ex)
//         {
//             Logger.Instance?.WLog("Failed to get failure flow by library: " + libraryUid + " => " + ex.Message);
//             return null;
//         }
//     }
// }
