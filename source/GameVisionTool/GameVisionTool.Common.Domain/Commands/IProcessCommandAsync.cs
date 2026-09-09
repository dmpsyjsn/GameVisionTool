using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Common.Domain.Commands;

public interface IProcessCommand
{
    Result Process(ICommand command);
    Result<TResult> Process<TResult>(ICommand<TResult> command);
}

public interface IProcessCommandAsync
{
    Task<Result> ProcessAsync(ICommand command);
    Task<Result<TResult>> ProcessAsync<TResult>(ICommand<TResult> command);
}