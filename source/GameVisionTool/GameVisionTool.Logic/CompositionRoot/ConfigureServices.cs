using GameVisionTool.Integration.LlamaSharp;
using GameVisionTool.Logic.Common;
using GameVisionTool.Persistence.LiteDb;
using Microsoft.Extensions.DependencyInjection;

namespace GameVisionTool.Logic.CompositionRoot;

internal static class ConfigureServices
{
    internal static IServiceCollection RegisterOtherServices(this IServiceCollection services)
    {
        services.AddLiteDbPersistence();
        services.AddLlama();
        services.AddSingleton<IDataStoreProcessor, DynamicDataStoreProcessor>();

        return services;
    }
}
