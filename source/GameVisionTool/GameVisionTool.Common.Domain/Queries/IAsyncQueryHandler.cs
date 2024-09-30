using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Common.Domain.Queries;

public interface IAsyncQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query);
}