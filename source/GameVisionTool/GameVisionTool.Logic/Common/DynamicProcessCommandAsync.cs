using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.Common;

public sealed class DynamicProcessCommand(IServiceProvider serviceProvider) : IProcessCommand
{
    public Result Process(ICommand command)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        dynamic handler = serviceProvider.GetService(handlerType)!;

        return handler.Handle((dynamic)command);
    }

    public Result<TResult> Process<TResult>(ICommand<TResult> command)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        dynamic handler = serviceProvider.GetService(handlerType)!;

        return handler.Handle((dynamic)command);
    }
}

public sealed class DynamicProcessCommandAsync(IServiceProvider serviceProvider) : IProcessCommandAsync
{
    public async Task<Result> ProcessAsync(ICommand command)
    {
        var handlerType = typeof(IAsyncCommandHandler<>).MakeGenericType(command.GetType());
        dynamic handler = serviceProvider.GetService(handlerType)!;

        return await handler.HandleAsync((dynamic)command);
    }

    public async Task<Result<TResult>> ProcessAsync<TResult>(ICommand<TResult> command)
    {
        var handlerType = typeof(IAsyncCommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        dynamic handler = serviceProvider.GetService(handlerType)!;

        return await handler.HandleAsync((dynamic)command);
    }
}