using FileFlows.Shared.Helpers;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer.Helpers;

/// <summary>
/// Output connection converter
/// </summary>
public class OutputConnectionConverter : IAuditValueConverter
{
    private object? newSource;
    private object? oldSource;
    /// <summary>
    /// Constructs a new instance of the output connection converter
    /// </summary>
    /// <param name="newSource">the overall source new object</param>
    /// <param name="oldSource">the overall source old object</param>
    public OutputConnectionConverter(object? newSource, object? oldSource)
    {
        this.newSource = newSource;
        this.oldSource = oldSource;
    }
    
    /// <inheritdoc />
    public static bool CanConvert(Type type)
        => type == typeof(List<FlowConnection>);
    
    /// <inheritdoc />
    public string? Convert(object? newValue, object? oldValue)
    {
        List<FlowConnection>? newConnections = newValue as List<FlowConnection> ?? new ();
        List<FlowConnection>? oldConnections = oldValue as List<FlowConnection> ?? new ();
        if (newConnections.Any() != true && oldConnections.Any() != true)
            return null;

        Dictionary<Guid, string> partDict = new();
        if (newSource is Flow flow && flow.Parts?.Any() == true)
            partDict = flow.Parts.DistinctBy(x => x.Uid).ToDictionary(x => x.Uid, FlowHelper.GetFlowPartName);
        if (oldSource is Flow oldFlow && oldFlow.Parts?.Any() == true)
        {
            foreach (var old in oldFlow.Parts)
            {
                if (partDict.ContainsKey(old.Uid))
                    continue;
                partDict[old.Uid] = FlowHelper.GetFlowPartName(old);
            }
        }

        var additions = newConnections?.Where(x => oldConnections?.Any(y => y.Output == x.Output) != true)?.ToList() ?? new ();
        var changes = newConnections?.Where(x =>
        {
            var oldConnection = oldConnections?.FirstOrDefault(y => y.Output == x.Output);
            if (oldConnection == null)
                return false;
            return oldConnection.InputNode != x.InputNode;
        })?.ToList() ?? new ();
        var deletions = oldConnections?.Where(x => newConnections?.Any(y => y.Output == x.Output) != true)?.ToList() ??
                        new();
        
        List<(int Output, int Sort, string Label)> diff = new();
        
        foreach (var connection in additions)
            diff.Add((connection.Output, 1, $"Output {connection.Output}: '{GetPartName(connection.InputNode)}' added"));
        foreach (var connection in deletions)
            diff.Add((connection.Output, 2, $"Output {connection.Output}: '{GetPartName(connection.InputNode)}' removed"));
        foreach (var connection in changes)
        {
            var oldConnection = oldConnections!.FirstOrDefault(x => x.Output == connection.Output);
            if(oldConnection != null) // shouldn't happen
                diff.Add((connection.Output, 3, $"Output {connection.Output}: '{GetPartName(oldConnection.InputNode)}' to '{GetPartName(connection.InputNode)}'"));
        }

        return string.Join("\n", diff.OrderBy(x => x.Output).ThenBy(x => x.Sort).Select(x => x.Label));
        
        string GetPartName(Guid uid)
        {
            if (partDict.TryGetValue(uid, out string? value) && string.IsNullOrWhiteSpace(value) == false)
                return value;
            return uid.ToString();
        }
    }
}