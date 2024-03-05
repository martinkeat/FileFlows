using System.Text;
using System.Text.Json;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.Converters;
using FileFlows.DataLayer.Helpers;
using FileFlows.DataLayer.Models;
using FileFlows.Shared;
using FileFlows.Shared.Models;

namespace FileFlows.DataLayer;

/// <summary>
/// Manages data access operations for the DbObject table
/// </summary>
public class DbObjectManager
{
    private readonly IDatabaseConnector? DbConnector;
    private readonly DatabaseType DbType; 
    public DbObjectManager(DatabaseType dbType, IDatabaseConnector? dbConnector)
    {
        DbType = dbType;
        DbConnector = dbConnector;
    }

    /// <summary>
    /// Adds or updates a DbObject directly in the database
    /// Note: NO revision will be saved
    /// </summary>
    /// <param name="dbObject">the object to add or update</param>
    internal async Task AddOrUpdate(DbObject dbObject)
    {
        using var db = await DbConnector.GetDb(write: true);
        bool updated = await db.Db.UpdateAsync(dbObject) > 0;
        if (updated)
            return;
        await db.Db.InsertAsync(dbObject);
    }

    /// <summary>
    /// Inserts a new DbObject
    /// </summary>
    /// <param name="dbObject">the new DbObject</param>
    internal async Task Insert(DbObject dbObject)
    {
        using var db = await DbConnector.GetDb(write: true);
        await db.Db.InsertAsync(dbObject);
    }

    /// <summary>
    /// Updates a new DbObject
    /// </summary>
    /// <param name="dbObject">the new DbObject</param>
    internal async Task Update(DbObject dbObject)
    {
        using var db = await DbConnector.GetDb(write: true);
        await db.Db.UpdateAsync(dbObject);
    }

    /// <summary>
    /// Fetches all items of a type
    /// </summary>
    /// <param name="typeName">the name of the type</param>
    /// <returns>the items</returns>
    internal async Task<List<DbObject>> GetAll(string typeName)
    {
        using var db = await DbConnector.GetDb();
        return (await db.Db.FetchAsync<DbObject>($"where {DbConnector.WrapFieldName(nameof(DbObject.Type))} = @0", typeName)).ToList();
    }
    
    /// <summary>
    /// Select a single instance of a type
    /// </summary>
    /// <param name="typeName">the name of the type</param>
    /// <returns>a single instance</returns>
    internal async Task<DbObject> Single(string typeName)
    {
        using var db = await DbConnector.GetDb();
        return await db.Db.FirstOrDefaultAsync<DbObject>($@"where {DbConnector.WrapFieldName("Type")}=@0", typeName);
    }

    /// <summary>
    /// Gets a DbObject by its name
    /// </summary>
    /// <param name="typeName">the name of the type</param>
    /// <param name="name">the name of the item</param>
    /// <param name="ignoreCase">if casing should be ignored</param>
    /// <returns>the item if found</returns>
    internal async Task<DbObject?> GetByName(string typeName, string name, bool ignoreCase)
    {
        using var db = await DbConnector.GetDb();
        if(ignoreCase)
            return await db.Db.FirstOrDefaultAsync<DbObject>($"where {DbConnector.WrapFieldName("Type")}=@0 and LOWER({DbConnector.WrapFieldName("Name")})=@1", typeName, name.ToLowerInvariant());
        return await db.Db.FirstOrDefaultAsync<DbObject>($"where {DbConnector.WrapFieldName("Type")}=@0 and {DbConnector.WrapFieldName("Name")}=@1", typeName, name);
    }
    

    /// <summary>
    /// Gets all names for a specific type
    /// </summary>
    /// <param name="typeName">the name of the type</param>
    /// <returns>all names for a specific type</returns>
    internal async Task<List<string>> GetNames(string typeName)
    {
        using var db = await DbConnector.GetDb();
        return await db.Db.FetchAsync<string>(
            $"select {DbConnector.WrapFieldName(nameof(DbObject.Name))} " +
            $"from {DbConnector.WrapFieldName(nameof(DbObject))} " +
            $"where {DbConnector.WrapFieldName(nameof(DbObject.Type))} = @0", typeName);
    }
    
    /// <summary>
    /// Select a single instance of a type
    /// </summary>
    /// <param name="uid">the UID of the item</param>
    /// <returns>a single instance</returns>
    internal async Task<DbObject> Single(Guid uid)
    {
        using var db = await DbConnector.GetDb();
        return await db.Db.FirstOrDefaultAsync<DbObject>($"where {DbConnector.WrapFieldName("Uid")}='{uid}'");
    }
    
    /// <summary>
    /// Updates the last modified date of an object
    /// </summary>
    /// <param name="uid">the UID of the object to update</param>
    internal async Task UpdateLastModified(Guid uid)
    {
        using var db = await DbConnector.GetDb(write: true);
        await db.Db.ExecuteAsync($"update {DbConnector.WrapFieldName(nameof(DbObject))} " +
                                 $"set {DbConnector.WrapFieldName("DateModified")} = @0 " +
                                 $"where {DbConnector.WrapFieldName("Uid")} = @1", DateTime.Now,
            uid);
    }

    /// <summary>
    /// Sets a value in the Data object of an item
    /// </summary>
    /// <param name="uid">The UID of the item being updated</param>
    /// <param name="typeName">The type name of the item being updated</param>
    /// <param name="field">The field in the Data object to update</param>
    /// <param name="value">The value to set</param>
    public async Task SetDataValue(Guid uid, string? typeName, string field, object value)
    {
        using var db = await DbConnector.GetDb(write: true);
        string sql = $"update {DbConnector.WrapFieldName(nameof(DbObject))} set ";
        string dataColumnName = DbConnector.WrapFieldName("Data");
        if (DbType == DatabaseType.SqlServer)
            sql += $" {dataColumnName} = json_modify({dataColumnName}, '$.{field}', @0)";
        else if (DbType == DatabaseType.Postgres)
        {
            if (value is bool b)
                value = b ? "'1'" : "'0'";
            else
                value = SqlHelper.Escape(JsonSerializer.Serialize(value));
            
            sql +=
                $" {dataColumnName} = jsonb_set({dataColumnName}::jsonb, '{{{field}}}', {value}::jsonb)::text"; 
        }
        else // mysql and sqlite are the same
            sql += $" {dataColumnName} = json_set({dataColumnName}, '$.{field}', @0)";

        if (value is DateTime dt)
            value = dt.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        if (value is bool bValue)
            value = bValue ? 1 : 0;
        
        sql += $" where {DbConnector.WrapFieldName(nameof(DbObject.Uid))} = '{uid}'" +
               $" and {DbConnector.WrapFieldName(nameof(DbObject.Type))} = {SqlHelper.Escape(typeName!)}";
        

        await db.Db.ExecuteAsync(sql, value, typeName);
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

                sql.AppendLine($"insert into {DbConnector.WrapFieldName(nameof(DbObject))} " +
                               $"({DbConnector.WrapFieldName("Uid")}, {DbConnector.WrapFieldName("Name")}," +
                               $" {DbConnector.WrapFieldName("Type")}, {DbConnector.WrapFieldName("Data")})" +
                               $" values (" +
                               obj.Uid.ToString().SqlEscape() + "," +
                               obj.Name.SqlEscape() + "," +
                               (type?.FullName ?? string.Empty).SqlEscape() + "," +
                               json.SqlEscape() +
                               ");");
            }

            if (sql.Length > 0)
            {
                using var db = await DbConnector.GetDb(write: true);
                await db.Db.ExecuteAsync(sql.ToString());
            }
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
        
        using var db = await DbConnector.GetDb(write: true);
        await db.Db.ExecuteAsync($"delete from {DbConnector.WrapFieldName(nameof(DbObject))}" +
                                 $" where {DbConnector.WrapFieldName("Uid")} in ({strUids})");
    }
}