using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;
using SimpleInjector;

namespace GameVisionTool.Logic.Common;

internal sealed class DynamicProcessCommandAsync(Container container) : IProcessCommandAsync
{
    public async Task<Result> ProcessAsync(ICommand command)
    {
        var handlerType = typeof(IAsyncCommandHandler<>).MakeGenericType(command.GetType());
        dynamic handler = container.GetInstance(handlerType);

        return await handler.HandleAsync((dynamic)command);
    }

    public async Task<Result<TResult>> ProcessAsync<TResult>(ICommand<TResult> command)
    {
        var handlerType = typeof(IAsyncCommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        dynamic handler = container.GetInstance(handlerType);

        return await handler.HandleAsync((dynamic)command);
    }
}