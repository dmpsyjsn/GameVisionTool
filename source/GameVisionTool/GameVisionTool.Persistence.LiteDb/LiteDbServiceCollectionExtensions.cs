using GameVisionTool.Common.Domain;
using GameVisionTool.Common.Domain.Services;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace GameVisionTool.Persistence.LiteDb;

public static class LiteDbServiceCollectionExtensions
{
    public static IServiceCollection AddLiteDbPersistence(this IServiceCollection services)
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, GameVisionToolGlobalConstants.DatabaseFileName);

        var connectionString = $"Filename={filePath};connection=direct";

        services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(connectionString));
        services.AddSingleton(typeof(IDataStore<,>), typeof(LiteDbDataStore<,>));
        services.AddSingleton<IUnitOfWorkDataStore, LiteDbUnitOfWorkDataStore>();
        services.AddSingleton<IAsyncUnitOfWorkDataStore, LiteDbAsyncUnitOfWorkDataStore>();

        return services;
    }
}
