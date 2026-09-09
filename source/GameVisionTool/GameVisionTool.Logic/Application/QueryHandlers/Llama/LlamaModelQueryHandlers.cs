using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Integration.LlamaSharp;
using GameVisionTool.Messages.Common;
using GameVisionTool.Messages.Queries.Llama;

namespace GameVisionTool.Logic.Application.QueryHandlers.Llama;

public class LlamaModelQueryHandlers(LlamaModelProvider llamaModelProvider) : IAsyncQueryHandler<LoadLlamaModel, ModelLoadedResponse>
{
    public async Task<Result<ModelLoadedResponse>> HandleAsync(LoadLlamaModel query)
    {
        await Task.Run(() => llamaModelProvider.GetOrLoad(query.ModelPath));

        return Result.Ok(new ModelLoadedResponse(query.ModelPath));
    }
}