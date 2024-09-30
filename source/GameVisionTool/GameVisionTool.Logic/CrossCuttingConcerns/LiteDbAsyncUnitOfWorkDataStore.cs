using GameVisionTool.Common.Domain.Services;
using LiteDB;

namespace GameVisionTool.Logic.CrossCuttingConcerns;

public class LiteDbAsyncUnitOfWorkDataStore(ILiteDatabase liteDb) : IAsyncUnitOfWorkDataStore
{
    public Task SaveChangesAsync()
    {
        liteDb.Commit();
        return Task.CompletedTask;
    }
}