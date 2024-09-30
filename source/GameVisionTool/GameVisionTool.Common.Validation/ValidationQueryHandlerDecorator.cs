using FluentValidation;
using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Common.Validation;

public class ValidationQueryHandlerDecorator<TQuery, TResult>(IValidator<TQuery> validator, IAsyncQueryHandler<TQuery, TResult> handler) : 
    IAsyncQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    public async Task<Result<TResult>> HandleAsync(TQuery query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));

        // validate the supplied command.
        await validator.ValidateAsync(query);

        // forward the (valid) command to the real command handler.
        return await handler.HandleAsync(query);
    }
}