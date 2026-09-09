using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Common.Domain.Commands;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Result Handle(TCommand command);
}

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Result<TResult> Handle(TCommand command);
}

public interface IAsyncCommandHandler<in TCommand> where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command);
}

public interface IAsyncCommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command);
}