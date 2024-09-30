using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using System.Diagnostics;
using SimpleInjector;

namespace GameVisionTool.Logic.Common;

internal sealed class DynamicProcessQueryAsync(Container container) : IProcessQueryAsync
{
    [DebuggerStepThrough]
    public async Task<Result<TResult>> ProcessAsync<TResult>(IQuery<TResult> query)
    {
        var handlerType = typeof(IAsyncQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));

        dynamic handler = container.GetInstance(handlerType);

        return await handler.HandleAsync((dynamic)query);
    }
}