using System.Linq.Expressions;

namespace GameVisionTool.Common.Domain.Services;

public interface IAsyncDataStore<TEntity, TKey> where TEntity : Entity<TKey> where TKey : IEquatable<TKey>
{
    public Task<TEntity> GetById(TKey id);
    public Task<TEntity?> GetByIdOrDefault(TKey id);
    public Task<IEnumerable<TEntity>> GetByIds(IEnumerable<TKey> ids);
    public Task<IEnumerable<TEntity>> GetAll();
    public Task<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>> wherePredicate);
    public Task<TKey> Store(TEntity entity);
    public Task Remove(TKey id);
    public Task<TKey> Update(TEntity entity);
    public Task<TKey> Upsert(TEntity entity);
}

public interface IDataStore<TEntity, TKey> where TEntity : Entity<TKey> where TKey : IEquatable<TKey>
{
    public TEntity GetById(TKey id);
    public TEntity? GetByIdOrDefault(TKey id);
    public IEnumerable<TEntity> GetByIds(IEnumerable<TKey> ids);
    public IEnumerable<TEntity> GetAll();
    public IEnumerable<TEntity> Get(Expression<Func<TEntity, bool>> wherePredicate);
    public TKey Store(TEntity entity);
    public void Remove(TKey id);
    public TKey Update(TEntity entity);
    public TKey Upsert(TEntity entity);
}
