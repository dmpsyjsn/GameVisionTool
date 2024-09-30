using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;
using LiteDB;

namespace GameVisionTool.Logic.CrossCuttingConcerns;

public class LiteDbTransactionCommandHandlerDecorator<TCommand>(IAsyncCommandHandler<TCommand> decorated, ILiteDatabase liteDb)
    : ITransactionCommandHandlerDecorator<TCommand>
    where TCommand : ICommand
{

    public async Task<Result> HandleAsync(TCommand command)
    {
        liteDb.BeginTrans();

        try
        {
            var result = await decorated.HandleAsync(command);
            if (result.IsFailure)
                liteDb.Rollback();

            return result;
        }
        catch
        {
            liteDb.Rollback();
            throw;
        }
    }
}

public class LiteDbTransactionCommandHandlerDecorator<TCommand, TResult>(IAsyncCommandHandler<TCommand, TResult> decorated, ILiteDatabase liteDb)
    : ITransactionCommandHandlerDecorator<TCommand, TResult>
    where TCommand : ICommand<TResult>
{

    public async Task<Result<TResult>> HandleAsync(TCommand command)
    {
        liteDb.BeginTrans();

        try
        {
            var result = await decorated.HandleAsync(command);
            if (result.IsFailure)
                liteDb.Rollback();


            return result;
        }
        catch
        {
            liteDb.Rollback();
            throw;
        }
    }
}