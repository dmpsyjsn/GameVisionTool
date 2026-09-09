using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Integration.LlamaSharp;
using GameVisionTool.Logic.Application.QueryHandlers.MainSettings;
using GameVisionTool.Logic.Common;
using GameVisionTool.Logic.CrossCuttingConcerns;
using Microsoft.Extensions.DependencyInjection;

namespace GameVisionTool.Logic.CompositionRoot;

internal static class ConfigureQueryHandlers
{
    public static IServiceCollection RegisterQueryHandlers(this IServiceCollection collection)
    {
        var assembly = new[] { typeof(MainSettingQueryHandlers).Assembly };

        // ---------------------------------------------------------------------------------
        // Synchronous pipeline
        // ---------------------------------------------------------------------------------

        collection.Scan(x => x.FromAssemblies(assembly)
            .AddClasses(y => y.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        // Queries carry no input to validate, so there is deliberately no validation decorator -
        // logging is the whole chain.
        collection.TryDecorate(typeof(IQueryHandler<,>), typeof(LlamaModelFileDecorator<,>));
        collection.TryDecorate(typeof(IQueryHandler<,>), typeof(LoggingQueryDecorator<,>));

        // ---------------------------------------------------------------------------------
        // Asynchronous pipeline
        // ---------------------------------------------------------------------------------

        collection.Scan(x => x.FromAssemblies(assembly)
            .AddClasses(y => y.AssignableTo(typeof(IAsyncQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());


        collection.TryDecorate(typeof(IAsyncQueryHandler<,>), typeof(LlamaModelFileDecoratorAsync<,>));
        collection.TryDecorate(typeof(IAsyncQueryHandler<,>), typeof(LoggingQueryDecoratorAsync<,>));

        // Query processors
        collection.AddSingleton<IProcessQuery, DynamicProcessQuery>();
        collection.AddSingleton<IProcessQueryAsync, DynamicProcessQueryAsync>();

        return collection;
    }
}
