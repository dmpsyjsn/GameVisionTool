using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.Common;

public interface IDataStoreProcessor
{
    public IDataStore<T, TKey> Process<T, TKey>() where T : Entity<TKey> where TKey : IEquatable<TKey>;
}


public class DynamicDataStoreProcessor(IServiceProvider serviceProvider) : IDataStoreProcessor
{
    public IDataStore<T, TKey> Process<T, TKey>() where T : Entity<TKey> where TKey : IEquatable<TKey>
    {
        return (IDataStore<T, TKey>) serviceProvider.GetService(typeof(IDataStore<T, TKey>))!;
    }
}