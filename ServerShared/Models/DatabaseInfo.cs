using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Models;

/// <summary>
/// Information regarding a database connection
/// </summary>
public class DatabaseInfo
{
    /// <summary>
    /// The database type
    /// </summary>
    public DatabaseType Type { get; init; }
    /// <summary>
    /// The connection string
    /// </summary>
    public string ConnectionString { get; init; }
}


/// <summary>
/// Database connection details
/// </summary>
public class DbConnectionInfo
{
    /// <summary>
    /// Gets or sets the server address
    /// </summary>
    public string Server { get; set; }
    /// <summary>
    /// Gets or sets the database name
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the port
    /// </summary>
    public int Port { get; set; }
    /// <summary>
    /// Gets or sets the connecting user
    /// </summary>
    public string User { get; set; }
    /// <summary>
    /// Gets or sets the password used
    /// </summary>
    public string Password { get; set; }
    /// <summary>
    /// Gets or sets the database type
    /// </summary>
    public DatabaseType Type { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        switch (Type)
        {
            case DatabaseType.Postgres: 
                return $"Server={Server};Port={Port};Database={Name};User Id={User};Password={Password};";
            case DatabaseType.MySql:
                return $"Server={Server};Port={Port};Database={Name};User Id={User};Password={Password};";
            case DatabaseType.SqlServer:
                return $"Server={Server},{Port};Database={Name};User Id={User};Password={Password};";
            case DatabaseType.Sqlite:
                return SqliteHelper.GetConnectionString("FileFlows.sqlite");
        }
        return Type.ToString();
    }
}
