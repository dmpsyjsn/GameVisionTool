using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Persistence.LiteDb;

public class LiteDbUnitOfWorkCommandHandlerDecoratorAsync<TCommand>(IAsyncCommandHandler<TCommand> decorated, IAsyncUnitOfWorkDataStore dataStore) :
    IAsyncUnitOfWorkCommandHandlerDecorator<TCommand> where TCommand : ICommand
{
    public async Task<Result> HandleAsync(TCommand command)
    {
        var result = await decorated.HandleAsync(command);

        if (result.IsSuccess)
            await dataStore.SaveChangesAsync();

        return result;
    }
}

public class LiteDbUnitOfWorkCommandHandlerDecoratorAsync<TCommand, TResult>(IAsyncCommandHandler<TCommand, TResult> decorated, IAsyncUnitOfWorkDataStore dataStore) :
    IAsyncUnitOfWorkCommandHandlerDecorator<TCommand, TResult> where TCommand : ICommand<TResult>
{
    public async Task<Result<TResult>> HandleAsync(TCommand command)
    {
        var result = await decorated.HandleAsync(command);

        if (result.IsSuccess)
            await dataStore.SaveChangesAsync();

        return result;
    }
}

public class LiteDbUnitOfWorkCommandHandlerDecorator<TCommand>(ICommandHandler<TCommand> decorated, IUnitOfWorkDataStore dataStore) :
    IUnitOfWorkCommandHandlerDecorator<TCommand> where TCommand : ICommand
{
    public Result Handle(TCommand command)
    {
        var result = decorated.Handle(command);

        if (result.IsSuccess)
            dataStore.SaveChanges();

        return result;
    }
}

public class LiteDbUnitOfWorkCommandHandlerDecorator<TCommand, TResult>(ICommandHandler<TCommand, TResult> decorated, IUnitOfWorkDataStore dataStore) :
    IUnitOfWorkCommandHandlerDecorator<TCommand, TResult> where TCommand : ICommand<TResult>
{
    public Result<TResult> Handle(TCommand command)
    {
        var result = decorated.Handle(command);

        if (result.IsSuccess)
            dataStore.SaveChanges();

        return result;
    }
}
