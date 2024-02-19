using FileFlows.Plugin;

namespace FileFlows.FlowRunner.TemplateRenders;

/// <summary>
/// Template renderer
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// Renders a template string
    /// </summary>
    /// <param name="args">the node parameters</param>
    /// <param name="text">the template text</param>
    /// <returns>the rendered template</returns>
    string Render(NodeParameters args, string text);
}