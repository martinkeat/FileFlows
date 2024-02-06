// using FileFlows.Plugin;
// using FileFlows.Shared.Models;
//
// namespace FileFlows.FlowRunner.RunnerFlowElements;
//
// /// <summary>
// /// Flow element that goes to another flow element
// /// </summary>
// public class GotoFlowFlowElement : Node
// {
//     public Flow Flow { get; set; }
//     
//     public Runner Runner { get; set; }
//
//     public override int Execute(NodeParameters args)
//     {
//         
//         if (gotoFlow != null)
//         {
//             var newFlow = Program.Config.Flows.FirstOrDefault(x => x.Uid == gotoFlow.Uid);
//             if (newFlow == null)
//             {
//                 args.Logger?.ELog("Unable goto flow with UID:" + gotoFlow.Uid + " (" + gotoFlow.Name + ")");
//                 args.Result = NodeResult.Failure;
//                 return FileStatus.ProcessingFailed;
//             }
//             flow = newFlow;
//
//             args.Logger?.ILog("Changing flows to: " + newFlow.Name);
//             this.Flow = newFlow;
//             runFlows.Add(gotoFlow.Uid);
//
//             // find the first node
//             part = flow.Parts.Where(x => x.Inputs == 0).FirstOrDefault();
//             if (part == null)
//             {
//                 args.Logger!.ELog("Failed to find Input node");
//                 return FileStatus.ProcessingFailed;
//             }
//             // update the flow properties if there are any
//             LoadFlowVariables(flow.Properties?.Variables);
//             Info.TotalParts = flow.Parts.Count;
//             step = 0;
//         }
//     }
// }