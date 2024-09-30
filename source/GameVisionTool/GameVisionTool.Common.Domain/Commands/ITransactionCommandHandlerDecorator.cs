namespace GameVisionTool.Common.Domain.Commands;

public interface ITransactionCommandHandlerDecorator<in TCommand> : IAsyncCommandHandler<TCommand> where TCommand : ICommand;

public interface ITransactionCommandHandlerDecorator<in TCommand, TResult> : IAsyncCommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>;