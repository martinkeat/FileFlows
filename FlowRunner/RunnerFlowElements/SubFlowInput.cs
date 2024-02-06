using FileFlows.Plugin;

namespace FileFlows.FlowRunner.RunnerFlowElements;

/// <summary>
/// Sub Flow Input used to pass into a sub flow
/// </summary>
public class SubFlowInput:Node
{
    /// <summary>
    /// Executes this dummy flow element
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <returns>returns 1</returns>
    public override int Execute(NodeParameters args)
        => 1;
}