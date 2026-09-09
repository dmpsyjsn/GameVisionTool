using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Common.Domain.Queries;

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Result<TResult> Handle(TQuery query);
}

public interface IAsyncQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query);
}
