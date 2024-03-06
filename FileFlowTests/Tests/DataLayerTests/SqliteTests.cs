using System.IO;
using System.Text.Json;
using System.Threading;
using FileFlows.DataLayer;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.DatabaseCreators;
using FileFlows.Shared.Json;

namespace FileFlowTests.Tests.DataLayerTests;

[TestClass]
public class SqliteTests : DbLayerTest
{
    private string GetConnectionString(DatabaseType type, string dbName)
    {
        const string IP_ADDRESS = "192.168.1.170";
        switch (type)
        {
            case DatabaseType.Sqlite:
            {
                var file = Path.Combine(TempPath, dbName + ".sqlite");
                if (File.Exists(file))
                    File.Delete(file);
                return SQLiteConnector.GetConnectionString(file);
            }
            case DatabaseType.SqlServer:
                return $@"Server={IP_ADDRESS};Database={dbName};User Id=sa;Password=Password123;";
            case DatabaseType.MySql:
                return $"Server={IP_ADDRESS};Port=3306;Database={dbName};Uid=root;Pwd=Password123;";
            case DatabaseType.Postgres:
                return $"Host=192.168.1.5;Port=5432;Username=postgres;Password=Password123;Database={dbName};Include Error Detail=true";
        }

        return null;
    }

    private IDatabaseConnector GetConnector(DatabaseType type, string dbName)
    {
        string connString = GetConnectionString(type, dbName);
        switch (type)
        {
            case DatabaseType.Sqlite:
                return new SQLiteConnector(Logger, connString);
            case DatabaseType.SqlServer:
                return new SqlServerConnector(Logger, connString);
            case DatabaseType.MySql:
                return new FileFlows.DataLayer.DatabaseConnectors.MySqlConnector(Logger, connString);
            case DatabaseType.Postgres:
                return new PostgresConnector(Logger, connString);
        }

        Assert.Fail("Invalid database type");
        return null;
    }
    private IDatabaseCreator GetCreator(DatabaseType type, string dbName, out string connectionString)
    {
        connectionString = GetConnectionString(type, dbName);
        switch (type)
        {
            case DatabaseType.Sqlite:
                return new SQLiteDatabaseCreator(Logger, connectionString);
            case DatabaseType.SqlServer:
                return new SqlServerDatabaseCreator(Logger, connectionString);
            case DatabaseType.MySql:
                return new MySqlDatabaseCreator(Logger, connectionString);
            case DatabaseType.Postgres:
                return new PostgresDatabaseCreator(Logger, connectionString);
        }

        Assert.Fail("Invalid database type");
        return null;
    }
    
    [TestMethod]
    public void CreateDatabase()
    {
        foreach (var dbType in new[] { DatabaseType.Postgres, DatabaseType.Sqlite, DatabaseType.SqlServer, DatabaseType.MySql })
        {
            var dbCreator = GetCreator(dbType, "FileFlows_" + TestContext.TestName, out _);
            Assert.AreEqual(DbCreateResult.Created, dbCreator.CreateDatabase(true).Value);
            Assert.AreEqual(true, dbCreator.CreateDatabaseStructure().Value);
        }
    }



    [TestMethod]
    public void InsertOne()
    {
        foreach (var dbType in new[]
                 {
                     DatabaseType.Postgres,
                     DatabaseType.Sqlite, 
                     DatabaseType.SqlServer,
                     DatabaseType.MySql
                 })
        {
            string dbName = "FileFlows_" + TestContext.TestName;
            var dbCreator = GetCreator(dbType, dbName, out string connectionString);
            var dbCreateResult = dbCreator.CreateDatabase(true).Value;
            Assert.AreNotEqual(DbCreateResult.Failed, dbCreateResult);
            if(dbCreateResult == DbCreateResult.Created)
                Assert.AreEqual(true, dbCreator.CreateDatabaseStructure().Value);

            var db = new DatabaseAccessManager(Logger, dbType, connectionString);

            var library = new Library()
            {
                Uid = Guid.NewGuid(),
                Name = "TestLibrary",
                Enabled = true,
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow,
                Description = "this is a test description",
                Path = "/a/b/c",
                Scan = true,
                HoldMinutes = 30,
                LastScanned = DateTime.UtcNow
            };
            
            string TYPE_NAME = library.GetType().FullName!;
            db.FileFlowsObjectManager.AddOrUpdateObject(library).Wait();

            var created = db.FileFlowsObjectManager.Single<Library>(library.Uid).Result.Value;
            Assert.IsNotNull(created);

            db.DbObjectManager.SetDataValue(library.Uid, TYPE_NAME, nameof(library.Description), "Updated String").Wait();
            db.DbObjectManager.SetDataValue(library.Uid, TYPE_NAME, nameof(library.HoldMinutes), 123456).Wait();
            db.DbObjectManager.SetDataValue(library.Uid, TYPE_NAME, nameof(library.LastScanned), new DateTime(2000,1,1)).Wait();
            db.DbObjectManager.SetDataValue(library.Uid, TYPE_NAME, nameof(library.Enabled), false).Wait();

            var updated = db.FileFlowsObjectManager.Single<Library>(library.Uid).Result.Value;
            Assert.AreEqual("Updated String", updated.Description);
            Assert.AreEqual(123456, updated.HoldMinutes);
            Assert.AreEqual("2000-01-01", updated.LastScanned.ToString("yyyy-MM-dd"));
            Assert.AreEqual(false, updated.Enabled);
        }
    }

    [TestMethod]
    public void HammerTime()
    {
        foreach (var dbType in new[]
                 {
                     // DatabaseType.Sqlite,
                     // DatabaseType.SqlServer,
                     // DatabaseType.MySql,
                     DatabaseType.Postgres,
                 })
        {
            string dbName = "FileFlows_" + TestContext.TestName;
            var dbCreator = GetCreator(dbType, dbName, out string connectionString);
            var dbCreateResult = dbCreator.CreateDatabase(true).Value;
            Assert.AreNotEqual(DbCreateResult.Failed, dbCreateResult);
            if (dbCreateResult == DbCreateResult.Created)
                Assert.AreEqual(true, dbCreator.CreateDatabaseStructure().Value);

            var db = new DatabaseAccessManager(Logger, dbType, connectionString);

            const string TYPE_NAME = "FileFlows.DataLayer.Models.DbObject";
            List<Task> tasks = new();

            const int TOTAL_I = 1000;
            const int TOTAL_J = 10;
            for (int i = 0; i < TOTAL_I; i++)
            {
                tasks.Add(Task.Run(async () =>
                    {
                        for (int j = 0; j < TOTAL_J; j++)
                        {
                            db.DbObjectManager.AddOrUpdate(new FileFlows.DataLayer.Models.DbObject()
                            {
                                Uid = Guid.NewGuid(),
                                Name = $"TestObject_{i}_{j}",
                                DateModified = DateTime.UtcNow,
                                DateCreated = DateTime.UtcNow,
                                Type = TYPE_NAME,
                                Data = System.Text.Json.JsonSerializer.Serialize(new
                                {
                                    Library = "Some Library",
                                    Size = 1000000000f
                                })

                            }).Wait();
                        }
                    }
                ));
            }

            Task.WhenAll(tasks).Wait();

            var items = db.DbObjectManager.GetAll(TYPE_NAME).Result;
            Assert.AreEqual(TOTAL_I * TOTAL_J, items.Count);
        }
    }

    [TestMethod]
    public void HammerTime_ReadWrite()
    {
        foreach (var dbType in new[]
                 {
                     // DatabaseType.Sqlite,
                     // DatabaseType.SqlServer,
                     DatabaseType.MySql,
                     //DatabaseType.Postgres,
                 })
        {
            string dbName = "FileFlows_" + TestContext.TestName;
            var dbCreator = GetCreator(dbType, dbName, out string connectionString);
            var dbCreateResult = dbCreator.CreateDatabase(true).Value;
            Assert.AreNotEqual(DbCreateResult.Failed, dbCreateResult);
            if (dbCreateResult == DbCreateResult.Created)
                Assert.AreEqual(true, dbCreator.CreateDatabaseStructure().Value);

            var db = new DatabaseAccessManager(Logger, dbType, connectionString);

            const string TYPE_NAME = "FileFlows.DataLayer.Models.DbObject";
            List<Task> tasks = new();

            const int TOTAL_OPERATIONS = 100;
            const int TOTAL_THREADS = 1000;

            int totalWrites = 0;
            Random random = new Random(DateTime.Now.Millisecond);

            int invalid = 0;
            for (int i = 0; i < TOTAL_THREADS; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < TOTAL_OPERATIONS; j++)
                    {
                        try
                        {
                            if (j > 1 && random.Next(2) == 0) // Randomly choose between read and write
                            {
                                // Read operation
                                var items = await db.DbObjectManager.GetAll(TYPE_NAME);
                                if (items.Count < 1)
                                    Interlocked.Increment(ref invalid);
                                Assert.IsTrue(items.Count > 0);
                                // Process or validate read results if needed
                            }
                            else
                            {
                                // Write operation
                                Interlocked.Increment(ref totalWrites); // Increment atomically
                                await db.DbObjectManager.AddOrUpdate(new FileFlows.DataLayer.Models.DbObject()
                                {
                                    Uid = Guid.NewGuid(),
                                    Name = $"TestObject_{i}_{j}",
                                    Type = TYPE_NAME,
                                    Data = System.Text.Json.JsonSerializer.Serialize(new
                                    {
                                        Library = "Some Library",
                                        Size = 1000000000f
                                    })

                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.ELog(ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                    }
                }));
            }

            Task.WhenAll(tasks).Wait();

            var finalItems = db.DbObjectManager.GetAll(TYPE_NAME).Result;
            Assert.IsTrue(totalWrites > 0);
            Assert.AreEqual(0, invalid);
            Assert.AreEqual(totalWrites, finalItems.Count);
        }

    }

    [TestMethod]
    public void BulkInsert()
    {
        var rand = new Random(DateTime.Now.Microsecond);
        foreach (var dbType in new[]
                 {
                     DatabaseType.Sqlite, 
                     DatabaseType.Postgres,
                     DatabaseType.SqlServer,
                     DatabaseType.MySql,
                 })
        {
            Logger.ILog("Database Type: " + dbType);
            
            DatabaseAccessManager.Reset();
            
            string dbName = "FileFlows_" + TestContext.TestName;
            var dbCreator = GetCreator(dbType, dbName, out string connectionString);
            var dbCreateResult = dbCreator.CreateDatabase(true).Value;

            
            Assert.AreNotEqual(DbCreateResult.Failed, dbCreateResult);
            if(dbCreateResult == DbCreateResult.Created)
                Assert.AreEqual(true, dbCreator.CreateDatabaseStructure().Value);
            
            var db = new DatabaseAccessManager(Logger, dbType, connectionString);
            
            var library = new Library()
            {
                Uid = Guid.NewGuid(),
                Name = "TestLibrary",
                Enabled = true,
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow,
                Description = "this is a test description",
                Path = "/a/b/c",
                Scan = true,
                HoldMinutes = 30,
                LastScanned = DateTime.UtcNow
            };
            
            db.FileFlowsObjectManager.AddOrUpdateObject(library).Wait();

            var created = db.FileFlowsObjectManager.Single<Library>(library.Uid).Result.Value;
            Assert.IsNotNull(created);

            List<LibraryFile> files = new();
            for (int i = 0; i < 100; i++)
            {
                files.Add(new()
                {
                    DateCreated = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow,
                    Uid = Guid.NewGuid(),
                    Name = "/unit-test/fake/file_" + (i + 1).ToString("D4") + ".mkv",
                    RelativePath = "file_" + (i + 1).ToString("D4") + ".mkv",
                    Fingerprint = "",
                    Flags = LibraryFileFlags.None,
                    LibraryName = library.Name,
                    LibraryUid = library.Uid,
                    OriginalSize = rand.NextInt64(1000, 1_000_000_000_000_000),
                    CreationTime = DateTime.UtcNow,
                    LastWriteTime = DateTime.UtcNow,
                });
            }

            db.LibraryFileManager.InsertBulk(files).Wait();
            //
            // if (dbType == DatabaseType.Sqlite)
            //     continue;
            
            var results = db.LibraryFileManager.GetAll().Result.OrderBy(x => x.Name).ToList();
            Assert.AreEqual(files.Count, results.Count);
            for (int i = 0; i < results.Count; i++)
            {
                var fileProperties = files[i].GetType().GetProperties();
                var resultProperties = results[i].GetType().GetProperties();

                Assert.AreEqual(fileProperties.Length, resultProperties.Length);

                foreach (var fileProperty in fileProperties)
                {
                    var resultProperty = resultProperties.FirstOrDefault(p => p.Name == fileProperty.Name);
                    Assert.IsNotNull(resultProperty, $"{dbType}: Property {fileProperty.Name} not found in result object.");

                    if (resultProperty.PropertyType == typeof(ObjectReference))
                        continue; // we dont care about this type
                    
                    var fileValue = fileProperty.GetValue(files[i]);
                    var resultValue = resultProperty.GetValue(results[i]);

                    if (resultProperty.PropertyType == typeof(string))
                    {
                        fileValue = fileValue as string ?? string.Empty;
                        resultValue = resultValue as string ?? string.Empty;
                    }
                    
                    if (resultProperty.PropertyType == typeof(DateTime))
                    {
                        // if (dbType == DatabaseType.Sqlite)
                        //      continue;
                        DateTime dtFile = ((DateTime)fileValue!).EnsureNotLessThan1970();
                        DateTime dtResult = ((DateTime)resultValue!).EnsureNotLessThan1970();
                        if (Math.Abs(dtFile.Subtract(dtResult).TotalSeconds) < 2)
                            continue; // mysql can be 1 second out, nfi why
                        fileValue = dtFile.ToString("yyyy-MM-dd HH:mm:ss");
                        resultValue = dtResult.ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    if (resultProperty.PropertyType == typeof(List<ExecutedNode>))
                    {
                        fileValue = JsonEncode(fileValue ?? new List<ExecutedNode>());
                        resultValue = JsonEncode(resultValue ?? new List<ExecutedNode>());
                    }
                    
                    if (resultProperty.PropertyType == typeof(Dictionary<string, object>))
                    {
                        fileValue = JsonEncode(fileValue ?? new Dictionary<string, object>());
                        resultValue = JsonEncode(resultValue ?? new Dictionary<string, object>());
                    }
                    
                    Assert.AreEqual(fileValue, resultValue, $"{dbType}: Property {fileProperty.Name} values do not match.");
                }
            }
        }
    }
    
    static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
    {
        Converters = { new TimeSpanConverter() }
    };
    
    /// <summary>
    /// JSON encodes an object for the database
    /// </summary>
    /// <param name="o">the object to encode</param>
    /// <returns>the JSON encoded object</returns>
    private static string JsonEncode(object? o)
    {
        if (o == null)
            return string.Empty;
        return JsonSerializer.Serialize(o, JsonOptions);
    }
}