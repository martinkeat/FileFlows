using NPoco;

namespace FileFlows.DataLayer.Converters;

public class FileFlowsMapper:DefaultMapper
{
    /// <summary>
    /// Dictionary to store static instances of subclasses of FileFlowsMapper&gt;T&lt;.
    /// Each subclass has its own static instance.
    /// </summary>
    protected static readonly Dictionary<Type, FileFlowsMapper> instances = new Dictionary<Type, FileFlowsMapper>();

    /// <summary>
    /// Disable all instances
    /// </summary>
    public static void DisableAll()
    {
        foreach (var instance in instances.Values)
            instance.Enable = false;
    }

    /// <summary>
    /// Gets or sets if this mapper is enabled,
    /// NPoco caches these so it may be loaded, but we dont want to use it
    /// If we're migrating data, this could effect us
    /// </summary>
    public bool Enable { get; set; }
}

public class FileFlowsMapper<T>:FileFlowsMapper  where T : FileFlowsMapper<T>, new()
{

    /// <summary>
    /// Static constructor to initialize the static instance for each subclass.
    /// </summary>
    static FileFlowsMapper()
    {
        // Initialize the static instance for each subclass
    }

    /// <summary>
    /// Gets the static instance of the subclass.
    /// </summary>
    /// <returns>The static instance of the subclass.</returns>
    public static T UseInstance()
    {
        if (instances.TryGetValue(typeof(T), out var instance) == false)
        {
            instance = new T();
            instances[typeof(T)] = instance;
        }

        instance.Enable = true;
        return (T)instance;
    }
}