using GameVisionTool.Integration.GoogleGemini.Agents;
using GameVisionTool.Integration.GoogleGemini.ClientConfiguration;
using Microsoft.Extensions.DependencyInjection;

namespace GameVisionTool.Integration.GoogleGemini;

public static class GoogleGeminiRegistration
{
    public static IServiceCollection AddGoogleGemini(this IServiceCollection services)
    {
        // The builder holds no state of its own - the API key arrives per call, from the setting the
        // user picked - so a singleton is enough.
        services.AddSingleton<GeminiClientBuilder>();
        services.AddSingleton<IGoogleGeminiAgent, GoogleGeminiAgentResponseGenerator>();

        return services;
    }
}
