using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Common.Domain.Commands;

public interface IProcessCommandAsync
{
    Task<Result> ProcessAsync(ICommand command);
    Task<Result<TResult>> ProcessAsync<TResult>(ICommand<TResult> command);
}