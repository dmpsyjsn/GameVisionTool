using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Messages.Common;

namespace GameVisionTool.Messages.Queries.Story.Gemini;

// Not IAmALlamaSharpQuery: there is no model file on disk to check for, so the LlamaModelFileDecorator
// deliberately passes this straight through.
//
// ApiKey travels on the message because the logging decorators destructure the whole query - the
// property name is what SensitiveDataDestructuringPolicy matches on to keep it out of the log file.
public class GetGeminiAgentResponse(
    string apiKey,
    string model,
    string content,
    int maxTokens,
    string systemInstructions,
    string thinkingLevel) : IQuery<GeminiResponse>
{
    public string ApiKey { get; } = apiKey;
    public string Model { get; } = model;
    public string Content { get; } = content;
    public int MaxTokens { get; } = maxTokens;
    public string SystemInstructions { get; } = systemInstructions;
    public string ThinkingLevel { get; } = thinkingLevel;
}
