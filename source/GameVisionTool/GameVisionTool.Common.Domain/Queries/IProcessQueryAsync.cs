using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Common.Domain.Queries;

public interface IProcessQueryAsync
{
    Task<Result<TResult>> ProcessAsync<TResult>(IQuery<TResult> query);
}

public interface IProcessQuery
{
    Result<TResult> Process<TResult>(IQuery<TResult> query);
}
