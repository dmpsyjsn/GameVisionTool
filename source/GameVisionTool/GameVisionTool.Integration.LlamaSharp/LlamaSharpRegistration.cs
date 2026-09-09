using Microsoft.Extensions.DependencyInjection;

namespace GameVisionTool.Integration.LlamaSharp;

public static class LlamaSharpRegistration
{
    public static IServiceCollection AddLlama(this IServiceCollection services)
    {
        services.AddSingleton<LlamaModelProvider>();

        return services;
    }
}