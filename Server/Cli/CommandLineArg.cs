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
    /// Initializes a new instance of the <see cref="CommandLineArg"/> class.
    /// </summary>
    /// <param name="@switch">The switch of the argument.</param>
    /// <param name="optional">A boolean indicating whether the argument is optional. Default is false.</param>
    public CommandLineArg(string @switch, string description, bool optional = false)
    {
        Switch = @switch;
        Description = description;
        Optional = optional;
    }
}
