using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Validation;
using GameVisionTool.Integration.LlamaSharp;
using GameVisionTool.Logic.Application.CommandHandlers.MainSettings;
using GameVisionTool.Logic.Common;
using GameVisionTool.Logic.CrossCuttingConcerns;
using GameVisionTool.Persistence.LiteDb;
using Microsoft.Extensions.DependencyInjection;

namespace GameVisionTool.Logic.CompositionRoot;

internal static class ConfigureCommandHandlers
{
    public static IServiceCollection RegisterCommandHandlers(this IServiceCollection collection)
    {
        var assembly = new[] { typeof(MainSettingHandlers).Assembly };

        // ---------------------------------------------------------------------------------
        // Synchronous pipeline
        // ---------------------------------------------------------------------------------

        collection.Scan(x => x.FromAssemblies(assembly)
            .AddClasses(y => y.AssignableTo(typeof(ICommandHandler<>)))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        collection.Scan(x => x.FromAssemblies(assembly)
            .AddClasses(y => y.AssignableTo(typeof(ICommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        // Order matters: last registered is outermost
        collection.TryDecorate(typeof(ICommandHandler<>), typeof(LiteDbUnitOfWorkCommandHandlerDecorator<>));
        collection.TryDecorate(typeof(ICommandHandler<,>), typeof(LiteDbUnitOfWorkCommandHandlerDecorator<,>));

        collection.TryDecorate(typeof(ICommandHandler<>), typeof(LiteDbTransactionCommandHandlerDecorator<>));
        collection.TryDecorate(typeof(ICommandHandler<,>), typeof(LiteDbTransactionCommandHandlerDecorator<,>));

        collection.TryDecorate(typeof(ICommandHandler<>), typeof(ValidationCommandHandlerDecorator<>));
        collection.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));

        collection.TryDecorate(typeof(ICommandHandler<>), typeof(LoggingCommandDecorator<>));
        collection.TryDecorate(typeof(ICommandHandler<,>), typeof(LoggingCommandDecorator<,>));

        // ---------------------------------------------------------------------------------
        // Asynchronous pipeline
        // ---------------------------------------------------------------------------------

        collection.Scan(x => x.FromAssemblies(assembly)
            .AddClasses(y => y.AssignableTo(typeof(IAsyncCommandHandler<>)))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        collection.Scan(x => x.FromAssemblies(assembly)
            .AddClasses(y => y.AssignableTo(typeof(IAsyncCommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        collection.TryDecorate(typeof(IAsyncCommandHandler<>), typeof(LiteDbUnitOfWorkCommandHandlerDecoratorAsync<>));
        collection.TryDecorate(typeof(IAsyncCommandHandler<,>), typeof(LiteDbUnitOfWorkCommandHandlerDecoratorAsync<,>));

        collection.TryDecorate(typeof(IAsyncCommandHandler<>), typeof(LiteDbTransactionCommandHandlerDecoratorAsync<>));
        collection.TryDecorate(typeof(IAsyncCommandHandler<,>), typeof(LiteDbTransactionCommandHandlerDecoratorAsync<,>));

        collection.TryDecorate(typeof(IAsyncCommandHandler<>), typeof(ValidationCommandHandlerDecoratorAsync<>));
        collection.TryDecorate(typeof(IAsyncCommandHandler<,>), typeof(ValidationCommandHandlerDecoratorAsync<,>));

        collection.TryDecorate(typeof(IAsyncCommandHandler<>), typeof(LoggingCommandDecoratorAsync<>));
        collection.TryDecorate(typeof(IAsyncCommandHandler<,>), typeof(LoggingCommandDecoratorAsync<,>));

        // Command processors
        collection.AddSingleton<IProcessCommand, DynamicProcessCommand>();
        collection.AddSingleton<IProcessCommandAsync, DynamicProcessCommandAsync>();

        return collection;
    }
}
