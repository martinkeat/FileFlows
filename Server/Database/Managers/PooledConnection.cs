using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using NPoco;
using NPoco.Linq;

namespace FileFlows.Server.Database.Managers;
/// <summary>
/// A pooled database connection
/// </summary>
public class PooledConnection: IDatabase, IDisposable, IDisposablePooledObject
{
    private NPoco.Database Db;
    private ObjectPool<PooledConnection> pool;
    /// <summary>
    /// Gets when the pooled connection was created
    /// </summary>
    public readonly DateTime CreationDate = DateTime.Now;
    
    public PooledConnection(NPoco.Database db, ObjectPool<PooledConnection> pool)
    {
        this.Db = db;
        this.pool = pool;
    }
    
    public void DisposePooledObject()
    {
        if(Db != null)
            Db.Dispose();
        Db = null;
    }
    
    public void Dispose()
    {
        if(pool != null)
            pool.Return(this);
    }

    public MapperCollection Mappers { 
        get => Db.Mappers;
        set { Db.Mappers = value; }
    }

    public IPocoDataFactory PocoDataFactory
    {
        get => Db.PocoDataFactory;
        set { Db.PocoDataFactory = value; }
    }

    public NPoco.DatabaseType DatabaseType => Db.DatabaseType;
    public List<IInterceptor> Interceptors => Db.Interceptors;
    public string ConnectionString => Db.ConnectionString;
    public DbParameter CreateParameter() => Db.CreateParameter();

    public void AddParameter(DbCommand cmd, object value) => Db.AddParameter(cmd, value);

    public DbCommand
        CreateCommand(DbConnection connection, CommandType commandType, string sql, params object[] args) =>
        Db.CreateCommand(connection, commandType, sql, args);

    public ITransaction GetTransaction() => Db.GetTransaction();

    public ITransaction GetTransaction(IsolationLevel isolationLevel) => Db.GetTransaction(isolationLevel);

    public void SetTransaction(DbTransaction tran) => Db.SetTransaction(tran);

    public void BeginTransaction() => Db.BeginTransaction();

    public void BeginTransaction(IsolationLevel isolationLevel) => Db.BeginTransaction(isolationLevel);

    public void AbortTransaction() => Db.AbortTransaction();

    public void CompleteTransaction() => Db.CompleteTransaction();

    public IDatabase OpenSharedConnection() => Db.OpenSharedConnection();

    public void CloseSharedConnection() => Db.CloseSharedConnection();

    public DbConnection Connection => Db.Connection;
    public DbTransaction Transaction => Db.Transaction;
    public IDictionary<string, object> Data => Db.Data;
    public int CommandTimeout { get; set; }
    public Task<T> SingleAsync<T>(string sql, params object[] args) => Db.SingleAsync<T>(sql, args);

    public Task<T> SingleAsync<T>(Sql sql) => Db.SingleAsync<T>(sql);

    public Task<T> SingleOrDefaultAsync<T>(string sql, params object[] args) => Db.SingleOrDefaultAsync<T>(sql, args);

    public Task<T> SingleOrDefaultAsync<T>(Sql sql) => Db.SingleOrDefaultAsync<T>(sql);

    public Task<T> SingleByIdAsync<T>(object primaryKey) => Db.SingleByIdAsync<T>(primaryKey);

    public Task<T> SingleOrDefaultByIdAsync<T>(object primaryKey) => Db.SingleOrDefaultByIdAsync<T>(primaryKey);

    public Task<T> FirstAsync<T>(string sql, params object[] args) => Db.FirstAsync<T>(sql, args);

    public Task<T> FirstAsync<T>(Sql sql) => Db.FirstAsync<T>(sql);

    public Task<T> FirstOrDefaultAsync<T>(string sql, params object[] args) => Db.FirstOrDefaultAsync<T>(sql, args);

    public Task<T> FirstOrDefaultAsync<T>(Sql sql) => Db.FirstOrDefaultAsync<T>(sql);

    public IAsyncEnumerable<T> QueryAsync<T>(string sql, params object[] args) => Db.QueryAsync<T>(sql, args);

    public IAsyncEnumerable<T> QueryAsync<T>(Sql sql) => Db.QueryAsync<T>(sql);

    public IAsyncQueryProviderWithIncludes<T> QueryAsync<T>() => Db.QueryAsync<T>();

    public Task<List<T>> FetchAsync<T>(string sql, params object[] args) => Db.FetchAsync<T>(sql, args);

    public Task<List<T>> FetchAsync<T>(Sql sql) => Db.FetchAsync<T>(sql);

    public Task<List<T>> FetchAsync<T>() => Db.FetchAsync<T>();

    public Task<Page<T>> PageAsync<T>(long page, long itemsPerPage, string sql, params object[] args) =>
        Db.PageAsync<T>(page, itemsPerPage, sql, args);

    public Task<Page<T>> PageAsync<T>(long page, long itemsPerPage, Sql sql) =>
        Db.PageAsync<T>(page, itemsPerPage, sql);

    public Task<List<T>> FetchAsync<T>(long page, long itemsPerPage, string sql, params object[] args) =>
        Db.FetchAsync<T>(page, itemsPerPage, sql, args);

    public Task<List<T>> FetchAsync<T>(long page, long itemsPerPage, Sql sql) =>
        Db.FetchAsync<T>(page, itemsPerPage, sql);

    public Task<List<T>> SkipTakeAsync<T>(long skip, long take, string sql, params object[] args) =>
        Db.SkipTakeAsync<T>(skip, take, sql, args);

    public Task<List<T>> SkipTakeAsync<T>(long skip, long take, Sql sql) => Db.SkipTakeAsync<T>(skip, take, sql);

    public Task<TRet> FetchMultipleAsync<T1, T2, TRet>(Func<List<T1>, List<T2>, TRet> cb, string sql,
        params object[] args) => Db.FetchMultipleAsync<T1, T2, TRet>(cb, sql, args);

    public Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql,
        params object[] args)
        => Db.FetchMultipleAsync<T1, T2, T3, TRet>(cb, sql, args);

    public Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb,
        string sql, params object[] args)
        => Db.FetchMultipleAsync<T1, T2, T3, T4, TRet>(cb, sql, args);

    public Task<TRet> FetchMultipleAsync<T1, T2, TRet>(Func<List<T1>, List<T2>, TRet> cb, Sql sql)
        => Db.FetchMultipleAsync<T1, T2, TRet>(cb, sql);

    public Task<TRet> FetchMultipleAsync<T1, T2, T3, TRet>(Func<List<T1>, List<T2>, List<T3>, TRet> cb, Sql sql)
        => Db.FetchMultipleAsync<T1, T2, T3, TRet>(cb, sql);

    public Task<TRet> FetchMultipleAsync<T1, T2, T3, T4, TRet>(Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb,
        Sql sql)
        => Db.FetchMultipleAsync<T1, T2, T3, T4, TRet>(cb, sql);

    public Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(string sql, params object[] args)
        => Db.FetchMultipleAsync<T1, T2>(sql, args);

    public Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(string sql, params object[] args)
        => Db.FetchMultipleAsync<T1, T2, T3>(sql, args);

    public Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(string sql,
        params object[] args)
        => Db.FetchMultipleAsync<T1, T2, T3, T4>(sql, args);

    public Task<(List<T1>, List<T2>)> FetchMultipleAsync<T1, T2>(Sql sql)
        => Db.FetchMultipleAsync<T1, T2>(sql);

    public Task<(List<T1>, List<T2>, List<T3>)> FetchMultipleAsync<T1, T2, T3>(Sql sql)
        => Db.FetchMultipleAsync<T1, T2, T3>(sql);

    public Task<(List<T1>, List<T2>, List<T3>, List<T4>)> FetchMultipleAsync<T1, T2, T3, T4>(Sql sql)
        => Db.FetchMultipleAsync<T1, T2, T3, T4>(sql);

    public Task<T> ExecuteScalarAsync<T>(string sql, params object[] args)
        => Db.ExecuteScalarAsync<T>(sql, args);

    public Task<T> ExecuteScalarAsync<T>(Sql sql)
        => Db.ExecuteScalarAsync<T>(sql);

    public Task<int> ExecuteAsync(string sql, params object[] args) => Db.ExecuteAsync(sql, args);

    public Task<int> ExecuteAsync(Sql sql) => Db.ExecuteAsync(sql);

    public Task<object> InsertAsync(string tableName, string primaryKeyName, object poco) =>
        Db.InsertAsync(tableName, primaryKeyName, poco);

    public Task<object> InsertAsync<T>(T poco) => Db.InsertAsync<T>(poco);

    public Task InsertBulkAsync<T>(IEnumerable<T> pocos, InsertBulkOptions options = null) =>
        Db.InsertBulkAsync<T>(pocos, options);

    public Task<int> InsertBatchAsync<T>(IEnumerable<T> pocos, BatchOptions options = null) =>
        Db.InsertBatchAsync<T>(pocos, options);

    public Task<int> UpdateAsync(object poco) => Db.UpdateAsync(poco);

    public Task<int> UpdateAsync(object poco, IEnumerable<string> columns) => Db.UpdateAsync(poco, columns);

    public Task<int> UpdateAsync<T>(T poco, Expression<Func<T, object>> fields) => Db.UpdateAsync<T>(poco, fields);

    public Task<int> UpdateBatchAsync<T>(IEnumerable<UpdateBatch<T>> pocos, BatchOptions options = null) =>
        Db.UpdateBatchAsync<T>(pocos, options);

    public Task<int> DeleteAsync(object poco) => Db.DeleteAsync(poco);

    public IAsyncUpdateQueryProvider<T> UpdateManyAsync<T>()
        => Db.UpdateManyAsync<T>();

    public IAsyncDeleteQueryProvider<T> DeleteManyAsync<T>() => Db.DeleteManyAsync<T>();

    public Task<bool> IsNewAsync<T>(T poco) => Db.IsNewAsync<T>(poco);

    public Task SaveAsync<T>(T poco) => Db.SaveAsync<T>(poco);

    public void BuildPageQueries<T>(long skip, long take, string sql, ref object[] args, out string sqlCount,
        out string sqlPage)
        => Db.BuildPageQueries<T>(skip, take, sql, ref args, out sqlCount, out sqlPage);

    public int Execute(string sql, params object[] args) => Db.Execute(sql, args);

    public int Execute(Sql sql) => Db.Execute(sql);

    public int Execute(string sql, CommandType commandType, params object[] args) => Db.Execute(sql, commandType, args);

    public T ExecuteScalar<T>(string sql, params object[] args) => Db.ExecuteScalar<T>(sql, args);

    public T ExecuteScalar<T>(Sql sql) => Db.ExecuteScalar<T>(sql);

    public T ExecuteScalar<T>(string sql, CommandType commandType, params object[] args) =>
        Db.ExecuteScalar<T>(sql, commandType, args);

    public List<object> Fetch(Type type, string sql, params object[] args) => Db.Fetch(type, sql, args);

    public List<object> Fetch(Type type, Sql Sql) => Db.Fetch(type, Sql);

    public IEnumerable<object> Query(Type type, string sql, params object[] args) => Db.Query(type, sql, args);

    public IEnumerable<object> Query(Type type, Sql Sql) => Db.Query(type, Sql);

    public List<T> Fetch<T>() => Db.Fetch<T>();

    public List<T> Fetch<T>(string sql, params object[] args) => Db.Fetch<T>(sql, args);

    public List<T> Fetch<T>(Sql sql) => Db.Fetch<T>(sql);

    public List<T> Fetch<T>(long page, long itemsPerPage, string sql, params object[] args)
        => Db.Fetch<T>(page, itemsPerPage, sql, args);

    public List<T> Fetch<T>(long page, long itemsPerPage, Sql sql) => Db.Fetch<T>(page, itemsPerPage, sql);

    public Page<T> Page<T>(long page, long itemsPerPage, string sql, params object[] args)
        => Db.Page<T>(page, itemsPerPage, sql, args);

    public Page<T> Page<T>(long page, long itemsPerPage, Sql sql)
        => Db.Page<T>(page, itemsPerPage, sql);

    public List<T> SkipTake<T>(long skip, long take, string sql, params object[] args)
        => Db.SkipTake<T>(skip, take, sql, args);

    public List<T> SkipTake<T>(long skip, long take, Sql sql) => Db.SkipTake<T>(skip, take, sql);

    public List<T> FetchOneToMany<T>(Expression<Func<T, IList>> many, string sql, params object[] args)
        => Db.FetchOneToMany<T>(many, sql, args);

    public List<T> FetchOneToMany<T>(Expression<Func<T, IList>> many, Sql sql)
        => Db.FetchOneToMany<T>(many, sql);

    public List<T> FetchOneToMany<T>(Expression<Func<T, IList>> many, Func<T, object> idFunc, string sql,
        params object[] args)
        => Db.FetchOneToMany<T>(many, idFunc, sql, args);

    public List<T> FetchOneToMany<T>(Expression<Func<T, IList>> many, Func<T, object> idFunc, Sql sql)
        => Db.FetchOneToMany<T>(many, idFunc, sql);

    public IEnumerable<T> Query<T>(string sql, params object[] args)
        => Db.Query<T>(sql, args);

    public IEnumerable<T> Query<T>(Sql sql) => Db.Query<T>(sql);


    public IQueryProviderWithIncludes<T> Query<T>() => Db.Query<T>();

    public T SingleById<T>(object primaryKey) => Db.SingleById<T>(primaryKey);

    public T Single<T>(string sql, params object[] args) => Db.Single<T>(sql, args);

    public T SingleInto<T>(T instance, string sql, params object[] args) => Db.SingleInto<T>(instance, sql, args);

    public T SingleOrDefaultById<T>(object primaryKey) => Db.SingleOrDefaultById<T>(primaryKey);

    public T SingleOrDefault<T>(string sql, params object[] args) => Db.SingleOrDefault<T>(sql, args);

    public T SingleOrDefaultInto<T>(T instance, string sql, params object[] args) =>
        Db.SingleOrDefaultInto<T>(instance, sql, args);

    public T First<T>(string sql, params object[] args) => Db.First<T>(sql, args);

    public T FirstInto<T>(T instance, string sql, params object[] args) => Db.FirstInto<T>(instance, sql, args);

    public T FirstOrDefault<T>(string sql, params object[] args) => Db.FirstOrDefault<T>(sql, args);

    public T FirstOrDefaultInto<T>(T instance, string sql, params object[] args) =>
        Db.FirstOrDefaultInto<T>(instance, sql, args);

    public T Single<T>(Sql sql) => Db.Single<T>(sql);

    public T SingleInto<T>(T instance, Sql sql) => Db.SingleInto<T>(instance, sql);

    public T SingleOrDefault<T>(Sql sql) => Db.SingleOrDefault<T>(sql);

    public T SingleOrDefaultInto<T>(T instance, Sql sql) => Db.SingleOrDefaultInto<T>(instance, sql);

    public T First<T>(Sql sql) => Db.First<T>(sql);

    public T FirstInto<T>(T instance, Sql sql) => Db.FirstInto<T>(instance, sql);

    public T FirstOrDefault<T>(Sql sql) => Db.FirstOrDefault<T>(sql);

    public T FirstOrDefaultInto<T>(T instance, Sql sql) => Db.FirstOrDefaultInto<T>(instance, sql);

    public Dictionary<TKey, TValue> Dictionary<TKey, TValue>(Sql Sql) where TKey : notnull => Db.Dictionary<TKey, TValue>(Sql);

    public Dictionary<TKey, TValue> Dictionary<TKey, TValue>(string sql, params object[] args) where TKey : notnull => Db.Dictionary<TKey, TValue>(sql, args);

    public bool Exists<T>(object primaryKey) => Db.Exists<T>(primaryKey);

    public TRet FetchMultiple<T1, T2, TRet>(Func<List<T1>, List<T2>, TRet> cb, string sql, params object[] args)
        => Db.FetchMultiple<T1, T2, TRet>(cb, sql, args);

    public TRet FetchMultiple<T1, T2, T3, TRet>(Func<List<T1>, List<T2>, List<T3>, TRet> cb, string sql,
        params object[] args)
        => Db.FetchMultiple<T1, T2, T3, TRet>(cb, sql, args);

    public TRet FetchMultiple<T1, T2, T3, T4, TRet>(Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, string sql, params object[] args)
        => Db.FetchMultiple<T1, T2, T3, T4, TRet>(cb, sql, args);

    public TRet FetchMultiple<T1, T2, TRet>(Func<List<T1>, List<T2>, TRet> cb, Sql sql)
        => Db.FetchMultiple<T1, T2, TRet>(cb, sql);

    public TRet FetchMultiple<T1, T2, T3, TRet>(Func<List<T1>, List<T2>, List<T3>, TRet> cb, Sql sql)
        => Db.FetchMultiple<T1, T2, T3, TRet>(cb, sql);

    public TRet FetchMultiple<T1, T2, T3, T4, TRet>(Func<List<T1>, List<T2>, List<T3>, List<T4>, TRet> cb, Sql sql)
        => Db.FetchMultiple<T1, T2, T3, T4, TRet>(cb, sql);

    public (List<T1>, List<T2>) FetchMultiple<T1, T2>(string sql, params object[] args)
        => Db.FetchMultiple<T1, T2>(sql, args);

    public (List<T1>, List<T2>, List<T3>) FetchMultiple<T1, T2, T3>(string sql, params object[] args)
        => Db.FetchMultiple<T1, T2, T3>(sql, args);

    public (List<T1>, List<T2>, List<T3>, List<T4>) FetchMultiple<T1, T2, T3, T4>(string sql, params object[] args)
        => Db.FetchMultiple<T1, T2, T3, T4>(sql, args);

    public (List<T1>, List<T2>) FetchMultiple<T1, T2>(Sql sql)
        => Db.FetchMultiple<T1, T2>(sql);
    
    public (List<T1>, List<T2>, List<T3>) FetchMultiple<T1, T2, T3>(Sql sql)
        => Db.FetchMultiple<T1, T2, T3>(sql);

    public (List<T1>, List<T2>, List<T3>, List<T4>) FetchMultiple<T1, T2, T3, T4>(Sql sql)
        => Db.FetchMultiple<T1, T2, T3, T4>(sql);

    public int OneTimeCommandTimeout
    {
        get => Db.OneTimeCommandTimeout;
        set { Db.OneTimeCommandTimeout = value; }
    }

    public object Insert<T>(string tableName, string primaryKeyName, bool autoIncrement, T poco)
        => Db.Insert<T>(tableName, primaryKeyName, autoIncrement, poco);

    public object Insert<T>(string tableName, string primaryKeyName, T poco)
        => Db.Insert<T>(tableName, primaryKeyName, poco);

    public object Insert<T>(T poco) => Db.Insert<T>(poco);

    public void InsertBulk<T>(IEnumerable<T> pocos, InsertBulkOptions? options = null) =>
        Db.InsertBulk<T>(pocos, options);

    public int InsertBatch<T>(IEnumerable<T> pocos, BatchOptions? options = null) => Db.InsertBatch<T>(pocos, options);

    public int Update(string tableName, string primaryKeyName, object poco, object primaryKeyValue)
        => Db.Update(tableName, primaryKeyName, poco, primaryKeyValue);

    public int Update(string tableName, string primaryKeyName, object poco) =>
        Db.Update(tableName, primaryKeyName, poco);

    public int Update(string tableName, string primaryKeyName, object poco, object? primaryKeyValue,
        IEnumerable<string>? columns)
        => Db.Update(tableName, primaryKeyName, poco, primaryKeyValue, columns);

    public int Update(string tableName, string primaryKeyName, object poco, IEnumerable<string>? columns)
        => Db.Update(tableName, primaryKeyName, poco, columns);

    public int Update(object poco, IEnumerable<string> columns) => Db.Update(poco, columns);

    public int Update(object poco, object primaryKeyValue, IEnumerable<string>? columns) =>
        Db.Update(poco, primaryKeyValue, columns);

    public int Update(object poco) => Db.Update(poco);

    public int Update<T>(T poco, Expression<Func<T, object>> fields) => Db.Update<T>(poco, fields);

    public int Update(object poco, object primaryKeyValue) => Db.Update(poco, primaryKeyValue);

    public int Update<T>(string sql, params object[] args) => Db.Update<T>(sql, args);

    public int Update<T>(Sql sql) => Db.Update<T>(sql);

    public int UpdateBatch<T>(IEnumerable<UpdateBatch<T>> pocos, BatchOptions? options = null)
        => Db.UpdateBatch<T>(pocos, options);

    public IUpdateQueryProvider<T> UpdateMany<T>() => Db.UpdateMany<T>();

    public int Delete(string tableName, string primaryKeyName, object poco) =>
        Db.Delete(tableName, primaryKeyName, poco);

    public int Delete(string tableName, string primaryKeyName, object? poco, object? primaryKeyValue)
        => Db.Delete(tableName, primaryKeyName, poco, primaryKeyValue);

    public int Delete(object poco) => Db.Delete(poco);

    public int Delete<T>(string sql, params object[] args) => Db.Delete<T>(sql, args);

    public int Delete<T>(Sql sql) => Db.Delete<T>(sql);

    public int Delete<T>(object pocoOrPrimaryKey) => Db.Delete<T>(pocoOrPrimaryKey);

    public IDeleteQueryProvider<T> DeleteMany<T>() => Db.DeleteMany<T>();

    public void Save<T>(T poco) => Db.Save<T>(poco);

    public bool IsNew<T>(T poco) => Db.IsNew<T>(poco);
}