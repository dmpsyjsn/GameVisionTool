using System.Security.Principal;
using GameVisionTool.Logic;
using SimpleInjector;

namespace GameVisionTool.Web.Razor.CompositionRoot;

public static class Bootstrapper
{
    public static IEnumerable<(Type, Type)> GetKnownCommandTypes() => BusinessLayerBootstrapper.GetCommandTypes();
    public static IEnumerable<(Type, Type)> GetKnownCommandResultTypes() => BusinessLayerBootstrapper.GetCommandResultTypes();
    public static IEnumerable<(Type QueryType, Type ResultType)> GetKnownQueryTypes() => BusinessLayerBootstrapper.GetQueryTypes();

    public static Container Bootstrap(Container container, IConfiguration config)
    {
        container
            .RegisterValidators()
            .RegisterCommandHandlers(config)
            .RegisterQueryHandlers()
            .RegisterServices(config);

        container.RegisterSingleton<IPrincipal, HttpContextPrincipal>();

        return container;
    }

    private sealed class HttpContextPrincipal(IHttpContextAccessor httpContextAccessor) : IPrincipal
    {
        public IIdentity Identity => this.Principal.Identity!;
        public bool IsInRole(string role) => this.Principal.IsInRole(role);
        private IPrincipal Principal => httpContextAccessor.HttpContext?.User!;
    }
}