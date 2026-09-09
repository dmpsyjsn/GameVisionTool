using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Integration.LlamaSharp;

public class LlamaModelFileDecorator<TQuery, TResult>(IQueryHandler<TQuery, TResult> handler) :
    IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    public Result<TResult> Handle(TQuery query)
    {
        if (query is IAmALlamaSharpQuery<TResult> llamaQuery && !File.Exists(llamaQuery.ModelPath))
        {
            return Result.Fail<TResult>($"Model file not found: {llamaQuery.ModelPath}");
        }

        return handler.Handle(query);
    }
}

public class LlamaModelFileDecoratorAsync<TQuery, TResult>(IAsyncQueryHandler<TQuery, TResult> handler) :
    IAsyncQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    public async Task<Result<TResult>> HandleAsync(TQuery query)
    {
        if (query is IAmALlamaSharpQuery<TResult> llamaQuery && !File.Exists(llamaQuery.ModelPath))
        {
            return Result.Fail<TResult>($"Model file not found: {llamaQuery.ModelPath}");
        }

        return await handler.HandleAsync(query);
    }
}
