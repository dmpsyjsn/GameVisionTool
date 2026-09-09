using System.Diagnostics;
using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.Common;

public sealed class DynamicProcessQuery(IServiceProvider serviceProvider) : IProcessQuery
{
    [DebuggerStepThrough]
    public Result<TResult> Process<TResult>(IQuery<TResult> query)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));

        dynamic handler = serviceProvider.GetService(handlerType)!;

        return handler.Handle((dynamic)query);
    }
}

public sealed class DynamicProcessQueryAsync(IServiceProvider serviceProvider) : IProcessQueryAsync
{
    [DebuggerStepThrough]
    public async Task<Result<TResult>> ProcessAsync<TResult>(IQuery<TResult> query)
    {
        var handlerType = typeof(IAsyncQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));

        dynamic handler = serviceProvider.GetService(handlerType)!;

        return await handler.HandleAsync((dynamic)query);
    }
}
