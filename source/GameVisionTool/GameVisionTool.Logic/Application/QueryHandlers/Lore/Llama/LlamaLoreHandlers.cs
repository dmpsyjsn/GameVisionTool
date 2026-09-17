using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Integration.LlamaSharp;
using GameVisionTool.Integration.LlamaSharp.Agents;
using GameVisionTool.Messages.Common;
using GameVisionTool.Messages.Queries.Backstory.Llama;

namespace GameVisionTool.Logic.Application.QueryHandlers.Lore.Llama;

public class LlamaLoreHandlers(LlamaModelProvider llamaModelProvider) : IAsyncQueryHandler<GetLlamaAgentResponse, LlamaResponse>
{
    public async Task<Result<LlamaResponse>> HandleAsync(GetLlamaAgentResponse query)
    {
        var parameters = new LlamaParametersGenerator(query.ModelPath, query.GpuLayerCount, query.ContextSize).Parameters;

        var model = await Task.Run(() => llamaModelProvider.GetOrLoad(query.ModelPath, query.ContextSize, query.GpuLayerCount));

        using var chatSessionGenerator = new LlamaChatSessionGenerator(model, parameters, query.SuppressThinking);
        using var backstoryAgent = new LlamaAgentResponseGenerator(
            chatSessionGenerator, query.MaxTokens, query.Temperature, query.AgentSystemPrompt, query.StopMarker);

        var response = await backstoryAgent.GetResponse(query.Content);

        return Result.Ok(new LlamaResponse(response.Text, response.WasTruncated));
    }
}
