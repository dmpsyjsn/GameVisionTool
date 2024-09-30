using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.CrossCuttingConcerns;

internal class AuthorizationQueryHandlerDecorator<TQuery, TResult>(IAsyncQueryHandler<TQuery, TResult> handler) : IAsyncQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    public async Task<Result<TResult>> HandleAsync(TQuery query)
    {
        // TODO: Implement authorization logic here.
        return await handler.HandleAsync(query);
    }
}