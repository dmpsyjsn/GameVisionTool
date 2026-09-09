using System.Diagnostics;
using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using Serilog;

namespace GameVisionTool.Logic.CrossCuttingConcerns;

public class LoggingQueryDecorator<TQuery, TResult>(IQueryHandler<TQuery, TResult> decorated)
    : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private static readonly ILogger Logger = Log.Logger.ForContext<LoggingQueryDecorator<TQuery, TResult>>();

    public Result<TResult> Handle(TQuery query)
    {
        var watch = Stopwatch.StartNew();

        var name = query.GetType().Name;

        Logger.Debug("Timer for {@query} started", query);

        var result = decorated.Handle(query);

        Logger.Debug("query Result = {@returnResult}", result);

        watch.Stop();

        Logger.Information("Processed {@query} in a total of {@elapsedMilliseconds} ms", name, watch.ElapsedMilliseconds);

        Logger.Debug("End query {@query}", query);

        return result;
    }
}
public class LoggingQueryDecoratorAsync<TQuery, TResult>(IAsyncQueryHandler<TQuery, TResult> decorated)
    : IAsyncQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private static readonly ILogger Logger = Log.Logger.ForContext<LoggingQueryDecoratorAsync<TQuery, TResult>>();

    public async Task<Result<TResult>> HandleAsync(TQuery query)
    {
        var watch = Stopwatch.StartNew();

        var name = query.GetType().Name;

        Logger.Debug("Timer for {@query} started", query);

        var result = await decorated.HandleAsync(query);

        Logger.Debug("query Result = {@returnResult}", result);

        watch.Stop();

        Logger.Information("Processed {@query} in a total of {@elapsedMilliseconds} ms", name, watch.ElapsedMilliseconds);

        Logger.Debug("End query {@query}", query);

        return result;
    }
}
