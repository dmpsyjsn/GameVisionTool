using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Integration.LlamaSharp;
using GameVisionTool.Messages.Common;
using GameVisionTool.Messages.Queries.Llama;

namespace GameVisionTool.Logic.Application.QueryHandlers.Llama;

public class LlamaModelQueryHandlers(ILlamaModelProvider llamaModelProvider) : IAsyncQueryHandler<LoadLlamaModel, ModelLoadedResponse>
{
    public async Task<Result<ModelLoadedResponse>> HandleAsync(LoadLlamaModel query)
    {
        await llamaModelProvider.EnsureLoaded(query.ModelPath, query.ContextSize, query.GpuLayerCount);

        return Result.Ok(new ModelLoadedResponse(query.ModelPath));
    }
}
