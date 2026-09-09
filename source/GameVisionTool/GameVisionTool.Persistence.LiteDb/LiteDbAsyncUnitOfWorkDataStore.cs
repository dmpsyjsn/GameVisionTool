using GameVisionTool.Common.Domain.Services;
using LiteDB;

namespace GameVisionTool.Persistence.LiteDb;

public class LiteDbAsyncUnitOfWorkDataStore(ILiteDatabase liteDb) : IAsyncUnitOfWorkDataStore
{
    // LiteDB has no async commit; this satisfies the async contract without pretending to be I/O bound.
    public Task SaveChangesAsync()
    {
        liteDb.Commit();
        return Task.CompletedTask;
    }
}

public class LiteDbUnitOfWorkDataStore(ILiteDatabase liteDb) : IUnitOfWorkDataStore
{
    public void SaveChanges()
    {
        liteDb.Commit();
    }
}
