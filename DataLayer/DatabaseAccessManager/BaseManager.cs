using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer;

internal  abstract class BaseManager
{
    /// <summary>
    /// The logger to use
    /// </summary>
    protected readonly ILogger Logger;
    /// <summary>
    /// The database connector
    /// </summary>
    protected readonly IDatabaseConnector DbConnector;
    /// <summary>
    /// The type of database
    /// </summary>
    protected  readonly DatabaseType DbType;
    
    /// <summary>
    /// Wraps a field name
    /// </summary>
    /// <param name="name">the name to wrap</param>
    /// <returns>the wrapped field name</returns>
    protected string Wrap(string name)
        => DbConnector.WrapFieldName(name);
    
    /// <summary>
    /// Initializes a new instance of the base manager
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="dbType">the type of database</param>
    /// <param name="dbConnector">the database connector</param>
    public BaseManager(ILogger logger, DatabaseType dbType, IDatabaseConnector dbConnector)
    {
        Logger = logger;
        DbType = dbType;
        DbConnector = dbConnector;
    }
    
}