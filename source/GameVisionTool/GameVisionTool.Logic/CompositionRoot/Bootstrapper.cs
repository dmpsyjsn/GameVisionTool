using Microsoft.Extensions.DependencyInjection;

namespace GameVisionTool.Logic.CompositionRoot;

public static class Bootstrapper
{
    public static IServiceCollection Bootstrap(this IServiceCollection services, string databaseDirectory)
    {
        return services
            .RegisterValidators()
            .RegisterCommandHandlers()
            .RegisterQueryHandlers()
            .RegisterOtherServices(databaseDirectory);
    }
}
