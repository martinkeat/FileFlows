using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Server.Controllers;
using FileFlows.Server.Helpers;
using FileFlows.Shared.Attributes;
using FileFlows.Shared.Models;
using NPoco;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// A database manager is responsible for the communication to and from the database instance
/// Each support database will have it's own manager
/// </summary>
public abstract class DbManager
{

    protected string ConnectionString { get; init; }
    private static ObjectPool<PooledConnection> DbConnectionPool;

    protected enum DbCreateResult
    {
        Failed = 0,
        Created = 1,
        AlreadyExisted = 2
    }

    /// <summary>
    /// Gets if the database manager should use a memory cache
    /// </summary>
    public virtual bool UseMemoryCache => false;

    /// <summary>
    /// Gets if this database uses TOP to limit queries, otherwise LIMIT will be used
    /// </summary>
    public virtual bool UseTop => false;

    /// <summary>
    /// Gets the method for random in the SQL
    /// </summary>
    public virtual string RandomMethod => "RANDOM()";

    /// <summary>
    /// Method used by the manager to extract a json variable, mysql/mariadb use JSON_EXTRACT
    /// </summary>
    protected virtual string JsonExtractMethod => "JSON_EXTRACT";

    /// <summary>
    /// Gets the database used by this configuration
    /// </summary>
    /// <param name="connectionString">the connection string</param>
    /// <returns>the database manager to use</returns>
    public static DbManager GetManager(string connectionString)
    {
        connectionString ??= SqliteDbManager.GetConnetionString(SqliteDbFile);

        if (connectionString.Contains(".sqlite"))
            return new SqliteDbManager(connectionString);

        if (connectionString.Contains(";Uid="))
            return new MySqlDbManager(connectionString);

        //return new SqlServerDbManager(connectionString);
        throw new Exception("Unknown database: " + connectionString);
    }

    /// <summary>
    /// Gets the file of the default database
    /// </summary>
    public static string SqliteDbFile => Path.Combine(DirectoryHelper.DatabaseDirectory, "FileFlows.sqlite");

    /// <summary>
    /// Gets the default database connection string using the Sqlite database file
    /// </summary>
    /// <returns>the default database connection string using the Sqlite database file</returns>
    public static string GetDefaultConnectionString() => SqliteDbManager.GetConnetionString(SqliteDbFile);

    public DbManager()
    {
        if (DbConnectionPool == null)
            DbConnectionPool = new ObjectPool<PooledConnection>(10,
                () => { return new PooledConnection(GetDbInstance(), DbConnectionPool); });
    }

    /// <summary>
    /// Get an instance of the IDatabase
    /// </summary>
    /// <returns>an instance of the IDatabase</returns>
    internal async Task<FlowDbConnection> GetDb()
    {
        if (UseMemoryCache)
            return await FlowDbConnection.Get(GetDbInstance);

        return await FlowDbConnection.Get(() => new FlowDatabase(this.ConnectionString));
    }

    /// <summary>
    /// Gets the number of open database connections
    /// </summary>
    /// <returns>the number of open database connections</returns>
    public static int GetOpenDbConnections() => DbConnectionPool?.Count ?? 0;

    /// <summary>
    /// Get an instance of the IDatabase
    /// </summary>
    /// <returns>an instance of the IDatabase</returns>
    protected abstract NPoco.Database GetDbInstance();


    #region Setup Code

    /// <summary>
    /// Creates the database and the initial data
    /// </summary>
    /// <param name="recreate">if the database should be recreated if already exists</param>
    /// <param name="insertInitialData">if the initial data should be inserted</param>
    /// <returns>if the database was successfully created or not</returns>
    public async Task<bool> CreateDb(bool recreate = false, bool insertInitialData = true)
    {
        var dbResult = CreateDatabase(recreate);
        if (dbResult == DbCreateResult.Failed)
            return false;

        if (dbResult == DbCreateResult.AlreadyExisted)
        {
            CreateStoredProcedures();
            return true;
        }

        if (CreateDatabaseStructure() == false)
            return false;

        CreateStoredProcedures();


        if (recreate == false && this is SqliteDbManager == false)
        {
            // not a sqlite database, check if one exists and migrate
            if (File.Exists(SqliteDbFile))
            {
                // migrate the data
                bool migrated = DbMigrater.Migrate(SqliteDbManager.GetConnetionString(SqliteDbFile),
                    this.ConnectionString);

                if (migrated)
                {
                    File.Move(SqliteDbFile, SqliteDbFile + ".migrated");
                }

                // migrated, we dont need to insert initial data
                return true;
            }
        }

        if (insertInitialData == false)
            return true;

        return await CreateInitialData();
    }

    /// <summary>
    /// Creates the actual Database
    /// </summary>
    /// <param name="recreate">if the database should be recreated if already exists</param>
    /// <returns>true if successfully created</returns>
    protected abstract DbCreateResult CreateDatabase(bool recreate);

    /// <summary>
    /// Creates the tables etc in the database
    /// </summary>
    /// <returns>true if successfully created</returns>
    protected abstract bool CreateDatabaseStructure();

    /// <summary>
    /// Creates (or recreates) any stored procedures and functions used by this database
    /// </summary>
    protected virtual void CreateStoredProcedures()
    {
    }

    /// <summary>
    /// Inserts the initial data into the database
    /// </summary>
    /// <returns>true if successfully inserted</returns>
    private async Task<bool> CreateInitialData()
    {
        using var flowDb = await GetDb();
        var db = flowDb.Db;
        bool windows =
            System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform
                .Windows);
        await AddOrUpdateObject(db, new Variable
        {
            Name = "ffmpeg",
            Value =
                windows ? Path.Combine(DirectoryHelper.BaseDirectory, @"Tools\ffmpeg.exe") : "/usr/local/bin/ffmpeg",
            DateCreated = DateTime.Now,
            DateModified = DateTime.Now
        });

        await AddOrUpdateObject(db, new Settings
        {
            Name = "Settings",
            AutoUpdatePlugins = true,
            DateCreated = DateTime.Now,
            DateModified = DateTime.Now
        });

        string tempPath;
        if (DirectoryHelper.IsDocker)
            tempPath = "/temp";
        else if (windows)
            tempPath = @Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FileFlows\\Temp");
        else
            tempPath = Path.Combine(DirectoryHelper.BaseDirectory, "Temp");

        if (Directory.Exists(tempPath) == false)
            Directory.CreateDirectory(tempPath);

        await AddOrUpdateObject(db, new ProcessingNode
        {
            Uid = Globals.InternalNodeUid,
            Name = Globals.InternalNodeName,
            Address = Globals.InternalNodeName,
            AllLibraries = ProcessingLibraries.All,
            Schedule = new string('1', 672),
            Enabled = true,
            FlowRunners = 1,
            TempPath = tempPath,
        });

        return true;
    }

    #endregion


    #region helper methods

    /// <summary>
    /// Escapes a string so it is safe to be used in a sql command
    /// </summary>
    /// <param name="input">the string to escape</param>
    /// <returns>the escaped string</returns>
    protected static string SqlEscape(string input) =>
        input == null ? string.Empty : "'" + input.Replace("'", "''") + "'";

    #endregion


    /// <summary>
    /// Executes a sql command and returns a single value
    /// </summary>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <param name="sql">The sql to execute</param>
    /// <param name="args">The arguments for the sql command</param>
    /// <returns>the single value</returns>
    public virtual async Task<T> ExecuteScalar<T>(string sql, params object[] args)
    {
        using (var db = await GetDb())
        {
            return db.Db.ExecuteScalar<T>(sql, args);
        }
    }

    /// <summary>
    /// Select a list of objects
    /// </summary>
    /// <typeparam name="T">the type of objects to select</typeparam>
    /// <returns>a list of objects</returns>
    public virtual async Task<IEnumerable<T>> Select<T>() where T : FileFlowObject, new()
    {
        List<DbObject> dbObjects;
        using (var db = await GetDb())
        {
            dbObjects = await db.Db.FetchAsync<DbObject>("where Type=@0", typeof(T).FullName);
        }

        return ConvertFromDbObject<T>(dbObjects);
    }


    /// <summary>
    /// Select a list of objects
    /// </summary>
    /// <typeparam name="T">the type of objects to select</typeparam>
    /// <param name="sql">the sql command</param>
    /// <param name="args">the sql arguments</param>
    /// <returns>a list of objects</returns>
    internal async Task<IEnumerable<T>> Fetch<T>(string sql, object[] args = null)
    {
        List<T> dbObjects;
        using (var db = await GetDb())
            dbObjects = await db.Db.FetchAsync<T>(sql, args);
        return dbObjects;
    }

    /// <summary>
    /// Select a list of objects
    /// </summary>
    /// <typeparam name="T">the type of objects to select</typeparam>
    /// <param name="sql">the sql command</param>
    /// <param name="args">the sql arguments</param>
    /// <param name="skip">the amount of rows to skip</param>
    /// <param name="rows">the number of rows to take</param>
    /// <returns>a list of objects</returns>
    internal async Task<IEnumerable<T>> Fetch<T>(string sql, object[] args, int skip, int rows)
    {
        List<T> dbObjects;
        using (var db = await GetDb())
        {
            if (skip > 0 && rows > 0)
            {
                if (UseMemoryCache)
                {
                    // sqlite
                    sql += $" limit {rows} offset {skip}";
                }
                else
                {
                    // mysql
                    sql += $" limit {skip}, {rows}";

                }
            }

            dbObjects = await db.Db.FetchAsync<T>(sql, args);
        }

        return dbObjects;
    }

    /// <summary>
    /// Converts DbObjects into strong types
    /// </summary>
    /// <param name="dbObjects">a collection of DbObjects</param>
    /// <typeparam name="T">The type to convert to</typeparam>
    /// <returns>A converted list of objects</returns>
    internal IEnumerable<T> ConvertFromDbObject<T>(IEnumerable<DbObject> dbObjects) where T : FileFlowObject, new()
    {
        var list = dbObjects.ToList();
        T[] results = new T [list.Count];
        Parallel.ForEach(list, (x, state, index) =>
        {
            var converted = Convert<T>(x);
            if (converted != null)
                results[index] = converted;
        });
        return results.Where(x => x != null);
    }

    /// <summary>
    /// Converts DbObject into strong type
    /// </summary>
    /// <param name="dbObject">the DbObject to convert</param>
    /// <typeparam name="T">The type to convert to</typeparam>
    /// <returns>A converted object</returns>
    internal T ConvertFromDbObject<T>(DbObject dbObject) where T : FileFlowObject, new()
    {
        if (dbObject == null)
            return default;
        return Convert<T>(dbObject);
    }

    /// <summary>
    /// Selects types from the database
    /// </summary>
    /// <param name="where">a where clause for the select</param>
    /// <param name="arguments">the arguments for the select</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>a list of objects</returns>
    public virtual async Task<IEnumerable<T>> Select<T>(string where, params object[] arguments)
        where T : FileFlowObject, new()
    {
        List<DbObject> dbObjects;
        using (var db = await GetDb())
        {
            dbObjects = await db.Db.FetchAsync<DbObject>($"where Type=@0 and {where} order by Name",
                typeof(T).FullName, arguments);
        }

        return dbObjects.Select(x => Convert<T>(x));
    }

    /// <summary>
    /// Get names of types
    /// </summary>
    /// <param name="andWhere">and where caluse</param>
    /// <param name="args">arguments for where clause</param>
    /// <typeparam name="T">the type to select</typeparam>
    /// <returns>a list of names</returns>
    public virtual async Task<IEnumerable<string>> GetNames<T>(string andWhere = "", params object[] args)
    {
        if (string.IsNullOrEmpty(andWhere) == false && andWhere.Trim().ToLower().StartsWith("and ") == false)
            andWhere = " and " + andWhere;
        args = new object[] { typeof(T).FullName }.Union(args ?? new object[] { }).ToArray();
        using (var db = await GetDb())
        {
            return await db.Db.FetchAsync<string>(
                $"select Name from {nameof(DbObject)} where Type=@0 {andWhere} order by name", args);
        }
    }


    /// <summary>
    /// Get names of types indexed by their UID
    /// </summary>
    /// <param name="andWhere">and where clause</param>
    /// <param name="args">arguments for where clause</param>
    /// <typeparam name="T">the type to select</typeparam>
    /// <returns>a list of names</returns>
    public virtual async Task<Dictionary<Guid, string>> GetIndexedNames<T>(string andWhere = "", params object[] args)
    {
        if (string.IsNullOrEmpty(andWhere) == false && andWhere.Trim().ToLower().StartsWith("and ") == false)
            andWhere = " and " + andWhere;
        args = new object[] { typeof(T).FullName }.Union(args ?? new object[] { }).ToArray();
        List<(Guid Uid, string Name)> results;
        using (var db = await GetDb())
        {
            results = await db.Db.FetchAsync<(Guid Uid, string Name)>(
                $"select Uid, Name from {nameof(DbObject)} where Type=@0 {andWhere} order by name", args);
        }

        return results.ToDictionary(x => x.Uid, x => x.Name);
    }

    /// <summary>
    /// Checks to see if a name is in use
    /// </summary>
    /// <param name="uid">the Uid of the item</param>
    /// <param name="name">the name of the item</param>
    /// <returns>true if name is in use</returns>
    public virtual async Task<bool> NameInUse<T>(Guid uid, string name)
    {
        string sql = $"Name from {nameof(DbObject)} where Type=@0 and uid <> @1 and Name = @2";
        if (UseTop)
            sql = "select top 1 " + sql;
        else
            sql = "select " + sql + " limit 1";

        string result;
        using (var db = await GetDb())
        {
            result = db.Db.FirstOrDefault<string>(sql, typeof(T).FullName, uid, name);
        }

        return string.IsNullOrEmpty(result) == false;
    }

    /// <summary>
    /// Select a single instance of a type
    /// </summary>
    /// <typeparam name="T">The type to select</typeparam>
    /// <returns>a single instance</returns>
    public virtual async Task<T> Single<T>() where T : FileFlowObject, new()
    {
        DbObject dbObject;
        using (var db = await GetDb())
        {
            dbObject = await db.Db.FirstOrDefaultAsync<DbObject>("where Type=@0", typeof(T).FullName);
        }

        if (string.IsNullOrEmpty(dbObject?.Data))
            return new T();
        return Convert<T>(dbObject);
    }

    /// <summary>
    /// Select a single DbObject
    /// </summary>
    /// <param name="uid">The UID of the object</param>
    /// <returns>a single instance</returns>
    internal virtual async Task<DbObject> SingleDbo(Guid uid)
    {
        DbObject dbObject;
        using (var db = await GetDb())
        {
            dbObject = await db.Db.FirstOrDefaultAsync<DbObject>("where Uid=@0", uid);
        }

        return dbObject;
    }

    /// <summary>
    /// Selects a single instance
    /// </summary>
    /// <param name="uid">the UID of the item to select</param>
    /// <typeparam name="T">the type of item to select</typeparam>
    /// <returns>a single instance</returns>
    public virtual async Task<T> Single<T>(Guid uid) where T : FileFlowObject, new()
    {
        DbObject dbObject;
        using (var db = await GetDb())
        {
            dbObject = await db.Db.FirstOrDefaultAsync<DbObject>("where Type=@0 and Uid=@1", typeof(T).FullName, uid);
        }

        if (string.IsNullOrEmpty(dbObject?.Data))
            return new T();
        return Convert<T>(dbObject);
    }

    /// <summary>
    /// Selects a single instance by its name
    /// </summary>
    /// <param name="name">the name of the item to select</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>a single instance</returns>
    public virtual async Task<T> SingleByName<T>(string name) where T : FileFlowObject, new()
    {
        DbObject dbObject;
        using (var db = await GetDb())
        {
            dbObject = await db.Db.FirstOrDefaultAsync<DbObject>("where Type=@0 and lower(Name)=lower(@1)",
                typeof(T).FullName, name);
        }

        if (string.IsNullOrEmpty(dbObject?.Data))
            return new T();
        return Convert<T>(dbObject);
    }

    /// <summary>
    /// Adds or updates an object in the database
    /// </summary>
    /// <param name="db">The IDatabase used for this operation</param>
    /// <param name="obj">The object being added or updated</param>
    /// <typeparam name="T">The type of object being added or updated</typeparam>
    /// <returns>The updated object</returns>
    private async Task<T> AddOrUpdateObject<T>(IDatabase db, T obj) where T : FileFlowObject, new()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { new DataConverter(), new BoolConverter(), new Shared.Json.ValidatorConverter() }
        };
        // need to case obj to (ViObject) here so the DataConverter is used
        string json = JsonSerializer.Serialize((FileFlowObject)obj, serializerOptions);
        bool changed = false;

        var type = obj.GetType();
        obj.Name = obj.Name?.EmptyAsNull() ?? type.Name;
        var dbObject = obj.Uid == Guid.Empty
            ? null
            : db.FirstOrDefault<DbObject>("where Type=@0 and Uid = @1", type.FullName, obj.Uid);
        if (dbObject == null)
        {
            changed = true;
            if (obj.Uid == Guid.Empty)
                obj.Uid = Guid.NewGuid();
            obj.DateCreated = DateTime.Now;
            obj.DateModified = obj.DateCreated;
            // create new 
            dbObject = new DbObject
            {
                Uid = obj.Uid,
                Name = obj.Name,
                DateCreated = obj.DateCreated,
                DateModified = obj.DateModified,

                Type = type.FullName,
                Data = json
            };
            await db.InsertAsync(dbObject);
        }
        else
        {
            obj.DateModified = DateTime.Now;
            if (dbObject.Name != obj.Name)
                changed = true;
            if (DataHasChanged(dbObject.Type, dbObject.Data, json))
                changed = true;
            dbObject.Name = obj.Name;
            dbObject.DateModified = obj.DateModified;
            if (obj.DateCreated != dbObject.DateCreated && obj.DateCreated > new DateTime(2020, 1, 1))
                dbObject.DateCreated = obj.DateCreated; // OnHeld moving to process now can change this date
            dbObject.Data = json;
            await db.UpdateAsync(dbObject); 
        }

        if (changed && (
                dbObject.Type == typeof(Library).FullName ||
                dbObject.Type == typeof(Flow).FullName ||
                dbObject.Type == typeof(PluginSettingsModel).FullName ||
                dbObject.Type == typeof(Dashboard).FullName
            ))
        {
            // can't await this, this would lock the database on Sqlite since we only allow a single connection
            // to SQLite at a time, and that connection is already being used
            _ = RevisionController.SaveRevision(dbObject);
        }

        if (UseMemoryCache == false)
        {
            dbObject = await db.FirstOrDefaultAsync<DbObject>("where Type=@0 and Uid=@1", typeof(T).FullName, dbObject.Uid);
            
            if (string.IsNullOrEmpty(dbObject?.Data))
                return new T();
            return Convert<T>(dbObject);
        }

        return obj;
    }


    /// <summary>
    /// Adds or updates a DbObject directly in the database
    /// Note: NO revision will be saved
    /// </summary>
    /// <param name="dbObject">the object to add or update</param>
    internal async Task AddOrUpdateDbo(DbObject dbObject)
    {
        using var db = await GetDb();
        bool updated = await db.Db.UpdateAsync(dbObject) > 0;
        if (updated)
            return;
        await db.Db.InsertAsync(dbObject);
    }

    /// <summary>
    /// Tests if the data has changed
    /// </summary>
    /// <param name="type">the type of object</param>
    /// <param name="origJson">the original json</param>
    /// <param name="newJson">the new json</param>
    /// <returns>true if the data has changed</returns>
    private bool DataHasChanged(string type, string origJson, string newJson)
    {
        // filter out some properties that do not trigger a change
        List<string> ignored = new();
        if (type == typeof(Library).FullName)
        {
            ignored.Add(nameof(Library.LastScanned));
            ignored.Add(nameof(Library.LastScannedAgo));
        }
        else if (type == typeof(ProcessingNode).FullName)
        {
            ignored.Add(nameof(ProcessingNode.LastSeen));
            ignored.Add(nameof(ProcessingNode.Version));
            ignored.Add(nameof(ProcessingNode.OperatingSystem));
            ignored.Add(nameof(ProcessingNode.SignalrUrl));
        }

        foreach (string prop in ignored)
        {
            Regex rgx = new Regex($",\"{prop}\":\"[^\"]+\"");
            origJson = rgx.Replace(origJson, string.Empty);
            newJson = rgx.Replace(newJson, string.Empty);
        }

        return origJson != newJson;
    }


    /// <summary>
    /// Updates the last modified date of an object
    /// </summary>
    /// <param name="uid">the UID of the object to update</param>
    internal virtual async Task UpdateLastModified(Guid uid)
    {
        using (var db = await GetDb())
        {
            await db.Db.ExecuteAsync($"update {nameof(DbObject)} set DateModified = @0 where Uid = @1", DateTime.Now,
                uid);
        }
    }

    /// <summary>
    /// This will batch insert many objects into thee database
    /// </summary>
    /// <param name="items">Items to insert</param>
    internal virtual async Task AddMany(FileFlowObject[] items)
    {
        if (items?.Any() != true)
            return;
        int max = 500;
        int count = items.Length;

        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { new DataConverter(), new BoolConverter() }
        };
        for (int i = 0; i < count; i += max)
        {
            StringBuilder sql = new StringBuilder();
            for (int j = i; j < i + max && j < count; j++)
            {
                var obj = items[j];
                // need to case obj to (ViObject) here so the DataConverter is used
                string json = JsonSerializer.Serialize(obj, serializerOptions);

                var type = obj.GetType();
                obj.Name = obj.Name?.EmptyAsNull() ?? type.Name;
                obj.Uid = Guid.NewGuid();
                obj.DateCreated = DateTime.Now;
                obj.DateModified = obj.DateCreated;

                sql.AppendLine($"insert into {nameof(DbObject)} (Uid, Name, Type, Data) values (" +
                               SqlEscape(obj.Uid.ToString()) + "," +
                               SqlEscape(obj.Name) + "," +
                               SqlEscape(type?.FullName ?? String.Empty) + "," +
                               SqlEscape(json) +
                               ");");
            }

            if (sql.Length > 0)
            {
                using (var db = await GetDb())
                {
                    await db.Db.ExecuteAsync(sql.ToString());
                }
            }
        }
    }

    /// <summary>
    /// Selects a single item from the database
    /// </summary>
    /// <param name="andWhere">the and where clause</param>
    /// <param name="args">any parameters to the select statement</param>
    /// <typeparam name="T">the type of object to select</typeparam>
    /// <returns>an single instance</returns>
    public virtual async Task<T> Single<T>(string andWhere, params object[] args) where T : FileFlowObject, new()
    {
        args = new object[] { typeof(T).FullName }.Union(args).ToArray();

        DbObject dbObject;
        using (var db = await GetDb())
        {
            dbObject = await db.Db.FirstOrDefaultAsync<DbObject>("where Type=@0 and " + andWhere, args);
        }

        if (string.IsNullOrEmpty(dbObject?.Data))
            return new T();
        return Convert<T>(dbObject);
    }

    private T Convert<T>(DbObject dbObject) where T : FileFlowObject, new()
    {
        if (dbObject == null)
            return default;

        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { new BoolConverter(), new Shared.Json.ValidatorConverter(), new DataConverter() }
        };

        // need to case obj to (ViObject) here so the DataConverter is used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        T result = JsonSerializer.Deserialize<T>(dbObject.Data, serializerOptions);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        foreach (var prop in result.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var dbencrypted = prop.GetCustomAttribute<EncryptedAttribute>();
            if (dbencrypted != null)
            {
                var value = prop.GetValue(result) as string;
                if (string.IsNullOrEmpty(value) == false)
                {
                    string decrypted = Decrypter.Decrypt(value);
                    if (decrypted != value)
                        prop.SetValue(result, decrypted);
                }
            }
        }

        //result.Uid = Guid.Parse(dbObject.Uid);
        result.Uid = dbObject.Uid;
        result.Name = dbObject.Name;
        result.DateModified = dbObject.DateModified;
        result.DateCreated = dbObject.DateCreated;
        return result;
    }

    /// <summary>
    /// Updates an object
    /// </summary>
    /// <param name="obj">the object to update</param>
    /// <typeparam name="T">the object type</typeparam>
    /// <returns>the updated object</returns>
    public virtual async Task<T> Update<T>(T obj) where T : FileFlowObject, new()
    {
        if (obj == null)
            return new T();
        T result;
        using (var db = await GetDb())
        {
            result = await AddOrUpdateObject(db.Db, obj);
        }

        return result;
    }

    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    /// <typeparam name="T">The type of objects being deleted</typeparam>
    public virtual async Task Delete<T>(params Guid[] uids) where T : FileFlowObject
    {
        if (uids?.Any() != true)
            return; // nothing to delete

        var typeName = typeof(T).FullName;
        string strUids = String.Join(",", uids.Select(x => "'" + x.ToString() + "'"));
        using (var db = await GetDb())
        {
            await db.Db.ExecuteAsync($"delete from {nameof(DbObject)} where Type=@0 and Uid in ({strUids})", typeName);
        }
    }

    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="andWhere">and where clause</param>
    /// <param name="args">arguments for where clause</param>
    /// <typeparam name="T">the type to delete</typeparam>
    public virtual async Task Delete<T>(string andWhere = "", params object[] args)
    {

        if (string.IsNullOrEmpty(andWhere) == false && andWhere.Trim().ToLower().StartsWith("and ") == false)
            andWhere = " and " + andWhere;
        args = new object[] { typeof(T).FullName }.Union(args ?? new object[] { }).ToArray();
        string sql = $"delete from {nameof(DbObject)} where Type=@0 {andWhere}";
        using (var db = await GetDb())
        {
            await db.Db.ExecuteAsync(sql, args);
        }
    }

    /// <summary>
    /// Delete items from a database
    /// </summary>
    /// <param name="uids">the UIDs of the items to delete</param>
    public virtual async Task Delete(params Guid[] uids)
    {
        if (uids?.Any() != true)
            return; // nothing to delete

        string strUids = String.Join(",", uids.Select(x => "'" + x.ToString() + "'"));
        using (var db = await GetDb())
        {
            await db.Db.ExecuteAsync($"delete from {nameof(DbObject)} where Uid in ({strUids})");
        }
    }

    /// <summary>
    /// Reads in a embedded SQL script
    /// </summary>
    /// <param name="dbType">The type of database this script is for</param>
    /// <param name="script">The script name</param>
    /// <param name="clean">if set to true, empty lines and comments will be removed</param>
    /// <returns>the sql script</returns>
    public static string GetSqlScript(string dbType, string script, bool clean = false)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"FileFlows.Server.Database.Scripts.{dbType}.{script}";

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            string resource = reader.ReadToEnd();

            if (clean)
                return SqlHelper.CleanSql(resource);
            return resource;
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog($"Failed getting embedded SQL script '{dbType}.{script}': {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the sql scripts for a database
    /// </summary>
    /// <param name="dbType">the type of database</param>
    /// <returns>a list of stored procedures</returns>
    protected static Dictionary<string, string> GetStoredProcedureScripts(string dbType)
    {
        Dictionary<string, string> scripts = new();
        foreach (string script in new[] { "DeleteOldLogs" })
        {
            string sql = GetSqlScript(dbType, script + ".sql");
            scripts.Add(script, sql);
        }

        return scripts;
    }

    /// <summary>
    /// Logs a message to the database
    /// </summary>
    /// <param name="clientUid">The UID of the client, use Guid.Empty for the server</param>
    /// <param name="type">the type of log message</param>
    /// <param name="message">the message to log</param>
    public virtual Task Log(Guid clientUid, LogType type, string message) => Task.CompletedTask;

    /// <summary>
    /// Prune old logs from the database
    /// </summary>
    /// <param name="maxLogs">the maximum number of log messages to keep</param>
    public virtual Task PruneOldLogs(int maxLogs) => Task.CompletedTask;

    /// <summary>
    /// Searches the log using the given filter
    /// </summary>
    /// <param name="filter">the search filter</param>
    /// <returns>the messages found in the log</returns>
    public virtual Task<IEnumerable<DbLogMessage>> SearchLog(LogSearchModel filter) =>
        Task.FromResult((IEnumerable<DbLogMessage>)new DbLogMessage[] { });

    /// <summary>
    /// Gets an item from the database by it's name
    /// </summary>
    /// <param name="name">the name of the object</param>
    /// <typeparam name="T">the type to fetch</typeparam>
    /// <returns>the object if found</returns>
    public virtual async Task<T> GetByName<T>(string name) where T : FileFlowObject, new()
    {
        DbObject dbObject;
        using (var db = await GetDb())
        {
            // first see if this file exists by its name
            dbObject = await db.Db.FirstOrDefaultAsync<DbObject>(
                "where Type=@0 and name = @1", typeof(T).FullName, name);
        }

        if (string.IsNullOrEmpty(dbObject?.Data) == false)
            return Convert<T>(dbObject);

        return new();
    }

    /// <summary>
    /// Gets the failure flow for a particular library
    /// </summary>
    /// <param name="libraryUid">the UID of the library</param>
    /// <returns>the failure flow</returns>
    public abstract Task<Flow> GetFailureFlow(Guid libraryUid);

    /// <summary>
    /// Records a statistic
    /// </summary>
    /// <param name="statistic">the statistic to record</param>
    public async virtual Task RecordStatistic(Statistic statistic)
    {
        if (statistic?.Value == null)
            return;
        DbStatistic stat;
        if (double.TryParse(statistic.Value.ToString(), out double number))
        {
            stat = new DbStatistic()
            {
                Type = StatisticType.Number,
                Name = statistic.Name,
                LogDate = DateTime.Now,
                NumberValue = number,
                StringValue = string.Empty
            };
        }
        else
        {
            // treat as string
            stat = new DbStatistic()
            {
                Type = StatisticType.String,
                Name = statistic.Name,
                LogDate = DateTime.Now,
                NumberValue = 0,
                StringValue = statistic.Value.ToString()
            };
        }

        using (var db = await GetDb())
        {
            await db.Db.InsertAsync(stat);
        }
    }



    /// <summary>
    /// Gets statistics by name
    /// </summary>
    /// <returns>the matching statistics</returns>
    public virtual Task<IEnumerable<Statistic>> GetStatisticsByName(string name) =>
        Task.FromResult((IEnumerable<Statistic>)new Statistic[] { });

    /// <summary>
    /// Executes SQL against the database
    /// </summary>
    /// <param name="sql">the SQL to execute</param>
    /// <param name="args">arguments for where clause</param>
    /// <returns>the rows effected</returns>
    public virtual async Task<int> Execute(string sql, object[] args)
    {
        int result;
        using (var db = await GetDb())
        {
            result = await db.Db.ExecuteAsync(sql, args);
        }

        return result;
    }

    /// <summary>
    /// Updates the last seen of a node
    /// </summary>
    /// <param name="uid">the UID of the node</param>
    public virtual async Task UpdateNodeLastSeen(Guid uid)
    {
        string dt = DateTime.Now.ToString("o"); // same format as json
        using var db = await GetDb();
        string sql =
            $"update DbObject set Data = json_set(Data, '$.LastSeen', '{dt}') " +
            $"where Type = 'FileFlows.Shared.Models.ProcessingNode' and Uid = '{uid}'";
        await db.Db.ExecuteAsync(sql);
    }


    /// <summary>
    /// Updates a value in the json data of a DbObject
    /// </summary>
    /// <param name="uid">the UID of the object</param>
    /// <param name="property">The name of the property</param>
    /// <param name="value">the value to update</param>
    /// <returns>>the awaited task</returns>
    public virtual async Task UpdateJsonProperty(Guid uid, string property, object value)
    {
        using var db = await GetDb();
        string strValue = "";
        if (value is bool b)
            strValue = b ? "1": "0";
        else if (value is DateTime dt)
            strValue = "'" + dt.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzzz") + "'";
        else if (value is int i)
            strValue = i.ToString();
        else if (value is long l)
            strValue = l.ToString();
        else if (value is byte b2)
            strValue = b2.ToString();
        else if (value is short s)
            strValue = s.ToString();
        else if (value is double dbl)
            strValue = dbl.ToString();
        else
            strValue = "'" + value.ToString().Replace("'", "''") + "'";
        string sql =
            $"update DbObject set Data = json_set(Data, '$.{property}', {strValue}) where Uid = '{uid}'";
        await db.Db.ExecuteAsync(sql);
    }

    /// <summary>
    /// Gets if a column exists in the given table
    /// </summary>
    /// <param name="table">the table name</param>
    /// <param name="column">the column to look for</param>
    /// <returns>true if it exists, otherwise false</returns>
    public abstract Task<bool> ColumnExists(string table, string column);
}
