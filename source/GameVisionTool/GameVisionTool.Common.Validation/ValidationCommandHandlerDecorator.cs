using FluentValidation;
using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Common.Validation;

public class ValidationCommandHandlerDecorator<TCommand>(IValidator<TCommand> validator, IAsyncCommandHandler<TCommand> handler) : 
    IAsyncCommandHandler<TCommand> where TCommand : ICommand
{
    public async Task<Result> HandleAsync(TCommand command)
    {
        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            return Result.Fail(validationResult.Errors.Select(x => x.ErrorMessage));
        }
        return await handler.HandleAsync(command);
    }
}

public class ValidationCommandHandlerDecorator<TCommand, TResult>(IValidator<TCommand> validator, IAsyncCommandHandler<TCommand, TResult> handler) : 
    IAsyncCommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    public async Task<Result<TResult>> HandleAsync(TCommand command)
    {
        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            return Result.Fail<TResult>(validationResult.Errors.Select(x => x.ErrorMessage));
        }
        return await handler.HandleAsync(command);
    }
}