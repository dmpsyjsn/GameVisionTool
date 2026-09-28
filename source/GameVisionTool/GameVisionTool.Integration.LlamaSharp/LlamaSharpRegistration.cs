using GameVisionTool.Integration.LlamaSharp.Agents;
using Microsoft.Extensions.DependencyInjection;

namespace GameVisionTool.Integration.LlamaSharp;

public static class LlamaSharpRegistration
{
    public static IServiceCollection AddLlama(this IServiceCollection services)
    {
        services.AddSingleton<LlamaModelProvider>();

        // Forwarded to the registration above rather than registered as its own implementation type,
        // which would build a second provider with a second set of weights: the warm-up path would then
        // load a model that the generation path never sees, doubling VRAM and warming nothing. The
        // provider is the one place weights live, so there has to be exactly one of it.
        services.AddSingleton<ILlamaModelProvider>(sp => sp.GetRequiredService<LlamaModelProvider>());

        // Safe as a singleton: the agent holds no state between calls - session and context are built
        // and disposed inside GenerateResponse.
        services.AddSingleton<ILlamaAgent, LlamaAgent>();

        return services;
    }
}
