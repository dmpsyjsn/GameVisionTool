using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.CrossCuttingConcerns;
using GameVisionTool.Logic.DataStores.LiteDb;
using LiteDB;
using SimpleInjector;

namespace GameVisionTool.Web.Razor.CompositionRoot;

internal static class ConfigureServices
{
    internal static Container RegisterServices(this Container container, IConfiguration config)
    {
        container.Register(typeof(IDataStore<>), typeof(LiteDbAsyncDataStore<>), Lifestyle.Scoped);
        container.Register<IAsyncUnitOfWorkDataStore, LiteDbAsyncUnitOfWorkDataStore>(Lifestyle.Scoped);

        if (config["DatabaseType"]!.Equals("LiteDb"))
        {
            var dbPath = config["DatabasePath"];
            var dbName = config["DatabaseName"]!;

            var path = string.IsNullOrEmpty(dbPath) ? AppContext.BaseDirectory : dbPath;
            var filePath = Path.Combine(path, dbName);

            var connectionString = $"Filename={filePath}; Connection=Shared";
            container.Register<ILiteDatabase>(() => new LiteDatabase(connectionString), Lifestyle.Scoped);
        }

        return container;
    }
}