using System.Diagnostics;
using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;
using Serilog;

namespace GameVisionTool.Logic.CrossCuttingConcerns;

public class LoggingCommandDecoratorAsync<TCommand, TResult>(IAsyncCommandHandler<TCommand, TResult> decorated) : IAsyncCommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    private static readonly ILogger Logger = Log.Logger.ForContext<LoggingCommandDecoratorAsync<TCommand, TResult>>();

    public async Task<Result<TResult>> HandleAsync(TCommand command)
    {
        try
        {
            var watch = Stopwatch.StartNew();

            var name = command.GetType().Name;

            Logger.Debug("Timer for {@command} started", name);
            Logger.Debug("Content for {@command}: {@content}", name, command);

            var result = await decorated.HandleAsync(command);

            Logger.Debug("Command Result = {@returnResult}", result);

            watch.Stop();

            Logger.Information("Processed {@command} in a total of {@elapsedMilliseconds} ms", name, watch.ElapsedMilliseconds);

            Logger.Debug("End command {@command}", name);

            return result;
        }
        catch (Exception e)
        {
            Logger.Error("{error}", e);
            throw;
        }
    }
}

public class LoggingCommandDecoratorAsync<TCommand>(IAsyncCommandHandler<TCommand> decorated) : IAsyncCommandHandler<TCommand> where TCommand : ICommand
{
    private static readonly ILogger Logger = Log.Logger.ForContext<LoggingCommandDecoratorAsync<TCommand>>();

    public async Task<Result> HandleAsync(TCommand command)
    {
        try
        {
            var watch = Stopwatch.StartNew();

            var name = command.GetType().Name;

            Logger.Debug("Timer for {@command} started", name);
            Logger.Debug("Content for {@command}: {@content}", name, command);

            var result = await decorated.HandleAsync(command);

            Logger.Debug("Command Result = {@returnResult}", result);

            watch.Stop();

            Logger.Information("Processed {@command} in a total of {@elapsedMilliseconds} ms", name, watch.ElapsedMilliseconds);

            Logger.Debug("End command {@command}", name);

            return result;
        }
        catch (Exception e)
        {
            Logger.Error("{error}", e);
            throw;
        }
    }
}

public class LoggingCommandDecorator<TCommand, TResult>(ICommandHandler<TCommand, TResult> decorated) : ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    private static readonly ILogger Logger = Log.Logger.ForContext<LoggingCommandDecorator<TCommand, TResult>>();

    public Result<TResult> Handle(TCommand command)
    {
        try
        {
            var watch = Stopwatch.StartNew();
            
            var name = command.GetType().Name;

            Logger.Debug("Timer for {@command} started", name);
            Logger.Debug("Content for {@command}: {@content}", name, command);

            var result = decorated.Handle(command);

            Logger.Debug("Command Result = {@returnResult}", result);

            watch.Stop();

            Logger.Information("Processed {@command} in a total of {@elapsedMilliseconds} ms", name, watch.ElapsedMilliseconds);

            Logger.Debug("End command {@command}", name);

            return result;
        }
        catch (Exception e)
        {
            Logger.Error("{error}", e);
            throw;
        }
    }
}

public class LoggingCommandDecorator<TCommand>(ICommandHandler<TCommand> decorated) : ICommandHandler<TCommand> where TCommand : ICommand
{
    private static readonly ILogger Logger = Log.Logger.ForContext<LoggingCommandDecorator<TCommand>>();

    public Result Handle(TCommand command)
    {
        try
        {
            var watch = Stopwatch.StartNew();
            
            var name = command.GetType().Name;
            
            Logger.Debug("Timer for {@command} started", name);
            Logger.Debug("Content for {@command}: {@content}", name, command);

            var result = decorated.Handle(command);

            Logger.Debug("Command Result = {@returnResult}", result);

            watch.Stop();

            Logger.Information("Processed {@command} in a total of {@elapsedMilliseconds} ms", name, watch.ElapsedMilliseconds);

            Logger.Debug("End command {@command}", name);

            return result;
        }
        catch (Exception e)
        {
            Logger.Error("{error}", e);
            throw;
        }
    }
}