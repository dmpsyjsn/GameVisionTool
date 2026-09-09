namespace GameVisionTool.Common.Domain.Services;

public interface IAsyncUnitOfWorkDataStore
{
    Task SaveChangesAsync();
}

public interface IUnitOfWorkDataStore
{
    void SaveChanges();
}
