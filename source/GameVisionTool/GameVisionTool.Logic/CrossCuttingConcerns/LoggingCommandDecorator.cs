using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;
using Serilog;
using System.Diagnostics;

namespace GameVisionTool.Logic.CrossCuttingConcerns;

public class LoggingCommandDecorator<TCommand, TResult>(IAsyncCommandHandler<TCommand, TResult> decorated) : IAsyncCommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    private static readonly ILogger Logger = Log.Logger.ForContext<LoggingCommandDecorator<TCommand, TResult>>();

    public async Task<Result<TResult>> HandleAsync(TCommand command)
    {
        var watch = Stopwatch.StartNew();

        Logger.Debug("Timer for {@command} started", command);

        var result = await decorated.HandleAsync(command);

        Logger.Debug("Command Result = {@returnResult}", result);

        watch.Stop();

        Logger.Information("Processed {@command} in a total of {@elapsedMilliseconds} ms", command, watch.ElapsedMilliseconds);

        Logger.Debug("End command {@command}", command);

        return result;
    }
}

public class LoggingCommandDecorator<TCommand>(IAsyncCommandHandler<TCommand> decorated) : IAsyncCommandHandler<TCommand> where TCommand : ICommand
{
    private static readonly ILogger Logger = Log.Logger.ForContext<LoggingCommandDecorator<TCommand>>();

    public async Task<Result> HandleAsync(TCommand command)
    {
        var watch = Stopwatch.StartNew();

        Logger.Debug("Timer for {@command} started", command);

        var result = await decorated.HandleAsync(command);

        Logger.Debug("Command Result = {@returnResult}", result);

        watch.Stop();

        Logger.Information("Processed {@command} in a total of {@elapsedMilliseconds} ms", command, watch.ElapsedMilliseconds);

        Logger.Debug("End command {@command}", command);

        return result;
    }
}