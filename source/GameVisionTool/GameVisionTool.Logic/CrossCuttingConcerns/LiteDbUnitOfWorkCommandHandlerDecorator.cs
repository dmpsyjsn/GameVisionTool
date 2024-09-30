using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.CrossCuttingConcerns;

public class LiteDbUnitOfWorkCommandHandlerDecorator<TCommand>(IAsyncCommandHandler<TCommand> decorated, IAsyncUnitOfWorkDataStore dataStore) : 
    IUnitOfWorkCommandHandlerDecorator<TCommand> where TCommand : ICommand
{
    public async Task<Result> HandleAsync(TCommand command)
    {
        var result = await decorated.HandleAsync(command);

        if (result.IsSuccess)
            await dataStore.SaveChangesAsync();

        return result;
    }
}

public class LiteDbUnitOfWorkCommandHandlerDecorator<TCommand, TResult>(IAsyncCommandHandler<TCommand, TResult> decorated, IAsyncUnitOfWorkDataStore dataStore) :
    IUnitOfWorkCommandHandlerDecorator<TCommand, TResult> where TCommand : ICommand<TResult>
{
    public async Task<Result<TResult>> HandleAsync(TCommand command)
    {
        var result = await decorated.HandleAsync(command);

        if (result.IsSuccess)
            await dataStore.SaveChangesAsync();

        return result;
    }
}
