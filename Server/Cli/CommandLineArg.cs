namespace FileFlows.Server.Cli;
/// <summary>
/// Represents a command line argument.
/// </summary>
internal class CommandLineArg : Attribute
{
    /// <summary>
    /// Gets the switch of the argument
    /// </summary>
    public string Switch { get; init; }
    
    /// <summary>
    /// Gets the description for this argument
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Gets a value indicating whether this argument is optional.
    /// </summary>
    public bool Optional { get; init; }

    /// <summary>
    /// Gets the override error if this argument is missing
    /// </summary>
    public string MissingErrorOverride { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandLineArg"/> class.
    /// </summary>
    /// <param name="switch">The switch of the argument.</param>
    /// <param name="description">the description of this command line argument.</param>
    /// <param name="optional">A boolean indicating whether the argument is optional. Default is false.</param>
    /// <param name="missingErrorOverride">the override error if this argument is missing</param>
    public CommandLineArg(string @switch, string description, bool optional = false, string missingErrorOverride = null)
    {
        Switch = @switch;
        Description = description;
        Optional = optional;
        MissingErrorOverride = missingErrorOverride;
    }
}
