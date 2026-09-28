using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Integration.LlamaSharp.Agents;
using GameVisionTool.Messages.Common;
using GameVisionTool.Messages.Queries.Story.Llama;
using LLama.Common;

namespace GameVisionTool.Logic.Application.QueryHandlers.Story.Llama;

public class LlamaStoryHandlers(ILlamaAgent llamaAgent) : IAsyncQueryHandler<GetLlamaAgentResponse, LlamaResponse>
{
    public async Task<Result<LlamaResponse>> HandleAsync(GetLlamaAgentResponse query)
    {
        var chatHistory = ToChatHistory(query.ChatHistory);
        // No history: the Backstory page generates one-shot, the same as the Gemini path.
        var response = await llamaAgent.GenerateResponse(
            query.ModelPath,
            query.ContextSize,
            query.GpuLayerCount,
            query.MaxTokens,
            query.Temperature,
            query.AgentSystemPrompt,
            query.SuppressThinking,
            query.StopMarker,
            query.Content,
            chatHistory);
        
        return Result.Ok(new LlamaResponse(response.Text, response.WasTruncated));
    }

    private static ChatHistory? ToChatHistory(List<KeyValuePair<string, string>>? listHistory)
    {
        if (listHistory == null || listHistory.Count == 0)
            return null;

        var chatHistory = new ChatHistory();
        foreach (var item in listHistory)
        {
            var role = Enum.TryParse(typeof(AuthorRole), item.Key, true, out var authorRoleEnum)
                ? (AuthorRole) authorRoleEnum
                : AuthorRole.Assistant;
            
            chatHistory.AddMessage(role, item.Value);
        }

        return chatHistory;
    }
}
