using FileFlows.Plugin;

namespace FileFlows.FlowRunner.RunnerFlowElements;

/// <summary>
/// Sub Flow Output used to pass into a sub flow
/// </summary>
public class SubFlowOutput:Node
{
    /// <summary>
    /// Gets or sets the output to call 
    /// </summary>
    public int Output { get; set; }
    
    /// <summary>
    /// Executes this dummy flow element
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <returns>returns the output</returns>
    public override int Execute(NodeParameters args)
        => Output;
}