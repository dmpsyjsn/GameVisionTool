using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;
using Serilog;
using SimpleInjector;

namespace GameVisionTool.Web.Razor.CompositionRoot;

// This class is named "Commands" to allow Swagger to group command handler routes.
public sealed record Commands(Container Container)
{
    public async Task<IResult> InvokeAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        try
        {
            var handler = Container.GetInstance<IAsyncCommandHandler<TCommand>>();

            var result = await handler.HandleAsync(command);

            return result.IsSuccess ? Results.Ok(Result.Ok()) : Results.BadRequest(result);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "An Error occurred during processing.");
            var response = WebApiErrorResponseBuilder.CreateErrorResponseOrNull(exception);

            if (response != null)
            {
                return response;
            }

            throw;
        }
    }
}

public sealed record CommandsWithResults(Container Container)
{
    public async Task<Result<TResult>> InvokeAsync<TCommand, TResult>(HttpContext context, TCommand command) where TCommand : ICommand<TResult>
    {
        try
        {
            var handler = Container.GetInstance<IAsyncCommandHandler<TCommand, TResult>>();

            return await handler.HandleAsync(command);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "An Error occurred during processing.");
            var response = WebApiErrorResponseBuilder.CreateErrorResponseOrNull(exception);
            if (response == null) throw;
            await response.ExecuteAsync(context);

            return default!;
        }
    }
}