using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Validation;
using GameVisionTool.Messages.Commands.OpenAi;
using Microsoft.Extensions.Configuration;

namespace GameVisionTool.Logic;

using FluentValidation;
using CrossCuttingConcerns;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameVisionTool.Logic.Common;


// This class allows registering all types that are defined in the business layer, and are shared across
// all applications that use this layer (WCF and Web API). For simplicity, this class is placed inside
// this assembly, but this does couple the business layer assembly to the used container. If this is a 
// concern, create a specific BusinessLayer.Bootstrap project with this class.
public static class BusinessLayerBootstrapper
{
    private static readonly Assembly[] ContractAssemblies = new[] { typeof(UpsertOpenAiSettings).Assembly };
    private static readonly Assembly[] BusinessLayerAssemblies = new[] { Assembly.GetExecutingAssembly() };

    public static Container RegisterValidators(this Container container)
    {
        //register all the validators
        container.Register(typeof(IValidator<>), BusinessLayerAssemblies);

        //for objects that don't have validators use the NullValidator (always returns true for validation)
        container.RegisterConditional(typeof(IValidator<>), typeof(NullValidator<>), c => !c.Handled);

        return container;
    }

    public static Container RegisterCommandHandlers(this Container container, IConfiguration config)
    {
        container.Register(typeof(IAsyncCommandHandler<>), BusinessLayerAssemblies);
        container.Register(typeof(IAsyncCommandHandler<,>), BusinessLayerAssemblies);

        container.RegisterDecorator(typeof(IAsyncCommandHandler<>), typeof(ValidationCommandHandlerDecorator<>));
        container.RegisterDecorator(typeof(IAsyncCommandHandler<,>), typeof(ValidationCommandHandlerDecorator<,>));

        if (config["DatabaseType"]!.Equals("LiteDb"))
        {
            //register the Unit of Work decorator
            container.RegisterDecorator(typeof(IAsyncCommandHandler<>), typeof(LiteDbUnitOfWorkCommandHandlerDecorator<>));
            container.RegisterDecorator(typeof(IAsyncCommandHandler<,>), typeof(LiteDbUnitOfWorkCommandHandlerDecorator<,>));

            //register the Transaction Scope decorator
            container.RegisterDecorator(typeof(IAsyncCommandHandler<>), typeof(LiteDbTransactionCommandHandlerDecorator<>));
            container.RegisterDecorator(typeof(IAsyncCommandHandler<,>), typeof(LiteDbTransactionCommandHandlerDecorator<,>));
        }
        // ToDo: SQLite database decorators & implementations

        //register the logging decorator
        container.RegisterDecorator(typeof(IAsyncCommandHandler<>), typeof(LoggingCommandDecorator<>));
        container.RegisterDecorator(typeof(IAsyncCommandHandler<,>), typeof(LoggingCommandDecorator<,>));

        container.RegisterDecorator(typeof(IAsyncCommandHandler<>), typeof(AuthorizationCommandHandlerDecorator<>));
        container.RegisterDecorator(typeof(IAsyncCommandHandler<,>), typeof(AuthorizationCommandHandlerDecorator<,>));

        // Register Command processor
        container.RegisterSingleton<IProcessCommandAsync, DynamicProcessCommandAsync>();

        return container;
    }

    public static Container RegisterQueryHandlers(this Container container)
    {
        container.Register(typeof(IAsyncQueryHandler<,>), BusinessLayerAssemblies);
        
        container.RegisterDecorator(typeof(IAsyncQueryHandler<,>), typeof(ValidationQueryHandlerDecorator<,>));
        
        //register the logging decorator
        container.RegisterDecorator(typeof(IAsyncQueryHandler<,>), typeof(LoggingQueryDecorator<,>));
        container.RegisterDecorator(typeof(IAsyncQueryHandler<,>), typeof(AuthorizationQueryHandlerDecorator<,>));

        // Register query processor
        container.RegisterSingleton<IProcessQueryAsync, DynamicProcessQueryAsync>();

        return container;
    }

       public static IEnumerable<(Type, Type)> GetCommandTypes() =>
           from assembly in ContractAssemblies
           from type in assembly.GetExportedTypes()
           where !type.IsAbstract
           let interfaces = type.GetInterfaces()
           where interfaces.Length > 0
           where interfaces[0] == typeof(ICommand)
           select (type, type);

    public static IEnumerable<(Type, Type)> GetCommandResultTypes() =>
        from assembly in ContractAssemblies
        from type in assembly.GetExportedTypes()
        where !type.IsAbstract
        let interfaces = type.GetInterfaces()
        where interfaces.Length > 0
        where interfaces[0].IsGenericType && interfaces[0].GetGenericTypeDefinition() == typeof(ICommand<>)
        select (type, GetResultTypeForCommand(interfaces[0]));

    private static Type GetResultTypeForCommand(Type type)
    {
        var resultType = type.GetGenericArguments().First();
        return resultType;
    }

    public static Type CreateQueryHandlerType(Type queryType) =>
        typeof(IAsyncQueryHandler<,>).MakeGenericType(queryType, DetermineResultTypes(queryType).Single());

    public static IEnumerable<(Type QueryType, Type ResultType)> GetQueryTypes() =>
        from assembly in ContractAssemblies
        from type in assembly.GetExportedTypes()
        where IsQuery(type)
        select (type, DetermineResultTypes(type).Single());

    public static Type GetQueryResultType(Type queryType) => DetermineResultTypes(queryType).Single();

    private static bool IsQuery(Type type) => DetermineResultTypes(type).Any();

    private static IEnumerable<Type> DetermineResultTypes(Type type) =>
        from interfaceType in type.GetInterfaces()
        where interfaceType.IsGenericType
        where interfaceType.GetGenericTypeDefinition() == typeof(IQuery<>)
        select interfaceType.GetGenericArguments()[0];
}