namespace GameVisionTool.Common.Domain.Services;

public interface IDataStore<TEntity> where TEntity : Entity
{
    public Task<TEntity?> GetSingleOrDefault();
    public Task<TEntity> GetById(int id);
    public Task<IEnumerable<TEntity>> GetByIds(IEnumerable<int> ids);
    public Task<int> Store(TEntity entity);
    public Task Remove(int id);
    public Task<int> Update(TEntity entity);
    public Task<int> Upsert(TEntity entity);
}
