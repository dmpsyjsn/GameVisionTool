using GameVisionTool.Common.Domain.Services;
using LiteDB;

namespace GameVisionTool.Logic.DataStores.LiteDb;

public class LiteDbAsyncDataStore<TEntity>(ILiteDatabase liteDb) : IDataStore<TEntity> where TEntity : Entity
{
    public Task<TEntity?> GetSingleOrDefault()
    {
        return Task.FromResult(liteDb.GetCollection<TEntity>().FindAll().SingleOrDefault());
    }
    public Task<TEntity> GetById(int id)
    {
        return Task.FromResult(liteDb.GetCollection<TEntity>().FindById(id));
    }

    public Task<IEnumerable<TEntity>> GetByIds(IEnumerable<int> ids)
    {
        return Task.FromResult(liteDb.GetCollection<TEntity>().Find(x => ids.Contains(x.Id)));
    }

    public Task<int> Store(TEntity entity)
    {
        var collection = liteDb.GetCollection<TEntity>();
        collection.Insert(entity);
        return Task.FromResult(entity.Id);
    }

    public Task Remove(int id)
    {
        var collection = liteDb.GetCollection<TEntity>();
        return Task.FromResult(collection.Delete(id));
    }

    public Task<int> Update(TEntity entity)
    {
        var collection = liteDb.GetCollection<TEntity>();
        collection.Update(entity);

        return Task.FromResult(entity.Id);
    }

    public Task<int> Upsert(TEntity entity)
    {
        var collection = liteDb.GetCollection<TEntity>();
        collection.Upsert(entity);

        return Task.FromResult(entity.Id);
    }
}