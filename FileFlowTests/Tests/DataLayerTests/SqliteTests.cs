using System.IO;
using System.Threading;
using FileFlows.DataLayer;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.DatabaseCreators;
using DatabaseType = FileFlows.DataLayer.DatabaseType;

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
                // connString = Regex.Replace(connString, "Database=[^;]+;", string.Empty);
                TestContext.WriteLine("Connecting to SQL Server: " + connectionString);
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
}