using FileFlows.Shared.Models;

namespace FileFlows.Server.Database.Layers;

internal class MySqlLayer : IDatabaseLayer
{
    public DbObject DbObjectGet(Guid uid)
    {
        throw new NotImplementedException();
    }

    public void DbObjectUpdate(DbObject dbObject)
    {
        throw new NotImplementedException();
    }

    public void DbObjectDelete(Guid uid)
    {
        throw new NotImplementedException();
    }

    public List<DbStatistic> DbStatisticGetAll()
    {
        throw new NotImplementedException();
    }

    public void DbStatisticInsert(DbStatistic stat)
    {
        throw new NotImplementedException();
    }

    public void RevisionedObjectInsert(RevisionedObject revision)
    {
        throw new NotImplementedException();
    }

    public void DbLogMessageInsert(DbLogMessage message)
    {
        throw new NotImplementedException();
    }

    public List<DbLogMessage> DbLogMessageGet()
    {
        throw new NotImplementedException();
    }

    public LibraryFile LibraryFileGet(Guid uid)
    {
        throw new NotImplementedException();
    }

    public void LibraryFileDelete(Guid uid)
    {
        throw new NotImplementedException();
    }
}