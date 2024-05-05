using System.Collections.Concurrent;
using FileFlows.Plugin;
using FileFlows.Shared;

namespace FileFlows.DataLayer.DatabaseConnectors;


    /// <summary>
    /// Represents a pool of NPoco.Database connections for read operations, with fair semaphore-based access control.
    /// </summary>
    public class DatabaseConnectionPool
    {
        private readonly ConcurrentQueue<DatabaseConnection> pool = new ConcurrentQueue<DatabaseConnection>();
        private readonly FairSemaphore semaphore;
        private readonly Func<DatabaseConnection> createConnectionFunc;
        private readonly TimeSpan connectionLifetime;
        private readonly object disposalLock = new object();

        /// <summary>
        /// Gets the number of current open connections
        /// </summary>
        public int OpenedConnections => semaphore.CurrentInUse;

        /// <summary>
        /// Initializes a new instance of the DatabaseConnectionPool class with the specified function to create database connections, pool size, and connection lifetime.
        /// </summary>
        /// <param name="createConnectionFunc">A function delegate that creates a new NPoco.Database connection.</param>
        /// <param name="poolSize">The maximum number of connections to be maintained in the pool.</param>
        /// <param name="connectionLifetime">The time span after which a connection should be disposed if it remains unused.</param>
        public DatabaseConnectionPool(Func<DatabaseConnection> createConnectionFunc, int poolSize, TimeSpan? connectionLifetime = null)
        {
            this.createConnectionFunc = createConnectionFunc ?? throw new ArgumentNullException(nameof(createConnectionFunc));
            semaphore = new FairSemaphore(poolSize);
            this.connectionLifetime = connectionLifetime ?? TimeSpan.Zero; // Default to zero if not provided
        }
        
        private void InitializeConnectionTimer(DatabaseConnection connection)
        {
            if (connectionLifetime > TimeSpan.Zero)
            {
                Timer timer = new Timer(_ =>
                {
                    lock (disposalLock)
                    {
                        if (!connection.IsDisposed)
                        {
                            connection.Dispose();
                        }
                    }
                }, null, connectionLifetime, Timeout.InfiniteTimeSpan);
            }
        }

        /// <summary>
        /// Asynchronously acquires a database connection from the pool.
        /// </summary>
        /// <returns>A task representing the asynchronous operation of acquiring a database connection.</returns>
        public async Task<DatabaseConnection> AcquireConnectionAsync()
        {
            await semaphore.WaitAsync();

            DatabaseConnection? connection;
            lock (disposalLock)
            {
                while (pool.TryDequeue(out connection))
                {
                    if (!connection.IsDisposed)
                    {
                        InitializeConnectionTimer(connection);
                        return connection;
                    }
                }
                pool.Clear();
            }

            // Pool is empty or all connections are disposed, create a new connection
            connection = createConnectionFunc();
            connection.OnDispose += (sender, args) =>
            {
                ReleaseConnection(connection);
            };
            InitializeConnectionTimer(connection);
            return connection;
        }

        /// <summary>
        /// Releases a database connection back to the pool for reuse.
        /// </summary>
        /// <param name="connection">The database connection to be released.</param>
        public void ReleaseConnection(DatabaseConnection connection)
        {
            pool.Enqueue(connection);
            semaphore.Release();
        }
    }