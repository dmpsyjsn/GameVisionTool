using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.CrossCuttingConcerns;

public class AuthorizationCommandHandlerDecorator<TCommand>(IAsyncCommandHandler<TCommand> handler) : IAsyncCommandHandler<TCommand> where TCommand : ICommand
{
    public async Task<Result> HandleAsync(TCommand command)
    {
        // ToDo: Implement authorization logic here.
        return await handler.HandleAsync(command);
    }
}

public class AuthorizationCommandHandlerDecorator<TCommand, TResult>(IAsyncCommandHandler<TCommand, TResult> handler)
    : IAsyncCommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    public async Task<Result<TResult>> HandleAsync(TCommand command)
    {
        // ToDo: Implement authorization logic here.
        return await handler.HandleAsync(command);
    }
}