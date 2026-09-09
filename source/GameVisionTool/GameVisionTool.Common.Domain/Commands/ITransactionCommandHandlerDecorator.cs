namespace GameVisionTool.Common.Domain.Commands;

public interface ITransactionCommandHandlerDecorator<in TCommand> : ICommandHandler<TCommand> where TCommand : ICommand;
public interface ITransactionCommandHandlerDecorator<in TCommand, TResult> : ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>;

public interface IAsyncTransactionCommandHandlerDecorator<in TCommand> : IAsyncCommandHandler<TCommand> where TCommand : ICommand;

public interface IAsyncTransactionCommandHandlerDecorator<in TCommand, TResult> : IAsyncCommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>;