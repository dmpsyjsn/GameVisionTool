using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Integration.GoogleGemini.Agents;
using GameVisionTool.Messages.Common;
using GameVisionTool.Messages.Queries.Backstory.Gemini;

namespace GameVisionTool.Logic.Application.QueryHandlers.Lore.Gemini;

public class GeminiLoreHandlers(IGoogleGeminiAgent geminiAgent) : IAsyncQueryHandler<GetGeminiAgentResponse, GeminiResponse>
{
    public async Task<Result<GeminiResponse>> HandleAsync(GetGeminiAgentResponse query)
    {
        // The agent's stored level is free-form text until it is checked here - see ThinkingLevelParser
        // for why the SDK's own conversion cannot do this.
        if (!ThinkingLevelParser.TryParse(query.ThinkingLevel, out var thinkingLevel))
        {
            return Result.Fail<GeminiResponse>(
                $"'{query.ThinkingLevel}' is not a recognized thinking level. Expected one of: {string.Join(", ", ThinkingLevelParser.KnownLevels)}.");
        }

        // No history: the Backstory page generates one-shot, the same as the Llama path. Revision
        // across turns is what ConversationTurn[] is there for when a page wants it.
        var response = await geminiAgent.GenerateResponse(
            query.ApiKey,
            query.Model,
            query.MaxTokens,
            query.SystemInstructions,
            thinkingLevel,
            query.Content,
            []);

        return Result.Ok(new GeminiResponse(response));
    }
}
