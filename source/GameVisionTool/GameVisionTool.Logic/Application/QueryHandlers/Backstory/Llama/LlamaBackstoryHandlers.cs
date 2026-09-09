using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Integration.LlamaSharp;
using GameVisionTool.Integration.LlamaSharp.Agents;
using GameVisionTool.Messages.Common;
using GameVisionTool.Messages.Queries.Backstory.Llama;

namespace GameVisionTool.Logic.Application.QueryHandlers.Backstory.Llama;

public class LlamaBackstoryHandlers(LlamaModelProvider llamaModelProvider) : IAsyncQueryHandler<GetLlamaScienceFictionBackstoryResponse, LlamaResponse>
{
    public async Task<Result<LlamaResponse>> HandleAsync(GetLlamaScienceFictionBackstoryResponse query)
    {
        var parameters = new LlamaParametersGenerator(query.ModelPath).Parameters;

        var model = await Task.Run(() => llamaModelProvider.GetOrLoad(query.ModelPath));

        var chatSessionGenerator = new LlamaChatSessionGenerator(model, parameters);
        using var backstoryAgent = new LlamaScienceFictionBackstoryAgent(chatSessionGenerator);

        var response = await backstoryAgent.GetResponse(query.Content);

        return Result.Ok(new LlamaResponse(response));
    }
}
