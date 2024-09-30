using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using SimpleInjector;

namespace GameVisionTool.Web.Razor.CompositionRoot;

// This class is named "Queries" to allow Swagger to group query handler routes.
public sealed record Queries(Container Container)
{
    public async Task<Result<TResult>> InvokeAsync<TQuery, TResult>(HttpContext context, TQuery query)
        where TQuery : IQuery<TResult>
    {
        var handler = Container.GetInstance<IAsyncQueryHandler<TQuery, TResult>>();

        try
        {
            return await handler.HandleAsync(query);
        }
        catch (Exception exception)
        {
            var response = WebApiErrorResponseBuilder.CreateErrorResponseOrNull(exception);

            if (response != null)
            {
                await response.ExecuteAsync(context);

                return default!;
            }
            else
            {
                throw;
            }
        }
    }
}