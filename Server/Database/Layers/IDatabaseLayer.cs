using FileFlows.Shared.Models;

namespace FileFlows.Server.Database.Layers;

internal interface IDatabaseLayer
{
    DbObject DbObjectGet(Guid uid);
    void DbObjectUpdate(DbObject dbObject);
    void DbObjectDelete(Guid uid);

    List<DbStatistic> DbStatisticGetAll();
    void DbStatisticInsert(DbStatistic stat);

    void RevisionedObjectInsert(RevisionedObject revision);

    void DbLogMessageInsert(DbLogMessage message);
    List<DbLogMessage> DbLogMessageGet();

    LibraryFile LibraryFileGet(Guid uid);
    void LibraryFileDelete(Guid uid);
}