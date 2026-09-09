namespace GameVisionTool.Common.Domain.Commands;

public interface IUnitOfWorkCommandHandlerDecorator<in TCommand> : ICommandHandler<TCommand> where TCommand : ICommand;

public interface IUnitOfWorkCommandHandlerDecorator<in TCommand, TResult> : ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>;

public interface IAsyncUnitOfWorkCommandHandlerDecorator<in TCommand> : IAsyncCommandHandler<TCommand> where TCommand : ICommand;

public interface IAsyncUnitOfWorkCommandHandlerDecorator<in TCommand, TResult> : IAsyncCommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>;