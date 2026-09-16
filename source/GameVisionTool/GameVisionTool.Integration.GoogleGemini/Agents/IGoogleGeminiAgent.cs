using GameVisionTool.Integration.GoogleGemini.ClientConfiguration;
using Google.GenAI.Types;

namespace GameVisionTool.Integration.GoogleGemini.Agents;

public interface IGoogleGeminiAgent
{
    Task<string> GenerateResponse(
        string apiKey, 
        string model,
        int maxTokens, 
        string systemInstructions,
        ThinkingLevel thinkingLevel,
        string userMessage, 
        ConversationTurn[] history);
}
