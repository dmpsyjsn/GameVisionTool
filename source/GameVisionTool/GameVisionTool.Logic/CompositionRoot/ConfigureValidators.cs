using FluentValidation;
using GameVisionTool.Common.Validation;
using GameVisionTool.Logic.Application.CommandHandlers.MainSettings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameVisionTool.Logic.CompositionRoot;

internal static class ConfigureValidators
{
    public static IServiceCollection RegisterValidators(this IServiceCollection collection)
    {
        var assembly = new[] { typeof(MainSettingHandlers).Assembly };

        // register all the validators
        collection.Scan(x => x.FromAssemblies(assembly)
            .AddClasses(y => y.AssignableTo(typeof(IValidator<>)))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        //for objects that don't have validators use the NullValidator (always returns true for validation)
        collection.TryAddSingleton(typeof(IValidator<>), typeof(NullValidator<>));

        return collection;
    }
}