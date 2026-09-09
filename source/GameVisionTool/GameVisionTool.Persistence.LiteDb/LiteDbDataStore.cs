using System.Linq.Expressions;
using GameVisionTool.Common.Domain.Services;
using LiteDB;

namespace GameVisionTool.Persistence.LiteDb;

public class LiteDbDataStore<TEntity, TKey>(ILiteDatabase liteDb) : IDataStore<TEntity, TKey> where TEntity : Entity<TKey> where TKey : IEquatable<TKey>
{
    public TEntity GetById(TKey id)
    {
        var bsonId = new BsonValue(id);
        var entity = liteDb.GetCollection<TEntity>().FindById(bsonId);
        if (entity == null)
        {
            throw new InvalidOperationException($"Entity with id {id} does not exist.");
        }
        return entity;
    }

    public TEntity? GetByIdOrDefault(TKey id)
    {
        var bsonId = new BsonValue(id);
        return liteDb.GetCollection<TEntity>().FindById(bsonId);
    }

    public IEnumerable<TEntity> GetByIds(IEnumerable<TKey> ids)
    {
        return liteDb.GetCollection<TEntity>().Find(x => ids.Contains(x.Id));
    }

    public IEnumerable<TEntity> GetAll()
    {
        return liteDb.GetCollection<TEntity>().FindAll();
    }

    public IEnumerable<TEntity> Get(Expression<Func<TEntity, bool>> wherePredicate)
    {
        return liteDb.GetCollection<TEntity>().Find(wherePredicate);
    }

    public TKey Store(TEntity entity)
    {
        var collection = liteDb.GetCollection<TEntity>();
        entity.CreatedOn ??= DateTime.UtcNow;
        entity.ModifiedOn = DateTime.UtcNow;
        collection.Insert(entity);
        return entity.Id;
    }

    public void Remove(TKey id)
    {
        var collection = liteDb.GetCollection<TEntity>();
        var bsonId = new BsonValue(id);
        if (!collection.Delete(bsonId)) { throw new InvalidOperationException($"Entity with id {id} does not exist."); }
    }

    public TKey Update(TEntity entity)
    {
        var collection = liteDb.GetCollection<TEntity>();

        var bsonId = new BsonValue(entity.Id);

        if (collection.FindById(bsonId) == null)
        {
            throw new InvalidOperationException($"Entity with id {entity.Id} does not exist.");
        }

        entity.ModifiedOn = DateTime.UtcNow;
        collection.Update(entity);

        return entity.Id;
    }

    public TKey Upsert(TEntity entity)
    {
        var collection = liteDb.GetCollection<TEntity>();

        var bsonId = new BsonValue(entity.Id);

        if (collection.FindById(bsonId) == null)
        {
            entity.CreatedOn ??= DateTime.UtcNow;
            entity.ModifiedOn = DateTime.UtcNow;
            collection.Insert(entity);
            return entity.Id;
        }

        entity.ModifiedOn = DateTime.UtcNow;

        collection.Update(entity);

        return entity.Id;
    }
}