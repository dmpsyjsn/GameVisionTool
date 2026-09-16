using GameVisionTool.Common.Domain;
using GameVisionTool.Common.Domain.Commands;

namespace GameVisionTool.Messages.Commands.Agents;

public class AddLlamaAgent(string name, AgentGroupType groupType, string systemPrompt, int maxTokens = 4096, float temperature = 0.9f, bool suppressThinking = false, string stopMarker = "") : ICommand<Guid>
{
    public string Name { get; } = name;
    public AgentGroupType GroupType { get; } = groupType;
    public string SystemPrompt { get; } = systemPrompt;
    public int MaxTokens { get; } = maxTokens;
    public float Temperature { get; } = temperature;
    public bool SuppressThinking { get; } = suppressThinking;
    public string StopMarker { get; } = stopMarker;
}

public class UpdateLlamaAgent(Guid id, string name, AgentGroupType groupType, string systemPrompt, int maxTokens = 4096, float temperature = 0.9f, bool suppressThinking = false, string stopMarker = "") : ICommand
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public AgentGroupType GroupType { get; } = groupType;
    public string SystemPrompt { get; } = systemPrompt;
    public int MaxTokens { get; } = maxTokens;
    public float Temperature { get; } = temperature;
    public bool SuppressThinking { get; } = suppressThinking;
    public string StopMarker { get; } = stopMarker;
}

public class RemoveLlamaAgent(Guid id) : ICommand
{
    public Guid Id { get; } = id;
}
