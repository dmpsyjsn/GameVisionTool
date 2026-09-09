using GameVisionTool.Integration.GoogleGemini.ClientConfiguration;

namespace GameVisionTool.Integration.GoogleGemini.Agents;

public interface IGoogleGeminiAgent
{
    Task<string> GenerateResponse(string apiKey, string userMessage, ClientParameters clientParams, ConversationTurn[] history);
}
