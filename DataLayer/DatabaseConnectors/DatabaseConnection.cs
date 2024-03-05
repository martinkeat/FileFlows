namespace FileFlows.DataLayer.DatabaseConnectors;

public class DatabaseConnection : IDisposable
{
    public NPoco.Database Db { get; private set; }
    private bool Disposable;
    public bool IsDisposed { get;private set; }

    public EventHandler OnDispose;
    
    public DatabaseConnection(NPoco.Database db, bool disposable)
    {
        Db = db;
        Disposable = disposable;
    }

    // public static implicit operator NPoco.Database(DatabaseConnection value)
    //     => value.Db;

    public void Dispose()
    {
        IsDisposed = true;
        if(Disposable)
            Db.Dispose();
        OnDispose?.Invoke(this, new EventArgs());
    }
}