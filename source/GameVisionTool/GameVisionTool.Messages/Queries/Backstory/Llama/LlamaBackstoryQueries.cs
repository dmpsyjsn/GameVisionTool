using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Messages.Common;

namespace GameVisionTool.Messages.Queries.Backstory.Llama;

public class GetLlamaAgentResponse(
    string modelPath,
    string content,
    uint contextSize,
    int gpuLayerCount,
    int maxTokens,
    float temperature,
    string agentSystemPrompt,
    bool suppressThinking,
    string stopMarker) : IAmALlamaSharpQuery<LlamaResponse>
{
    public string ModelPath { get; } = modelPath;
    public string Content { get; } = content;
    public uint ContextSize { get; } = contextSize;
    public int GpuLayerCount { get; } = gpuLayerCount;
    public int MaxTokens { get; } = maxTokens;
    public float Temperature { get; } = temperature;
    public string AgentSystemPrompt { get; } = agentSystemPrompt;
    public bool SuppressThinking { get; } = suppressThinking;
    public string StopMarker { get; } = stopMarker;
}