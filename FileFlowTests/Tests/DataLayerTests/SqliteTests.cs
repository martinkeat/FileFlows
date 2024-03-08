using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading;
using FileFlows.DataLayer;
using FileFlows.DataLayer.DatabaseConnectors;
using FileFlows.DataLayer.DatabaseCreators;
using FileFlows.DataLayer.Helpers;
using FileFlows.DataLayer.Upgrades;
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

    private IDatabaseConnector GetConnector(DatabaseType type, string connectionString)
    {
        switch (type)
        {
            case DatabaseType.Sqlite:
                return new SQLiteConnector(Logger, connectionString);
            case DatabaseType.SqlServer:
                return new SqlServerConnector(Logger, connectionString);
            case DatabaseType.MySql:
                return new FileFlows.DataLayer.DatabaseConnectors.MySqlConnector(Logger, connectionString);
            case DatabaseType.Postgres:
                return new PostgresConnector(Logger, connectionString);
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
                     // DatabaseType.Postgres,
                     // DatabaseType.SqlServer,
                     // DatabaseType.MySql,
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
            for (int i = 0; i < 10_000; i++)
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
    
    

    [TestMethod]
    public void OneBillionDollars()
    {
        var rand = new Random(DateTime.Now.Microsecond);
        foreach (var dbType in new[]
                 {
                     //DatabaseType.Sqlite, 
                     // DatabaseType.Postgres,
                     // DatabaseType.SqlServer,
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
            for (int i = 0; i < 1_000_000; i++)
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
            
            var total = db.LibraryFileManager.GetTotal().Result;
            Assert.AreEqual(files.Count, total);
        }
    }
    
    

    [TestMethod]
    public void StatusTests()
    {
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
            
            var dam = new DatabaseAccessManager(Logger, dbType, connectionString);

            var testHelper = new TestDataHelper(Logger, dam);
            
            var expected = testHelper.BulkInsert(1_000);

            int max = 100;
            LibraryFilterSystemInfo sysInfo = new()
            {
                Executors = new (),
                AllLibraries = expected.Libraries.ToDictionary(x => x.Uid, x => x),
                LicensedForProcessingOrder = true,
            };


            var statusList = dam.LibraryFileManager.GetStatus(sysInfo.AllLibraries.Values.ToList()).Result.ToDictionary(x => x.Status, x => x.Count);
            Assert.AreEqual(expected.Disabled.Count, statusList[FileStatus.Disabled]);
            Assert.AreEqual(expected.OutOfSchedule.Count, statusList[FileStatus.OutOfSchedule]);
            Assert.AreEqual(expected.Held.Count, statusList[FileStatus.OnHold]);
            Assert.AreEqual(expected.Active.Count, statusList[FileStatus.Unprocessed]);

            DateTime start = DateTime.Now;
            var actualDisabled  = dam.LibraryFileManager.GetAll(new LibraryFileFilter()
            {
                Status = FileStatus.Disabled,
                Rows = max,
                SysInfo = sysInfo
            }).Result;
            Logger.ILog($"Disabled time taken [{statusList[FileStatus.Disabled]}]: " + (DateTime.Now.Subtract(start)));
            Assert.AreEqual(Math.Min(max, expected.Disabled.Count), actualDisabled.Count);
            
            start = DateTime.Now;
            var actualOutOfSchedule = dam.LibraryFileManager.GetAll(new LibraryFileFilter()
            {
                Status = FileStatus.OutOfSchedule,
                Rows = max,
                SysInfo = sysInfo
            }).Result;
            Logger.ILog($"Out of schedule time taken [{statusList[FileStatus.OutOfSchedule]}]: " + (DateTime.Now.Subtract(start)));
            Assert.AreEqual(Math.Min(max, expected.OutOfSchedule.Count), actualOutOfSchedule.Count);
            
            
            start = DateTime.Now;
            var actualOnHold = dam.LibraryFileManager.GetAll(new LibraryFileFilter()
            {
                Status = FileStatus.OnHold,
                Rows = max,
                SysInfo = sysInfo
            }).Result;
            Logger.ILog($"On Hold time taken [{statusList[FileStatus.OnHold]}]: " + (DateTime.Now.Subtract(start)));
            Assert.AreEqual(Math.Min(max, expected.Held.Count), actualOnHold.Count);
            
            
            start = DateTime.Now;
            var actualUnprocessed  = dam.LibraryFileManager.GetAll(new LibraryFileFilter()
            {
                Status = FileStatus.Unprocessed,
                Rows = max,
                SysInfo = sysInfo
            }).Result;
            Logger.ILog($"Unprocessed time taken [{statusList[FileStatus.Unprocessed]}]: " + (DateTime.Now.Subtract(start)));
            Assert.AreEqual(Math.Min(max, expected.Active.Count), actualUnprocessed.Count);
            
        }
    }
    
    
    
    
    [TestMethod]
    public void UpgradeTest()
    {
        var rand = new Random(DateTime.Now.Microsecond);
        DateTime nowDate = DateTime.Now;
        DateTime utcDate = nowDate.ToUniversalTime();
        foreach (var dbType in new[]
                 {
                     //DatabaseType.Sqlite, 
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
            
            
            
            var library = new Library()
            {
                Uid = Guid.NewGuid(),
                Name = "TestLibrary",
                Enabled = true,
                DateCreated = nowDate,
                DateModified = nowDate,
                Description = "this is a test description",
                Path = "/a/b/c",
                Scan = true,
                HoldMinutes = 30,
                LastScanned = nowDate
            };
            var dboLibrary = FileFlowsObjectManager.ConvertToDbObject(library);
            var dbConnector = GetConnector(dbType, connectionString);
            var db = dbConnector.GetDb().Result;
            // manually insert so we control the old datetime format
            string sql = $"insert into DbObject (Uid, Name, Type, DateCreated, DateModified, Data) values (" +
                         $"'{dboLibrary.Uid}', '{dboLibrary.Name}', '{dboLibrary.Type}', " +
                         $"'{dboLibrary.DateCreated:yyyy-MM-ddTHH:mm:ss.fff}', " +
                         $"'{dboLibrary.DateModified:yyyy-MM-ddTHH:mm:ss.fff}', " +
                         $"{SqlHelper.Escape(dboLibrary.Data)})";
            db.Db.Execute(sql);

            sql = $"insert into RevisionedObject (Uid, RevisionName, RevisionType, RevisionUid, RevisionCreated, RevisionDate, RevisionData) values (" +
                         $"'{Guid.NewGuid()}', 'test', 'test', '{Guid.NewGuid()}', " +
                         $"'{nowDate:yyyy-MM-ddTHH:mm:ss.fff}', " +
                         $"'{nowDate:yyyy-MM-ddTHH:mm:ss.fff}', " +
                         $"'')";
            db.Db.Execute(sql);

            sql = @$"insert into LibraryFile(Uid,Name,RelativePath,Status,ProcessingOrder,Fingerprint,FinalFingerprint,IsDirectory,Flags,OriginalSize,FinalSize,LibraryUid,LibraryName,
                        FlowUid,FlowName,DuplicateUid,DuplicateName,NodeUid,NodeName,WorkerUid,ProcessOnNodeUid,OutputPath,FailureReason,NoLongerExistsAfterProcessing,OriginalMetadata,FinalMetadata,ExecutedNodes,
                        DateCreated,DateModified,CreationTime,LastWriteTime,HoldUntil,ProcessingStarted,ProcessingEnded)
                values ('{Guid.NewGuid()}', 'name', 'relativepath', 0, 0, 'fingerprint', 'finalfinger', 0, 0, 123,456, '', 'libname', 
                        '', 'flow-name', '', 'dupname', '', 'node-name', '', '', 'output', '', 0, '', '', '',
                    '{nowDate:yyyy-MM-ddTHH:mm:ss.fff}',
                    '{nowDate:yyyy-MM-ddTHH:mm:ss.fff}',
                    '{nowDate:yyyy-MM-ddTHH:mm:ss.fff}',
                    '{nowDate:yyyy-MM-ddTHH:mm:ss.fff}',
                    '{nowDate:yyyy-MM-ddTHH:mm:ss.fff}',
                    '{nowDate:yyyy-MM-ddTHH:mm:ss.fff}',
                    '{nowDate:yyyy-MM-ddTHH:mm:ss.fff}'
                    )";
            db.Db.Execute(sql);
            
            var dam = new DatabaseAccessManager(Logger, dbType, connectionString);

            var dboOriginal = dam.DbObjectManager.GetAll().Result;
            var roOriginal = dam.DbRevisionManager.GetAll().Result;
            var lfOriginal = dam.LibraryFileManager.GetAll().Result;

            //  now upgrade the data
            var upgradeResult = new Upgrade_24_03_2().Run(Logger, dbType, connectionString);
            if(upgradeResult.Failed(out string error))
                Assert.Fail(error);

            var dboUpdated = dam.DbObjectManager.GetAll().Result;
            var roUpdated = dam.DbRevisionManager.GetAll().Result;
            var lfUpdated = dam.LibraryFileManager.GetAll().Result;

            foreach (var dbo in dboOriginal)
            {
                Assert.AreEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), dbo.DateCreated.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), dbo.DateModified.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), dbo.DateCreated.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), dbo.DateModified.ToString("yyyy-MM-dd HH:mm"));
            }
            foreach (var dbo in dboUpdated)
            {
                Assert.AreNotEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), dbo.DateCreated.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), dbo.DateModified.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), dbo.DateCreated.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), dbo.DateModified.ToString("yyyy-MM-dd HH:mm"));
            }
            
            
            foreach (var _ro in roOriginal)
            {
                Assert.AreEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), _ro.RevisionDate.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), _ro.RevisionCreated.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), _ro.RevisionDate.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), _ro.RevisionCreated.ToString("yyyy-MM-dd HH:mm"));
            }
            foreach (var _ro in roUpdated)
            {
                Assert.AreNotEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), _ro.RevisionDate.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), _ro.RevisionCreated.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), _ro.RevisionDate.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), _ro.RevisionCreated.ToString("yyyy-MM-dd HH:mm"));
            }
            
            
            foreach (var lf in lfOriginal)
            {
                Assert.AreEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.DateCreated.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.DateModified.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.CreationTime.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.ProcessingEnded.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.ProcessingStarted.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.HoldUntil.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.DateCreated.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.DateModified.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.CreationTime.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.ProcessingEnded.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.ProcessingStarted.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.HoldUntil.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
            }
            foreach (var lf in lfUpdated)
            {
                Assert.AreNotEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.DateCreated.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.DateModified.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.CreationTime.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.ProcessingEnded.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.ProcessingStarted.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.HoldUntil.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreNotEqual(nowDate.ToString("yyyy-MM-dd HH:mm"), lf.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.DateCreated.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.DateModified.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.CreationTime.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.ProcessingEnded.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.ProcessingStarted.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.HoldUntil.ToString("yyyy-MM-dd HH:mm"));
                Assert.AreEqual(utcDate.ToString("yyyy-MM-dd HH:mm"), lf.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
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