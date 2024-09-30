using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Common.Domain.Commands;

public interface IUnitOfWorkCommandHandlerDecorator<in TCommand> : IAsyncCommandHandler<TCommand> where TCommand : ICommand;

public interface IUnitOfWorkCommandHandlerDecorator<in TCommand, TResult> : IAsyncCommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>;